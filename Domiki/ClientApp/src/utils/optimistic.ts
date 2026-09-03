import type { DomikDto, DomikTypeDto, GameCommand, GameStateDto, ManufactureDto, QueuedIntent, ResourceDto, UpgradeLevelDto, WorkerDto } from '../types/api';
import { hasResourcesFor, isWorkerFree, workerFitness } from './game';
import { computeManufactureDuration } from './manufactureDuration';

export const SMART_AUTO_UNLOCK_LEVEL = 8;
export const LONG_HABIT_PERK_TYPE = 2;
export const LONG_HABIT_DURATION_PERCENT_PER_STEP = 5;
export const MARKET_DOMIK_LOGIC_NAME = 'market';

export interface PredictedState {
    state: GameStateDto;
    predictedManufactureIds: number[];
    predictedDomikIds: number[];
    waitingManufactureIds: number[];
    waitingOrderIds: number[];
}

interface Draft {
    state: GameStateDto;
    nextManufactureId: number;
    predictedManufactureIds: number[];
    predictedDomikIds: number[];
    waitingManufactureIds: number[];
    waitingOrderIds: number[];
}

export function predictState(state: GameStateDto, intents: readonly QueuedIntent[]): PredictedState {
    const draft: Draft = {
        state,
        nextManufactureId: -1,
        predictedManufactureIds: [],
        predictedDomikIds: [],
        waitingManufactureIds: [],
        waitingOrderIds: [],
    };

    for (const intent of intents) {
        applyCommand(draft, intent.command, Math.floor(intent.queuedAtMs / 1000));
    }

    return {
        state: draft.state,
        predictedManufactureIds: draft.predictedManufactureIds,
        predictedDomikIds: draft.predictedDomikIds,
        waitingManufactureIds: draft.waitingManufactureIds,
        waitingOrderIds: draft.waitingOrderIds,
    };
}

function applyCommand(draft: Draft, command: GameCommand, nowSeconds: number): void {
    switch (command.kind) {
        case 'StartManufacture':
            startManufacture(draft, command.args, nowSeconds);
            return;
        case 'UpgradeDomik':
            upgradeDomik(draft, command.args.domikId, nowSeconds);
            return;
        case 'BuyDomik':
            buyDomik(draft, command.args.typeId, nowSeconds);
            return;
        case 'SetManufactureAutoRepeat':
            setAutoRepeat(draft, command.args.manufactureId, command.args.autoRepeat);
            return;
        case 'HurryManufacture':
            draft.waitingManufactureIds = [...draft.waitingManufactureIds, command.args.manufactureId];
            return;
        case 'CompleteOrder':
            draft.waitingOrderIds = [...draft.waitingOrderIds, command.args.orderId];
            return;
    }
}

function startManufacture(
    draft: Draft,
    args: { domikId: number; receiptId: number; useOptional: boolean; autoRepeat: boolean; workerIds: number[] },
    nowSeconds: number,
): void {
    const state = draft.state;
    const domik = state.domiks.find(x => x.id === args.domikId);
    if (domik == null || domik.level === 0 || domik.finishDate != null) {
        return;
    }

    const domikType = state.domikTypes.find(x => x.id === domik.typeId);
    const level = domikType?.levels.find(x => x.value === domik.level);
    if (domikType == null || level == null || !level.receiptIds.includes(args.receiptId)) {
        return;
    }

    const receipt = state.receipts.find(x => x.id === args.receiptId);
    if (receipt == null) {
        return;
    }

    const blueprint = state.blueprints.find(x => x.receiptId === receipt.id);
    if (blueprint != null && !blueprint.owned) {
        return;
    }

    const manufactures = domik.manufactures ?? [];
    if (manufactures.length + 1 > level.maxManufactureCount) {
        return;
    }

    const inputs = mergeResources(args.useOptional && receipt.optionalInputResources.length > 0
        ? [...receipt.inputResources, ...receipt.optionalInputResources]
        : receipt.inputResources);
    if (!hasResourcesFor(inputs, state.resources)) {
        return;
    }

    const selected = selectWorkers(state, domikType, args.workerIds, receipt.plodderCount, nowSeconds * 1000);
    if (selected == null) {
        return;
    }

    const duration = computeManufactureDuration({
        receiptDurationSeconds: receipt.durationSeconds,
        traitDurationPercents: selected.map(worker => worker.traitDurationPercent),
        skillBonusPercents: selected.map(worker => worker.skills.find(skill => skill.domikTypeId === domikType.id)?.bonusPercent ?? 0),
        profilePercent: profilePercent(state, domikType.id),
        perkPercent: perkPercent(state),
        isMarketDomik: domikType.logicName === MARKET_DOMIK_LOGIC_NAME,
        zealCharges: state.goals.zealCharges,
    });

    const manufactureId = draft.nextManufactureId;
    draft.nextManufactureId -= 1;

    const manufacture: ManufactureDto = {
        id: manufactureId,
        finishDate: toIso(nowSeconds + duration.seconds),
        durationSeconds: duration.seconds,
        plodderCount: receipt.plodderCount,
        receiptId: receipt.id,
        autoRepeat: args.autoRepeat,
        measureResourceTypeId: null,
        measureValue: null,
    };

    const selectedIds = new Set(selected.map(worker => worker.id));
    draft.state = {
        ...state,
        resources: writeOff(state.resources, inputs),
        goals: duration.zealSpent ? { ...state.goals, zealCharges: state.goals.zealCharges - 1 } : state.goals,
        workers: state.workers.map(worker => selectedIds.has(worker.id) ? { ...worker, manufactureId } : worker),
        domiks: state.domiks.map(item => item.id === domik.id ? { ...item, manufactures: [...manufactures, manufacture] } : item),
    };
    draft.predictedManufactureIds = [...draft.predictedManufactureIds, manufactureId];
}

function upgradeDomik(draft: Draft, domikId: number, nowSeconds: number): void {
    const state = draft.state;
    const domik = state.domiks.find(x => x.id === domikId);
    const domikType = state.domikTypes.find(x => x.id === domik?.typeId);
    if (domik == null || domikType == null || domik.upgradeSeconds != null || domik.level === 0 || domik.level >= domikType.maxLevel) {
        return;
    }

    const nextLevel = domikType.levels.find(x => x.value === domik.level + 1);
    if (nextLevel == null || !hasResourcesFor(nextLevel.resources, state.resources)) {
        return;
    }

    draft.state = {
        ...state,
        resources: writeOff(state.resources, nextLevel.resources),
        domiks: state.domiks.map(item => item.id === domik.id
            ? { ...item, upgradeSeconds: nextLevel.upgradeSeconds, finishDate: toIso(nowSeconds + nextLevel.upgradeSeconds) }
            : item),
    };
    draft.predictedDomikIds = [...draft.predictedDomikIds, domik.id];
}

function buyDomik(draft: Draft, typeId: number, nowSeconds: number): void {
    const state = draft.state;
    const domikType = state.purchaseAvailableDomiks.find(x => x.id === typeId);
    if (domikType == null || domikType.availableCount <= 0 || domikType.unlockLevel > state.villageLevel.level) {
        return;
    }

    const blueprint = state.blueprints.find(x => x.domikTypeId === typeId);
    if (blueprint != null && !blueprint.owned) {
        return;
    }

    const firstLevel: UpgradeLevelDto | undefined = domikType.levels.find(x => x.value === 1);
    if (firstLevel == null || !hasResourcesFor(firstLevel.resources, state.resources)) {
        return;
    }

    const domikId = nextDomikId(state.domiks);
    const domik: DomikDto = {
        id: domikId,
        typeId,
        level: 0,
        finishDate: toIso(nowSeconds + firstLevel.upgradeSeconds),
        upgradeSeconds: firstLevel.upgradeSeconds,
        manufactures: [],
    };

    draft.state = {
        ...state,
        resources: writeOff(state.resources, firstLevel.resources),
        domiks: [...state.domiks, domik],
        purchaseAvailableDomiks: state.purchaseAvailableDomiks.map(item => item.id === typeId
            ? { ...item, availableCount: item.availableCount - 1 }
            : item),
    };
    draft.predictedDomikIds = [...draft.predictedDomikIds, domikId];
}

function nextDomikId(domiks: DomikDto[]): number {
    const used = new Set(domiks.map(domik => domik.id));
    let next = 1;
    while (used.has(next)) {
        next += 1;
    }

    return next;
}

function setAutoRepeat(draft: Draft, manufactureId: number, autoRepeat: boolean): void {
    const state = draft.state;
    draft.state = {
        ...state,
        domiks: state.domiks.map(domik => domik.manufactures == null || !domik.manufactures.some(x => x.id === manufactureId)
            ? domik
            : {
                ...domik,
                manufactures: domik.manufactures.map(manufacture => manufacture.id !== manufactureId
                    ? manufacture
                    : {
                        ...manufacture,
                        autoRepeat,
                        measureResourceTypeId: autoRepeat ? manufacture.measureResourceTypeId : null,
                        measureValue: autoRepeat ? manufacture.measureValue : null,
                    }),
            }),
    };
}

function selectWorkers(
    state: GameStateDto,
    domikType: DomikTypeDto,
    workerIds: number[],
    plodderCount: number,
    nowMs: number,
): WorkerDto[] | null {
    const free = state.workers.filter(worker => isWorkerFree(worker, nowMs)).sort((left, right) => left.id - right.id);
    if (free.length < plodderCount) {
        return null;
    }

    if (workerIds.length > 0) {
        if (workerIds.length !== plodderCount || new Set(workerIds).size !== workerIds.length) {
            return null;
        }

        const chosen = workerIds.map(id => free.find(worker => worker.id === id));
        return chosen.every((worker): worker is WorkerDto => worker != null) ? chosen : null;
    }

    const auto = state.villageLevel.level >= SMART_AUTO_UNLOCK_LEVEL
        ? [...free].sort((left, right) => workerFitness(right, domikType.id) - workerFitness(left, domikType.id) || left.id - right.id)
        : free;

    return auto.slice(0, plodderCount);
}

function profilePercent(state: GameStateDto, domikTypeId: number): number {
    const neighborId = state.village.profileNeighborId;
    if (neighborId == null) {
        return 100;
    }

    return state.villageProfiles.find(x => x.neighborId === neighborId && x.domikTypeId === domikTypeId)?.durationPercent ?? 100;
}

function perkPercent(state: GameStateDto): number {
    const level = state.relocation.perks.find(perk => perk.perkType === LONG_HABIT_PERK_TYPE)?.level ?? 0;
    return 100 - level * LONG_HABIT_DURATION_PERCENT_PER_STEP;
}

function mergeResources(resources: ResourceDto[]): ResourceDto[] {
    const byType = new Map<number, number>();
    resources.forEach(resource => byType.set(resource.typeId, (byType.get(resource.typeId) ?? 0) + resource.value));
    return [...byType].map(([typeId, value]) => ({ typeId, value }));
}

function writeOff(owned: ResourceDto[], cost: ResourceDto[]): ResourceDto[] {
    const spent = new Map(mergeResources(cost).map(resource => [resource.typeId, resource.value]));
    return owned.map(resource => {
        const value = spent.get(resource.typeId);
        return value == null ? resource : { ...resource, value: resource.value - value };
    });
}

function toIso(seconds: number): string {
    return new Date(seconds * 1000).toISOString();
}

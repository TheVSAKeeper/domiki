import type { DomikDto, DomikTypeDto, ManufactureDto, PlodderCount, ReceiptDto, ReceiptView, ResourceDto, SelectedDomikView, UpgradeView, WorkerDto } from '../types/api';
import { formatDuration, remainingSeconds } from './time';

export const INSTA_FINISH_SECONDS_PER_GOLD = 3600;
export const INSTA_FINISH_MAX_GOLD = 6;
export const GOLD_RESOURCE_TYPE_ID = 5;
export const COIN_RESOURCE_TYPE_ID = 1;
export const ZEAL_X4_THRESHOLD = 16;

export const EXPEDITION_LOOT_KIND_RESOURCE = 1;
export const EXPEDITION_LOOT_KIND_DECOR = 2;
export const EXPEDITION_LOOT_KIND_TRAIT_UPGRADE = 3;
export const EXPEDITION_LOOT_KIND_BLUEPRINT = 4;

const plodderTypeId = 1;

export function nextUpgradeLevel(domikType: DomikTypeDto, level: number) {
    return domikType.levels.find(x => x.value === level + 1) ?? null;
}

export function hasResourcesFor(cost: ResourceDto[], owned: ResourceDto[]): boolean {
    return cost.every(resource => {
        const have = owned.find(x => x.typeId === resource.typeId);
        return have != null && have.value >= resource.value;
    });
}

export function resourceShortfall(cost: ResourceDto[], owned: ResourceDto[]): ResourceDto[] {
    const required = new Map<number, number>();
    cost.forEach(resource => required.set(resource.typeId, (required.get(resource.typeId) ?? 0) + resource.value));

    return [...required].flatMap(([typeId, value]) => {
        const available = owned.find(resource => resource.typeId === typeId)?.value ?? 0;
        const missing = Math.max(0, value - available);
        return missing > 0 ? [{ typeId, value: missing }] : [];
    });
}

export interface ResourceSource {
    logicName: string;
    name: string;
}

export function resourceSourceMap(domikTypes: DomikTypeDto[], receipts: ReceiptDto[]): Map<number, ResourceSource[]> {
    const receiptById = new Map(receipts.map(receipt => [receipt.id, receipt]));
    const map = new Map<number, ResourceSource[]>();

    for (const type of domikTypes) {
        const outputs = new Set<number>();
        for (const level of type.levels) {
            for (const receiptId of level.receiptIds) {
                receiptById.get(receiptId)?.outputResources.forEach(output => outputs.add(output.typeId));
            }
        }

        for (const typeId of outputs) {
            const list = map.get(typeId) ?? [];
            if (!list.some(source => source.logicName === type.logicName)) {
                list.push({ logicName: type.logicName, name: type.name });
                map.set(typeId, list);
            }
        }
    }

    return map;
}

export type TradeDeal = 'good' | 'fair' | 'bad';

export function tradeDeal(giveValue: number, giveMarketValue: number, wantValue: number, wantMarketValue: number): TradeDeal {
    const received = giveValue * giveMarketValue;
    const paid = wantValue * wantMarketValue;
    if (paid <= 0 || received <= 0) {
        return 'fair';
    }

    const ratio = received / paid;
    if (ratio >= 1.15) {
        return 'good';
    }

    return ratio <= 0.85 ? 'bad' : 'fair';
}

export function tradeRatio(giveValue: number, wantValue: number): [number, number] {
    const gcd = (a: number, b: number): number => (b === 0 ? a : gcd(b, a % b));
    const divisor = gcd(Math.abs(giveValue), Math.abs(wantValue)) || 1;
    return [giveValue / divisor, wantValue / divisor];
}

export function canAffordUpgrade(domik: DomikDto, domikType: DomikTypeDto, resources: ResourceDto[]): boolean {
    if (domik.level <= 0 || domik.level >= domikType.maxLevel || domik.finishDate != null) {
        return false;
    }

    const nextLevel = nextUpgradeLevel(domikType, domik.level);
    return nextLevel != null && hasResourcesFor(nextLevel.resources, resources);
}

export function computePlodderCount(domiks: DomikDto[], domikTypes: DomikTypeDto[]): PlodderCount {
    let maxPlodderCount = 0;
    let workingPlodderCount = 0;

    domiks.forEach(domik => {
        if (domik.level > 0) {
            const domikType = domikTypes.find(x => x.id === domik.typeId);
            const domikLevel = domikType?.levels.find(x => x.value === domik.level);
            const modificator = domikLevel?.modificators.find(x => x.typeId === plodderTypeId);
            if (modificator != null) {
                maxPlodderCount += modificator.value;
            }
        }

        if (domik.manufactures != null) {
            domik.manufactures.forEach(manufacture => {
                workingPlodderCount += manufacture.plodderCount;
            });
        }
    });

    return { max: maxPlodderCount, free: maxPlodderCount - workingPlodderCount };
}

export function computeSelectedDomikView(
    selectedId: number | null,
    domiks: DomikDto[],
    domikTypes: DomikTypeDto[],
    receipts: ReceiptDto[],
    resources: ResourceDto[],
    now: number,
): SelectedDomikView | null {
    if (selectedId == null) {
        return null;
    }

    const domik = domiks.find(x => x.id === selectedId);
    if (domik == null || domik.level === 0) {
        return null;
    }

    const domikType = domikTypes.find(x => x.id === domik.typeId);
    if (domikType == null) {
        return null;
    }

    let domikReceipts: ReceiptDto[] = [];
    let upgrade: UpgradeView | null = null;

    if (domik.level > 0) {
        const domikLevel = domikType.levels.find(x => x.value === domik.level);
        if (domikLevel != null) {
            domikReceipts = domikLevel.receiptIds
                .map(receiptId => receipts.find(x => x.id === receiptId))
                .filter((receipt): receipt is ReceiptDto => receipt != null);

            const nextLevel = nextUpgradeLevel(domikType, domik.level);
            if (nextLevel != null && domik.level < domikType.maxLevel && domik.finishDate == null) {
                upgrade = {
                    nextLevel: domik.level + 1,
                    resources: nextLevel.resources,
                    hasResources: hasResourcesFor(nextLevel.resources, resources),
                };
            }
        }
    }

    const remainingText = domik.finishDate != null ? formatDuration(remainingSeconds(domik.finishDate, now)) : null;

    return { domik, domikType, receipts: domikReceipts, upgrade, remainingText };
}

function mergeResources(resources: ResourceDto[]): ResourceDto[] {
    const byType = new Map<number, number>();
    resources.forEach(res => byType.set(res.typeId, (byType.get(res.typeId) ?? 0) + res.value));
    return [...byType].map(([typeId, value]) => ({ typeId, value }));
}

export function zealMultiplier(zealCharges: number): number {
    if (zealCharges > ZEAL_X4_THRESHOLD) {
        return 4;
    }

    return zealCharges > 0 ? 2 : 1;
}

export function zealApplies(receipt: ReceiptDto, domikType: DomikTypeDto): boolean {
    return receipt.durationSeconds <= 3600 && domikType.logicName !== 'market';
}

export function computeReceiptView(
    receipt: ReceiptDto,
    resources: ResourceDto[],
    freePlodders: number,
    useOptional: boolean,
    zealCharges?: number,
    domikType?: DomikTypeDto,
): ReceiptView {
    const withOptional = useOptional && receipt.optionalInputResources.length > 0;
    const inputs = mergeResources(
        withOptional ? [...receipt.inputResources, ...receipt.optionalInputResources] : receipt.inputResources,
    );
    const durationSeconds = receipt.durationSeconds;
    const multiplier = domikType != null && zealApplies(receipt, domikType) ? zealMultiplier(zealCharges ?? 0) : 1;
    const effectiveDurationSeconds = Math.max(1, Math.floor(durationSeconds / multiplier));
    const hasResources = hasResourcesFor(inputs, resources);
    const hasPlodders = freePlodders >= receipt.plodderCount;

    return { receipt, inputs, durationSeconds, effectiveDurationSeconds, zealMultiplier: multiplier, hasResources, hasPlodders, canRun: hasResources && hasPlodders };
}

export function isWorkerFree(worker: WorkerDto, now: number): boolean {
    return worker.manufactureId == null && worker.expeditionId == null
        && (worker.restUntil == null || remainingSeconds(worker.restUntil, now) <= 0);
}

export function workerFitness(worker: WorkerDto, domikTypeId: number): number {
    const skillBonus = worker.skills.find(x => x.domikTypeId === domikTypeId)?.bonusPercent ?? 0;
    return -worker.traitDurationPercent + skillBonus;
}

export function progressPercent(finishDate: string, totalSeconds: number, now: number): number {
    if (totalSeconds <= 0) {
        return 0;
    }

    const current = remainingSeconds(finishDate, now);
    const percent = 100 - Math.floor((current * 100) / totalSeconds);
    return Math.min(100, Math.max(0, percent));
}

export function manufactureProgressPercent(manufacture: ManufactureDto, receipt: ReceiptDto, now: number): number {
    return progressPercent(manufacture.finishDate, receipt.durationSeconds, now);
}

export function instaFinishCost(finishDate: string, now: number): number {
    return Math.ceil(remainingSeconds(finishDate, now) / INSTA_FINISH_SECONDS_PER_GOLD);
}

export function canInstaFinish(finishDate: string, now: number): boolean {
    const remaining = remainingSeconds(finishDate, now);
    return remaining > 0 && remaining <= INSTA_FINISH_SECONDS_PER_GOLD * INSTA_FINISH_MAX_GOLD;
}

export type DomikStatus = 'upgradeReady' | 'upgrading' | 'producing' | 'idle';
export type DomikSortMode = 'attention' | 'type' | 'level';

export function domikStatus(domik: DomikDto, domikType: DomikTypeDto, resources: ResourceDto[]): DomikStatus {
    if (domik.finishDate != null) {
        return 'upgrading';
    }
    if (domik.manufactures != null && domik.manufactures.length > 0) {
        return 'producing';
    }
    if (canAffordUpgrade(domik, domikType, resources)) {
        return 'upgradeReady';
    }
    return 'idle';
}

const ATTENTION_ORDER: Record<DomikStatus, number> = { upgradeReady: 0, idle: 1, producing: 2, upgrading: 3 };

function attentionRank(domik: DomikDto, domikTypes: DomikTypeDto[], resources: ResourceDto[]): number {
    const type = domikTypes.find(x => x.id === domik.typeId);
    if (type == null) {
        return 9;
    }
    return ATTENTION_ORDER[domikStatus(domik, type, resources)];
}

export function sortDomiks(domiks: DomikDto[], domikTypes: DomikTypeDto[], resources: ResourceDto[], mode: DomikSortMode): DomikDto[] {
    const copy = [...domiks];
    if (mode === 'type') {
        return copy.sort((a, b) => a.typeId - b.typeId || b.level - a.level);
    }
    if (mode === 'level') {
        return copy.sort((a, b) => b.level - a.level || a.typeId - b.typeId);
    }
    return copy.sort((a, b) => attentionRank(a, domikTypes, resources) - attentionRank(b, domikTypes, resources));
}

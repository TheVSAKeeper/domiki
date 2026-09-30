import type { BlueprintDto, DomikDto, DomikTypeDto, ManufactureDto, ReceiptDto, ReceiptView, ResourceDto, SelectedDomikView, UpgradeView, WorkerDto } from '../types/api';
import { ZEAL_MAX_RECIPE_SECONDS, ZEAL_X4_THRESHOLD, type ManufactureDurationResult } from './manufactureDuration';
import { formatDuration, remainingSeconds } from './time';

export const INSTA_FINISH_SECONDS_PER_GOLD = 3600;
export const INSTA_FINISH_MAX_SECONDS = 6 * 3600;
export const INSTA_FINISH_MIN_SECONDS = 15 * 60;
export const INSTA_FINISH_CONFIRM_GOLD = 6;
export const GOLD_RESOURCE_TYPE_ID = 5;
export const COIN_RESOURCE_TYPE_ID = 1;
export const SICK_MIN_VILLAGE_LEVEL = 15;

export const EXPEDITION_LOOT_KIND_RESOURCE = 1;
export const EXPEDITION_LOOT_KIND_DECOR = 2;
export const EXPEDITION_LOOT_KIND_TRAIT_UPGRADE = 3;
export const EXPEDITION_LOOT_KIND_BLUEPRINT = 4;

export const PLODDER_MODIFICATOR_TYPE_ID = 1;

export function keyResourceTypeIds(receipts: ReceiptDto[], blueprints: BlueprintDto[]): number[] {
    const gated = new Set(blueprints.map(blueprint => blueprint.receiptId).filter(id => id != null));
    const ids = new Set<number>();
    for (const receipt of receipts.filter(x => gated.has(x.id))) {
        for (const output of receipt.outputResources) {
            ids.add(output.typeId);
        }
    }

    return [...ids];
}

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

export interface UpgradeIntentView {
    domikId: number;
    domikType: DomikTypeDto;
    nextLevel: number;
    resources: ResourceDto[];
    shortfall: ResourceDto[];
    ready: boolean;
}

export function computeUpgradeIntentView(
    intentDomikId: number | null,
    domiks: DomikDto[],
    domikTypes: DomikTypeDto[],
    resources: ResourceDto[],
): UpgradeIntentView | null {
    if (intentDomikId == null) {
        return null;
    }

    const domik = domiks.find(x => x.id === intentDomikId);
    const domikType = domikTypes.find(x => x.id === domik?.typeId);
    if (domik == null || domikType == null || domik.level === 0 || domik.finishDate != null) {
        return null;
    }

    const nextLevel = nextUpgradeLevel(domikType, domik.level);
    if (nextLevel == null || domik.level >= domikType.maxLevel) {
        return null;
    }

    const shortfall = resourceShortfall(nextLevel.resources, resources);

    return {
        domikId: domik.id,
        domikType,
        nextLevel: domik.level + 1,
        resources: nextLevel.resources,
        shortfall,
        ready: shortfall.length === 0,
    };
}

export function residentsGain(residents: number, residentsCap: number, bedsDelta: number): number {
    return Math.max(0, Math.min(residentsCap, residents + bedsDelta) - residents);
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
    return receipt.durationSeconds <= ZEAL_MAX_RECIPE_SECONDS && domikType.logicName !== 'market';
}

export function computeReceiptView(
    receipt: ReceiptDto,
    resources: ResourceDto[],
    freePlodders: number,
    useOptional: boolean,
    zealCharges?: number,
    domikType?: DomikTypeDto,
    plannedDuration?: ManufactureDurationResult | null,
): ReceiptView {
    const withOptional = useOptional && receipt.optionalInputResources.length > 0;
    const inputs = mergeResources(
        withOptional ? [...receipt.inputResources, ...receipt.optionalInputResources] : receipt.inputResources,
    );
    const durationSeconds = receipt.durationSeconds;
    const multiplier = domikType != null && zealApplies(receipt, domikType) ? zealMultiplier(zealCharges ?? 0) : 1;
    const effectiveDurationSeconds = plannedDuration?.seconds ?? Math.max(1, Math.floor(durationSeconds / multiplier));
    const hasResources = hasResourcesFor(inputs, resources);
    const hasPlodders = freePlodders >= receipt.plodderCount;

    return { receipt, inputs, durationSeconds, effectiveDurationSeconds, zealMultiplier: multiplier, hasResources, hasPlodders, canRun: hasResources && hasPlodders };
}

export function isWorkerFree(worker: WorkerDto, now: number): boolean {
    return !worker.isAway
        && worker.manufactureId == null && worker.expeditionId == null && worker.errandId == null && worker.incidentId == null
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

export function manufactureProgressPercent(manufacture: ManufactureDto, now: number): number {
    return progressPercent(manufacture.finishDate, manufacture.durationSeconds, now);
}

export function instaFinishCost(remaining: number, plodderCount: number): number {
    return remaining <= 0 ? 0 : Math.ceil(remaining * Math.max(1, plodderCount) / INSTA_FINISH_SECONDS_PER_GOLD);
}

export type InstaFinishBlock = 'tooFar' | 'tooSoon' | null;

export function instaFinishBlock(remaining: number): InstaFinishBlock {
    if (remaining > INSTA_FINISH_MAX_SECONDS) return 'tooFar';
    if (remaining < INSTA_FINISH_MIN_SECONDS) return 'tooSoon';
    return null;
}

export type DomikStatus = 'upgradeReady' | 'upgrading' | 'producing' | 'idle';
export type DomikSortMode = 'attention' | 'type' | 'level';
export type WorkIntensity = 'slow' | 'normal' | 'fast';

export function workIntensity(domik: DomikDto, domikType: DomikTypeDto): WorkIntensity {
    const active = domik.manufactures?.length ?? 0;
    const maxSlots = domikType.levels.find(level => level.value === domik.level)?.maxManufactureCount;
    if (active === 0 || maxSlots == null || maxSlots <= 1) {
        return 'normal';
    }
    if (active >= maxSlots) {
        return 'fast';
    }
    return active === 1 ? 'slow' : 'normal';
}

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

const MECHANIC_LOGIC_NAMES = new Set(['market_yard', 'gathering', 'scout_hut']);

function typeCategoryRank(domikType: DomikTypeDto): number {
    if (domikType.levels.some(level => level.receiptIds.length > 0)) {
        return 0;
    }
    if (MECHANIC_LOGIC_NAMES.has(domikType.logicName)) {
        return 1;
    }
    return 2;
}

export function sortDomiks(domiks: DomikDto[], domikTypes: DomikTypeDto[], resources: ResourceDto[], mode: DomikSortMode): DomikDto[] {
    const copy = [...domiks];
    if (mode === 'type') {
        return copy.sort((a, b) => {
            const typeA = domikTypes.find(x => x.id === a.typeId);
            const typeB = domikTypes.find(x => x.id === b.typeId);
            const rankA = typeA == null ? 3 : typeCategoryRank(typeA);
            const rankB = typeB == null ? 3 : typeCategoryRank(typeB);
            return rankA - rankB || a.typeId - b.typeId || b.level - a.level;
        });
    }
    if (mode === 'level') {
        return copy.sort((a, b) => b.level - a.level || a.typeId - b.typeId);
    }
    return copy.sort((a, b) => attentionRank(a, domikTypes, resources) - attentionRank(b, domikTypes, resources));
}

export function weatherEffects(effects: { domikTypeId: number; outputPercent: number }[], domikTypes: DomikTypeDto[]): { delta: number; domikType: DomikTypeDto }[] {
    const typeById = new Map(domikTypes.map(type => [type.id, type]));
    const rows: { delta: number; domikType: DomikTypeDto }[] = [];
    for (const effect of effects) {
        const domikType = typeById.get(effect.domikTypeId);
        if (effect.outputPercent === 100 || domikType == null) {
            continue;
        }
        rows.push({ delta: effect.outputPercent - 100, domikType });
    }
    return rows.sort((a, b) => Math.abs(b.delta) - Math.abs(a.delta) || a.domikType.id - b.domikType.id);
}

const DAY_MS = 86400000;
const HOUR_MS = 3600000;

export interface GoldVeinView {
    mined: number;
    cap: number;
    exhausted: boolean;
    hoursToFresh: number;
    blockReason: string | null;
}

export interface GoldVeinContext {
    domiks: DomikDto[];
    receipts: ReceiptDto[];
    goldMinedToday: number;
    now: number;
}

export function goldVeinView(receipt: ReceiptDto, domik: DomikDto, { domiks, receipts, goldMinedToday, now }: GoldVeinContext): GoldVeinView | null {
    if (domik.level <= 0 || !receipt.outputResources.some(output => output.typeId === GOLD_RESOURCE_TYPE_ID)) {
        return null;
    }

    const nextUtcMidnight = (Math.floor(now / DAY_MS) + 1) * DAY_MS;
    const cap = domik.level;
    const mined = Math.min(goldMinedToday, cap);
    const remaining = cap - mined;
    const hoursToFresh = Math.max(1, Math.ceil((nextUtcMidnight - now) / HOUR_MS));
    const crossesMidnight = now + receipt.durationSeconds * 1000 >= nextUtcMidnight;
    const reserved = domiks.flatMap(item => item.manufactures ?? []).reduce((sum, manufacture) => {
        if (Date.parse(manufacture.finishDate) >= nextUtcMidnight) {
            return sum;
        }
        const running = receipts.find(x => x.id === manufacture.receiptId);
        return sum + (running?.outputResources.filter(output => output.typeId === GOLD_RESOURCE_TYPE_ID).reduce((gold, output) => gold + output.value, 0) ?? 0);
    }, 0);
    const blockReason = crossesMidnight
        ? null
        : remaining <= 0
            ? `Жила на сегодня выбрана: ${mined} из ${cap} – новая через ${hoursToFresh} ч`
            : remaining <= reserved
                ? 'Остаток жилы уже на вороте – его заберёт смена, что сейчас идёт'
                : null;
    return { mined, cap, exhausted: remaining <= 0, hoursToFresh, blockReason };
}

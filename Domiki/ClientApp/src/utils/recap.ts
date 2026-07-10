import type { RecapEventDto } from '../types/api';

export interface RecapView {
    produced: { typeId: number; value: number }[];
    expeditions: { expeditionTypeId: number; loot: { typeId: number; value: number; isRare: boolean }[] }[];
    market: { kind: 'sold' | 'expired'; give: { typeId: number; value: number }; want?: { typeId: number; value: number } }[];
    upgrades: { domikTypeId: number; level: number }[];
    toloka: { tolokaTypeId: number }[];
}

export const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null;
export const isNumber = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value);

export const readResource = (value: unknown): { typeId: number; value: number } | null => {
    if (!isRecord(value) || !isNumber(value.resourceTypeId) || !isNumber(value.value)) {
        return null;
    }

    return { typeId: value.resourceTypeId, value: value.value };
};

export function buildRecapView(events: RecapEventDto[]): RecapView {
    const produced = new Map<number, number>();
    const expeditions: RecapView['expeditions'] = [];
    const market: RecapView['market'] = [];
    const upgrades: RecapView['upgrades'] = [];
    const toloka: RecapView['toloka'] = [];

    for (const event of events) {
        if (!isRecord(event.data)) {
            continue;
        }

        if (event.type === 'ManufactureFinished' && Array.isArray(event.data.resources)) {
            for (const resource of event.data.resources) {
                const parsed = readResource(resource);
                if (parsed != null) {
                    produced.set(parsed.typeId, (produced.get(parsed.typeId) ?? 0) + parsed.value);
                }
            }
        }

        if (event.type === 'ExpeditionReturned' && isNumber(event.data.expeditionTypeId) && Array.isArray(event.data.loot)) {
            const loot = event.data.loot.flatMap(entry => {
                if (!isRecord(entry) || !isNumber(entry.resourceTypeId) || !isNumber(entry.value) || typeof entry.isRare !== 'boolean') {
                    return [];
                }
                return [{ typeId: entry.resourceTypeId, value: entry.value, isRare: entry.isRare }];
            });
            expeditions.push({ expeditionTypeId: event.data.expeditionTypeId, loot });
        }

        if (event.type === 'LotSold') {
            const give = readResource({ resourceTypeId: event.data.giveResourceTypeId, value: event.data.giveValue });
            const want = readResource({ resourceTypeId: event.data.wantResourceTypeId, value: event.data.wantValue });
            if (give != null && want != null) {
                market.push({ kind: 'sold', give, want });
            }
        }

        if (event.type === 'LotExpired') {
            const give = readResource({ resourceTypeId: event.data.giveResourceTypeId, value: event.data.giveValue });
            if (give != null) {
                market.push({ kind: 'expired', give });
            }
        }

        if (event.type === 'DomikUpgraded' && isNumber(event.data.domikTypeId) && isNumber(event.data.level)) {
            upgrades.push({ domikTypeId: event.data.domikTypeId, level: event.data.level });
        }

        if (event.type === 'TolokaCompleted' && isNumber(event.data.tolokaTypeId)) {
            toloka.push({ tolokaTypeId: event.data.tolokaTypeId });
        }
    }

    return {
        produced: [...produced.entries()].map(([typeId, value]) => ({ typeId, value })),
        expeditions,
        market,
        upgrades,
        toloka,
    };
}

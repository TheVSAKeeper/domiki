import type { RecapEventDto } from '../types/api';
import { EXPEDITION_LOOT_KIND_RESOURCE } from './game';

export interface RecapLootEntry {
    kind: number;
    isRare: boolean;
    typeId?: number;
    value?: number;
    decorTypeId?: number;
    workerName?: string;
    workerGender?: number;
    newTrait?: string;
    newTraitLogicName?: string;
    blueprintId?: number;
    blueprintName?: string;
}

export interface RecapView {
    produced: { typeId: number; value: number }[];
    expeditions: { expeditionTypeId: number; loot: RecapLootEntry[] }[];
    market: { kind: 'sold' | 'expired'; give: { typeId: number; value: number }; want?: { typeId: number; value: number } }[];
    upgrades: { domikTypeId: number; level: number }[];
    toloka: { tolokaTypeId: number }[];
    gifts: { neighborId: number; resources: { resourceTypeId: number; value: number }[]; decorTypeId: number | null; visitIndex: number; big: boolean; date: string }[];
    guestbookEntries: { guestVillageName: string; guestCrestIcon: number; guestCrestColor: number; phraseId: number; date: string }[];
    villageHelped: { guestVillageName: string; guestCrestIcon: number; guestCrestColor: number; domikTypeName: string; reducedSeconds: number; date: string }[];
    incidents: { kind: 'missing' | 'resolved'; autoReturned?: boolean; workerName: string; workerGender: number; templateId: number; clueId?: number; resourceTypeId?: number; value?: number; traitUpgraded?: boolean; newTrait?: string; newTraitLogicName?: string }[];
    domikIncidents: { kind: 'started' | 'resolved'; autoResolved?: boolean; domikTypeId: number; templateId: number; clueId?: number; resourceTypeId?: number; value?: number; traitUpgraded?: boolean; newTrait?: string; newTraitLogicName?: string; heroWorkerName?: string; heroWorkerGender?: number; upgradedWorkerName?: string }[];
}

export const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null;
export const isNumber = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value);

export const readResource = (value: unknown): { typeId: number; value: number } | null => {
    if (!isRecord(value) || !isNumber(value.resourceTypeId) || !isNumber(value.value)) {
        return null;
    }

    return { typeId: value.resourceTypeId, value: value.value };
};

export const lootEntryKey = (entry: RecapLootEntry) => `${entry.kind}:${entry.typeId ?? ''}:${entry.decorTypeId ?? ''}:${entry.workerName ?? ''}:${entry.blueprintName ?? ''}`;

export const readLootEntry = (value: unknown): RecapLootEntry[] => {
    if (!isRecord(value) || typeof value.isRare !== 'boolean') {
        return [];
    }

    const kind = isNumber(value.kind) ? value.kind : (isNumber(value.resourceTypeId) ? EXPEDITION_LOOT_KIND_RESOURCE : null);
    if (kind == null) {
        return [];
    }

    return [{
        kind,
        isRare: value.isRare,
        ...(isNumber(value.resourceTypeId) ? { typeId: value.resourceTypeId } : {}),
        ...(isNumber(value.value) ? { value: value.value } : {}),
        ...(isNumber(value.decorTypeId) ? { decorTypeId: value.decorTypeId } : {}),
        ...(typeof value.workerName === 'string' ? { workerName: value.workerName } : {}),
        ...(isNumber(value.workerGender) ? { workerGender: value.workerGender } : {}),
        ...(typeof value.newTrait === 'string' ? { newTrait: value.newTrait } : {}),
        ...(typeof value.newTraitLogicName === 'string' ? { newTraitLogicName: value.newTraitLogicName } : {}),
        ...(isNumber(value.blueprintId) ? { blueprintId: value.blueprintId } : {}),
        ...(typeof value.blueprintName === 'string' ? { blueprintName: value.blueprintName } : {}),
    }];
};

export function buildRecapView(events: RecapEventDto[]): RecapView {
    const produced = new Map<number, number>();
    const expeditions: RecapView['expeditions'] = [];
    const market: RecapView['market'] = [];
    const upgrades: RecapView['upgrades'] = [];
    const toloka: RecapView['toloka'] = [];
    const gifts: RecapView['gifts'] = [];
    const guestbookEntries: RecapView['guestbookEntries'] = [];
    const villageHelped: RecapView['villageHelped'] = [];
    const incidents: RecapView['incidents'] = [];
    const domikIncidents: RecapView['domikIncidents'] = [];

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
            const loot = event.data.loot.flatMap(entry => readLootEntry(entry));
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

        if (event.type === 'NeighborGift') {
            const { neighborId, resources, decorTypeId, visitIndex, big } = event.data;
            if (!isNumber(neighborId) || !Array.isArray(resources) || !isNumber(visitIndex) || typeof big !== 'boolean') {
                continue;
            }

            gifts.push({
                neighborId,
                resources: resources.flatMap(resource => {
                    const parsed = readResource(resource);
                    return parsed == null ? [] : [{ resourceTypeId: parsed.typeId, value: parsed.value }];
                }),
                decorTypeId: isNumber(decorTypeId) ? decorTypeId : null,
                visitIndex,
                big,
                date: event.date,
            });
        }

        if (event.type === 'GuestbookEntryLeft') {
            const { guestVillageName, guestCrestIcon, guestCrestColor, phraseId } = event.data;
            if (typeof guestVillageName !== 'string' || !isNumber(guestCrestIcon) || !isNumber(guestCrestColor) || !isNumber(phraseId)) {
                continue;
            }

            guestbookEntries.push({ guestVillageName, guestCrestIcon, guestCrestColor, phraseId, date: event.date });
        }

        if (event.type === 'VillageHelped') {
            const { guestVillageName, guestCrestIcon, guestCrestColor, domikTypeName, reducedSeconds } = event.data;
            if (typeof guestVillageName !== 'string' || !isNumber(guestCrestIcon) || !isNumber(guestCrestColor) || typeof domikTypeName !== 'string' || !isNumber(reducedSeconds)) {
                continue;
            }

            villageHelped.push({ guestVillageName, guestCrestIcon, guestCrestColor, domikTypeName, reducedSeconds, date: event.date });
        }

        if (event.type === 'IncidentResolved') {
            const { autoReturned, workerName, workerGender, templateId, clueId, resourceTypeId, value, traitUpgraded, newTrait, newTraitLogicName } = event.data;
            if (typeof autoReturned !== 'boolean' || typeof workerName !== 'string' || !isNumber(workerGender) || !isNumber(templateId)) {
                continue;
            }
            incidents.push({ kind: 'resolved', autoReturned, workerName, workerGender, templateId, traitUpgraded: traitUpgraded === true, ...(isNumber(clueId) ? { clueId } : {}), ...(isNumber(resourceTypeId) ? { resourceTypeId } : {}), ...(isNumber(value) ? { value } : {}), ...(typeof newTrait === 'string' ? { newTrait } : {}), ...(typeof newTraitLogicName === 'string' ? { newTraitLogicName } : {}) });
        }

        if (event.type === 'WorkerMissing') {
            const { workerName, workerGender, templateId } = event.data;
            if (typeof workerName !== 'string' || !isNumber(workerGender) || !isNumber(templateId)) {
                continue;
            }
            incidents.push({ kind: 'missing', workerName, workerGender, templateId });
        }

        if (event.type === 'DomikIncidentStarted') {
            const { domikTypeId, templateId } = event.data;
            if (!isNumber(domikTypeId) || !isNumber(templateId)) {
                continue;
            }
            domikIncidents.push({ kind: 'started', domikTypeId, templateId });
        }

        if (event.type === 'DomikIncidentResolved') {
            const { autoResolved, domikTypeId, templateId, clueId, resourceTypeId, value, traitUpgraded, newTrait, newTraitLogicName, heroWorkerName, heroWorkerGender, upgradedWorkerName } = event.data;
            if (typeof autoResolved !== 'boolean' || !isNumber(domikTypeId) || !isNumber(templateId)) {
                continue;
            }
            domikIncidents.push({ kind: 'resolved', autoResolved, domikTypeId, templateId, traitUpgraded: traitUpgraded === true, ...(isNumber(clueId) ? { clueId } : {}), ...(isNumber(resourceTypeId) ? { resourceTypeId } : {}), ...(isNumber(value) ? { value } : {}), ...(typeof newTrait === 'string' ? { newTrait } : {}), ...(typeof newTraitLogicName === 'string' ? { newTraitLogicName } : {}), ...(typeof heroWorkerName === 'string' ? { heroWorkerName } : {}), ...(isNumber(heroWorkerGender) ? { heroWorkerGender } : {}), ...(typeof upgradedWorkerName === 'string' ? { upgradedWorkerName } : {}) });
        }
    }

    return {
        produced: [...produced.entries()].map(([typeId, value]) => ({ typeId, value })),
        expeditions,
        market,
        upgrades,
        toloka,
        gifts,
        guestbookEntries,
        villageHelped,
        incidents,
        domikIncidents,
    };
}

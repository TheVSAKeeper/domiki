import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { JournalBox } from './JournalBox';
import type { RecapEventDto } from '../types/api';

const backendDirectory = resolve(fileURLToPath(import.meta.url), '../../../..');

const readPlayerEventTypeNames = (): string[] => {
    const source = readFileSync(resolve(backendDirectory, 'Data/Entities/Infrastructure/PlayerEventType.cs'), 'utf8');
    const names = [...source.matchAll(/^\s*(?<name>\w+)\s*=\s*[^,\r\n]+,?\s*$/gm)].flatMap(match => match.groups?.name ?? []);
    return names.filter(name => name !== 'None');
};

const eventDataByType: Record<string, Record<string, unknown>> = {
    ManufactureFinished: { resources: [{ resourceTypeId: 1, value: 5 }] },
    DomikUpgraded: { domikTypeId: 1, level: 2 },
    ExpeditionReturned: { loot: [] },
    LotSold: { giveResourceTypeId: 1, giveValue: 5, wantResourceTypeId: 2, wantValue: 5 },
    LotExpired: { giveResourceTypeId: 1, giveValue: 5 },
    TolokaCompleted: { tolokaTypeId: 1 },
    GoalCompleted: { name: 'Наказ', rewardCoins: 10 },
    NeighborGift: { neighborId: 1, resources: [{ resourceTypeId: 1, value: 5 }], decorTypeId: null, visitIndex: 1, big: false },
    GuestbookEntryLeft: { guestVillageName: 'Заречье', guestCrestIcon: 0, guestCrestColor: 0, phraseId: 0 },
    VillageHelped: { guestVillageName: 'Заречье', guestCrestIcon: 0, guestCrestColor: 0, domikTypeName: 'Кузня', reducedSeconds: 60 },
    ErrandResolved: { neighborId: 1, templateId: 0, clueId: 0, coins: 10, reputation: 3 },
    WorkerMissing: { workerName: 'Аким', workerGender: 1, templateId: 0 },
    IncidentResolved: { workerName: 'Аким', workerGender: 1, templateId: 0, autoReturned: true },
    DomikIncidentStarted: { domikTypeId: 1, templateId: 0 },
    DomikIncidentResolved: { domikTypeId: 1, templateId: 0, autoResolved: true },
    WorkerMilestone: { milestoneType: 1, workerId: 1, workerName: 'Аким', workerGender: 1 },
    CloakWornOut: {},
    WorkerMeal: { count: 1, variant: 0, resources: [] },
    ManufactureRepeatFailed: { domikTypeId: 1, reason: 'нет припасов' },
    ManufactureMeasureMet: { domikTypeId: 1, resourceTypeId: 1, value: 10 },
    ManufactureReserveHeld: { domikTypeId: 1, resourceTypeId: 1 },
    Relocated: { workers: 3, blueprints: 2, knots: 1 },
    ManufactureGoldCapReached: { domikTypeId: 1, mined: 10, cap: 10 },
};

describe('journal event-type coverage contract', () => {
    it('renders the neutral fallback for an event type it does not know', () => {
        const event: RecapEventDto = { id: 1, type: 'ЧегоТоНовое', date: '2026-08-26T12:00:00Z', data: {} };
        const { container } = render(
            <JournalBox events={[event]} resourceTypes={[]} domikTypes={[]} decorTypes={[]} neighbors={[]} now={Date.parse(event.date)} />,
        );

        expect(container.querySelectorAll('.journal-entry[data-fallback]')).toHaveLength(1);
    });

    const eventTypeNames = readPlayerEventTypeNames();

    it('lists at least one PlayerEventType member', () => {
        expect(eventTypeNames.length).toBeGreaterThan(0);
    });

    it.each(eventTypeNames)('renders a journal entry for PlayerEventType.%s', typeName => {
        const data = eventDataByType[typeName];
        if (data === undefined) {
            throw new Error(`Нет тестовых данных для PlayerEventType.${typeName} – добавьте payload в eventDataByType`);
        }

        const event: RecapEventDto = { id: 1, type: typeName, date: '2026-08-26T12:00:00Z', data };
        const { container } = render(
            <JournalBox events={[event]} resourceTypes={[]} domikTypes={[]} decorTypes={[]} neighbors={[]} now={Date.parse(event.date)} />,
        );

        expect(container.querySelectorAll('.journal-entry')).toHaveLength(1);
        expect(container.querySelectorAll('.journal-entry[data-fallback]')).toHaveLength(0);
    });
});

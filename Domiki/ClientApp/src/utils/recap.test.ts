import { describe, expect, it } from 'vitest';
import type { RecapEventDto } from '../types/api';
import { buildRecapView } from './recap';

const events: RecapEventDto[] = [
    { type: 'ManufactureFinished', date: '2026-07-10T00:00:00Z', data: { resources: [{ resourceTypeId: 2, value: 3 }, { resourceTypeId: 3, value: 1 }] } },
    { type: 'ManufactureFinished', date: '2026-07-10T00:01:00Z', data: { resources: [{ resourceTypeId: 2, value: 7 }] } },
    { type: 'LotSold', date: '2026-07-10T00:02:00Z', data: { giveResourceTypeId: 4, giveValue: 20, wantResourceTypeId: 5, wantValue: 3 } },
    { type: 'LotExpired', date: '2026-07-10T00:03:00Z', data: { giveResourceTypeId: 6, giveValue: 8 } },
    { type: 'ManufactureFinished', date: '2026-07-10T00:04:00Z', data: { resources: [{ resourceTypeId: 'bad', value: 10 }] } },
    {
        type: 'ExpeditionReturned', date: '2026-07-10T00:05:00Z', data: {
            expeditionTypeId: 1,
            loot: [
                { resourceTypeId: 3, value: 20, isRare: false },
                { kind: 2, decorTypeId: 6, isRare: true },
                { kind: 3, workerName: 'Аким', newTrait: 'Везучий', isRare: true },
                { kind: 4, isRare: true, blueprintId: 2, blueprintName: 'Чертёж каменотёса' },
            ],
        },
    },
];

describe('buildRecapView', () => {
    it('aggregates only valid manufacture output by resource', () => {
        const recap = buildRecapView(events);

        expect(recap.produced).toEqual([{ typeId: 2, value: 10 }, { typeId: 3, value: 1 }]);
    });

    it.each([
        ['sold', { kind: 'sold', give: { typeId: 4, value: 20 }, want: { typeId: 5, value: 3 } }],
        ['expired', { kind: 'expired', give: { typeId: 6, value: 8 } }],
    ] as const)('keeps %s market event distinct', (_kind, expected) => {
        const recap = buildRecapView(events);

        expect(recap.market).toContainEqual(expected);
    });

    it('parses legacy resource loot without a kind field as resource loot', () => {
        const recap = buildRecapView(events);
        const loot = recap.expeditions[0]?.loot ?? [];

        expect(loot[0]).toEqual({ kind: 1, isRare: false, typeId: 3, value: 20 });
    });

    it('parses decor and trait-upgrade loot kinds', () => {
        const recap = buildRecapView(events);
        const loot = recap.expeditions[0]?.loot ?? [];

        expect(loot[1]).toEqual({ kind: 2, isRare: true, decorTypeId: 6 });
        expect(loot[2]).toEqual({ kind: 3, isRare: true, workerName: 'Аким', newTrait: 'Везучий' });
    });

    it('parses blueprint loot kind', () => {
        const recap = buildRecapView(events);
        const loot = recap.expeditions[0]?.loot ?? [];

        expect(loot[3]).toEqual({ kind: 4, isRare: true, blueprintId: 2, blueprintName: 'Чертёж каменотёса' });
    });

    it('parses ordinary and big neighbor gifts', () => {
        const recap = buildRecapView([
            { type: 'NeighborGift', date: '2026-07-10T00:06:00Z', data: { neighborId: 2, resources: [{ resourceTypeId: 3, value: 12 }], decorTypeId: null, visitIndex: 4, big: false } },
            { type: 'NeighborGift', date: '2026-07-10T00:07:00Z', data: { neighborId: 5, resources: [], decorTypeId: 8, visitIndex: 7, big: true } },
        ]);

        expect(recap.gifts).toEqual([
            { neighborId: 2, resources: [{ resourceTypeId: 3, value: 12 }], decorTypeId: null, visitIndex: 4, big: false, date: '2026-07-10T00:06:00Z' },
            { neighborId: 5, resources: [], decorTypeId: 8, visitIndex: 7, big: true, date: '2026-07-10T00:07:00Z' },
        ]);
    });

    it('silently ignores a neighbor gift without a neighbor id', () => {
        const recap = buildRecapView([
            { type: 'NeighborGift', date: '2026-07-10T00:08:00Z', data: { resources: [], visitIndex: 1, big: false } },
        ]);

        expect(recap.gifts).toEqual([]);
    });

    it('parses a guestbook entry left while away', () => {
        const recap = buildRecapView([
            { type: 'GuestbookEntryLeft', date: '2026-07-10T00:09:00Z', data: { guestVillageName: 'Заречье', guestCrestIcon: 2, guestCrestColor: 4, phraseId: 3 } },
        ]);

        expect(recap.guestbookEntries).toEqual([
            { guestVillageName: 'Заречье', guestCrestIcon: 2, guestCrestColor: 4, phraseId: 3, date: '2026-07-10T00:09:00Z' },
        ]);
    });

    it('silently ignores a guestbook entry without a guest village name', () => {
        const recap = buildRecapView([
            { type: 'GuestbookEntryLeft', date: '2026-07-10T00:10:00Z', data: { guestCrestIcon: 2, guestCrestColor: 4, phraseId: 3 } },
        ]);

        expect(recap.guestbookEntries).toEqual([]);
    });

    it('parses a village help received while away', () => {
        const recap = buildRecapView([
            { type: 'VillageHelped', date: '2026-07-10T00:11:00Z', data: { guestVillageName: 'Заречье', guestCrestIcon: 2, guestCrestColor: 4, domikTypeName: 'Лесопилка', reducedSeconds: 600 } },
        ]);

        expect(recap.villageHelped).toEqual([
            { guestVillageName: 'Заречье', guestCrestIcon: 2, guestCrestColor: 4, domikTypeName: 'Лесопилка', reducedSeconds: 600, date: '2026-07-10T00:11:00Z' },
        ]);
    });

    it('silently ignores a village help entry without a duration', () => {
        const recap = buildRecapView([
            { type: 'VillageHelped', date: '2026-07-10T00:12:00Z', data: { guestVillageName: 'Заречье', guestCrestIcon: 2, guestCrestColor: 4, domikTypeName: 'Лесопилка' } },
        ]);

        expect(recap.villageHelped).toEqual([]);
    });

    it('parses started and resolved building incidents', () => {
        const recap = buildRecapView([
            { type: 'DomikIncidentStarted', date: '2026-07-10T00:13:00Z', data: { domikTypeId: 3, templateId: 2 } },
            { type: 'DomikIncidentResolved', date: '2026-07-10T00:14:00Z', data: { autoResolved: false, domikTypeId: 3, templateId: 2, clueId: 1, resourceTypeId: 5, value: 12, traitUpgraded: true, newTrait: 'Везучий', newTraitLogicName: 'lucky', heroWorkerName: 'Аким', heroWorkerGender: 0, upgradedWorkerName: 'Степан' } },
        ]);

        expect(recap.domikIncidents).toEqual([
            { kind: 'started', domikTypeId: 3, templateId: 2 },
            { kind: 'resolved', autoResolved: false, domikTypeId: 3, templateId: 2, clueId: 1, resourceTypeId: 5, value: 12, traitUpgraded: true, newTrait: 'Везучий', newTraitLogicName: 'lucky', heroWorkerName: 'Аким', heroWorkerGender: 0, upgradedWorkerName: 'Степан' },
        ]);
    });
});

import { describe, expect, it } from 'vitest';
import type { DomikDto, DomikTypeDto, ManufactureDto, ReceiptDto, ResourceDto } from '../types/api';
import { canAffordUpgrade, computePlodderCount, computeReceiptView, manufactureProgressPercent, progressPercent, sortDomiks } from './game';

const domikTypes: DomikTypeDto[] = [
    {
        id: 1,
        name: 'Рынок',
        logicName: 'market',
        maxCount: 1,
        availableCount: 0,
        maxLevel: 2,
        unlockLevel: 0,
        blueprintId: null,
        levels: [
            { value: 1, resources: [], modificators: [{ typeId: 1, value: 3 }], receiptIds: [] },
            { value: 2, resources: [], modificators: [{ typeId: 1, value: 5 }], receiptIds: [] },
        ],
    },
];

describe('computePlodderCount', () => {
    it('sums modificator values for domiks with a level and subtracts working manufactures', () => {
        const domiks: DomikDto[] = [
            { id: 1, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null },
            { id: 2, typeId: 1, level: 2, finishDate: null, upgradeSeconds: null, manufactures: [{ id: 1, finishDate: '2026-01-01T00:00:00.000Z', plodderCount: 2, receiptId: 1, autoRepeat: false }] },
        ];

        expect(computePlodderCount(domiks, domikTypes)).toEqual({ max: 8, free: 6 });
    });

    it('ignores domiks at level 0 and domiks with no matching level', () => {
        const domiks: DomikDto[] = [
            { id: 1, typeId: 1, level: 0, finishDate: null, upgradeSeconds: null, manufactures: null },
            { id: 2, typeId: 1, level: 99, finishDate: null, upgradeSeconds: null, manufactures: null },
        ];

        expect(computePlodderCount(domiks, domikTypes)).toEqual({ max: 0, free: 0 });
    });
});

describe('canAffordUpgrade', () => {
    const mineType: DomikTypeDto = {
        id: 1,
        name: 'Шахта',
        logicName: 'mine',
        maxCount: 1,
        availableCount: 0,
        maxLevel: 2,
        unlockLevel: 0,
        blueprintId: null,
        levels: [
            { value: 1, resources: [{ typeId: 1, value: 100 }], modificators: [], receiptIds: [] },
            { value: 2, resources: [], modificators: [], receiptIds: [] },
        ],
    };
    const base: DomikDto = { id: 1, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null };

    it('true only when upgrade available and resources suffice', () => {
        expect(canAffordUpgrade(base, mineType, [{ typeId: 1, value: 100 }])).toBe(true);
    });

    it.each<[string, DomikDto, ResourceDto[]]>([
        ['not enough resources', base, [{ typeId: 1, value: 99 }]],
        ['upgrade in progress', { ...base, finishDate: '2026-01-01T00:00:00.000Z' }, [{ typeId: 1, value: 100 }]],
        ['at max level', { ...base, level: 2 }, [{ typeId: 1, value: 100 }]],
        ['level zero', { ...base, level: 0 }, [{ typeId: 1, value: 100 }]],
    ])('false when %s', (_label, domik, resources) => {
        expect(canAffordUpgrade(domik, mineType, resources)).toBe(false);
    });
});

describe('computeReceiptView', () => {
    const receipt: ReceiptDto = {
        id: 1,
        name: 'Доски',
        logicName: 'planks',
        inputResources: [{ typeId: 2, value: 10 }],
        optionalInputResources: [{ typeId: 2, value: 5 }],
        outputResources: [{ typeId: 3, value: 1 }],
        durationSeconds: 100,
        speedupPercent: 20,
        plodderCount: 2,
    };

    it('runnable when resources and free plodders suffice', () => {
        const view = computeReceiptView(receipt, [{ typeId: 2, value: 10 }], 2, false);
        expect(view).toMatchObject({ hasResources: true, hasPlodders: true, canRun: true, durationSeconds: 100 });
        expect(view.inputs).toEqual([{ typeId: 2, value: 10 }]);
    });

    it('blocks on missing resources and on missing plodders independently', () => {
        expect(computeReceiptView(receipt, [{ typeId: 2, value: 9 }], 5, false)).toMatchObject({ hasResources: false, canRun: false });
        expect(computeReceiptView(receipt, [{ typeId: 2, value: 10 }], 1, false)).toMatchObject({ hasPlodders: false, canRun: false });
    });

    it('with optional tool merges inputs by type and shortens duration', () => {
        const view = computeReceiptView(receipt, [{ typeId: 2, value: 15 }], 2, true);
        expect(view.inputs).toEqual([{ typeId: 2, value: 15 }]);
        expect(view.durationSeconds).toBe(80);
        expect(view.canRun).toBe(true);
    });
});

describe('manufactureProgressPercent', () => {
    const receipt: ReceiptDto = {
        id: 1,
        name: 'Доски',
        logicName: 'planks',
        inputResources: [],
        optionalInputResources: [],
        outputResources: [],
        durationSeconds: 100,
        speedupPercent: 0,
        plodderCount: 1,
    };

    it.each<[number, number]>([
        [0, 100],
        [50, 50],
        [100, 0],
        [150, 0],
        [-10, 100],
    ])('finishDate %i seconds from now -> %i%%', (secondsFromNow, expected) => {
        const now = 0;
        const manufacture: ManufactureDto = { id: 1, finishDate: new Date(secondsFromNow * 1000).toISOString(), plodderCount: 1, receiptId: 1, autoRepeat: false };
        expect(manufactureProgressPercent(manufacture, receipt, now)).toBe(expected);
    });
});

describe('progressPercent', () => {
    it('returns 0 when total duration is not positive', () => {
        expect(progressPercent('2026-01-01T00:00:00.000Z', 0, 0)).toBe(0);
    });
});

describe('sortDomiks', () => {
    const sortTypes: DomikTypeDto[] = [
        {
            id: 1,
            name: 'Шахта',
            logicName: 'mine',
            maxCount: 1,
            availableCount: 0,
            maxLevel: 2,
            unlockLevel: 0,
            blueprintId: null,
            levels: [
                { value: 1, resources: [{ typeId: 1, value: 100 }], modificators: [], receiptIds: [] },
                { value: 2, resources: [], modificators: [], receiptIds: [] },
            ],
        },
        {
            id: 2,
            name: 'Ферма',
            logicName: 'farm',
            maxCount: 1,
            availableCount: 0,
            maxLevel: 1,
            unlockLevel: 0,
            blueprintId: null,
            levels: [
                { value: 1, resources: [{ typeId: 1, value: 100 }], modificators: [], receiptIds: [] },
            ],
        },
    ];

    const idle: DomikDto = { id: 1, typeId: 1, level: 2, finishDate: null, upgradeSeconds: null, manufactures: null };
    const upgradeReady: DomikDto = { id: 2, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null };
    const producing: DomikDto = {
        id: 3, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null,
        manufactures: [{ id: 1, finishDate: '2026-01-01T00:00:00.000Z', plodderCount: 1, receiptId: 1, autoRepeat: false }],
    };
    const upgrading: DomikDto = { id: 4, typeId: 1, level: 1, finishDate: '2026-01-01T00:00:00.000Z', upgradeSeconds: 60, manufactures: null };
    const poorResources: ResourceDto[] = [{ typeId: 1, value: 0 }];
    const richResources: ResourceDto[] = [{ typeId: 1, value: 100 }];

    it('attention: orders upgradeReady, idle, producing, upgrading', () => {
        const domiks = [upgrading, producing, idle, upgradeReady];
        const sorted = sortDomiks(domiks, sortTypes, richResources, 'attention');
        expect(sorted.map(x => x.id)).toEqual([2, 1, 3, 4]);
    });

    it('attention: keeps original order for domiks with equal rank', () => {
        const first: DomikDto = { id: 5, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null };
        const second: DomikDto = { id: 6, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null };
        const sorted = sortDomiks([first, second], sortTypes, poorResources, 'attention');
        expect(sorted.map(x => x.id)).toEqual([5, 6]);
    });

    it('does not mutate the input array', () => {
        const domiks = [upgrading, idle];
        sortDomiks(domiks, sortTypes, richResources, 'attention');
        expect(domiks.map(x => x.id)).toEqual([4, 1]);
    });

    it('type: sorts by typeId ascending, then level descending', () => {
        const domiks: DomikDto[] = [
            { id: 1, typeId: 2, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null },
            { id: 2, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null },
            { id: 3, typeId: 1, level: 2, finishDate: null, upgradeSeconds: null, manufactures: null },
        ];
        expect(sortDomiks(domiks, sortTypes, poorResources, 'type').map(x => x.id)).toEqual([3, 2, 1]);
    });

    it('level: sorts by level descending, then typeId ascending', () => {
        const domiks: DomikDto[] = [
            { id: 1, typeId: 2, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null },
            { id: 2, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null },
            { id: 3, typeId: 1, level: 2, finishDate: null, upgradeSeconds: null, manufactures: null },
        ];
        expect(sortDomiks(domiks, sortTypes, poorResources, 'level').map(x => x.id)).toEqual([3, 2, 1]);
    });
});

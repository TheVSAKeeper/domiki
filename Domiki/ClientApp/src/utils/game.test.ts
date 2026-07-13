import { describe, expect, it } from 'vitest';
import type { DomikDto, DomikTypeDto, ManufactureDto, ReceiptDto, ResourceDto } from '../types/api';
import { canAffordUpgrade, computePlodderCount, computeReceiptView, manufactureProgressPercent, progressPercent, resourceShortfall, resourceSourceMap, sortDomiks, tradeDeal, tradeRatio, zealApplies, zealMultiplier } from './game';

describe('resourceShortfall', () => {
    it('returns exact deficits and merges repeated costs', () => {
        expect(resourceShortfall(
            [{ typeId: 2, value: 8 }, { typeId: 3, value: 4 }, { typeId: 2, value: 3 }],
            [{ typeId: 2, value: 7 }, { typeId: 3, value: 9 }],
        )).toEqual([{ typeId: 2, value: 4 }]);
    });
});

describe('resourceSourceMap', () => {
    const receipt = (id: number, outputTypeIds: number[]): ReceiptDto => ({
        id, name: `r${id}`, logicName: `r${id}`, inputResources: [], optionalInputResources: [],
        durationSeconds: 10, outputBonusPercent: 0, plodderCount: 1,
        outputResources: outputTypeIds.map(typeId => ({ typeId, value: 1 })),
    });
    const building = (id: number, name: string, logicName: string, receiptIds: number[]): DomikTypeDto => ({
        id, name, logicName, maxCount: 1, availableCount: 0, maxLevel: 2, unlockLevel: 0,
        blueprintId: null, nextCountGateLevel: null,
        levels: [{ value: 1, resources: [], modificators: [], receiptIds, maxManufactureCount: 0 }],
    });

    it('maps each output resource to the buildings that produce it, without duplicates', () => {
        const receipts = [receipt(1, [10, 11]), receipt(2, [11])];
        const buildings = [
            building(1, 'Маслобойня', 'creamery', [1]),
            building(2, 'Кузница', 'forge', [2]),
        ];

        const map = resourceSourceMap(buildings, receipts);

        expect(map.get(10)).toEqual([{ logicName: 'creamery', name: 'Маслобойня' }]);
        expect(map.get(11)).toEqual([
            { logicName: 'creamery', name: 'Маслобойня' },
            { logicName: 'forge', name: 'Кузница' },
        ]);
        expect(map.get(99)).toBeUndefined();
    });
});

describe('tradeDeal', () => {
    it.each([
        { give: 20, giveMv: 1, want: 10, wantMv: 1, deal: 'good' },
        { give: 10, giveMv: 1, want: 10, wantMv: 1, deal: 'fair' },
        { give: 5, giveMv: 1, want: 10, wantMv: 1, deal: 'bad' },
        { give: 10, giveMv: 2, want: 1, wantMv: 1, deal: 'good' },
        { give: 10, giveMv: 1, want: 1, wantMv: 0, deal: 'fair' },
    ])('rates $give×$giveMv for $want×$wantMv as $deal', ({ give, giveMv, want, wantMv, deal }) => {
        expect(tradeDeal(give, giveMv, want, wantMv)).toBe(deal);
    });
});

describe('tradeRatio', () => {
    it.each([
        { give: 10, want: 1, expected: [10, 1] },
        { give: 20, want: 4, expected: [5, 1] },
        { give: 7, want: 3, expected: [7, 3] },
    ])('reduces $give:$want', ({ give, want, expected }) => {
        expect(tradeRatio(give, want)).toEqual(expected);
    });
});

const marketDomikType: DomikTypeDto = {
    id: 1,
    name: 'Рынок',
    logicName: 'market',
    maxCount: 1,
    availableCount: 0,
    maxLevel: 2,
    unlockLevel: 0,
    blueprintId: null,
    nextCountGateLevel: null,
    levels: [
        { value: 1, resources: [], modificators: [{ typeId: 1, value: 3 }], receiptIds: [], maxManufactureCount: 0 },
        { value: 2, resources: [], modificators: [{ typeId: 1, value: 5 }], receiptIds: [], maxManufactureCount: 0 },
    ],
};

const domikTypes: DomikTypeDto[] = [marketDomikType];

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
        maxLevel: 3,
        unlockLevel: 0,
        blueprintId: null,
        nextCountGateLevel: null,
        levels: [
            { value: 1, resources: [{ typeId: 1, value: 10 }], modificators: [], receiptIds: [], maxManufactureCount: 0 },
            { value: 2, resources: [{ typeId: 1, value: 100 }], modificators: [], receiptIds: [], maxManufactureCount: 0 },
            { value: 3, resources: [{ typeId: 1, value: 999 }], modificators: [], receiptIds: [], maxManufactureCount: 0 },
        ],
    };
    const base: DomikDto = { id: 1, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null };

    it('checks the next level cost, not the current one', () => {
        expect(canAffordUpgrade(base, mineType, [{ typeId: 1, value: 100 }])).toBe(true);
        expect(canAffordUpgrade({ ...base, level: 2 }, mineType, [{ typeId: 1, value: 100 }])).toBe(false);
    });

    it.each<[string, DomikDto, ResourceDto[]]>([
        ['not enough resources', base, [{ typeId: 1, value: 99 }]],
        ['upgrade in progress', { ...base, finishDate: '2026-01-01T00:00:00.000Z' }, [{ typeId: 1, value: 100 }]],
        ['at max level', { ...base, level: 3 }, [{ typeId: 1, value: 999 }]],
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
        outputBonusPercent: 20,
        plodderCount: 2,
    };

    it('runnable when resources and free plodders suffice', () => {
        const view = computeReceiptView(receipt, [{ typeId: 2, value: 10 }], 2, false);
        expect(view).toMatchObject({ hasResources: true, hasPlodders: true, canRun: true, durationSeconds: 100 });
        expect(view.inputs).toEqual([{ typeId: 2, value: 10 }]);
    });

    const mineType: DomikTypeDto = { ...marketDomikType, logicName: 'clay_mine' };

    it('blocks on missing resources and on missing plodders independently', () => {
        expect(computeReceiptView(receipt, [{ typeId: 2, value: 9 }], 5, false)).toMatchObject({ hasResources: false, canRun: false });
        expect(computeReceiptView(receipt, [{ typeId: 2, value: 10 }], 1, false)).toMatchObject({ hasPlodders: false, canRun: false });
    });

    it('with optional tool merges inputs by type and preserves duration', () => {
        const view = computeReceiptView(receipt, [{ typeId: 2, value: 15 }], 2, true);
        expect(view.inputs).toEqual([{ typeId: 2, value: 15 }]);
        expect(view.durationSeconds).toBe(100);
        expect(view.canRun).toBe(true);
    });

    it('uses zeal charges for the effective duration', () => {
        const view = computeReceiptView({ ...receipt, durationSeconds: 3600 }, [{ typeId: 2, value: 10 }], 2, false, 24, mineType);
        expect(view).toMatchObject({ durationSeconds: 3600, effectiveDurationSeconds: 900, zealMultiplier: 4 });
    });
});

describe('zealMultiplier', () => {
    it.each<[number, number]>([
        [17, 4],
        [16, 2],
        [1, 2],
        [0, 1],
    ])('%i charges → ×%i', (charges, expected) => {
        expect(zealMultiplier(charges)).toBe(expected);
    });
});

describe('zealApplies', () => {
    const mineType: DomikTypeDto = { ...marketDomikType, logicName: 'clay_mine' };
    const receipt: ReceiptDto = {
        id: 1,
        name: 'Глина',
        logicName: 'clay',
        inputResources: [],
        optionalInputResources: [],
        outputResources: [],
        durationSeconds: 3600,
        outputBonusPercent: 0,
        plodderCount: 1,
    };

    it.each<[string, ReceiptDto, DomikTypeDto, boolean]>([
        ['долгой смены', { ...receipt, durationSeconds: 28800 }, mineType, false],
        ['лавки', receipt, marketDomikType, false],
        ['смены ровно на час', receipt, mineType, true],
    ])('возвращает %s', (_label, candidate, domikType, expected) => {
        expect(zealApplies(candidate, domikType)).toBe(expected);
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
        outputBonusPercent: 0,
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
            nextCountGateLevel: null,
            levels: [
                { value: 1, resources: [{ typeId: 1, value: 100 }], modificators: [], receiptIds: [], maxManufactureCount: 0 },
                { value: 2, resources: [], modificators: [], receiptIds: [], maxManufactureCount: 0 },
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
            nextCountGateLevel: null,
            levels: [
                { value: 1, resources: [{ typeId: 1, value: 100 }], modificators: [], receiptIds: [], maxManufactureCount: 0 },
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

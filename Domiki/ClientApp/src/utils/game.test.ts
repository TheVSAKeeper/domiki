import { describe, expect, it } from 'vitest';
import type { DomikDto, DomikTypeDto, GameStateDto, ManufactureDto, ReceiptDto, ResourceDto, WorkerDto } from '../types/api';
import { planManufacture, predictState } from './optimistic';
import { canAffordUpgrade, computeReceiptView, goldVeinView, instaFinishBlock, instaFinishCost, isWorkerFree, manufactureProgressPercent, progressPercent, residentsGain, resourceShortfall, resourceSourceMap, sortDomiks, tradeDeal, tradeRatio, weatherEffects, workIntensity, zealApplies, zealMultiplier } from './game';
import type { WorkIntensity } from './game';

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
        levels: [{ value: 1, resources: [], modificators: [], receiptIds, upgradeSeconds: 0, maxManufactureCount: 0 }],
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
        { value: 1, resources: [], modificators: [{ typeId: 1, value: 3 }], receiptIds: [], upgradeSeconds: 0, maxManufactureCount: 0 },
        { value: 2, resources: [], modificators: [{ typeId: 1, value: 5 }], receiptIds: [], upgradeSeconds: 0, maxManufactureCount: 0 },
    ],
};

describe('residentsGain', () => {
    it.each([
        ['койки ниже потолка идут целиком', 20, 35, 5, 5],
        ['потолок срезает хвост прибавки', 33, 35, 5, 2],
        ['на потолке прибавки нет', 35, 35, 5, 0],
        ['за потолком прибавки нет', 40, 35, 5, 0],
        ['уровень без коек ничего не даёт', 20, 35, 0, 0],
    ])('%s', (_, residents, cap, delta, expected) => {
        expect(residentsGain(residents, cap, delta)).toBe(expected);
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
            { value: 1, resources: [{ typeId: 1, value: 10 }], modificators: [], receiptIds: [], upgradeSeconds: 0, maxManufactureCount: 0 },
            { value: 2, resources: [{ typeId: 1, value: 100 }], modificators: [], receiptIds: [], upgradeSeconds: 0, maxManufactureCount: 0 },
            { value: 3, resources: [{ typeId: 1, value: 999 }], modificators: [], receiptIds: [], upgradeSeconds: 0, maxManufactureCount: 0 },
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

    describe('с составом смены', () => {
        const nowMs = Date.UTC(2026, 8, 30, 12, 0, 0);
        const shiftReceipt: ReceiptDto = { ...receipt, durationSeconds: 1000, plodderCount: 1 };
        const forge: DomikTypeDto = {
            ...mineType,
            id: 5,
            logicName: 'forge',
            levels: [{ value: 1, resources: [], modificators: [], receiptIds: [1], upgradeSeconds: 0, maxManufactureCount: 2 }],
        };
        const worker = (id: number, over: Partial<WorkerDto> = {}): WorkerDto => ({
            id, name: `Трудяга ${String(id)}`, gender: 0, traitId: 0, traitName: '', traitLogicName: '', traitDurationPercent: 0,
            noFatigue: false, noSick: false, manufactureId: null, expeditionId: null, errandId: null, incidentId: null,
            workedSeconds: 0, restUntil: null, sickUntil: null, sickTypeId: null, isAway: false, skills: [], ...over,
        });
        const skilled = (bonusPercent: number) => ({ skills: [{ domikTypeId: forge.id, bonusPercent }] }) as Partial<WorkerDto>;
        const gameState = (over: Partial<GameStateDto>): GameStateDto => ({
            domiks: [{ id: 1, typeId: forge.id, level: 1, finishDate: null, upgradeSeconds: null, manufactures: [] }],
            domikTypes: [forge],
            receipts: [shiftReceipt],
            resources: [{ typeId: 2, value: 100 }],
            blueprints: [],
            workers: [worker(1)],
            villageLevel: { level: 1 },
            village: { profileNeighborId: null },
            villageProfiles: [],
            relocation: { perks: [] },
            goals: { zealCharges: 0 },
            ...over,
        } as unknown as GameStateDto);

        it.each<[string, Partial<GameStateDto>, number[], number]>([
            ['черта Работящий у авто-подбора умной артели', { villageLevel: { level: 8 } as GameStateDto['villageLevel'], workers: [worker(1), worker(2, { traitDurationPercent: -20 })] }, [], 800],
            ['навык у трудяги, выбранного вручную', { workers: [worker(1), worker(2, skilled(25))] }, [2], 750],
            ['черта и навык под рвением ×4', { workers: [worker(1, { traitDurationPercent: 10, ...skilled(20) })], goals: { zealCharges: 17 } as GameStateDto['goals'] }, [], 220],
        ])('%s: карточка обещает столько же, сколько нарисует смена', (_, over, workerIds, expected) => {
            const state = gameState(over);
            const plan = planManufacture(state, forge, shiftReceipt, workerIds, nowMs);
            const card = computeReceiptView(shiftReceipt, state.resources, state.workers.length, false, state.goals.zealCharges, forge, plan?.duration);
            const legacy = computeReceiptView(shiftReceipt, state.resources, state.workers.length, false, state.goals.zealCharges, forge);
            const predicted = predictState(state, [{
                command: { kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds } },
                queuedAtMs: nowMs,
            }]);

            expect(card.effectiveDurationSeconds).toBe(expected);
            expect(predicted.state.domiks[0]?.manufactures?.[0]?.durationSeconds).toBe(expected);
            expect(legacy.effectiveDurationSeconds).not.toBe(expected);
        });

        it('без свободных трудяг оставляет оценку по рвению', () => {
            const state = gameState({ workers: [worker(1, { manufactureId: 7, traitDurationPercent: -20 })] });
            const plan = planManufacture(state, forge, shiftReceipt, [], nowMs);
            expect(plan).toBeNull();
            expect(computeReceiptView(shiftReceipt, state.resources, 0, false, 0, forge, plan?.duration).effectiveDurationSeconds).toBe(1000);
        });
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
    it.each<[number, number]>([
        [0, 100],
        [50, 50],
        [100, 0],
        [150, 0],
        [-10, 100],
    ])('finishDate %i seconds from now -> %i%%', (secondsFromNow, expected) => {
        const now = 0;
        const manufacture: ManufactureDto = { id: 1, finishDate: new Date(secondsFromNow * 1000).toISOString(), durationSeconds: 100, plodderCount: 1, receiptId: 1, autoRepeat: false };
        expect(manufactureProgressPercent(manufacture, now)).toBe(expected);
    });
});

describe('progressPercent', () => {
    it('returns 0 when total duration is not positive', () => {
        expect(progressPercent('2026-01-01T00:00:00.000Z', 0, 0)).toBe(0);
    });
});

describe('isWorkerFree', () => {
    const worker = (over: Partial<WorkerDto>): WorkerDto => ({
        id: 1, name: 'Прохор', gender: 0, traitId: 0, traitName: '', traitLogicName: '', traitDurationPercent: 0,
        noFatigue: false, noSick: false, manufactureId: null, expeditionId: null, errandId: null, incidentId: null,
        workedSeconds: 0, restUntil: null, sickUntil: null, sickTypeId: null, isAway: false, skills: [], ...over,
    });

    it.each<[string, Partial<WorkerDto>, boolean]>([
        ['ничем не занят', {}, true],
        ['в отходе – койки не досталось', { isAway: true }, false],
        ['на смене', { manufactureId: 1 }, false],
        ['отдыхает', { restUntil: new Date(3600 * 1000).toISOString() }, false],
    ])('%s -> %s', (_case, over, expected) => {
        expect(isWorkerFree(worker(over), 0)).toBe(expected);
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
                { value: 1, resources: [{ typeId: 1, value: 100 }], modificators: [], receiptIds: [], upgradeSeconds: 0, maxManufactureCount: 0 },
                { value: 2, resources: [], modificators: [], receiptIds: [], upgradeSeconds: 0, maxManufactureCount: 0 },
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
                { value: 1, resources: [{ typeId: 1, value: 100 }], modificators: [], receiptIds: [], upgradeSeconds: 0, maxManufactureCount: 0 },
            ],
        },
    ];

    const idle: DomikDto = { id: 1, typeId: 1, level: 2, finishDate: null, upgradeSeconds: null, manufactures: null };
    const upgradeReady: DomikDto = { id: 2, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null };
    const producing: DomikDto = {
        id: 3, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null,
        manufactures: [{ id: 1, finishDate: '2026-01-01T00:00:00.000Z', durationSeconds: 10, plodderCount: 1, receiptId: 1, autoRepeat: false }],
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

describe('workIntensity', () => {
    const shift = (id: number): ManufactureDto =>
        ({ id, finishDate: '2026-01-01T00:00:00.000Z', durationSeconds: 100, plodderCount: 1, receiptId: 1, autoRepeat: false });
    const domikType = (maxManufactureCount: number): DomikTypeDto => ({
        id: 1, name: 'Кузница', logicName: 'forge', maxCount: 1, availableCount: 0, maxLevel: 5, unlockLevel: 0,
        blueprintId: null, nextCountGateLevel: null,
        levels: [{ value: 1, resources: [], upgradeSeconds: 0, modificators: [], receiptIds: [], maxManufactureCount }],
    });
    const domik = (shifts: number): DomikDto => ({
        id: 1, typeId: 1, level: 1, finishDate: null, upgradeSeconds: null,
        manufactures: shifts === 0 ? null : Array.from({ length: shifts }, (_, index) => shift(index + 1)),
    });

    it.each<[string, number, number, WorkIntensity]>([
        ['idle building', 0, 3, 'normal'],
        ['one shift of three slots', 1, 3, 'slow'],
        ['two shifts of three slots', 2, 3, 'normal'],
        ['all slots taken', 3, 3, 'fast'],
        ['one of two slots', 1, 2, 'slow'],
        ['single-slot building at work', 1, 1, 'normal'],
        ['more shifts than slots', 2, 1, 'normal'],
    ])('%s', (_label, shifts, slots, expected) => {
        expect(workIntensity(domik(shifts), domikType(slots))).toBe(expected);
    });

    it('normal when the level is missing from the reference data', () => {
        expect(workIntensity({ ...domik(2), level: 4 }, domikType(2))).toBe('normal');
    });
});

describe('goldVeinView', () => {
    const goldReceipt: ReceiptDto = {
        id: 3, name: 'Надоблить золотишка', logicName: 'gold_dig', inputResources: [], optionalInputResources: [],
        durationSeconds: 3600, outputBonusPercent: 0, plodderCount: 1,
        outputResources: [{ typeId: 5, value: 1 }],
    };
    const oreReceipt: ReceiptDto = {
        ...goldReceipt, id: 59, name: 'Подобрать руду', logicName: 'ore_pick',
        outputResources: [{ typeId: 20, value: 2 }],
    };
    const mine = (manufactures: ManufactureDto[] = [], level = 3): DomikDto =>
        ({ id: 15, typeId: 4, level, finishDate: null, upgradeSeconds: null, manufactures });
    const shift = (receiptId: number, finishMs: number): ManufactureDto =>
        ({ id: 1, finishDate: new Date(finishMs).toISOString(), durationSeconds: 3600, plodderCount: 1, receiptId, autoRepeat: true, measureResourceTypeId: null, measureValue: null });
    const now = Date.UTC(2026, 7, 24, 18, 0, 0);
    const midnight = Date.UTC(2026, 7, 25, 0, 0, 0);

    const context = (goldMinedToday: number, domiks: DomikDto[] = [mine()], nowMs: number = now) =>
        ({ domiks, receipts: [goldReceipt, oreReceipt], goldMinedToday, now: nowMs });

    it('returns null for a receipt without gold output', () => {
        expect(goldVeinView(oreReceipt, mine(), context(3))).toBeNull();
    });

    it('shows mined progress without blocking while the vein has remainder', () => {
        expect(goldVeinView(goldReceipt, mine(), context(2)))
            .toEqual({ mined: 2, cap: 3, exhausted: false, hoursToFresh: 6, blockReason: null });
    });

    it('blocks with hours to fresh vein when the daily cap is mined out', () => {
        expect(goldVeinView(goldReceipt, mine(), context(3)))
            .toEqual({ mined: 3, cap: 3, exhausted: true, hoursToFresh: 6, blockReason: 'Жила на сегодня выбрана: 3 из 3 – новая через 6 ч' });
    });

    it('does not block a shift that finishes after UTC midnight', () => {
        const lateNow = midnight - 30 * 60 * 1000;
        expect(goldVeinView(goldReceipt, mine(), context(3, [mine()], lateNow))?.blockReason).toBeNull();
    });

    it('blocks when the remainder is already taken by a running gold shift finishing today', () => {
        const running = shift(goldReceipt.id, now + 30 * 60 * 1000);
        expect(goldVeinView(goldReceipt, mine([running]), context(2, [mine([running])]))?.blockReason)
            .toBe('Остаток жилы уже на вороте – его заберёт смена, что сейчас идёт');
    });

    it('ignores running shifts that finish after UTC midnight when counting the remainder', () => {
        const overnight = shift(goldReceipt.id, midnight + 30 * 60 * 1000);
        expect(goldVeinView(goldReceipt, mine([overnight]), context(2, [mine([overnight])]))?.blockReason).toBeNull();
    });

    it('counts a gold shift running in another mine of the same yard', () => {
        const other = { ...mine([shift(goldReceipt.id, now + 30 * 60 * 1000)]), id: 16 };
        expect(goldVeinView(goldReceipt, mine(), context(2, [mine(), other]))?.blockReason)
            .toBe('Остаток жилы уже на вороте – его заберёт смена, что сейчас идёт');
    });

    it('counts an overdue gold shift the planner has not finished yet', () => {
        const overdue = shift(goldReceipt.id, now - 30 * 60 * 1000);
        expect(goldVeinView(goldReceipt, mine([overdue]), context(2, [mine([overdue])]))?.blockReason)
            .toBe('Остаток жилы уже на вороте – его заберёт смена, что сейчас идёт');
    });
});

describe('weatherEffects', () => {
    const type = (id: number, name: string): DomikTypeDto => ({
        id, name, logicName: `type_${id}`, maxCount: 1, availableCount: 0, maxLevel: 5, unlockLevel: 0,
        blueprintId: null, nextCountGateLevel: null, levels: [],
    });
    const types = [type(1, 'Кузница'), type(2, 'Каменоломня'), type(3, 'Мельница')];

    it('drops neutral effects and unknown building types', () => {
        expect(weatherEffects(
            [{ domikTypeId: 1, outputPercent: 100 }, { domikTypeId: 9, outputPercent: 150 }, { domikTypeId: 2, outputPercent: 75 }],
            types,
        )).toEqual([{ delta: -25, domikType: types[1] }]);
    });

    it('sorts by effect strength, then by building id', () => {
        expect(weatherEffects(
            [{ domikTypeId: 3, outputPercent: 125 }, { domikTypeId: 1, outputPercent: 125 }, { domikTypeId: 2, outputPercent: 50 }],
            types,
        ).map(row => row.domikType.id)).toEqual([2, 1, 3]);
    });
});

describe('instaFinishCost', () => {
    it.each([
        { remaining: 3600, plodders: 1, cost: 1 },
        { remaining: 1200, plodders: 1, cost: 1 },
        { remaining: 3600, plodders: 5, cost: 5 },
        { remaining: 1800, plodders: 5, cost: 3 },
        { remaining: 900, plodders: 5, cost: 2 },
        { remaining: 21600, plodders: 5, cost: 30 },
        { remaining: 4320, plodders: 5, cost: 6 },
        { remaining: 21600, plodders: 1, cost: 6 },
    ])('charges $cost gold for $remaining s with $plodders plodders', ({ remaining, plodders, cost }) => {
        expect(instaFinishBlock(remaining)).toBeNull();
        expect(instaFinishCost(remaining, plodders)).toBe(cost);
    });

    it.each([
        { remaining: 600, block: 'tooSoon' },
        { remaining: 300, block: 'tooSoon' },
        { remaining: 60, block: 'tooSoon' },
        { remaining: 899, block: 'tooSoon' },
        { remaining: 21601, block: 'tooFar' },
    ])('refuses $remaining s as $block', ({ remaining, block }) => {
        expect(instaFinishBlock(remaining)).toBe(block);
    });
});

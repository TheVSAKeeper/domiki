import { describe, expect, it } from 'vitest';
import type { DomikDto, DomikTypeDto, ExpeditionStateDto, OrderDto, ReceiptDto, ResourceDto, ResourceTypeDto, WorkerDto } from '../types/api';
import { computeHudDigest, groupStockByDen, STOCK_DENS, type StockEntry } from './hud';

const NOW = Date.parse('2026-07-23T00:00:00.000Z');
const iso = (hoursFromNow: number) => new Date(NOW + hoursFromNow * 3600 * 1000).toISOString();

const forge: DomikTypeDto = {
    id: 10, name: 'Кузница', logicName: 'forge', maxCount: 3, availableCount: 0, maxLevel: 3, unlockLevel: 0,
    blueprintId: null, nextCountGateLevel: null,
    levels: [
        { value: 1, resources: [], modificators: [], receiptIds: [1], upgradeSeconds: 0, maxManufactureCount: 1 },
        { value: 2, resources: [{ typeId: 200, value: 5 }], modificators: [], receiptIds: [1], upgradeSeconds: 0, maxManufactureCount: 1 },
    ],
};
const well: DomikTypeDto = {
    id: 20, name: 'Колодец', logicName: 'well', maxCount: 1, availableCount: 0, maxLevel: 1, unlockLevel: 0,
    blueprintId: null, nextCountGateLevel: null,
    levels: [{ value: 1, resources: [], modificators: [], receiptIds: [], upgradeSeconds: 0, maxManufactureCount: 0 }],
};
const domikTypes = [forge, well];

const domik = (id: number, typeId: number, over: Partial<DomikDto> = {}): DomikDto => ({
    id, typeId, level: 1, finishDate: null, upgradeSeconds: null, manufactures: null, ...over,
});
const manufacture = { id: 1, finishDate: iso(2), durationSeconds: 10, plodderCount: 1, receiptId: 1, autoRepeat: false };

const receipt = (id: number, name: string, inputResources: ResourceDto[] = []): ReceiptDto => ({
    id, name, logicName: name, inputResources, optionalInputResources: [], durationSeconds: 100, outputBonusPercent: 0, outputResources: [], plodderCount: 1,
});

const order = (over: Partial<OrderDto>): OrderDto => ({
    id: 1, neighborId: 1, neighborName: 'Заречье', neighborLogicName: 'zarechye', expireDate: iso(1),
    required: [], rewardCoins: 0, rewardGold: 0, rewardReputation: 0, ...over,
});

const worker = (id: number, over: Partial<WorkerDto>): WorkerDto => ({
    id, name: `w${id}`, gender: 0, traitId: 0, traitName: '', traitLogicName: '', traitDurationPercent: 0,
    noFatigue: false, noSick: false, manufactureId: null, expeditionId: null, errandId: null, incidentId: null,
    workedSeconds: 0, restUntil: null, sickUntil: null, sickTypeId: null, isAway: false, skills: [], ...over,
});

const expeditionState = (active: ExpeditionStateDto['active']): ExpeditionStateDto => ({
    active, types: [], expeditionsSincePity: 0, pityThreshold: 5, maxActive: 2,
});
const expedition = (id: number, finishDate: string) => ({ id, expeditionTypeId: 1, expeditionName: 'Лес', startDate: iso(-4), finishDate });

const resType = (logicName: string, marketValue: number): ResourceTypeDto =>
    ({ id: 0, name: logicName, logicName, isFood: false, marketValue });
const entry = (logicName: string, value: number, marketValue = 10): StockEntry =>
    ({ type: resType(logicName, marketValue), value });

const digest = (
    domiks: DomikDto[],
    orders: OrderDto[] = [],
    expeditions: ExpeditionStateDto | null = null,
    workers: WorkerDto[] = [],
    receipts: ReceiptDto[] = [],
    resources: ResourceDto[] = [],
) => computeHudDigest(domiks, domikTypes, receipts, resources, orders, expeditions, workers, NOW);

describe('STOCK_DENS', () => {
    it('assigns every reference resource to exactly one den', () => {
        const allStockLogicNames = [
            'stone', 'wood', 'clay', 'brick', 'board', 'tool', 'furniture', 'block', 'millstone', 'dishes',
            'grain', 'flour', 'bread', 'ore', 'iron', 'wool', 'cloth', 'cloak', 'cheese',
        ];
        const placed = STOCK_DENS.flatMap(den => den.logicNames);

        expect(new Set(placed).size).toBe(placed.length);
        expect([...allStockLogicNames].sort()).toEqual([...placed].sort());
    });
});

describe('groupStockByDen', () => {
    it('keeps only non-empty dens in reference order and sorts each by market value', () => {
        const dens = groupStockByDen([entry('bread', 3, 20), entry('grain', 8, 10), entry('board', 2, 35), entry('wood', 5, 10)]);

        expect(dens.map(den => den.key)).toEqual(['forest', 'food']);
        expect(dens[0]?.items.map(item => item.type.logicName)).toEqual(['wood', 'board']);
        expect(dens[1]?.items.map(item => item.type.logicName)).toEqual(['grain', 'bread']);
    });

    it('drops unknown resources into a trailing Прочее den so nothing vanishes', () => {
        const dens = groupStockByDen([entry('amber', 1), entry('wood', 2)]);

        expect(dens.map(den => den.label)).toEqual(['Лесное', 'Прочее']);
        expect(dens[1]?.items.map(item => item.type.logicName)).toEqual(['amber']);
    });
});

describe('computeHudDigest idle domiks', () => {
    it('counts production-capable domiks that stand with no active manufacture', () => {
        const domiks = [domik(1, 10), domik(2, 10, { manufactures: [manufacture] }), domik(3, 10, { finishDate: iso(2) }), domik(4, 20)];

        expect(digest(domiks).idleDomiks).toBe(1);
        expect(digest(domiks).idleBuildings.map(building => building.domikId)).toEqual([1]);
    });
});

describe('computeHudDigest idle vs blocked buildings', () => {
    const recipe = receipt(1, 'Сковать инструмент', [{ typeId: 200, value: 4 }]);

    it('reports a plain idle row when the recipe is affordable', () => {
        const domiks = [domik(1, 10)];

        const result = digest(domiks, [], null, [], [recipe], [{ typeId: 200, value: 4 }]);

        expect(result.idleBuildings).toEqual([{ domikId: 1, typeId: 10, logicName: 'forge', displayName: 'Кузница', level: 1 }]);
        expect(result.blockedBuildings).toEqual([]);
    });

    it('reports a blocked row naming the shortfall when no recipe can run', () => {
        const domiks = [domik(1, 10)];

        const result = digest(domiks, [], null, [], [recipe], [{ typeId: 200, value: 1 }]);

        expect(result.idleBuildings).toEqual([]);
        expect(result.blockedBuildings).toEqual([{
            domikId: 1, typeId: 10, logicName: 'forge', displayName: 'Кузница', level: 1,
            missing: [{ typeId: 200, value: 3 }],
        }]);
    });

    it('picks the recipe closest to affordable among several', () => {
        const closeRecipe = receipt(2, 'Починить', [{ typeId: 200, value: 2 }]);
        const farRecipe = receipt(3, 'Отковать', [{ typeId: 200, value: 9 }]);
        const domiks = [domik(1, 10, { level: 2 })];
        const forgeLevel2 = { ...forge, levels: [{ value: 2, resources: [], modificators: [], receiptIds: [2, 3], upgradeSeconds: 0, maxManufactureCount: 1 }] };

        const result = computeHudDigest(domiks, [forgeLevel2, well], [closeRecipe, farRecipe], [{ typeId: 200, value: 0 }], [], null, [], NOW);

        expect(result.blockedBuildings).toEqual([{
            domikId: 1, typeId: 10, logicName: 'forge', displayName: 'Кузница', level: 2,
            missing: [{ typeId: 200, value: 2 }],
        }]);
    });
});

describe('computeHudDigest idle and upgradeable building order', () => {
    const sortableForge: DomikTypeDto = {
        id: 10, name: 'Кузница', logicName: 'forge', maxCount: 5, availableCount: 0, maxLevel: 3, unlockLevel: 0,
        blueprintId: null, nextCountGateLevel: null,
        levels: [1, 2, 3].map(value => ({ value, resources: [], modificators: [], receiptIds: [1], upgradeSeconds: 0, maxManufactureCount: 1 })),
    };

    it.each([
        { title: 'higher level sorts first regardless of id order', domiks: [domik(2, 10, { level: 1 }), domik(1, 10, { level: 3 })], expectedOrder: [1, 2] },
        { title: 'equal level falls back to ru name asc', domiks: [domik(1, 10, { level: 2 }), domik(2, 10, { level: 2 })], expectedOrder: [2, 1] },
    ])('sorts idle buildings – $title', ({ domiks, expectedOrder }) => {
        const result = computeHudDigest(domiks, [sortableForge, well], [], [], [], null, [], NOW);

        expect(result.idleBuildings.map(building => building.domikId)).toEqual(expectedOrder);
    });

    it.each([
        { title: 'higher level sorts first regardless of id order', domiks: [domik(2, 10, { level: 1 }), domik(1, 10, { level: 2 })], expectedOrder: [1, 2] },
        { title: 'equal level falls back to ru name asc', domiks: [domik(1, 10, { level: 1 }), domik(2, 10, { level: 1 })], expectedOrder: [2, 1] },
    ])('sorts upgradeable buildings – $title', ({ domiks, expectedOrder }) => {
        const result = computeHudDigest(domiks, [sortableForge, well], [], [], [], null, [], NOW);

        expect(result.upgradeableBuildings.map(building => building.domikId)).toEqual(expectedOrder);
    });
});

describe('computeHudDigest blocked building order', () => {
    const blockedType = (id: number, receiptId: number): DomikTypeDto => ({
        id, name: `Тип${id}`, logicName: 'forge', maxCount: 5, availableCount: 0, maxLevel: 1, unlockLevel: 0,
        blueprintId: null, nextCountGateLevel: null,
        levels: [{ value: 1, resources: [], modificators: [], receiptIds: [receiptId], upgradeSeconds: 0, maxManufactureCount: 1 }],
    });
    const shortfallTypes = [blockedType(101, 1), blockedType(102, 2), blockedType(103, 3)];
    const shortfallRecipes = [
        receipt(1, 'Большая нужда', [{ typeId: 200, value: 10 }]),
        receipt(2, 'Средняя нужда', [{ typeId: 200, value: 8 }]),
        receipt(3, 'Малая нужда', [{ typeId: 200, value: 5 }]),
    ];
    const domikById = (id: number, typeId: number) => domik(id, typeId, { level: 1 });

    it.each([
        { title: 'defined in id order', order: [1, 2, 3] },
        { title: 'defined in reverse order', order: [3, 2, 1] },
    ])('sorts blocked buildings by ascending total shortfall – $title', ({ order }) => {
        const typeById = new Map([[1, 101], [2, 102], [3, 103]]);
        const domiks = order.map(id => domikById(id, typeById.get(id) ?? 0));

        const result = computeHudDigest(domiks, shortfallTypes, shortfallRecipes, [{ typeId: 200, value: 4 }], [], null, [], NOW);

        expect(result.blockedBuildings.map(building => building.domikId)).toEqual([3, 2, 1]);
    });
});

describe('computeHudDigest upgradeable buildings', () => {
    it('lists buildings whose next level is affordable', () => {
        const domiks = [domik(1, 10, { level: 1 })];

        const result = digest(domiks, [], null, [], [], [{ typeId: 200, value: 5 }]);

        expect(result.upgradeableBuildings).toEqual([{ domikId: 1, typeId: 10, logicName: 'forge', displayName: 'Кузница', level: 1 }]);
    });

    it('stays empty when the next level is not affordable', () => {
        const domiks = [domik(1, 10, { level: 1 })];

        const result = digest(domiks, [], null, [], [], []);

        expect(result.upgradeableBuildings).toEqual([]);
    });
});

describe('computeHudDigest standing shifts', () => {
    it('extracts every manufacture with autoRepeat as a наряд row', () => {
        const recipe = receipt(1, 'Сковать инструмент');
        const domiks = [domik(1, 10, { manufactures: [{ ...manufacture, autoRepeat: true }] })];

        const result = digest(domiks, [], null, [], [recipe]);

        expect(result.standingShifts).toEqual([{
            manufactureId: 1, domikId: 1, domikLogicName: 'forge', domikName: 'Кузница', receiptId: 1, receiptName: 'Сковать инструмент', finishDate: manufacture.finishDate, starving: false,
            measure: null, inputTypeIds: [],
        }]);
    });

    it('ignores manufactures without a standing наряд', () => {
        const recipe = receipt(1, 'Сковать инструмент');
        const domiks = [domik(1, 10, { manufactures: [manufacture] })];

        expect(digest(domiks, [], null, [], [recipe]).standingShifts).toEqual([]);
    });

    it('calls a наряд starving when its inputs are short and nothing running makes them', () => {
        const recipe = receipt(1, 'Сковать инструмент', [{ typeId: 200, value: 5 }]);
        const domiks = [domik(1, 10, { manufactures: [{ ...manufacture, autoRepeat: true }] })];

        expect(digest(domiks, [], null, [], [recipe], [{ typeId: 200, value: 2 }]).standingShifts[0]?.starving).toBe(true);
    });

    it('stays calm about a short наряд while a running shift produces the missing input', () => {
        const smelting: ReceiptDto = { ...receipt(2, 'Выплавить железо'), outputResources: [{ typeId: 200, value: 3 }] };
        const domiks = [
            domik(1, 10, { manufactures: [{ ...manufacture, autoRepeat: true }] }),
            domik(2, 10, { manufactures: [{ ...manufacture, id: 2, receiptId: 2 }] }),
        ];
        const recipes = [receipt(1, 'Сковать инструмент', [{ typeId: 200, value: 5 }]), smelting];

        expect(digest(domiks, [], null, [], recipes, [{ typeId: 200, value: 2 }]).standingShifts[0]?.starving).toBe(false);
    });
});

describe('computeHudDigest hands and running shifts', () => {
    it('counts free hands and stays silent about the next free one while somebody is idle', () => {
        const workers = [worker(1, {}), worker(2, { manufactureId: 1 })];

        const result = digest([domik(1, 10, { manufactures: [manufacture] })], [], null, workers, [receipt(1, 'Сковать инструмент')]);

        expect(result.workersFree).toBe(1);
        expect(result.handsFreeEarliest).toBeNull();
    });

    it('leaves трудяга without a bed out of the free hands', () => {
        const workers = [worker(1, { isAway: true }), worker(2, { isAway: true })];

        const result = digest([domik(1, 10)], [], null, workers, [receipt(1, 'Сковать инструмент')]);

        expect(result.workersFree).toBe(0);
        expect(result.workersAway).toBe(2);
    });

    it('names the earliest release when every трудяга is taken', () => {
        const workers = [worker(1, { manufactureId: 1 }), worker(2, { restUntil: iso(5) })];
        const domiks = [domik(1, 10, { manufactures: [manufacture] })];

        const result = digest(domiks, [], expeditionState([expedition(1, iso(3))]), workers, [receipt(1, 'Сковать инструмент')]);

        expect(result.workersFree).toBe(0);
        expect(result.handsFreeEarliest).toBe(manufacture.finishDate);
    });

    it('sums running shifts across buildings and names the nearest finish', () => {
        const domiks = [
            domik(1, 10, { manufactures: [{ ...manufacture, finishDate: iso(4) }] }),
            domik(2, 10, { manufactures: [{ ...manufacture, id: 2, finishDate: iso(1) }] }),
        ];

        const result = digest(domiks, [], null, [], [receipt(1, 'Сковать инструмент')]);

        expect(result.runningShifts).toBe(2);
        expect(result.runningEarliest).toBe(iso(1));
    });
});

describe('computeHudDigest soonest order', () => {
    it('surfaces the nearest order only within the alert window and rounds hours up', () => {
        const orders = [order({ id: 1, expireDate: iso(2.2) }), order({ id: 2, neighborId: 2, neighborName: 'Боровое', neighborLogicName: 'borovoe', expireDate: iso(0.4) })];

        expect(digest([], orders).soonestOrder).toEqual({ neighborName: 'Боровое', neighborLogicName: 'borovoe', hours: 1 });
    });

    it('stays silent when the nearest order is beyond the alert window or already expired', () => {
        const orders = [order({ id: 1, expireDate: iso(9) }), order({ id: 2, expireDate: iso(-1) })];

        expect(digest([], orders).soonestOrder).toBeNull();
    });
});

describe('computeHudDigest expeditions and workers', () => {
    it('counts returned expeditions, not those still afield', () => {
        const state = expeditionState([expedition(1, iso(-0.1)), expedition(2, iso(3))]);

        expect(digest([], [], state).expeditionsBack).toBe(1);
    });

    it('splits sick from resting and reports the earliest of each', () => {
        const workers = [
            worker(1, { sickUntil: iso(4) }),
            worker(2, { restUntil: iso(2), sickUntil: iso(4) }),
            worker(3, { restUntil: iso(1) }),
            worker(4, { restUntil: iso(-1) }),
        ];

        const result = digest([], [], null, workers);

        expect(result.workersSick).toBe(2);
        expect(result.workersResting).toBe(1);
        expect(result.sickEarliest).toEqual(iso(4));
        expect(result.restingEarliest).toEqual(iso(1));
    });
});

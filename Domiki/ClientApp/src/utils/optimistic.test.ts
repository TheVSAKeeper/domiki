import { describe, expect, it } from 'vitest';
import type { DomikDto, DomikTypeDto, GameCommand, GameStateDto, QueuedIntent, ReceiptDto, WorkerDto } from '../types/api';
import { predictState } from './optimistic';

const NOW_MS = Date.UTC(2026, 8, 3, 12, 0, 0);

const intent = (command: GameCommand, queuedAtMs = NOW_MS): QueuedIntent => ({ command, queuedAtMs });

function first<T>(items: readonly T[] | null | undefined): T {
    const item = items?.[0];
    if (item === undefined) {
        throw new Error('Ожидался непустой список');
    }

    return item;
}

const receipt = (overrides: Partial<ReceiptDto> = {}): ReceiptDto => ({
    id: 1,
    name: 'Кирпич',
    logicName: 'brick',
    inputResources: [{ typeId: 10, value: 4 }],
    optionalInputResources: [{ typeId: 11, value: 2 }],
    durationSeconds: 600,
    outputBonusPercent: 20,
    outputResources: [{ typeId: 12, value: 1 }],
    plodderCount: 1,
    ...overrides,
});

const domikType = (overrides: Partial<DomikTypeDto> = {}): DomikTypeDto => ({
    id: 5,
    name: 'Кузница',
    logicName: 'forge',
    maxCount: 3,
    availableCount: 1,
    maxLevel: 3,
    unlockLevel: 0,
    blueprintId: null,
    nextCountGateLevel: null,
    levels: [
        { value: 1, resources: [{ typeId: 10, value: 20 }], upgradeSeconds: 300, modificators: [], receiptIds: [1], maxManufactureCount: 2 },
        { value: 2, resources: [{ typeId: 10, value: 50 }], upgradeSeconds: 900, modificators: [], receiptIds: [1], maxManufactureCount: 2 },
    ],
    ...overrides,
});

const domik = (overrides: Partial<DomikDto> = {}): DomikDto => ({
    id: 1,
    typeId: 5,
    level: 1,
    finishDate: null,
    upgradeSeconds: null,
    manufactures: [],
    ...overrides,
});

const worker = (id: number, overrides: Partial<WorkerDto> = {}): WorkerDto => ({
    id,
    name: `Трудяга ${String(id)}`,
    gender: 1,
    traitId: 1,
    traitName: 'Обычный',
    traitLogicName: 'plain',
    traitDurationPercent: 0,
    noFatigue: false,
    noSick: false,
    manufactureId: null,
    expeditionId: null,
    errandId: null,
    incidentId: null,
    workedSeconds: 0,
    restUntil: null,
    sickUntil: null,
    sickTypeId: null,
    isAway: false,
    skills: [],
    ...overrides,
});

const state = (overrides: Partial<GameStateDto> = {}): GameStateDto => ({
    domiks: [domik()],
    domikTypes: [domikType()],
    receipts: [receipt()],
    resources: [{ typeId: 10, value: 100 }, { typeId: 11, value: 100 }],
    blueprints: [],
    workers: [worker(1), worker(2)],
    villageLevel: { level: 1 },
    village: { profileNeighborId: null },
    villageProfiles: [],
    relocation: { perks: [] },
    goals: { zealCharges: 0 },
    purchaseAvailableDomiks: [domikType()],
    ...overrides,
} as unknown as GameStateDto);

describe('predictState', () => {
    it('списывает входы, занимает трудягу и рисует таймер смены', () => {
        const predicted = predictState(state(), [
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }),
        ]);

        const shift = first(first(predicted.state.domiks).manufactures);
        expect(predicted.state.resources).toEqual([{ typeId: 10, value: 96 }, { typeId: 11, value: 100 }]);
        expect(shift).toMatchObject({ receiptId: 1, durationSeconds: 600, plodderCount: 1, autoRepeat: false });
        expect(shift.finishDate).toBe(new Date(NOW_MS + 600_000).toISOString());
        expect(predicted.state.workers.map(item => item.manufactureId)).toEqual([shift.id, null]);
        expect(predicted.predictedManufactureIds).toEqual([shift.id]);
    });

    it('добавляет необязательные входы, когда игрок их выбрал', () => {
        const predicted = predictState(state(), [
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: true, autoRepeat: false, workerIds: [] } }),
        ]);

        expect(predicted.state.resources).toEqual([{ typeId: 10, value: 96 }, { typeId: 11, value: 98 }]);
    });

    it('вторая смена видит списание первой и берёт свободного трудягу', () => {
        const predicted = predictState(state(), [
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }),
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }),
        ]);

        expect(predicted.state.resources).toEqual([{ typeId: 10, value: 92 }, { typeId: 11, value: 100 }]);
        expect(first(predicted.state.domiks).manufactures).toHaveLength(2);
        expect(predicted.predictedManufactureIds).toHaveLength(2);
        expect(new Set(predicted.state.workers.map(item => item.manufactureId))).toEqual(new Set(predicted.predictedManufactureIds));
    });

    it('каждое намерение считает срок от своей минуты, а не от последней', () => {
        const predicted = predictState(state(), [
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }),
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }, NOW_MS + 5000),
        ]);

        const shifts = first(predicted.state.domiks).manufactures ?? [];
        expect(shifts.map(shift => shift.finishDate)).toEqual([
            new Date(NOW_MS + 600_000).toISOString(),
            new Date(NOW_MS + 5000 + 600_000).toISOString(),
        ]);
    });

    it('не рисует смену, на которую не хватает ресурсов', () => {
        const scarce = state({ resources: [{ typeId: 10, value: 3 }] });

        const predicted = predictState(scarce, [
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }),
        ]);

        expect(predicted.state).toBe(scarce);
        expect(predicted.predictedManufactureIds).toEqual([]);
    });

    it('не рисует смену сверх числа слотов уровня', () => {
        const busy = state({ domikTypes: [domikType({ levels: [{ value: 1, resources: [], upgradeSeconds: 300, modificators: [], receiptIds: [1], maxManufactureCount: 1 }] })] });

        const predicted = predictState(busy, [
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }),
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }),
        ]);

        expect(first(predicted.state.domiks).manufactures).toHaveLength(1);
    });

    it('умная артель отдаёт смену тому, кто справится быстрее', () => {
        const smart = state({
            villageLevel: { level: 8 } as GameStateDto['villageLevel'],
            workers: [worker(1), worker(2, { traitDurationPercent: -20 })],
        });

        const predicted = predictState(smart, [
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }),
        ]);

        expect(predicted.state.workers.find(item => item.manufactureId != null)?.id).toBe(2);
        expect(first(first(predicted.state.domiks).manufactures).durationSeconds).toBe(480);
    });

    it('до умной артели смена уходит первому по счёту', () => {
        const plain = state({ workers: [worker(1), worker(2, { traitDurationPercent: -20 })] });

        const predicted = predictState(plain, [
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }),
        ]);

        expect(predicted.state.workers.find(item => item.manufactureId != null)?.id).toBe(1);
        expect(first(first(predicted.state.domiks).manufactures).durationSeconds).toBe(600);
    });

    it('рвение сокращает смену и списывает заряд', () => {
        const zealous = state({ goals: { zealCharges: 17 } as GameStateDto['goals'] });

        const predicted = predictState(zealous, [
            intent({ kind: 'StartManufacture', args: { domikId: 1, receiptId: 1, useOptional: false, autoRepeat: false, workerIds: [] } }),
        ]);

        expect(first(first(predicted.state.domiks).manufactures).durationSeconds).toBe(150);
        expect(predicted.state.goals.zealCharges).toBe(16);
    });

    it('улучшение списывает цену уровня и ставит срок', () => {
        const predicted = predictState(state(), [intent({ kind: 'UpgradeDomik', args: { domikId: 1 } })]);

        expect(first(predicted.state.resources).value).toBe(50);
        expect(first(predicted.state.domiks)).toMatchObject({ upgradeSeconds: 900, finishDate: new Date(NOW_MS + 900_000).toISOString() });
        expect(predicted.predictedDomikIds).toEqual([1]);
    });

    it('покупка ставит во дворе стройку со своим сроком', () => {
        const predicted = predictState(state(), [intent({ kind: 'BuyDomik', args: { typeId: 5 } })]);

        expect(first(predicted.state.resources).value).toBe(80);
        expect(predicted.state.domiks).toHaveLength(2);
        expect(predicted.state.domiks.at(-1)).toMatchObject({ id: 2, typeId: 5, level: 0, upgradeSeconds: 300, finishDate: new Date(NOW_MS + 300_000).toISOString() });
    });

    it('новая постройка занимает первый свободный номер двора, как на сервере', () => {
        const gapped = state({ domiks: [domik({ id: 1 }), domik({ id: 3 })] });

        const predicted = predictState(gapped, [intent({ kind: 'BuyDomik', args: { typeId: 5 } })]);

        expect(predicted.state.domiks.at(-1)?.id).toBe(2);
    });

    it('наряд снимает свою меру вместе с собой', () => {
        const running = state({
            domiks: [domik({ manufactures: [{ id: 7, finishDate: '2026-09-03T13:00:00.000Z', durationSeconds: 600, plodderCount: 1, receiptId: 1, autoRepeat: true, measureResourceTypeId: 10, measureValue: 50 }] })],
        });

        const predicted = predictState(running, [intent({ kind: 'SetManufactureAutoRepeat', args: { manufactureId: 7, autoRepeat: false } })]);

        expect(first(first(predicted.state.domiks).manufactures)).toMatchObject({ autoRepeat: false, measureResourceTypeId: null, measureValue: null });
    });

    it('действия с серверным броском не рисуются, а ждут связи', () => {
        const base = state();

        const predicted = predictState(base, [
            intent({ kind: 'HurryManufacture', args: { manufactureId: 7 } }),
            intent({ kind: 'CompleteOrder', args: { orderId: 3 } }),
        ]);

        expect(predicted.state).toBe(base);
        expect(predicted.waitingManufactureIds).toEqual([7]);
        expect(predicted.waitingOrderIds).toEqual([3]);
    });
});

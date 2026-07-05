import { describe, expect, it } from 'vitest';
import type { DomikDto, DomikTypeDto, ManufactureDto, ReceiptDto } from '../types/api';
import { computePlodderCount, manufactureProgressPercent } from './game';

const domikTypes: DomikTypeDto[] = [
    {
        id: 1,
        name: 'Рынок',
        logicName: 'market',
        maxCount: 1,
        availableCount: 0,
        maxLevel: 2,
        levels: [
            { value: 1, resources: [], modificators: [{ typeId: 1, value: 3 }], receiptIds: [] },
            { value: 2, resources: [], modificators: [{ typeId: 1, value: 5 }], receiptIds: [] },
        ],
    },
];

describe('computePlodderCount', () => {
    it('sums modificator values for domiks with a level and subtracts working manufactures', () => {
        const domiks: DomikDto[] = [
            { id: 1, typeId: 1, level: 1, finishDate: null, manufactures: null },
            { id: 2, typeId: 1, level: 2, finishDate: null, manufactures: [{ id: 1, finishDate: '2026-01-01T00:00:00.000Z', plodderCount: 2, receiptId: 1 }] },
        ];

        expect(computePlodderCount(domiks, domikTypes)).toEqual({ max: 8, free: 6 });
    });

    it('ignores domiks at level 0 and domiks with no matching level', () => {
        const domiks: DomikDto[] = [
            { id: 1, typeId: 1, level: 0, finishDate: null, manufactures: null },
            { id: 2, typeId: 1, level: 99, finishDate: null, manufactures: null },
        ];

        expect(computePlodderCount(domiks, domikTypes)).toEqual({ max: 0, free: 0 });
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
        const manufacture: ManufactureDto = { id: 1, finishDate: new Date(secondsFromNow * 1000).toISOString(), plodderCount: 1, receiptId: 1 };
        expect(manufactureProgressPercent(manufacture, receipt, now)).toBe(expected);
    });
});

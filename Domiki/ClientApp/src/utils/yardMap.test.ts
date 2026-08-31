import { describe, expect, it } from 'vitest';
import { layoutYard, yardHeight, yardRowCount } from './yardMap';
import type { DomikDto, PlayerDecorDto } from '../types/api';

const domik = (id: number): DomikDto => ({
    id,
    typeId: 1 + (id % 5),
    level: 1 + (id % 4),
    finishDate: null,
    upgradeSeconds: null,
    manufactures: null,
});

const decor = (decorTypeId: number, count: number): PlayerDecorDto => ({ decorTypeId, count });

const yard = (domikCount: number, owned: PlayerDecorDto[], level = 30) =>
    layoutYard(Array.from({ length: domikCount }, (_, index) => domik(index + 1)), owned, level);

describe('yardMap', () => {
    it('двор растёт рядами, а не шириной', () => {
        expect(yardRowCount(1)).toBe(1);
        expect(yardRowCount(8)).toBe(1);
        expect(yardRowCount(9)).toBe(2);
        expect(yardRowCount(24)).toBe(3);

        const small = yard(3, []);
        const big = yard(24, []);
        expect(big.width).toBe(small.width);
        expect(big.height).toBe(yardHeight(3));
        expect(big.height).toBeGreaterThan(small.height);
    });

    it('постройки не наезжают друг на друга', () => {
        const layout = yard(24, []);
        expect(layout.spots).toHaveLength(24);
        for (const spot of layout.spots) {
            for (const other of layout.spots) {
                if (other.domik.id === spot.domik.id) {
                    continue;
                }
                expect(Math.hypot(spot.x - other.x, spot.y - other.y)).toBeGreaterThan(56);
            }
        }
    });

    it('декор не садится на декор и не на постройки', () => {
        const layout = yard(12, [decor(1, 4), decor(2, 3), decor(3, 5)]);
        expect(layout.decors).toHaveLength(12);
        for (const [index, item] of layout.decors.entries()) {
            for (const other of layout.decors.slice(index + 1)) {
                expect(Math.hypot(item.x - other.x, item.y - other.y)).toBeGreaterThanOrEqual(32);
            }
            for (const spot of layout.spots) {
                expect(Math.hypot(item.x - spot.x, item.y - spot.y)).toBeGreaterThanOrEqual(48);
            }
        }
    });

    it('декор не теряется, пока есть место', () => {
        const layout = yard(8, [decor(1, 4), decor(2, 3)]);
        expect(layout.decors).toHaveLength(7);
    });

    it('переполненный двор режет декор по капу, но без наложений', () => {
        const layout = yard(8, [decor(1, 20), decor(2, 20)]);
        expect(layout.decors).toHaveLength(16);
        for (const [index, item] of layout.decors.entries()) {
            for (const other of layout.decors.slice(index + 1)) {
                expect(Math.hypot(item.x - other.x, item.y - other.y)).toBeGreaterThanOrEqual(32);
            }
        }
    });

    it('плотный двор не даёт наложений даже при полном капе', () => {
        const counts = [28, 18, 17, 5, 10, 0, 4, 18, 2, 0, 1, 1, 1];
        const layout = yard(28, counts.map((count, index) => decor(index + 1, count)));
        for (const [index, item] of layout.decors.entries()) {
            for (const other of layout.decors.slice(index + 1)) {
                expect(Math.hypot(item.x - other.x, item.y - other.y)).toBeGreaterThanOrEqual(32);
            }
        }
    });

    it('раскладка не зависит от порядка входа', () => {
        const owned = [decor(2, 2), decor(1, 3)];
        const direct = layoutYard([domik(1), domik(2), domik(3)], owned, 12);
        const shuffled = layoutYard([domik(3), domik(1), domik(2)], [...owned].reverse(), 12);
        expect(shuffled.spots.map(spot => [spot.domik.id, spot.x, spot.y]))
            .toEqual(direct.spots.map(spot => [spot.domik.id, spot.x, spot.y]));
        expect(shuffled.decors).toEqual(direct.decors);
    });
});

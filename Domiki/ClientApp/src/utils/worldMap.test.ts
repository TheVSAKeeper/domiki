import { describe, expect, it } from 'vitest';
import { WORLD_H, WORLD_W, buildRiver, buildRoads, layoutVillages, riverXAt, scatterTrees, villageTier } from './worldMap';
import type { WorldVillageDto } from '../types/api';

const village = (name: string, level: number, overrides?: Partial<WorldVillageDto>): WorldVillageDto => ({
    playerId: null,
    villageName: name,
    crestIcon: 0,
    crestColor: 0,
    level,
    isNpc: false,
    isMe: false,
    npcResourceTypeId: null,
    npcLogicName: null,
    seasonOrders: 0,
    seasonToloka: 0,
    seasonExpeditions: 0,
    comfort: 0,
    ...overrides,
});

describe('worldMap layout', () => {
    const villages = Array.from({ length: 60 }, (_, i) => village(`Деревня ${i}`, i * 3, { playerId: i + 1 }));
    const river = buildRiver();

    it('детерминирован и не зависит от порядка входа', () => {
        const direct = layoutVillages(villages, river);
        const reversed = layoutVillages([...villages].reverse(), river);
        const byKey = new Map(reversed.map(spot => [spot.key, spot]));
        for (const spot of direct) {
            const twin = byKey.get(spot.key);
            expect(twin, spot.key).toBeDefined();
            expect({ x: twin?.x, y: twin?.y }).toEqual({ x: spot.x, y: spot.y });
        }
    });

    it('деревни в границах мира и не в реке', () => {
        for (const spot of layoutVillages(villages, river)) {
            expect(spot.x).toBeGreaterThanOrEqual(0);
            expect(spot.x).toBeLessThanOrEqual(WORLD_W);
            expect(spot.y).toBeGreaterThanOrEqual(0);
            expect(spot.y).toBeLessThanOrEqual(WORLD_H);
            expect(Math.abs(spot.x - riverXAt(river, spot.y))).toBeGreaterThanOrEqual(spot.clearing + 56);
        }
    });

    it('деревья не залезают на поляны и реку', () => {
        const spots = layoutVillages(villages, river);
        const trees = scatterTrees(spots, river);
        expect(trees.length).toBeGreaterThan(40);
        for (const tree of trees) {
            expect(Math.abs(tree.x - riverXAt(river, tree.y))).toBeGreaterThanOrEqual(56);
            for (const spot of spots) {
                expect(Math.hypot(tree.x - spot.x, tree.y - spot.y)).toBeGreaterThanOrEqual(spot.clearing + 60);
            }
        }
    });

    it('дороги связывают главные поселения детерминированно', () => {
        const spots = layoutVillages(villages, river);
        const roads = buildRoads(spots, river);
        expect(roads.length).toBeGreaterThan(10);
        expect(roads).toEqual(buildRoads(spots, river));
        expect(roads.every(road => !road.points.includes('undefined'))).toBe(true);
    });
});

describe('villageTier', () => {
    it.each([
        [0, 0],
        [9, 0],
        [10, 1],
        [24, 1],
        [25, 2],
        [44, 2],
        [45, 3],
        [69, 3],
        [70, 4],
        [500, 4],
    ])('обжитость %i – ярус %i', (level, tier) => {
        expect(villageTier(level)).toBe(tier);
    });
});

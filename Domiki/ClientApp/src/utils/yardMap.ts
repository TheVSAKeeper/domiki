import { hashString, mulberry32, villageTier } from './worldMap';
import type { DomikDto, PlayerDecorDto } from '../types/api';

export const YARD_H = 320;

const MARGIN = 110;
const SPACING = 140;
const PATH_SEED = 0x9a4d01;
const TREE_SEED = 0x7ee5;
const TUFT_SEED = 0x70f7;
const PATH_STEP = 64;
const DECOR_CAP = 24;
const DECOR_ATTEMPTS = 20;

const snap = (v: number) => Math.round(v / 4) * 4;

export interface YardSpot { domik: DomikDto; x: number; y: number; }
export interface YardDecor { key: string; decorTypeId: number; x: number; y: number; }
export interface YardGreen { x: number; y: number; kind: number; scale: number; }
export interface YardLayout { width: number; path: string; spots: YardSpot[]; decors: YardDecor[]; trees: YardGreen[]; tufts: YardGreen[]; }

interface PathPoint { x: number; y: number; }

const buildPath = (width: number): PathPoint[] => {
    const rng = mulberry32(PATH_SEED);
    const points: PathPoint[] = [];
    let y = YARD_H / 2;
    for (let x = -PATH_STEP; x <= width + PATH_STEP; x += PATH_STEP) {
        y += (rng() - 0.5) * 44;
        y = Math.min(YARD_H / 2 + 44, Math.max(YARD_H / 2 - 44, y));
        points.push({ x, y: snap(y) });
    }
    return points;
};

const pathYAt = (points: PathPoint[], x: number) => {
    const index = Math.min(points.length - 1, Math.max(0, Math.round((x + PATH_STEP) / PATH_STEP)));
    return points[index]?.y ?? YARD_H / 2;
};

const buildSpots = (domiks: DomikDto[], points: PathPoint[]): YardSpot[] =>
    [...domiks].sort((a, b) => a.id - b.id).map((domik, i) => {
        const rng = mulberry32(hashString('h' + String(domik.id)));
        const x = snap(MARGIN + i * SPACING + (rng() - 0.5) * 40);
        const side = i % 2 === 0 ? -1 : 1;
        let y = snap(pathYAt(points, x) + side * (64 + rng() * 20));
        y = Math.min(YARD_H - 40, Math.max(56, y));
        return { domik, x, y };
    });

const buildDecorInstances = (owned: PlayerDecorDto[]) => {
    const instances: { decorTypeId: number; i: number }[] = [];
    const maxCount = owned.reduce((max, item) => Math.max(max, item.count), 0);
    for (let round = 0; round < maxCount && instances.length < DECOR_CAP; round++) {
        for (const item of owned) {
            if (round < item.count) {
                instances.push({ decorTypeId: item.decorTypeId, i: round });
                if (instances.length >= DECOR_CAP) {
                    break;
                }
            }
        }
    }
    return instances;
};

const buildDecors = (owned: PlayerDecorDto[], points: PathPoint[], width: number, spots: YardSpot[]): YardDecor[] => {
    const decors: YardDecor[] = [];
    for (const instance of buildDecorInstances(owned)) {
        const rng = mulberry32(hashString('d' + String(instance.decorTypeId) + ':' + String(instance.i)));
        for (let attempt = 0; attempt < DECOR_ATTEMPTS; attempt++) {
            const x = snap(MARGIN * 0.5 + rng() * (width - MARGIN));
            let y = snap(pathYAt(points, x) + (rng() < 0.5 ? -1 : 1) * (36 + rng() * 60));
            y = Math.min(YARD_H - 32, Math.max(48, y));
            if (spots.some(spot => (x - spot.x) ** 2 + (y - spot.y) ** 2 < 72 ** 2)) {
                continue;
            }
            decors.push({ key: `${instance.decorTypeId}:${instance.i}`, decorTypeId: instance.decorTypeId, x, y });
            break;
        }
    }
    return decors;
};

const scatterGreens = (seed: number, count: number, width: number, points: PathPoint[], spots: YardSpot[], decors: YardDecor[], pathGuard: number, spotGuard: number, decorGuard: number | null, kindCount: number): YardGreen[] => {
    const rng = mulberry32(seed);
    const greens: YardGreen[] = [];
    for (let attempt = 0; attempt < count * 30 && greens.length < count; attempt++) {
        const x = snap(rng() * width);
        const y = snap(rng() * YARD_H);
        if (Math.abs(y - pathYAt(points, x)) <= pathGuard) {
            continue;
        }
        if (spots.some(spot => (x - spot.x) ** 2 + (y - spot.y) ** 2 < spotGuard ** 2)) {
            continue;
        }
        if (decorGuard != null && decors.some(decor => (x - decor.x) ** 2 + (y - decor.y) ** 2 < decorGuard ** 2)) {
            continue;
        }
        const kind = Math.floor(rng() * kindCount);
        const scale = kindCount === 3 ? 0.7 + rng() * 0.5 : 1;
        greens.push({ x, y, kind, scale });
    }
    return greens;
};

export const layoutYard = (domiks: DomikDto[], owned: PlayerDecorDto[], level: number): YardLayout => {
    const width = Math.max(1040, MARGIN * 2 + Math.max(0, domiks.length - 1) * SPACING);
    const points = buildPath(width);
    const spots = buildSpots(domiks, points);
    const decors = buildDecors(owned, points, width, spots);
    const tier = villageTier(level);
    const trees = scatterGreens(TREE_SEED, 8 + tier * 5, width, points, spots, decors, 52, 80, 40, 3);
    const tufts = scatterGreens(TUFT_SEED, 18 + tier * 8, width, points, spots, decors, 20, 56, null, 2);
    const path = points.map(point => `${point.x},${point.y}`).join(' ');
    return { width, path, spots, decors, trees, tufts };
};

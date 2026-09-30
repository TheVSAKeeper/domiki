import { hashString, mulberry32, villageTier } from './worldMap';
import type { DomikDto, PlayerDecorDto } from '../types/api';

export const YARD_W = 1040;
export const YARD_ROW_H = 150;
export const YARD_BASE_H = 320;

const MARGIN = 72;
const FIRST_ROW_Y = 150;
const PER_ROW = 8;
const PATH_SEED = 0x9a4d01;
const TREE_SEED = 0x7ee5;
const TUFT_SEED = 0x70f7;
const PATH_STEP = 64;
const DECOR_PER_ROW = 16;
const DECOR_ATTEMPTS = 20;
const DECOR_GUARD = 36;
const SPOT_GUARD = 72;
const SPOT_LIFT = 26;

const snap = (v: number) => Math.round(v / 4) * 4;

export interface YardSpot { domik: DomikDto; x: number; y: number; row: number; }
export interface YardDecor { key: string; decorTypeId: number; x: number; y: number; }
export interface YardGreen { x: number; y: number; kind: number; scale: number; }
export interface YardLink { x: number; y1: number; y2: number; }
export interface YardLayout {
    width: number;
    height: number;
    rows: number;
    paths: string[];
    links: YardLink[];
    spots: YardSpot[];
    decors: YardDecor[];
    trees: YardGreen[];
    tufts: YardGreen[];
}

interface PathPoint { x: number; y: number; }

export const yardRowCount = (count: number) => Math.max(1, Math.ceil(count / PER_ROW));

export const yardHeight = (rows: number) => YARD_BASE_H + (rows - 1) * YARD_ROW_H;

export const yardRowBaseY = (row: number) => FIRST_ROW_Y + row * YARD_ROW_H;

const buildRowPath = (row: number, width: number): PathPoint[] => {
    const rng = mulberry32(PATH_SEED + row * 7919);
    const base = yardRowBaseY(row);
    const points: PathPoint[] = [];
    let y = base;
    for (let x = -PATH_STEP; x <= width + PATH_STEP; x += PATH_STEP) {
        y += (rng() - 0.5) * 30;
        y = Math.min(base + 20, Math.max(base - 20, y));
        points.push({ x, y: snap(y) });
    }
    return points;
};

const pathYAt = (points: PathPoint[], x: number) => {
    const index = Math.min(points.length - 1, Math.max(0, Math.round((x + PATH_STEP) / PATH_STEP)));
    return points[index]?.y ?? FIRST_ROW_Y;
};

const buildLinks = (rows: PathPoint[][], width: number): YardLink[] => {
    const links: YardLink[] = [];
    for (let row = 1; row < rows.length; row++) {
        const upper = rows[row - 1];
        const lower = rows[row];
        if (upper == null || lower == null) {
            continue;
        }
        const rng = mulberry32(PATH_SEED + row * 104729);
        const x = snap(MARGIN + rng() * (width - MARGIN * 2));
        links.push({ x, y1: pathYAt(upper, x), y2: pathYAt(lower, x) });
    }
    return links;
};

const rowSizes = (count: number, rows: number) => {
    const sizes: number[] = [];
    let left = count;
    for (let row = 0; row < rows; row++) {
        const size = Math.ceil(left / (rows - row));
        sizes.push(size);
        left -= size;
    }
    return sizes;
};

const buildSpots = (domiks: DomikDto[], rows: PathPoint[][], width: number): YardSpot[] => {
    const ordered = [...domiks].sort((a, b) => a.id - b.id);
    const sizes = rowSizes(ordered.length, rows.length);
    const spots: YardSpot[] = [];
    let index = 0;
    for (let row = 0; row < rows.length; row++) {
        const size = sizes[row] ?? 0;
        const points = rows[row];
        const step = size > 1 ? (width - MARGIN * 2) / (size - 1) : 0;
        for (let col = 0; col < size; col++) {
            const domik = ordered[index];
            index++;
            if (domik == null || points == null) {
                continue;
            }
            const rng = mulberry32(hashString('h' + String(domik.id)));
            const x = snap(size > 1 ? MARGIN + col * step + (rng() - 0.5) * 28 : width / 2);
            const y = snap(pathYAt(points, x) - SPOT_LIFT + (rng() - 0.5) * 12);
            spots.push({ domik, x, y, row });
        }
    }
    return spots;
};

const buildDecorInstances = (owned: PlayerDecorDto[], cap: number) => {
    const instances: { decorTypeId: number; i: number }[] = [];
    const ordered = [...owned].sort((a, b) => a.decorTypeId - b.decorTypeId);
    const maxCount = ordered.reduce((max, item) => Math.max(max, item.count), 0);
    for (let round = 0; round < maxCount && instances.length < cap; round++) {
        for (const item of ordered) {
            if (round < item.count) {
                instances.push({ decorTypeId: item.decorTypeId, i: round });
                if (instances.length >= cap) {
                    break;
                }
            }
        }
    }
    return instances;
};

const clearance = (x: number, y: number, spots: YardSpot[], decors: YardDecor[]) => {
    let worst = Infinity;
    for (const spot of spots) {
        worst = Math.min(worst, Math.hypot(x - spot.x, y - spot.y) - SPOT_GUARD);
    }
    for (const decor of decors) {
        worst = Math.min(worst, Math.hypot(x - decor.x, y - decor.y) - DECOR_GUARD);
    }
    return worst;
};

const gridSpot = (rows: PathPoint[][], width: number, height: number, spots: YardSpot[], decors: YardDecor[]) => {
    for (const points of rows) {
        for (let x = snap(MARGIN * 0.5); x <= width - MARGIN * 0.5; x += DECOR_GUARD) {
            const base = pathYAt(points, x);
            for (let dy = 30; dy <= 90; dy += DECOR_GUARD) {
                const y = snap(Math.min(height - 32, Math.max(48, base + dy)));
                if (clearance(snap(x), y, spots, decors) >= 0) {
                    return { x: snap(x), y };
                }
            }
        }
    }
    return null;
};

const buildDecors = (owned: PlayerDecorDto[], rows: PathPoint[][], width: number, height: number, spots: YardSpot[]): YardDecor[] => {
    const decors: YardDecor[] = [];
    for (const instance of buildDecorInstances(owned, rows.length * DECOR_PER_ROW)) {
        const rng = mulberry32(hashString('d' + String(instance.decorTypeId) + ':' + String(instance.i)));
        const key = `${instance.decorTypeId}:${instance.i}`;
        let placed: { x: number; y: number } | null = null;
        for (let attempt = 0; attempt < DECOR_ATTEMPTS && placed == null; attempt++) {
            const row = Math.min(rows.length - 1, Math.floor(rng() * rows.length));
            const points = rows[row];
            if (points == null) {
                continue;
            }
            const x = snap(MARGIN * 0.5 + rng() * (width - MARGIN));
            const y = snap(Math.min(height - 32, Math.max(48, pathYAt(points, x) + 30 + rng() * 60)));
            if (clearance(x, y, spots, decors) >= 0) {
                placed = { x, y };
            }
        }
        placed ??= gridSpot(rows, width, height, spots, decors);
        if (placed != null) {
            decors.push({ key, decorTypeId: instance.decorTypeId, x: placed.x, y: placed.y });
        }
    }
    return decors;
};

const nearestPathDistance = (rows: PathPoint[][], x: number, y: number) => {
    let best = Infinity;
    for (const points of rows) {
        best = Math.min(best, Math.abs(y - pathYAt(points, x)));
    }
    return best;
};

const scatterGreens = (seed: number, count: number, width: number, height: number, rows: PathPoint[][], spots: YardSpot[], decors: YardDecor[], pathGuard: number, spotGuard: number, decorGuard: number | null, kindCount: number): YardGreen[] => {
    const rng = mulberry32(seed);
    const greens: YardGreen[] = [];
    const used = new Set<string>();
    for (let attempt = 0; attempt < count * 30 && greens.length < count; attempt++) {
        const x = snap(rng() * width);
        const y = snap(rng() * height);
        const key = `${x}:${y}`;
        if (used.has(key)) {
            continue;
        }
        if (nearestPathDistance(rows, x, y) <= pathGuard) {
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
        used.add(key);
        greens.push({ x, y, kind, scale });
    }
    return greens;
};

const placeYard = (domiks: DomikDto[], owned: PlayerDecorDto[]) => {
    const width = YARD_W;
    const rowCount = yardRowCount(domiks.length);
    const height = yardHeight(rowCount);
    const rows = Array.from({ length: rowCount }, (_, row) => buildRowPath(row, width));
    const spots = buildSpots(domiks, rows, width);
    const decors = buildDecors(owned, rows, width, height, spots);
    return { width, rowCount, height, rows, spots, decors };
};

export interface YardDecorFill { placed: number; total: number; capacity: number; }

export const yardDecorFill = (domiks: DomikDto[], owned: PlayerDecorDto[]): YardDecorFill => ({
    placed: placeYard(domiks, owned).decors.length,
    total: owned.reduce((sum, item) => sum + item.count, 0),
    capacity: yardRowCount(domiks.length) * DECOR_PER_ROW,
});

export const layoutYard = (domiks: DomikDto[], owned: PlayerDecorDto[], level: number): YardLayout => {
    const { width, rowCount, height, rows, spots, decors } = placeYard(domiks, owned);
    const tier = villageTier(level);
    const greenScale = rowCount;
    const trees = scatterGreens(TREE_SEED, (8 + tier * 5) * greenScale, width, height, rows, spots, decors, 52, 80, 40, 3);
    const tufts = scatterGreens(TUFT_SEED, (18 + tier * 8) * greenScale, width, height, rows, spots, decors, 20, 56, null, 2);
    const paths = rows.map(points => points.map(point => `${point.x},${point.y}`).join(' '));
    return { width, height, rows: rowCount, paths, links: buildLinks(rows, width), spots, decors, trees, tufts };
};

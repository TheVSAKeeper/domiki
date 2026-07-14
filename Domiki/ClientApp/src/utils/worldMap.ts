import type { WorldVillageDto } from '../types/api';

export const WORLD_W = 2600;
export const WORLD_H = 1800;

const GOLDEN_ANGLE = Math.PI * (3 - Math.sqrt(5));
const RIVER_STEP = 48;
const RIVER_WIDTH = 40;
const SPIRAL_SPACING = 118;
const SPIRAL_CX = WORLD_W * 0.46;
const SPIRAL_CY = WORLD_H * 0.52;
const RIVER_SEED = 0xd0e11a;
const FOREST_SEED = 0x51e5ed;
const TUFT_SEED = 0x7a57ef;

export const TIER_THRESHOLDS = [10, 25, 45, 70] as const;
export const TIER_CLEARING = [40, 52, 62, 74, 88] as const;

export interface MapSpot {
    village: WorldVillageDto;
    key: string;
    x: number;
    y: number;
    tier: number;
    clearing: number;
    seed: number;
}

export interface MapTree {
    x: number;
    y: number;
    scale: number;
    kind: number;
}

export interface MapTuft {
    x: number;
    y: number;
    wide: boolean;
}

export interface RiverSegment {
    x: number;
    y: number;
    w: number;
    h: number;
}

export interface MapRoad {
    key: string;
    points: string;
}

export const villageKey = (village: WorldVillageDto) =>
    village.playerId != null ? `p${village.playerId}` : `npc:${village.villageName}`;

export const hashString = (value: string) => {
    let hash = 2166136261;
    for (let i = 0; i < value.length; i++) {
        hash ^= value.charCodeAt(i);
        hash = Math.imul(hash, 16777619);
    }
    return hash >>> 0;
};

export const mulberry32 = (seed: number) => {
    let state = seed >>> 0;
    return () => {
        state = (state + 0x6d2b79f5) | 0;
        let t = Math.imul(state ^ (state >>> 15), 1 | state);
        t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
};

export const villageTier = (level: number) => TIER_THRESHOLDS.filter(threshold => level >= threshold).length;

const snap = (value: number) => Math.round(value / 4) * 4;

export const buildRiver = () => {
    const rng = mulberry32(RIVER_SEED);
    const segments: RiverSegment[] = [];
    let x = WORLD_W * 0.64;
    for (let y = -RIVER_STEP; y <= WORLD_H + RIVER_STEP; y += RIVER_STEP) {
        x += (rng() - 0.5) * 64;
        x = Math.min(WORLD_W * 0.8, Math.max(WORLD_W * 0.54, x));
        segments.push({ x: snap(x - RIVER_WIDTH / 2), y, w: RIVER_WIDTH, h: RIVER_STEP });
    }
    return segments;
};

export const riverXAt = (river: RiverSegment[], y: number) => {
    const index = Math.min(river.length - 1, Math.max(0, Math.round((y + RIVER_STEP) / RIVER_STEP)));
    const segment = river[index];
    return segment == null ? WORLD_W * 0.64 : segment.x + segment.w / 2;
};

export const layoutVillages = (villages: WorldVillageDto[], river: RiverSegment[]): MapSpot[] => {
    const ordered = [...villages].sort((a, b) => b.level - a.level || a.villageName.localeCompare(b.villageName, 'ru'));
    return ordered.map((village, index) => {
        const seed = hashString(villageKey(village));
        const jitter = ((seed % 997) / 997 - 0.5) * 0.6;
        const radius = SPIRAL_SPACING * Math.sqrt(index + 0.72);
        const angle = index * GOLDEN_ANGLE + jitter;
        const tier = villageTier(village.level);
        const clearing = TIER_CLEARING[tier] ?? 40;
        const margin = clearing + 40;
        let x = SPIRAL_CX + radius * Math.cos(angle);
        let y = SPIRAL_CY + radius * Math.sin(angle) * 0.7;
        x = snap(Math.min(WORLD_W - margin, Math.max(margin, x)));
        y = snap(Math.min(WORLD_H - margin - 24, Math.max(margin + 24, y)));
        const riverX = riverXAt(river, y);
        const offset = x - riverX;
        const guard = clearing + 64;
        if (Math.abs(offset) < guard) {
            x = snap(offset >= 0 ? riverX + guard : riverX - guard);
        }
        return { village, key: villageKey(village), x, y, tier, clearing, seed };
    });
};

export const scatterTrees = (spots: MapSpot[], river: RiverSegment[]): MapTree[] => {
    const rng = mulberry32(FOREST_SEED);
    const trees: MapTree[] = [];
    const used = new Set<string>();
    for (let i = 0; i < 2400 && trees.length < 270; i++) {
        const x = snap(20 + rng() * (WORLD_W - 40));
        const y = snap(20 + rng() * (WORLD_H - 40));
        const key = `${x}:${y}`;
        if (used.has(key)) {
            continue;
        }
        const edgeX = Math.abs(x / WORLD_W - 0.5) * 2;
        const edgeY = Math.abs(y / WORLD_H - 0.5) * 2;
        const edge = Math.max(edgeX, edgeY) ** 2;
        const keep = rng() < 0.1 + 0.9 * edge;
        const scale = 0.8 + rng() * 0.7;
        const kind = Math.floor(rng() * 3);
        if (!keep) {
            continue;
        }
        if (Math.abs(x - riverXAt(river, y)) < 56) {
            continue;
        }
        if (spots.some(spot => (x - spot.x) ** 2 + (y - spot.y) ** 2 < (spot.clearing + 60) ** 2)) {
            continue;
        }
        used.add(key);
        trees.push({ x, y, scale, kind });
    }
    return trees;
};

export const scatterTufts = (spots: MapSpot[], river: RiverSegment[]): MapTuft[] => {
    const rng = mulberry32(TUFT_SEED);
    const tufts: MapTuft[] = [];
    const used = new Set<string>();
    for (let i = 0; i < 560 && tufts.length < 210; i++) {
        const x = snap(24 + rng() * (WORLD_W - 48));
        const y = snap(24 + rng() * (WORLD_H - 48));
        const wide = rng() < 0.4;
        const key = `${x}:${y}`;
        if (used.has(key)) {
            continue;
        }
        if (Math.abs(x - riverXAt(river, y)) < 44) {
            continue;
        }
        if (spots.some(spot => (x - spot.x) ** 2 + (y - spot.y) ** 2 < (spot.clearing + 12) ** 2)) {
            continue;
        }
        used.add(key);
        tufts.push({ x, y, wide });
    }
    return tufts;
};

export const bridgeSegment = (river: RiverSegment[]) => {
    const targetY = WORLD_H * 0.5;
    let best: RiverSegment | null = null;
    for (const segment of river) {
        if (best == null || Math.abs(segment.y - targetY) < Math.abs(best.y - targetY)) {
            best = segment;
        }
    }
    return best;
};

export const buildRoads = (spots: MapSpot[], river: RiverSegment[]): MapRoad[] => {
    const featured = spots.filter(spot => spot.tier >= 2 || spot.village.isNpc || spot.village.isMe);
    const anchors = [...new Map([...spots.slice(0, 24), ...featured].map(spot => [spot.key, spot])).values()]
        .slice(0, 34);
    const roads: MapRoad[] = [];

    for (let index = 1; index < anchors.length; index++) {
        const from = anchors[index];
        if (from == null) continue;
        const fromSide = from.x < riverXAt(river, from.y) ? -1 : 1;
        const candidates = anchors.slice(0, index).filter(candidate =>
            (candidate.x < riverXAt(river, candidate.y) ? -1 : 1) === fromSide,
        );
        let nearest: MapSpot | null = null;
        let nearestDistance = Infinity;
        for (const candidate of candidates) {
            const distance = (candidate.x - from.x) ** 2 + (candidate.y - from.y) ** 2;
            if (distance < nearestDistance) {
                nearest = candidate;
                nearestDistance = distance;
            }
        }
        if (nearest == null) continue;
        const bendX = snap((from.x + nearest.x) / 2);
        roads.push({
            key: `${from.key}>${nearest.key}`,
            points: `${from.x},${from.y} ${bendX},${from.y} ${bendX},${nearest.y} ${nearest.x},${nearest.y}`,
        });
    }

    const bridge = bridgeSegment(river);
    if (bridge != null) {
        const bridgeX = bridge.x + bridge.w / 2;
        const bridgeY = bridge.y + bridge.h / 2;
        for (const side of [-1, 1] as const) {
            const candidates = anchors.filter(spot => (spot.x < bridgeX ? -1 : 1) === side);
            let nearest: MapSpot | null = null;
            let nearestDistance = Infinity;
            for (const candidate of candidates) {
                const distance = (candidate.x - bridgeX) ** 2 + (candidate.y - bridgeY) ** 2;
                if (distance < nearestDistance) {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }
            if (nearest != null) {
                const bridgeEdgeX = bridgeX + side * (bridge.w / 2 + 12);
                roads.push({
                    key: `bridge:${side}`,
                    points: `${nearest.x},${nearest.y} ${nearest.x},${bridgeY} ${bridgeEdgeX},${bridgeY}`,
                });
            }
        }
    }

    return roads;
};

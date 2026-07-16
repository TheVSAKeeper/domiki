import { memo, useCallback, useEffect, useMemo, useRef, useState, type PointerEvent as ReactPointerEvent } from 'react';
import ZoomInIcon from 'pixelarticons/svg/zoom-in.svg?react';
import ZoomOutIcon from 'pixelarticons/svg/zoom-out.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import OverviewIcon from 'pixelarticons/svg/grid-3x3.svg?react';
import { NeighborSprite } from './sprites';
import { VILLAGE_CREST_COLORS } from '../constants/village';
import {
    WORLD_H,
    WORLD_W,
    bridgeSegment,
    buildRoads,
    buildRiver,
    layoutVillages,
    mulberry32,
    scatterTrees,
    scatterTufts,
    villageKey,
    type MapRoad,
    type MapSpot,
    type MapTree,
    type MapTuft,
    type RiverSegment,
} from '../utils/worldMap';
import type { WorldVillageDto } from '../types/api';

export type WorldMetricKey = 'level' | 'seasonOrders' | 'seasonToloka' | 'seasonExpeditions' | 'comfort';

export interface WorldMapProps {
    villages: WorldVillageDto[];
    metricKey: WorldMetricKey;
    metricLabel: string;
    selectedKey: string | null;
    onSelect: (village: WorldVillageDto) => void;
    focus: { key: string; seq: number } | null;
}

interface Camera {
    cx: number;
    cy: number;
    k: number;
}

const MIN_K = 0.82;
const MAX_K = 3.4;

const INITIAL_CAMERA: Camera = { cx: WORLD_W / 2, cy: WORLD_H / 2, k: MIN_K };

const INK = '#262626';
const WATER = '#4a7ab5';
const WATER_LIGHT = '#7ab3a9';
const BANK = '#d6c28e';
const GRASS_CLEAR = '#a9bd8d';
const GRASS_SHADOW = '#7f9863';
const PINE_DARK = '#345126';
const PINE = '#40632f';
const LEAF = '#4f9a3c';
const TRUNK = '#6f4a24';
const WOOD = '#9a6a38';
const SMOKE = '#c9c2b4';
const GOLD = '#e8b83a';
const STONE = '#8f8f8f';
const STONE_DARK = '#565656';
const ROAD = '#d6c28e';
const ROAD_SHADOW = '#9a6a38';

const HUT_STYLES = [
    { wall: '#efe6d4', trim: WOOD, roof: '#c25b33' },
    { wall: '#b07c42', trim: TRUNK, roof: '#c03434' },
    { wall: '#efe6d4', trim: WOOD, roof: WOOD },
    { wall: '#b07c42', trim: TRUNK, roof: TRUNK },
] as const;

interface HutPlace {
    dx: number;
    dy: number;
    w: number;
    smoke?: boolean;
}

const TIER_HUTS: readonly (readonly HutPlace[])[] = [
    [{ dx: 0, dy: 14, w: 28, smoke: true }],
    [{ dx: -18, dy: 18, w: 26 }, { dx: 16, dy: 8, w: 28, smoke: true }],
    [{ dx: -26, dy: 20, w: 26 }, { dx: 6, dy: 4, w: 32, smoke: true }, { dx: 30, dy: 24, w: 22 }],
    [{ dx: -34, dy: 22, w: 26 }, { dx: -2, dy: 0, w: 34, smoke: true }, { dx: 30, dy: 12, w: 26 }, { dx: 6, dy: 32, w: 24 }],
    [
        { dx: -44, dy: 20, w: 26 },
        { dx: -12, dy: -6, w: 36, smoke: true },
        { dx: 24, dy: 4, w: 30, smoke: true },
        { dx: 50, dy: 24, w: 24 },
        { dx: -4, dy: 36, w: 26 },
    ],
];

const clampCamera = (camera: Camera): Camera => {
    const k = Math.min(MAX_K, Math.max(MIN_K, camera.k));
    const viewW = WORLD_W / k;
    const viewH = WORLD_H / k;
    return {
        k,
        cx: viewW >= WORLD_W ? WORLD_W / 2 : Math.min(WORLD_W - viewW / 2, Math.max(viewW / 2, camera.cx)),
        cy: viewH >= WORLD_H ? WORLD_H / 2 : Math.min(WORLD_H - viewH / 2, Math.max(viewH / 2, camera.cy)),
    };
};

const toViewBox = (camera: Camera) => {
    const viewW = WORLD_W / camera.k;
    const viewH = WORLD_H / camera.k;
    return `${camera.cx - viewW / 2} ${camera.cy - viewH / 2} ${viewW} ${viewH}`;
};

const prefersReducedMotion = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;

const fitSpots = (spots: MapSpot[]): Camera => {
    if (spots.length === 0) {
        return clampCamera({ cx: WORLD_W / 2, cy: WORLD_H / 2, k: MIN_K });
    }
    let minX = Infinity;
    let maxX = -Infinity;
    let minY = Infinity;
    let maxY = -Infinity;
    for (const spot of spots) {
        minX = Math.min(minX, spot.x - 170);
        maxX = Math.max(maxX, spot.x + 170);
        minY = Math.min(minY, spot.y - 150);
        maxY = Math.max(maxY, spot.y + 150);
    }
    const k = Math.min(2, Math.max(MIN_K, Math.min(WORLD_W / (maxX - minX), WORLD_H / (maxY - minY))));
    return clampCamera({ cx: (minX + maxX) / 2, cy: (minY + maxY) / 2, k });
};

const Hut = ({ cx, cy, w, style, smoke }: { cx: number; cy: number; w: number; style: (typeof HUT_STYLES)[number]; smoke: boolean }) => {
    const h = Math.round((w * 0.55) / 2) * 2;
    const roofTop = cy - h - 12;
    return (
        <g>
            <rect x={cx - w / 2 - 2} y={cy - 2} width={w + 4} height={4} fill={GRASS_SHADOW} />
            <rect x={cx - w / 2} y={cy - h} width={w} height={h} fill={style.wall} />
            <rect x={cx - w / 2} y={cy - Math.round(h / 4) * 2} width={w} height={2} fill={style.trim} />
            <rect x={cx - 4} y={cy - 10} width={8} height={10} fill={style.trim} />
            {w >= 28 && <rect x={cx + w / 2 - 10} y={cy - h + 4} width={6} height={6} fill={WATER} />}
            <rect x={cx - w / 2 - 4} y={cy - h - 6} width={w + 8} height={6} fill={style.roof} />
            <rect x={cx - w / 2 + 2} y={roofTop} width={w - 4} height={6} fill={style.roof} />
            <rect x={cx - w / 2 + 8} y={roofTop - 4} width={w - 16} height={4} fill={INK} />
            {smoke && (
                <g>
                    <rect x={cx + w / 2 - 14} y={roofTop - 8} width={6} height={10} fill={STONE} />
                    <rect x={cx + w / 2 - 14} y={roofTop - 10} width={6} height={2} fill={STONE_DARK} />
                    <g className="wm-smoke">
                        <rect x={cx + w / 2 - 14} y={roofTop - 18} width={4} height={4} fill={SMOKE} />
                        <rect x={cx + w / 2 - 10} y={roofTop - 26} width={4} height={4} fill={SMOKE} />
                        <rect x={cx + w / 2 - 18} y={roofTop - 32} width={4} height={4} fill={SMOKE} />
                    </g>
                </g>
            )}
        </g>
    );
};

const s2 = (value: number) => Math.round(value / 2) * 2;

const Clearing = ({ r }: { r: number }) => {
    return (
        <g>
            <rect x={s2(-r * 0.7)} y={s2(-r * 0.64)} width={s2(r * 1.4)} height={s2(r * 0.32)} fill={GRASS_CLEAR} />
            <rect x={s2(-r)} y={s2(-r * 0.32)} width={s2(r * 2)} height={s2(r * 0.74)} fill={GRASS_CLEAR} />
            <rect x={s2(-r * 0.7)} y={s2(r * 0.42)} width={s2(r * 1.4)} height={s2(r * 0.22)} fill={GRASS_CLEAR} />
            <rect x={s2(-r * 0.7)} y={s2(r * 0.64)} width={s2(r * 1.4)} height={4} fill={GRASS_SHADOW} />
        </g>
    );
};

const TREE_COLORS = [PINE_DARK, PINE, LEAF] as const;

const Tree = ({ tree }: { tree: MapTree }) => {
    const color = TREE_COLORS[tree.kind] ?? PINE;
    return (
        <g transform={`translate(${tree.x} ${tree.y}) scale(${tree.scale.toFixed(2)})`}>
            {tree.kind === 2 ? (
                <g>
                    <rect x={-2} y={6} width={4} height={10} fill={TRUNK} />
                    <rect x={-10} y={-8} width={20} height={14} fill={color} />
                    <rect x={-6} y={-12} width={12} height={4} fill={color} />
                    <rect x={-10} y={6} width={20} height={2} fill={GRASS_SHADOW} />
                </g>
            ) : (
                <g>
                    <rect x={-2} y={8} width={4} height={8} fill={TRUNK} />
                    <rect x={-12} y={0} width={24} height={8} fill={color} />
                    <rect x={-8} y={-8} width={16} height={8} fill={color} />
                    <rect x={-4} y={-14} width={8} height={6} fill={color} />
                    <rect x={-12} y={8} width={24} height={2} fill={GRASS_SHADOW} />
                </g>
            )}
        </g>
    );
};

const Mountain = ({ x, y, scale = 1 }: { x: number; y: number; scale?: number }) => (
    <g transform={`translate(${x} ${y}) scale(${scale})`} aria-hidden="true">
        <rect x={-42} y={28} width={84} height={18} fill={STONE_DARK} />
        <rect x={-34} y={10} width={68} height={18} fill={STONE} />
        <rect x={-24} y={-8} width={48} height={18} fill={STONE} />
        <rect x={-14} y={-22} width={28} height={14} fill={STONE_DARK} />
        <rect x={-8} y={-28} width={16} height={12} fill="#efe6d4" />
        <rect x={-14} y={-18} width={10} height={6} fill="#efe6d4" />
    </g>
);

const Field = ({ x, y, w, h }: { x: number; y: number; w: number; h: number }) => (
    <g transform={`translate(${x} ${y})`} aria-hidden="true">
        <rect width={w} height={h} fill="#d6c28e" />
        {Array.from({ length: Math.max(1, Math.floor(h / 12)) }, (_, index) => (
            <rect key={index} x={4} y={6 + index * 12} width={w - 8} height={4} fill="#b07c42" />
        ))}
    </g>
);

const MOUNTAINS = [
    { x: 150, y: 150, scale: 1.35 },
    { x: 290, y: 110, scale: 0.9 },
    { x: 2260, y: 150, scale: 1.15 },
    { x: 2420, y: 230, scale: 1.4 },
    { x: 2500, y: 470, scale: 0.85 },
] as const;

const REGIONS = [
    { x: 360, y: 330, label: 'СТАРЫЙ БОР' },
    { x: 2020, y: 360, label: 'СЕВЕРНЫЕ КРЯЖИ' },
    { x: 1540, y: 1020, label: 'ТИХАЯ ПОЙМА' },
    { x: 420, y: 1510, label: 'ЯНТАРНЫЕ ПОЛЯ' },
] as const;

const Pennant = ({ x, y, color, gold }: { x: number; y: number; color: string; gold: boolean }) => (
    <g transform={`translate(${x} ${y})`}>
        {gold ? (
            <g>
                <rect x={0} y={-10} width={4} height={38} fill={TRUNK} />
                <rect x={4} y={-8} width={18} height={10} fill={GOLD} />
                <rect x={4} y={2} width={10} height={4} fill={GOLD} />
                <rect x={4} y={6} width={14} height={6} fill={color} />
                <rect x={8} y={-5} width={8} height={4} fill={INK} />
            </g>
        ) : (
            <g>
                <rect x={0} y={0} width={4} height={28} fill={TRUNK} />
                <rect x={4} y={2} width={14} height={8} fill={color} />
                <rect x={4} y={10} width={8} height={4} fill={color} />
            </g>
        )}
    </g>
);

const RankBanner = ({ rank }: { rank: number }) => (
    <g className="wm-rank">
        <rect x={-2} y={-40} width={4} height={40} fill={TRUNK} />
        <rect x={-18} y={-60} width={36} height={22} fill={GOLD} />
        <rect x={-18} y={-60} width={36} height={2} fill={INK} />
        <rect x={-18} y={-38} width={36} height={2} fill={INK} />
        <text x={0} y={-43} textAnchor="middle" className="wm-rank-num">{rank}</text>
    </g>
);

const BRACKET_CORNERS = [
    { sx: -1, sy: -1 },
    { sx: 1, sy: -1 },
    { sx: -1, sy: 1 },
    { sx: 1, sy: 1 },
] as const;

const Brackets = ({ r }: { r: number }) => (
    <g className="wm-select" aria-hidden="true">
        {BRACKET_CORNERS.map(corner => (
            <g key={`${corner.sx}:${corner.sy}`} transform={`translate(${corner.sx * r} ${corner.sy * Math.round(r * 0.78)})`}>
                <rect x={corner.sx < 0 ? 0 : -16} y={corner.sy < 0 ? 0 : -4} width={16} height={4} fill={GOLD} />
                <rect x={corner.sx < 0 ? 0 : -4} y={corner.sy < 0 ? 0 : -16} width={4} height={16} fill={GOLD} />
            </g>
        ))}
    </g>
);

const SettlementArt = memo(({ spot }: { spot: MapSpot }) => {
    const rng = mulberry32(spot.seed);
    const fallbackStyle = HUT_STYLES[0];
    const huts = (TIER_HUTS[spot.tier] ?? TIER_HUTS[0] ?? [])
        .map(place => ({ place, style: HUT_STYLES[Math.floor(rng() * HUT_STYLES.length)] ?? fallbackStyle }))
        .sort((a, b) => a.place.dy - b.place.dy);
    const crestColor = VILLAGE_CREST_COLORS[spot.village.crestColor] ?? VILLAGE_CREST_COLORS[0];
    const gardenX = Math.round(-spot.clearing * 0.72 / 2) * 2;
    const gardenY = Math.round(spot.clearing * 0.28 / 2) * 2;
    return (
        <g>
            <Clearing r={spot.clearing} />
            {spot.village.isNpc ? (
                <NeighborSprite
                    logicName={spot.village.npcLogicName ?? 'generic'}
                    size={32}
                    width={spot.clearing * 1.5}
                    height={spot.clearing * 1.5}
                    x={-spot.clearing * 0.75}
                    y={-spot.clearing * 0.95}
                    className="wm-npc-sprite"
                />
            ) : (
                <g>
                    {spot.tier >= 2 && (
                        <g>
                            <rect x={gardenX} y={gardenY} width={20} height={4} fill={TRUNK} />
                            <rect x={gardenX} y={gardenY + 6} width={20} height={4} fill={TRUNK} />
                            <rect x={gardenX + 2} y={gardenY} width={4} height={10} fill={LEAF} />
                            <rect x={gardenX + 10} y={gardenY} width={4} height={10} fill={LEAF} />
                        </g>
                    )}
                    {huts.map(hut => (
                        <Hut
                            key={`${hut.place.dx}:${hut.place.dy}`}
                            cx={hut.place.dx}
                            cy={hut.place.dy}
                            w={hut.place.w}
                            style={hut.style}
                            smoke={hut.place.smoke === true && spot.tier >= 2}
                        />
                    ))}
                </g>
            )}
            <Pennant
                x={Math.round(spot.clearing * 0.56 / 2) * 2}
                y={Math.round(-spot.clearing * 0.9 / 2) * 2}
                color={crestColor}
                gold={spot.village.isMe}
            />
        </g>
    );
});

SettlementArt.displayName = 'SettlementArt';

interface WorldSceneryProps {
    river: RiverSegment[];
    roads: MapRoad[];
    trees: MapTree[];
    tufts: MapTuft[];
    bridge: RiverSegment | null;
}

const WorldScenery = memo(({ river, roads, trees, tufts, bridge }: WorldSceneryProps) => {
    const shimmer: { segment: RiverSegment; offset: number }[] = [];
    let n = 0;
    river.forEach((segment, index) => {
        if (index % 3 === 1) {
            shimmer.push({ segment, offset: (n % 2) * 10 });
            n += 1;
        }
    });

    return (
        <>
            <defs>
                <pattern id="wm-meadow" width="64" height="64" patternUnits="userSpaceOnUse">
                    <rect width="64" height="64" fill="#90a878" />
                    <rect width="32" height="32" fill="#8ba273" />
                    <rect x="32" y="32" width="32" height="32" fill="#8ba273" />
                </pattern>
            </defs>
            <rect x={-400} y={-400} width={WORLD_W + 800} height={WORLD_H + 800} fill="url(#wm-meadow)" />
            <Field x={170} y={1370} w={190} h={120} />
            <Field x={2140} y={1300} w={220} h={132} />
            {REGIONS.map(region => (
                <text key={region.label} className="wm-region-label" x={region.x} y={region.y} textAnchor="middle">
                    {region.label}
                </text>
            ))}
            {MOUNTAINS.map(mountain => (
                <Mountain key={`${mountain.x}:${mountain.y}`} {...mountain} />
            ))}
            {tufts.map(tuft => (
                <rect
                    key={`${tuft.x}:${tuft.y}`}
                    x={tuft.x}
                    y={tuft.y}
                    width={tuft.wide ? 10 : 6}
                    height={4}
                    fill={GRASS_SHADOW}
                />
            ))}
            <g className="wm-roads" aria-hidden="true">
                {roads.map(road => (
                    <g key={road.key}>
                        <polyline points={road.points} fill="none" stroke={ROAD_SHADOW} strokeWidth={14} />
                        <polyline points={road.points} fill="none" stroke={ROAD} strokeWidth={8} />
                    </g>
                ))}
            </g>
            <g>
                {river.map(segment => (
                    <rect key={`b${segment.y}`} x={segment.x - 8} y={segment.y} width={segment.w + 16} height={segment.h} fill={BANK} />
                ))}
                {river.map(segment => (
                    <rect key={`w${segment.y}`} x={segment.x} y={segment.y} width={segment.w} height={segment.h} fill={WATER} />
                ))}
                {shimmer.map(item => (
                    <rect
                        key={`s${item.segment.y}`}
                        className="wm-water"
                        x={item.segment.x + 6 + item.offset}
                        y={item.segment.y + 14}
                        width={8}
                        height={4}
                        fill={WATER_LIGHT}
                    />
                ))}
            </g>
            {bridge != null && (
                <g>
                    <rect x={bridge.x - 12} y={bridge.y + 10} width={bridge.w + 24} height={18} fill={WOOD} />
                    <rect x={bridge.x - 12} y={bridge.y + 10} width={bridge.w + 24} height={2} fill={INK} />
                    <rect x={bridge.x - 12} y={bridge.y + 26} width={bridge.w + 24} height={2} fill={TRUNK} />
                </g>
            )}
            {trees.map(tree => (
                <Tree key={`${tree.x}:${tree.y}`} tree={tree} />
            ))}
        </>
    );
});

WorldScenery.displayName = 'WorldScenery';

interface MapControlsProps {
    onOverview: () => void;
    onZoomIn: () => void;
    onZoomOut: () => void;
    onHome: (() => void) | null;
}

const MapControls = ({ onOverview, onZoomIn, onZoomOut, onHome }: MapControlsProps) => (
    <div className="world-map-ctrl">
        <button type="button" className="wm-btn" onClick={onOverview} aria-label="Показать весь мир" title="Показать весь мир">
            <OverviewIcon className="wm-btn-ico" aria-hidden="true" />
        </button>
        <button type="button" className="wm-btn" onClick={onZoomIn} aria-label="Приблизить" title="Приблизить">
            <ZoomInIcon className="wm-btn-ico" aria-hidden="true" />
        </button>
        <button type="button" className="wm-btn" onClick={onZoomOut} aria-label="Отдалить" title="Отдалить">
            <ZoomOutIcon className="wm-btn-ico" aria-hidden="true" />
        </button>
        {onHome != null && (
            <button type="button" className="wm-btn wm-btn-home" onClick={onHome} aria-label="К моей деревне" title="К моей деревне">
                <HomeIcon className="wm-btn-ico" aria-hidden="true" />
            </button>
        )}
    </div>
);

interface HoverTipProps {
    hover: { spot: MapSpot; x: number; y: number } | null;
    metricKey: WorldMetricKey;
    metricLabel: string;
}

const HoverTip = ({ hover, metricKey, metricLabel }: HoverTipProps) => {
    if (hover == null) {
        return null;
    }
    return (
        <div className="wm-tip" style={{ left: hover.x, top: hover.y }}>
            <div className="wm-tip-name">{hover.spot.village.villageName}</div>
            <div className="wm-tip-row">Обжитость {hover.spot.village.level}</div>
            {metricKey !== 'level' && hover.spot.village[metricKey] > 0 && (
                <div className="wm-tip-row">{metricLabel}: {hover.spot.village[metricKey]}</div>
            )}
            {hover.spot.village.isNpc && <div className="wm-tip-row wm-tip-npc">Торговое село соседей</div>}
            {hover.spot.village.isMe && <div className="wm-tip-row wm-tip-me">Моя деревня</div>}
        </div>
    );
};

export const WorldMap = ({ villages, metricKey, metricLabel, selectedKey, onSelect, focus }: WorldMapProps) => {
    const svgRef = useRef<SVGSVGElement | null>(null);
    const camRef = useRef<Camera>(INITIAL_CAMERA);
    const flyRaf = useRef(0);
    const bucketRaf = useRef(0);
    const dragState = useRef<{ pointerId: number; startX: number; startY: number; lastX: number; lastY: number } | null>(null);
    const movedRef = useRef(false);
    const lastFocusSeq = useRef(0);
    const [kBucket, setKBucket] = useState(() => Math.round(INITIAL_CAMERA.k * 4));
    const [hover, setHover] = useState<{ spot: MapSpot; x: number; y: number } | null>(null);

    const river = useMemo(() => buildRiver(), []);
    const spots = useMemo(() => layoutVillages(villages, river), [villages, river]);
    const trees = useMemo(() => scatterTrees(spots, river), [spots, river]);
    const tufts = useMemo(() => scatterTufts(spots, river), [spots, river]);
    const bridge = useMemo(() => bridgeSegment(river), [river]);
    const roads = useMemo(() => buildRoads(spots, river), [spots, river]);
    const mySpot = useMemo(() => spots.find(spot => spot.village.isMe) ?? null, [spots]);

    const ranks = useMemo(() => {
        const rated = villages
            .filter(village => village[metricKey] > 0)
            .sort((a, b) => b[metricKey] - a[metricKey] || a.villageName.localeCompare(b.villageName, 'ru'));
        const map = new Map<string, number>();
        rated.slice(0, 3).forEach((village, index) => map.set(villageKey(village), index + 1));
        return map;
    }, [villages, metricKey]);

    const applyCamera = useCallback((camera: Camera) => {
        camRef.current = camera;
        svgRef.current?.setAttribute('viewBox', toViewBox(camera));
        cancelAnimationFrame(bucketRaf.current);
        bucketRaf.current = requestAnimationFrame(() => {
            setKBucket(Math.round(camRef.current.k * 4));
        });
    }, []);

    const flyTo = useCallback((target: Camera, ms = 700) => {
        cancelAnimationFrame(flyRaf.current);
        const from = camRef.current;
        const to = clampCamera(target);
        if (prefersReducedMotion() || ms <= 0) {
            applyCamera(to);
            return;
        }
        const start = performance.now();
        const tick = (time: number) => {
            const p = Math.min(1, (time - start) / ms);
            const e = p < 0.5 ? 4 * p ** 3 : 1 - (-2 * p + 2) ** 3 / 2;
            applyCamera(clampCamera({
                cx: from.cx + (to.cx - from.cx) * e,
                cy: from.cy + (to.cy - from.cy) * e,
                k: from.k + (to.k - from.k) * e,
            }));
            if (p < 1) {
                flyRaf.current = requestAnimationFrame(tick);
            }
        };
        flyRaf.current = requestAnimationFrame(tick);
    }, [applyCamera]);

    const clientToWorld = useCallback((clientX: number, clientY: number) => {
        const svg = svgRef.current;
        const ctm = svg?.getScreenCTM();
        if (svg == null || ctm == null) {
            return null;
        }
        return new DOMPoint(clientX, clientY).matrixTransform(ctm.inverse());
    }, []);

    useEffect(() => () => {
        cancelAnimationFrame(flyRaf.current);
        cancelAnimationFrame(bucketRaf.current);
    }, []);

    useEffect(() => {
        if (spots.length === 0) {
            return;
        }
        applyCamera(fitSpots(spots));
        const me = spots.find(spot => spot.village.isMe);
        if (me != null) {
            flyTo({ cx: me.x, cy: me.y, k: Math.max(1.35, camRef.current.k) }, 1100);
        }
    }, [spots, applyCamera, flyTo]);

    useEffect(() => {
        if (focus == null || focus.seq === lastFocusSeq.current) {
            return;
        }
        lastFocusSeq.current = focus.seq;
        const spot = spots.find(item => item.key === focus.key);
        if (spot != null) {
            flyTo({ cx: spot.x, cy: spot.y, k: Math.max(1.6, camRef.current.k) }, 700);
        }
    }, [focus, spots, flyTo]);

    useEffect(() => {
        const svg = svgRef.current;
        if (svg == null) {
            return;
        }
        const onWheel = (event: WheelEvent) => {
            event.preventDefault();
            cancelAnimationFrame(flyRaf.current);
            const cam = camRef.current;
            const k = Math.min(MAX_K, Math.max(MIN_K, cam.k * Math.exp(-event.deltaY * 0.0016)));
            const point = clientToWorld(event.clientX, event.clientY);
            if (point == null) {
                applyCamera(clampCamera({ ...cam, k }));
                return;
            }
            const ratio = cam.k / k;
            applyCamera(clampCamera({
                k,
                cx: point.x + (cam.cx - point.x) * ratio,
                cy: point.y + (cam.cy - point.y) * ratio,
            }));
        };
        svg.addEventListener('wheel', onWheel, { passive: false });
        return () => { svg.removeEventListener('wheel', onWheel); };
    }, [applyCamera, clientToWorld]);

    const onPointerDown = (event: ReactPointerEvent<SVGSVGElement>) => {
        if (event.pointerType === 'mouse' && event.button !== 0) {
            return;
        }
        if ((event.target as Element).closest('.wm-village') != null) {
            movedRef.current = false;
            return;
        }
        cancelAnimationFrame(flyRaf.current);
        event.currentTarget.setPointerCapture(event.pointerId);
        dragState.current = {
            pointerId: event.pointerId,
            startX: event.clientX,
            startY: event.clientY,
            lastX: event.clientX,
            lastY: event.clientY,
        };
        movedRef.current = false;
        setHover(null);
    };

    const onPointerMove = (event: ReactPointerEvent<SVGSVGElement>) => {
        const drag = dragState.current;
        if (drag == null || drag.pointerId !== event.pointerId) {
            return;
        }
        const svg = svgRef.current;
        const ctm = svg?.getScreenCTM();
        if (svg == null || ctm == null) {
            return;
        }
        const dx = (event.clientX - drag.lastX) / ctm.a;
        const dy = (event.clientY - drag.lastY) / ctm.d;
        if (Math.abs(event.clientX - drag.startX) + Math.abs(event.clientY - drag.startY) > 5) {
            movedRef.current = true;
        }
        drag.lastX = event.clientX;
        drag.lastY = event.clientY;
        const cam = camRef.current;
        applyCamera(clampCamera({ ...cam, cx: cam.cx - dx, cy: cam.cy - dy }));
    };

    const onPointerUp = (event: ReactPointerEvent<SVGSVGElement>) => {
        if (dragState.current?.pointerId === event.pointerId) {
            dragState.current = null;
        }
    };

    const zoomBy = (factor: number) => {
        const cam = camRef.current;
        flyTo({ ...cam, k: cam.k * factor }, 220);
    };

    const k = kBucket / 4;
    const showAllLabels = k >= 2.15;

    return (
        <div className="world-map pixel-panel">
            <svg
                ref={svgRef}
                className="world-map-svg"
                viewBox={toViewBox(INITIAL_CAMERA)}
                preserveAspectRatio="xMidYMid slice"
                shapeRendering="crispEdges"
                role="application"
                aria-label="Карта долины: деревни мира"
                onPointerDown={onPointerDown}
                onPointerMove={onPointerMove}
                onPointerUp={onPointerUp}
                onPointerCancel={onPointerUp}
            >
                <WorldScenery river={river} roads={roads} trees={trees} tufts={tufts} bridge={bridge} />
                {spots.map(spot => {
                    const rank = ranks.get(spot.key);
                    const selected = selectedKey === spot.key;
                    const special = spot.village.isMe || spot.village.isNpc || rank != null || selected;
                    return (
                        <g
                            key={spot.key}
                            className={'wm-village' + (selected ? ' wm-village-selected' : '')}
                            transform={`translate(${spot.x} ${spot.y})`}
                            role="button"
                            tabIndex={0}
                            aria-label={`Деревня ${spot.village.villageName}, обжитость ${spot.village.level}`}
                            onClick={() => {
                                if (!movedRef.current) {
                                    onSelect(spot.village);
                                }
                            }}
                            onKeyDown={event => {
                                if (event.key === 'Enter' || event.key === ' ') {
                                    event.preventDefault();
                                    onSelect(spot.village);
                                }
                            }}
                            onPointerMove={event => {
                                if (dragState.current == null) {
                                    setHover({
                                        spot,
                                        x: Math.max(8, Math.min(window.innerWidth - 258, event.clientX + 14)),
                                        y: Math.max(8, Math.min(window.innerHeight - 130, event.clientY + 14)),
                                    });
                                }
                            }}
                            onPointerLeave={() => { setHover(null); }}
                        >
                            <SettlementArt spot={spot} />
                            {rank != null && metricKey !== 'level' && <RankBanner rank={rank} />}
                            <Brackets r={spot.clearing + 10} />
                            {(showAllLabels || special) && (
                                <text
                                    className={'wm-label' + (spot.village.isMe ? ' wm-label-me' : '')}
                                    x={0}
                                    y={spot.clearing + 24}
                                    textAnchor="middle"
                                    style={{ fontSize: Math.round((special ? 38 : 34) / k) }}
                                >
                                    {spot.village.villageName}
                                </text>
                            )}
                        </g>
                    );
                })}
            </svg>
            <div className="wm-title">Долина Домиков</div>
            <div className="wm-hint">Тяни карту · колесо — масштаб</div>
            <MapControls
                onOverview={() => { flyTo(fitSpots(spots), 700); }}
                onZoomIn={() => { zoomBy(1.45); }}
                onZoomOut={() => { zoomBy(1 / 1.45); }}
                onHome={mySpot != null ? () => { flyTo({ cx: mySpot.x, cy: mySpot.y, k: 1.9 }, 700); } : null}
            />
            <HoverTip hover={hover} metricKey={metricKey} metricLabel={metricLabel} />
        </div>
    );
};

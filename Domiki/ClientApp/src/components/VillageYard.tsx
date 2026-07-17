import { useMemo, useState } from 'react';
import type { DecorStateDto, DomikDto, DomikTypeDto, VillageLevelDto, WeatherPeriodDto, WorkerDto } from '../types/api';
import { hashString } from '../utils/worldMap';
import { layoutYard, YARD_H, type YardGreen, type YardSpot } from '../utils/yardMap';
import { DecorSprite, DomikSprite } from './sprites';

const GRASS_CLEAR = '#a9bd8d';
const GRASS_SHADOW = '#7f9863';
const MEADOW = '#90a878';
const MEADOW_ALT = '#8ba273';
const ROAD = '#d6c28e';
const ROAD_SHADOW = '#9a6a38';
const PINE_DARK = '#345126';
const PINE = '#40632f';
const LEAF = '#4f9a3c';
const TRUNK = '#6f4a24';
const GOLD = '#e8b83a';

const TREE_COLORS = [PINE_DARK, PINE, LEAF] as const;
const FOLK_SHIRTS = ['#b04a3a', '#4a7ab5', '#7f9863', '#b8863b'] as const;
const FOLK_CAP = 6;

const YardTree = ({ green }: { green: YardGreen }) => {
    const color = TREE_COLORS[green.kind] ?? PINE;
    return (
        <g transform={`translate(${green.x} ${green.y}) scale(${green.scale.toFixed(2)})`}>
            {green.kind === 2 ? (
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

const YardFolk = ({ x, y, name }: { x: number; y: number; name: string }) => {
    const shirt = FOLK_SHIRTS[hashString(name) % FOLK_SHIRTS.length] ?? FOLK_SHIRTS[0];
    return (
        <g transform={`translate(${x} ${y})`}>
            <title>{name}</title>
            <rect x={-5} y={0} width={10} height={2} fill={GRASS_SHADOW} />
            <rect x={-3} y={-4} width={3} height={4} fill="#4a3a2e" />
            <rect x={0} y={-4} width={3} height={4} fill="#4a3a2e" />
            <rect x={-4} y={-10} width={8} height={6} fill={shirt} />
            <rect x={-3} y={-12} width={6} height={2} fill="#4a3020" />
            <rect x={-3} y={-17} width={6} height={5} fill="#e7c9a9" />
        </g>
    );
};

const SelectionBrackets = ({ x, y }: { x: number; y: number }) => {
    const left = x - 36;
    const right = x + 36;
    const top = y - 50;
    const bottom = y + 22;
    return (
        <g aria-hidden="true">
            <rect x={left} y={top} width={12} height={3} fill={GOLD} />
            <rect x={left} y={top} width={3} height={12} fill={GOLD} />
            <rect x={right - 12} y={top} width={12} height={3} fill={GOLD} />
            <rect x={right - 3} y={top} width={3} height={12} fill={GOLD} />
            <rect x={left} y={bottom - 3} width={12} height={3} fill={GOLD} />
            <rect x={left} y={bottom - 12} width={3} height={12} fill={GOLD} />
            <rect x={right - 12} y={bottom - 3} width={12} height={3} fill={GOLD} />
            <rect x={right - 3} y={bottom - 12} width={3} height={12} fill={GOLD} />
        </g>
    );
};

interface VillageYardProps {
    domiks: DomikDto[];
    domikTypes: DomikTypeDto[];
    decor: DecorStateDto | null;
    workers: WorkerDto[] | null;
    villageLevel: VillageLevelDto | null;
    currentWeather: WeatherPeriodDto | null;
    selectedDomikId: number | null;
    displayName: (domik: DomikDto) => string;
    onSelect: (id: number) => void;
}

interface FolkPlacement { x: number; y: number; name: string; }

const busyFolkForSpot = (spot: YardSpot, workers: WorkerDto[]): FolkPlacement[] => {
    const manufactureIds = new Set((spot.domik.manufactures ?? []).map(manufacture => manufacture.id));
    const positions = [{ x: spot.x + 36, y: spot.y + 26 }, { x: spot.x - 40, y: spot.y + 30 }];
    return workers
        .filter(worker => worker.manufactureId != null && manufactureIds.has(worker.manufactureId))
        .slice(0, 2)
        .map((worker, index) => ({ ...(positions[index] ?? positions[0] ?? { x: spot.x, y: spot.y }), name: worker.name }));
};

const freeFolkPlacements = (workers: WorkerDto[], spots: YardSpot[], domikTypes: DomikTypeDto[], firstPathY: number): FolkPlacement[] => {
    const free = workers
        .filter(worker => worker.manufactureId == null && worker.expeditionId == null && worker.restUntil == null && worker.sickUntil == null)
        .slice(0, FOLK_CAP);
    const barracksSpot = spots.find(spot => domikTypes.find(type => type.id === spot.domik.typeId)?.logicName === 'barracks');
    return free.map((worker, index) => barracksSpot != null
        ? { x: barracksSpot.x - 30 + index * 14, y: barracksSpot.y + 34, name: worker.name }
        : { x: 40 + index * 14, y: firstPathY + 30, name: worker.name });
};

export const VillageYard = ({ domiks, domikTypes, decor, workers, villageLevel, currentWeather, selectedDomikId, displayName, onSelect }: VillageYardProps) => {
    const [collapsed, setCollapsed] = useState<boolean>(() => localStorage.getItem('domiki.yard.collapsed') === '1');
    const owned = decor?.owned;
    const level = villageLevel?.level;
    const layout = useMemo(
        () => layoutYard(domiks, owned ?? [], level ?? 0),
        [domiks, owned, level],
    );

    if (domiks.length === 0) {
        return null;
    }

    const toggle = () => {
        const next = !collapsed;
        setCollapsed(next);
        localStorage.setItem('domiki.yard.collapsed', next ? '1' : '0');
    };

    const workerList = workers ?? [];
    const firstPathY = Number(layout.path.split(' ')[0]?.split(',')[1] ?? YARD_H / 2);
    const busyFolk = layout.spots.flatMap(spot => busyFolkForSpot(spot, workerList));
    const freeFolk = freeFolkPlacements(workerList, layout.spots, domikTypes, firstPathY);
    const depthSpots = [...layout.spots].sort((a, b) => a.y - b.y);

    return (
        <section className="yard pixel-panel">
            <header className="yard-head">
                <h3 className="yard-title panel-title">Мой двор</h3>
                <button type="button" className="yard-toggle" aria-expanded={!collapsed} onClick={toggle}>
                    {collapsed ? 'Показать' : 'Свернуть'}
                </button>
            </header>
            {!collapsed &&
                <div className="yard-stage" data-weather={currentWeather?.logicName}>
                    <svg className="yard-svg" viewBox={`0 0 ${layout.width} ${YARD_H}`} preserveAspectRatio="xMidYMid slice"
                        shapeRendering="crispEdges" aria-label="Двор деревни: постройки, декор и трудяги">
                        <defs>
                            <pattern id="yard-meadow" width="64" height="64" patternUnits="userSpaceOnUse">
                                <rect width="64" height="64" fill={MEADOW} />
                                <rect width="32" height="32" fill={MEADOW_ALT} />
                                <rect x="32" y="32" width="32" height="32" fill={MEADOW_ALT} />
                            </pattern>
                        </defs>
                        <rect x={-200} y={-200} width={layout.width + 400} height={YARD_H + 400} fill="url(#yard-meadow)" />
                        {layout.tufts.map(tuft =>
                            <rect key={`${tuft.x}:${tuft.y}`} x={tuft.x} y={tuft.y} width={tuft.kind === 1 ? 10 : 6} height={4} fill={GRASS_SHADOW} />,
                        )}
                        <polyline points={layout.path} fill="none" stroke={ROAD_SHADOW} strokeWidth={14} />
                        <polyline points={layout.path} fill="none" stroke={ROAD} strokeWidth={8} />
                        {layout.spots.map(spot =>
                            <g key={spot.domik.id}>
                                <rect x={spot.x - 40} y={spot.y + 18} width={80} height={14} fill={GRASS_CLEAR} />
                                <rect x={spot.x - 40} y={spot.y + 32} width={80} height={3} fill={GRASS_SHADOW} />
                            </g>,
                        )}
                        {layout.decors.map(d => {
                            const type = decor?.types.find(t => t.id === d.decorTypeId);
                            return type == null ? null : (
                                <DecorSprite key={d.key} logicName={type.logicName} x={d.x - 16} y={d.y - 28} width={32} height={32} aria-hidden="true" />
                            );
                        })}
                        {depthSpots.map(spot => {
                            const domikType = domikTypes.find(type => type.id === spot.domik.typeId);
                            if (domikType == null) {
                                return null;
                            }
                            const selected = selectedDomikId === spot.domik.id;
                            return (
                                <g key={spot.domik.id} className={'yard-domik' + (selected ? ' yard-domik-selected' : '')}
                                    role="button" tabIndex={0} aria-label={displayName(spot.domik)}
                                    onClick={() => { onSelect(spot.domik.id); }}
                                    onKeyDown={event => {
                                        if (event.key === 'Enter' || event.key === ' ') {
                                            event.preventDefault();
                                            onSelect(spot.domik.id);
                                        }
                                    }}>
                                    <DomikSprite logicName={domikType.logicName} level={spot.domik.level}
                                        working={(spot.domik.manufactures?.length ?? 0) > 0}
                                        x={spot.x - 32} y={spot.y - 46} width={64} height={64} />
                                    {selected && <SelectionBrackets x={spot.x} y={spot.y} />}
                                </g>
                            );
                        })}
                        {layout.trees.map(tree => <YardTree key={`${tree.x}:${tree.y}`} green={tree} />)}
                        {busyFolk.map(folk => <YardFolk key={`b:${folk.x}:${folk.y}:${folk.name}`} x={folk.x} y={folk.y} name={folk.name} />)}
                        {freeFolk.map(folk => <YardFolk key={`f:${folk.x}:${folk.y}:${folk.name}`} x={folk.x} y={folk.y} name={folk.name} />)}
                    </svg>
                </div>
            }
        </section>
    );
};

import { useMemo, useRef, useState } from 'react';
import type { PointerEvent as ReactPointerEvent } from 'react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import type { DomikTypeDto, WorkerDto } from '../types/api';
import { isWorkerFree } from '../utils/game';
import { bestSkill, workerSkillPercent } from '../utils/assign';
import { isSkilledWorker } from '../utils/worker';
import { useElementHeightVar } from '../hooks/useElementHeightVar';
import { useNarrowScreen } from '../hooks/useNarrowScreen';
import { termLore } from '../utils/termLore';
import { remainingSeconds } from '../utils/time';
import { DomikSprite, WorkerSprite } from './sprites';

type RailState = 'free' | 'busy' | 'resting' | 'sick' | 'trip' | 'away';

const GROUP_LABELS: Record<Exclude<RailState, 'free'>, string> = {
    busy: 'За работой',
    resting: 'Отдыхают',
    sick: 'Хворают',
    trip: 'В пути',
    away: 'В отходе',
};

const GROUP_ORDER: Exclude<RailState, 'free'>[] = ['busy', 'resting', 'sick', 'trip', 'away'];

const GROUPS_AFTER_FREE = 1000;

const RAIL_STACK_LIMIT = 4;

function railState(worker: WorkerDto, now: number): RailState {
    if (worker.sickUntil != null && remainingSeconds(worker.sickUntil, now) > 0) {
        return 'sick';
    }
    if (worker.expeditionId != null || worker.errandId != null || worker.incidentId != null) {
        return 'trip';
    }
    if (worker.manufactureId != null) {
        return 'busy';
    }
    if (worker.isAway) {
        return 'away';
    }
    return isWorkerFree(worker, now) ? 'free' : 'resting';
}

interface WorkerRailCardProps {
    worker: WorkerDto;
    domikTypes: DomikTypeDto[];
    state: RailState;
    skillDomikTypeId: number | null;
    skillTypeName: string | null;
    order: number;
    held: boolean;
    onGrab: ((workerId: number, event: ReactPointerEvent) => void) | null;
}

const WorkerRailCard = ({ worker, domikTypes, state, skillDomikTypeId, skillTypeName, order, held, onGrab }: WorkerRailCardProps) => {
    const own = bestSkill(worker);
    const ownType = own == null ? null : domikTypes.find(type => type.id === own.domikTypeId) ?? null;
    const bonus = skillDomikTypeId != null ? workerSkillPercent(worker, skillDomikTypeId) : own?.bonusPercent ?? 0;
    const tier = bonus >= 10 ? 'high' : bonus > 0 ? 'some' : 'none';
    const speed = -worker.traitDurationPercent;
    const skillOf = skillTypeName ?? ownType?.name ?? null;

    return (
        <div className={'rail-card' + ` rail-card--${state}` + (held ? ' rail-card--held' : '')}
            style={{ order }}
            data-assign-worker={onGrab == null ? undefined : worker.id}
            role={onGrab == null ? undefined : 'button'}
            tabIndex={onGrab == null ? undefined : -1}
            title={`${worker.name} – ${worker.traitName}${skillOf == null ? '' : `, ${skillOf}: ${bonus > 0 ? '+' : ''}${bonus} %`}`}
            onPointerDown={onGrab == null ? undefined : event => { if (event.pointerType !== 'touch' || held) { onGrab(worker.id, event); } }}
            onPointerUp={onGrab == null ? undefined : event => { if (event.pointerType === 'touch' && !held) { onGrab(worker.id, event); } }}>
            <span className="rail-card-portrait">
                <WorkerSprite name={worker.name} state={state === 'free' || state === 'away' ? 'idle' : state === 'sick' ? 'sick' : state === 'resting' ? 'resting' : 'working'}
                    skilled={isSkilledWorker(worker)} aria-hidden="true" />
            </span>
            <span className="rail-card-text">
                <span className={`rail-card-skill rail-card-skill--${tier}`}>
                    {ownType != null &&
                        <DomikSprite logicName={ownType.logicName} level={1} className="rail-card-skill-ico" aria-hidden="true" />
                    }
                    <b>{bonus > 0 ? `+${bonus}` : bonus}&nbsp;%</b>
                </span>
                <span className="rail-card-name">{worker.name}</span>
            </span>
            {speed !== 0 &&
                <i className={`rail-card-trait rail-card-trait--${speed > 0 ? 'fast' : 'slow'}`} title={worker.traitName}>{speed > 0 ? `+${speed}` : speed}</i>
            }
        </div>
    );
};

interface WorkerRailProps {
    workers: WorkerDto[];
    domikTypes: DomikTypeDto[];
    now: number;
    skillDomikTypeId: number | null;
    heldWorkerId: number | null;
    onGrab: (workerId: number, event: ReactPointerEvent) => void;
    onCancel: () => void;
}

export const WorkerRail = ({ workers, domikTypes, now, skillDomikTypeId, heldWorkerId, onGrab, onCancel }: WorkerRailProps) => {
    const [openGroups, setOpenGroups] = useState<RailState[]>([]);
    const [railOpen, setRailOpen] = useState(false);
    const narrow = useNarrowScreen();
    const railRef = useRef<HTMLElement>(null);

    useElementHeightVar(railRef, '--worker-rail-height');

    const byState = useMemo(() => {
        const groups = new Map<RailState, WorkerDto[]>();
        workers.forEach(worker => {
            const state = railState(worker, now);
            groups.set(state, [...groups.get(state) ?? [], worker]);
        });
        return groups;
    }, [workers, now]);

    const held = heldWorkerId == null ? null : workers.find(worker => worker.id === heldWorkerId) ?? null;
    const free = useMemo(
        () => [...byState.get('free') ?? []].sort((a, b) => bestSkillValue(b) - bestSkillValue(a) || a.id - b.id),
        [byState],
    );
    const ranks = useMemo(() => {
        const sortType = skillDomikTypeId;
        const ordered = sortType == null
            ? free
            : [...free].sort((a, b) => workerSkillPercent(b, sortType) - workerSkillPercent(a, sortType) || a.id - b.id);
        return new Map(ordered.map((worker, index) => [worker.id, index]));
    }, [free, skillDomikTypeId]);

    const skillTypeName = skillDomikTypeId == null
        ? null
        : domikTypes.find(type => type.id === skillDomikTypeId)?.name ?? null;

    const toggleGroup = (state: RailState) =>
        { setOpenGroups(prev => prev.includes(state) ? prev.filter(item => item !== state) : [...prev, state]); };

    const expanded = !narrow || railOpen || held != null;

    const headBody = (
        <>
            <span className="worker-rail-title">Трудяги</span>
            {narrow && !expanded &&
                <span className="worker-rail-stack" aria-hidden="true">
                    {free.slice(0, RAIL_STACK_LIMIT).map(worker =>
                        <WorkerSprite key={worker.id} name={worker.name} state="idle" skilled={isSkilledWorker(worker)} />,
                    )}
                </span>
            }
            <span className="worker-rail-tally">{free.length} из {workers.length}</span>
        </>
    );

    return (
        <aside ref={railRef} className="worker-rail" aria-label="Трудяги деревни" data-open={expanded}>
            {narrow && held == null
                ? <button type="button" className="worker-rail-head worker-rail-toggle" aria-expanded={expanded}
                    onClick={() => { setRailOpen(prev => !prev); }}>
                    {headBody}
                    {expanded
                        ? <ChevronDownIcon className="worker-rail-caret" aria-hidden="true" />
                        : <ChevronUpIcon className="worker-rail-caret" aria-hidden="true" />
                    }
                </button>
                : <div className="worker-rail-head">{headBody}</div>
            }
            <p className="worker-rail-skill-of" title={skillTypeName ?? undefined}>{skillTypeName == null ? '' : `Навык: ${skillTypeName}`}</p>
            {held != null &&
                <div className="worker-rail-held">
                    <span>Приставить: <b>{held.name}</b></span>
                    <button type="button" className="btn-game btn-ghost worker-rail-cancel" onClick={onCancel}>Отмена</button>
                </div>
            }
            <div className="worker-rail-list">
                {free.length === 0 &&
                    <p className="worker-rail-empty">Все при деле – ждём, пока освободятся.</p>
                }
                {free.map(worker =>
                    <WorkerRailCard key={worker.id} worker={worker} domikTypes={domikTypes} state="free" order={ranks.get(worker.id) ?? 0}
                        skillDomikTypeId={skillDomikTypeId} skillTypeName={skillTypeName} held={heldWorkerId === worker.id} onGrab={onGrab} />,
                )}
                {GROUP_ORDER.filter(state => (byState.get(state)?.length ?? 0) > 0).map((state, index) => {
                    const group = byState.get(state) ?? [];
                    const open = openGroups.includes(state);
                    return (
                        <div key={state} className="worker-rail-group" style={{ order: GROUPS_AFTER_FREE + index }}>
                            <button type="button" className="worker-rail-group-head" aria-expanded={open}
                                onClick={() => { toggleGroup(state); }}>
                                {GROUP_LABELS[state]} {group.length}
                            </button>
                            {state === 'away' && open && <p className="worker-rail-away-hint">{termLore.worker_away}</p>}
                            {open && group.map(worker =>
                                <WorkerRailCard key={worker.id} worker={worker} domikTypes={domikTypes} state={state} order={0}
                                    skillDomikTypeId={skillDomikTypeId} skillTypeName={skillTypeName} held={false} onGrab={null} />,
                            )}
                        </div>
                    );
                })}
            </div>
        </aside>
    );
};

function bestSkillValue(worker: WorkerDto): number {
    return bestSkill(worker)?.bonusPercent ?? 0;
}

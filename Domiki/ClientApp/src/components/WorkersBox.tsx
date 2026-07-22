import { useMemo, useState } from 'react';
import type { CSSProperties } from 'react';
import { createPortal } from 'react-dom';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import CrownIcon from 'pixelarticons/svg/crown.svg?react';
import type { CloakStateDto, DomikDto, DomikIncidentDto, DomikTypeDto, ErrandDto, ExpeditionStateDto, IncidentDto, SickTypeDto, WorkerDto } from '../types/api';
import { buildDomikNamer, type DomikNamer } from '../utils/domikNames';
import { formatDuration, formatDurationShort, remainingSeconds } from '../utils/time';
import { describeWorker, describeWorkerParts, isSkilledWorker, rankedSkills } from '../utils/worker';
import { AbstractSprite, DomikSprite, MechanicSprite, TraitSprite, WorkerSprite } from './sprites';
import { genderForm, traitLabel } from '../utils/gender';

type WorkerState = 'expedition' | 'errand' | 'incidentMissing' | 'incidentSearch' | 'domikIncidentSearch' | 'busy' | 'resting' | 'free';

interface WorkersBoxProps {
    workers: WorkerDto[];
    domikTypes: DomikTypeDto[];
    domiks: DomikDto[];
    expeditions: ExpeditionStateDto | null;
    errand: ErrandDto | null;
    incident: IncidentDto | null;
    domikIncident: DomikIncidentDto | null;
    cloaks: CloakStateDto;
    sickTypes: SickTypeDto[];
    feedWorkers: boolean;
    now: number;
    onToggleFeedWorkers: (enabled: boolean) => void;
}

const stateLabels: Record<WorkerState, string> = { expedition: 'В экспедиции', errand: 'В поручении', incidentMissing: 'Задержался', incidentSearch: 'В поисках', domikIncidentSearch: 'Разбирается', busy: 'Работает', resting: 'Отдыхает', free: 'Свободен' };
const tallyLabels: Record<WorkerState, string> = { expedition: 'в пути', errand: 'в поручении', incidentMissing: 'задержались', incidentSearch: 'в поисках', domikIncidentSearch: 'разбираются', busy: 'за работой', resting: 'отдыхают', free: 'свободны' };
const tallyOrder: WorkerState[] = ['free', 'busy', 'resting', 'incidentMissing', 'incidentSearch', 'domikIncidentSearch', 'errand', 'expedition'];
const FATIGUE_THRESHOLD_SECONDS = 28800;

const WorkerDetails = ({ worker, domikTypes, domiks, namer, style }: { worker: WorkerDto; domikTypes: DomikTypeDto[]; domiks: DomikDto[]; namer: DomikNamer; style: CSSProperties }) => {
    const effect = worker.traitDurationPercent === 0 ? '' : ` ${worker.traitDurationPercent} %`;
    const visibleSkills = worker.skills.filter(skill => skill.bonusPercent > 0);
    const workplaceDomik = worker.manufactureId == null
        ? null
        : domiks.find(d => (d.manufactures ?? []).some(m => m.id === worker.manufactureId));
    const workplaceType = workplaceDomik == null ? null : domikTypes.find(t => t.id === workplaceDomik.typeId) ?? null;
    return (
        <div className="worker-details" style={style}>
            {workplaceDomik != null && workplaceType != null &&
                <span className="worker-workplace worker-detail-workplace">
                    <DomikSprite logicName={workplaceType.logicName} className="worker-workplace-ico" aria-hidden="true" />
                    {namer(workplaceType.id, workplaceDomik.id, workplaceType.name, workplaceType.logicName)}
                </span>
            }
            <span className="worker-trait">
                <TraitSprite logicName={worker.traitLogicName} size={24} className="worker-trait-ico" aria-hidden="true" />
                {traitLabel(worker.traitLogicName, worker.traitName, worker.gender)}{effect}
            </span>
            <span className="worker-desc">{describeWorker(worker, domikTypes)}</span>
            {(worker.noFatigue || visibleSkills.length > 0) &&
                <div className="worker-skills">
                    {worker.noFatigue && <span className="worker-flag"><AbstractSprite logicName="fatigue_rest" size={24} className="worker-flag-ico" aria-hidden="true" />не устаёт</span>}
                    {visibleSkills.length > 0 && <AbstractSprite logicName="worker_skill" size={24} className="worker-skill-label" aria-hidden="true" />}
                    {visibleSkills.map(skill => {
                        const domikType = domikTypes.find(x => x.id === skill.domikTypeId);
                        if (domikType == null) {
                            return null;
                        }

                        return (
                            <span
                                key={skill.domikTypeId}
                                className="worker-skill"
                                title={`${domikType.name}: +${skill.bonusPercent} % · ${skill.uses} завершённых работ`}
                            >
                                <DomikSprite logicName={domikType.logicName} className="worker-skill-ico" aria-hidden="true" />
                                +{skill.bonusPercent} %
                            </span>
                        );
                    })}
                </div>
            }
        </div>
    );
};

export const WorkersBox = ({ workers, domikTypes, domiks, expeditions, errand, incident, domikIncident, cloaks, sickTypes, feedWorkers, now, onToggleFeedWorkers }: WorkersBoxProps) => {
    const [hover, setHover] = useState<{ worker: WorkerDto; rect: DOMRect } | null>(null);
    const clearHover = (id: number) => setHover(prev => (prev?.worker.id === id ? null : prev));
    const namer = useMemo(() => buildDomikNamer(domiks), [domiks]);
    const freeCloaks = Math.max(0, cloaks.stock - cloaks.outOnShifts);
    const hasCloaks = cloaks.stock > 0 || cloaks.outOnShifts > 0 || cloaks.wearPoints > 0;

    const stateOf = (worker: WorkerDto): WorkerState => {
        if (worker.incidentId != null) {
            if (worker.id === incident?.missingWorkerId) {
                return 'incidentMissing';
            }
            if (incident?.searchWorkerIds.includes(worker.id)) {
                return 'incidentSearch';
            }
            if (domikIncident?.searchWorkerIds.includes(worker.id)) {
                return 'domikIncidentSearch';
            }
            return 'incidentSearch';
        }
        if (worker.expeditionId != null) {
            return 'expedition';
        }
        if (worker.errandId != null) {
            return 'errand';
        }
        if (worker.manufactureId != null) {
            return 'busy';
        }
        if (worker.restUntil != null && remainingSeconds(worker.restUntil, now) > 0) {
            return 'resting';
        }
        return 'free';
    };

    const tally = workers.reduce<Record<WorkerState, number>>(
        (acc, worker) => { acc[stateOf(worker)] += 1; return acc; },
        { expedition: 0, errand: 0, incidentMissing: 0, incidentSearch: 0, domikIncidentSearch: 0, busy: 0, resting: 0, free: 0 },
    );

    return (
        <section className="workers-panel pixel-panel">
            <div className="workers-head">
                <div className="workers-hero">
                    <span className="workers-hero-emblem"><MechanicSprite logicName="workers" size={40} aria-hidden="true" /></span>
                    <div className="workers-hero-text">
                        <h3 className="panel-title workers-hero-title">Трудяги</h3>
                        {workers.length > 0 &&
                            <div className="workers-tally">
                                <span className="workers-tally-total">{workers.length}</span>
                                {tallyOrder.filter(key => tally[key] > 0).map(key => (
                                    <span key={key} className={`workers-tally-item workers-tally--${key}`}>
                                        <i className="workers-tally-dot" aria-hidden="true" />{tally[key]} {tallyLabels[key]}
                                    </span>
                                ))}
                            </div>
                        }
                    </div>
                </div>
                <label className="receipt-optional workers-feed" title="Хлеб вдвое сокращает отдых">
                    <input type="checkbox" checked={feedWorkers} onChange={event => onToggleFeedWorkers(event.target.checked)} />
                    <span className="workers-feed-text">Кормить трудяг хлебом</span>
                    <span className="workers-feed-effect">вдвое сокращает отдых</span>
                </label>
                {hasCloaks &&
                    <div className="workers-cloaks" title="Плащи сами уходят на смены с погодным бонусом">
                        <b>Плащи:</b> свободно {freeCloaks} · на сменах {cloaks.outOnShifts} · износ {cloaks.wearPoints}/{cloaks.lifetimeShifts}
                    </div>
                }
            </div>
            <div className="workers-list">
                {workers.length === 0 &&
                    <span className="hint">Постройте барак, чтобы поселить трудяг.</span>
                }
                {workers.map(worker => {
                    const restingSeconds = worker.restUntil == null ? 0 : remainingSeconds(worker.restUntil, now);
                    const isSick = worker.sickUntil != null && remainingSeconds(worker.sickUntil, now) > 0;
                    const sickName = isSick ? sickTypes.find(sickType => sickType.id === worker.sickTypeId)?.name ?? 'Хворает' : '';
                    const stateKey = stateOf(worker);
                    const stateLabel = stateKey === 'free'
                        ? genderForm(worker.gender, 'Свободен', 'Свободна')
                        : stateKey === 'resting' && isSick
                            ? sickName
                            : stateLabels[stateKey];
                    const restTitle = worker.restUntil == null
                        ? undefined
                        : `${isSick ? sickName : 'Отдыхает'} до ${new Date(worker.restUntil).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} (${formatDuration(restingSeconds)})`;
                    const timer = (() => {
                        const build = (verb: string, seconds: number) =>
                            seconds > 0 ? { seconds, full: `${verb} через ${formatDuration(seconds)}` } : null;
                        if (stateKey === 'resting') {
                            return build(isSick ? 'поправится' : 'отдохнёт', restingSeconds);
                        }
                        if (stateKey === 'busy') {
                            const manufacture = domiks.flatMap(d => d.manufactures ?? []).find(m => m.id === worker.manufactureId);
                            return manufacture == null ? null : build('освободится', remainingSeconds(manufacture.finishDate, now));
                        }
                        if (stateKey === 'expedition') {
                            const expedition = expeditions?.active.find(e => e.id === worker.expeditionId);
                            return expedition == null ? null : build('вернётся', remainingSeconds(expedition.finishDate, now));
                        }
                        if (stateKey === 'errand') {
                            return errand?.finishDate == null ? null : build('вернётся', remainingSeconds(errand.finishDate, now));
                        }
                        if (stateKey === 'incidentMissing') {
                            return incident == null ? null : build('вернётся', remainingSeconds(incident.searchEndDate ?? incident.autoReturnDate, now));
                        }
                        if (stateKey === 'incidentSearch') {
                            return incident?.searchEndDate == null ? null : build('вернётся', remainingSeconds(incident.searchEndDate, now));
                        }
                        if (stateKey === 'domikIncidentSearch') {
                            return domikIncident?.searchEndDate == null ? null : build('вернётся', remainingSeconds(domikIncident.searchEndDate, now));
                        }
                        return null;
                    })();
                    const workplaceType = (() => {
                        if (stateKey !== 'busy') {
                            return null;
                        }
                        const domik = domiks.find(d => (d.manufactures ?? []).some(m => m.id === worker.manufactureId));
                        return domik == null ? null : domikTypes.find(t => t.id === domik.typeId) ?? null;
                    })();
                    const portraitState = isSick
                        ? 'sick'
                        : stateKey === 'resting'
                            ? 'resting'
                            : stateKey === 'busy' || stateKey === 'expedition' || stateKey === 'errand' || stateKey === 'incidentMissing' || stateKey === 'incidentSearch' || stateKey === 'domikIncidentSearch'
                                ? 'working'
                                : 'idle';
                    const fatigueFraction = Math.min(worker.workedSeconds / FATIGUE_THRESHOLD_SECONDS, 1);
                    const fatigueLevel = fatigueFraction >= 0.8 ? 'high' : fatigueFraction >= 0.5 ? 'mid' : 'low';
                    const craft = describeWorkerParts(worker, domikTypes);
                    const ranked = rankedSkills(worker);
                    const best = ranked[0];
                    const bestType = best == null ? undefined : domikTypes.find(t => t.id === best.domikTypeId);
                    const extra = ranked.slice(1);
                    const extraTitle = extra
                        .flatMap(skill => {
                            const type = domikTypes.find(t => t.id === skill.domikTypeId);
                            return type == null ? [] : [`${type.name}: +${skill.bonusPercent} %`];
                        })
                        .join(' · ');
                    return (
                        <article key={worker.id} className={`worker-card worker--${stateKey}`} tabIndex={0}
                            onMouseEnter={event => setHover({ worker, rect: event.currentTarget.getBoundingClientRect() })}
                            onMouseLeave={() => clearHover(worker.id)}
                            onFocus={event => setHover({ worker, rect: event.currentTarget.getBoundingClientRect() })}
                            onBlur={() => clearHover(worker.id)}>
                            <div className="worker-topline" title={stateKey === 'resting' ? restTitle : undefined}>
                                <span className="worker-badge">
                                    {stateKey === 'resting' && <AbstractSprite logicName="fatigue_rest" size={24} className="worker-badge-ico" aria-hidden="true" />}
                                    {stateLabel}
                                </span>
                                {workplaceType != null &&
                                    <span className="worker-workplace" title={`Работает в постройке «${workplaceType.name}»`}>
                                        <DomikSprite logicName={workplaceType.logicName} className="worker-workplace-ico" aria-hidden="true" />
                                        {workplaceType.name}
                                    </span>
                                }
                                {timer != null &&
                                    <span className="worker-timer" title={timer.full}>
                                        <ClockIcon className="worker-timer-ico" aria-hidden="true" />
                                        {formatDurationShort(timer.seconds)}
                                    </span>
                                }
                            </div>
                            <div className="worker-card-body">
                                <span className="worker-portrait">
                                    <WorkerSprite name={worker.name} state={portraitState} skilled={isSkilledWorker(worker)} className="worker-avatar" aria-hidden="true" />
                                    {craft.tier === 'master' && <CrownIcon className="worker-seal" aria-hidden="true" />}
                                    {!worker.noFatigue && worker.workedSeconds > 0 &&
                                        <span className="worker-fatigue" data-level={fatigueLevel}
                                            title={`Усталость: ${formatDuration(worker.workedSeconds)} из ${formatDuration(FATIGUE_THRESHOLD_SECONDS)}`}>
                                            <span className="worker-fatigue-fill" style={{ width: `${String(Math.round(fatigueFraction * 100))}%` }} />
                                        </span>
                                    }
                                </span>
                                <div className="worker-headings">
                                    <span className="worker-name">{worker.name}</span>
                                    <span className={`worker-title worker-title--${craft.tier}`}>{craft.primaryTitle}</span>
                                    <p className="worker-flavor">{craft.flavor}</p>
                                    {(best != null && bestType != null || worker.noFatigue) &&
                                        <div className="worker-card-tags">
                                            {best != null && bestType != null &&
                                                <span className="worker-skill" title={`${bestType.name}: +${best.bonusPercent} % · ${best.uses} завершённых работ`}>
                                                    <DomikSprite logicName={bestType.logicName} className="worker-skill-ico" aria-hidden="true" />
                                                    +{best.bonusPercent} %
                                                </span>
                                            }
                                            {extra.length > 0 &&
                                                <span className="worker-more" title={extraTitle}>ещё {extra.length}</span>
                                            }
                                            {worker.noFatigue &&
                                                <span className="worker-flag"><AbstractSprite logicName="fatigue_rest" size={24} className="worker-flag-ico" aria-hidden="true" />не устаёт</span>
                                            }
                                        </div>
                                    }
                                </div>
                            </div>
                        </article>
                    );
                })}
            </div>
            {hover != null && createPortal(
                <WorkerDetails worker={hover.worker} domikTypes={domikTypes} domiks={domiks} namer={namer}
                    style={{ position: 'fixed', top: hover.rect.bottom + 4, left: hover.rect.left, width: Math.max(hover.rect.width, 240) }} />,
                document.body)}
        </section>
    );
};

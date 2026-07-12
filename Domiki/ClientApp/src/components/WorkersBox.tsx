import { useState } from 'react';
import type { CSSProperties } from 'react';
import { createPortal } from 'react-dom';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import type { DomikDto, DomikTypeDto, ExpeditionStateDto, WorkerDto } from '../types/api';
import { formatDuration, formatDurationShort, remainingSeconds } from '../utils/time';
import { describeWorker, isSkilledWorker } from '../utils/worker';
import { AbstractSprite, DomikSprite, MechanicSprite, TraitSprite, WorkerSprite } from './sprites';

type WorkerState = 'expedition' | 'busy' | 'resting' | 'free';

interface WorkersBoxProps {
    workers: WorkerDto[];
    domikTypes: DomikTypeDto[];
    domiks: DomikDto[];
    expeditions: ExpeditionStateDto | null;
    feedWorkers: boolean;
    now: number;
    onToggleFeedWorkers: (enabled: boolean) => void;
}

const WorkerDetails = ({ worker, domikTypes, style }: { worker: WorkerDto; domikTypes: DomikTypeDto[]; style: CSSProperties }) => {
    const effect = worker.traitDurationPercent === 0 ? '' : ` ${worker.traitDurationPercent} %`;
    const visibleSkills = worker.skills.filter(skill => skill.bonusPercent > 0);
    return (
        <div className="worker-details" style={style}>
            <span className="worker-trait">
                <TraitSprite logicName={worker.traitLogicName} size={24} className="worker-trait-ico" aria-hidden="true" />
                {worker.traitName}{effect}
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

export const WorkersBox = ({ workers, domikTypes, domiks, expeditions, feedWorkers, now, onToggleFeedWorkers }: WorkersBoxProps) => {
    const [hover, setHover] = useState<{ worker: WorkerDto; rect: DOMRect } | null>(null);
    const clearHover = (id: number) => setHover(prev => (prev?.worker.id === id ? null : prev));

    return (
        <section className="workers-panel pixel-panel">
            <div className="workers-head">
                <h3 className="panel-title mech-title"><MechanicSprite logicName="workers" size={24} className="panel-title-ico" aria-hidden="true" />Трудяги</h3>
                <label className="receipt-optional expedition-manual-toggle" title="Хлеб вдвое сокращает отдых">
                    <input type="checkbox" checked={feedWorkers} onChange={event => onToggleFeedWorkers(event.target.checked)} />
                    Кормить трудяг хлебом
                    <span className="workers-feed-effect">хлеб вдвое сокращает отдых</span>
                </label>
            </div>
            <div className="workers-list">
                {workers.length === 0 &&
                    <span className="hint">Постройте барак, чтобы поселить трудяг.</span>
                }
                {workers.map(worker => {
                    const restingSeconds = worker.restUntil == null ? 0 : remainingSeconds(worker.restUntil, now);
                    const isResting = worker.manufactureId == null && worker.expeditionId == null && worker.restUntil != null && restingSeconds > 0;
                    const stateKey: WorkerState = worker.expeditionId != null
                        ? 'expedition'
                        : worker.manufactureId != null
                            ? 'busy'
                            : isResting
                                ? 'resting'
                                : 'free';
                    const stateLabel = { expedition: 'В экспедиции', busy: 'Работает', resting: 'Отдыхает', free: 'Свободен' }[stateKey];
                    const restTitle = worker.restUntil == null
                        ? undefined
                        : `Отдыхает до ${new Date(worker.restUntil).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} (${formatDuration(restingSeconds)})`;
                    const timer = (() => {
                        const build = (verb: string, seconds: number) =>
                            seconds > 0 ? { seconds, full: `${verb} через ${formatDuration(seconds)}` } : null;
                        if (stateKey === 'resting') {
                            return build('отдохнёт', restingSeconds);
                        }
                        if (stateKey === 'busy') {
                            const manufacture = domiks.flatMap(d => d.manufactures ?? []).find(m => m.id === worker.manufactureId);
                            return manufacture == null ? null : build('освободится', remainingSeconds(manufacture.finishDate, now));
                        }
                        if (stateKey === 'expedition') {
                            const expedition = expeditions?.active.find(e => e.id === worker.expeditionId);
                            return expedition == null ? null : build('вернётся', remainingSeconds(expedition.finishDate, now));
                        }
                        return null;
                    })();
                    const portraitState = stateKey === 'resting'
                        ? 'resting'
                        : stateKey === 'busy' || stateKey === 'expedition'
                            ? 'working'
                            : 'idle';
                    return (
                        <article key={worker.id} className={`worker-card worker--${stateKey}`} tabIndex={0}
                            onMouseEnter={event => setHover({ worker, rect: event.currentTarget.getBoundingClientRect() })}
                            onMouseLeave={() => clearHover(worker.id)}
                            onFocus={event => setHover({ worker, rect: event.currentTarget.getBoundingClientRect() })}
                            onBlur={() => clearHover(worker.id)}>
                            <div className="worker-card-face">
                                <WorkerSprite name={worker.name} state={portraitState} skilled={isSkilledWorker(worker)} className="worker-avatar" aria-hidden="true" />
                                <div className="worker-headings">
                                    <span className="worker-name">{worker.name}</span>
                                    <span className="worker-badge" title={stateKey === 'resting' ? restTitle : undefined}>
                                        {stateKey === 'resting' && <AbstractSprite logicName="fatigue_rest" size={24} className="worker-badge-ico" aria-hidden="true" />}
                                        {stateLabel}
                                    </span>
                                    {timer != null &&
                                        <span className="worker-timer" title={timer.full}>
                                            <ClockIcon className="worker-timer-ico" aria-hidden="true" />
                                            {formatDurationShort(timer.seconds)}
                                        </span>
                                    }
                                </div>
                            </div>
                        </article>
                    );
                })}
            </div>
            {hover != null && createPortal(
                <WorkerDetails worker={hover.worker} domikTypes={domikTypes}
                    style={{ position: 'fixed', top: hover.rect.bottom + 4, left: hover.rect.left, width: hover.rect.width }} />,
                document.body)}
        </section>
    );
};

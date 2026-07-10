import type { DomikDto, DomikTypeDto, ExpeditionStateDto, WorkerDto } from '../types/api';
import { formatDuration, remainingSeconds } from '../utils/time';
import { describeWorker } from '../utils/worker';
import { AbstractSprite, DomikSprite, MechanicSprite, TraitSprite, WorkerSprite } from './sprites';

type WorkerState = 'expedition' | 'busy' | 'resting' | 'free';

interface WorkersBoxProps {
    workers: WorkerDto[];
    domikTypes: DomikTypeDto[];
    domiks: DomikDto[];
    expeditions: ExpeditionStateDto | null;
    now: number;
}

export const WorkersBox = ({ workers, domikTypes, domiks, expeditions, now }: WorkersBoxProps) => {
    return (
        <section className="workers-panel pixel-panel">
            <h3 className="panel-title mech-title"><MechanicSprite logicName="workers" size={24} className="panel-title-ico" aria-hidden="true" />Трудяги</h3>
            <div className="workers-list">
                {workers.length === 0 &&
                    <span className="hint">Постройте барак, чтобы поселить трудяг.</span>
                }
                {workers.map(worker => {
                    const effect = worker.traitDurationPercent === 0
                        ? ''
                        : ` ${worker.traitDurationPercent} %`;
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
                    const visibleSkills = worker.skills.filter(skill => skill.bonusPercent > 0);
                    const freeInLabel = (() => {
                        if (stateKey === 'resting') {
                            return restingSeconds > 0 ? `отдохнёт через ${formatDuration(restingSeconds)}` : null;
                        }
                        if (stateKey === 'busy') {
                            const manufacture = domiks.flatMap(d => d.manufactures ?? []).find(m => m.id === worker.manufactureId);
                            if (manufacture == null) {
                                return null;
                            }
                            const seconds = remainingSeconds(manufacture.finishDate, now);
                            return seconds > 0 ? `освободится через ${formatDuration(seconds)}` : null;
                        }
                        if (stateKey === 'expedition') {
                            const expedition = expeditions?.active.find(e => e.id === worker.expeditionId);
                            if (expedition == null) {
                                return null;
                            }
                            const seconds = remainingSeconds(expedition.finishDate, now);
                            return seconds > 0 ? `вернётся через ${formatDuration(seconds)}` : null;
                        }
                        return null;
                    })();
                    const portraitState = stateKey === 'resting'
                        ? 'resting'
                        : stateKey === 'busy' || stateKey === 'expedition'
                            ? 'working'
                            : 'idle';
                    return (
                        <article key={worker.id} className={`worker-card worker--${stateKey}`}>
                            <div className="worker-card-head">
                                <WorkerSprite name={worker.name} state={portraitState} className="worker-avatar" aria-hidden="true" />
                                <div className="worker-headings">
                                    <span className="worker-name">{worker.name}</span>
                                    <span className="worker-badge" title={stateKey === 'resting' ? restTitle : undefined}>
                                        {stateKey === 'resting' && <AbstractSprite logicName="fatigue_rest" size={24} className="worker-badge-ico" aria-hidden="true" />}
                                        {stateLabel}
                                    </span>
                                    {freeInLabel != null && <span className="worker-free-in">{freeInLabel}</span>}
                                </div>
                            </div>
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
                        </article>
                    );
                })}
            </div>
        </section>
    );
};

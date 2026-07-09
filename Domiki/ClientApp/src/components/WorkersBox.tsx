import type { DomikTypeDto, WorkerDto } from '../types/api';
import { formatDuration, remainingSeconds } from '../utils/time';
import { DomikSprite, WorkerSprite } from './sprites';

type WorkerState = 'expedition' | 'busy' | 'resting' | 'free';

interface WorkersBoxProps {
    workers: WorkerDto[];
    domikTypes: DomikTypeDto[];
    now: number;
}

export const WorkersBox = ({ workers, domikTypes, now }: WorkersBoxProps) => {
    return (
        <section className="workers-panel pixel-panel">
            <h3 className="panel-title">Трудяги</h3>
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
                    return (
                        <article key={worker.id} className={`worker-card worker--${stateKey}`}>
                            <div className="worker-card-head">
                                <WorkerSprite name={worker.name} className="worker-avatar" aria-hidden="true" />
                                <div className="worker-headings">
                                    <span className="worker-name">{worker.name}</span>
                                    <span className="worker-badge" title={stateKey === 'resting' ? restTitle : undefined}>
                                        {stateLabel}
                                    </span>
                                </div>
                            </div>
                            <span className="worker-trait">{worker.traitName}{effect}</span>
                            {(worker.noFatigue || visibleSkills.length > 0) &&
                                <div className="worker-skills">
                                    {worker.noFatigue && <span className="worker-flag">не устаёт</span>}
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

import type { DomikTypeDto, WorkerDto } from '../types/api';
import { formatDuration, remainingSeconds } from '../utils/time';

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
                    const state = worker.expeditionId != null
                        ? 'в экспедиции'
                        : worker.manufactureId != null
                            ? 'работает'
                            : isResting
                                ? `отдыхает до ${new Date(worker.restUntil as string).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} (${formatDuration(restingSeconds)})`
                                : 'свободен';
                    const className = 'worker-chip'
                        + (worker.manufactureId == null && worker.expeditionId == null ? '' : ' worker-busy')
                        + (isResting ? ' worker-resting' : '');
                    const visibleSkills = worker.skills.filter(skill => skill.bonusPercent > 0);
                    return (
                        <div key={worker.id} className={className}>
                            <span className="worker-name">{worker.name}</span>
                            <span className="worker-trait">{worker.traitName}{effect}</span>
                            <span className="worker-state">{state}</span>
                            {worker.noFatigue &&
                                <span className="worker-effect">не устаёт</span>
                            }
                            {visibleSkills.length > 0 &&
                                <span className="worker-skills">
                                    {visibleSkills.map(skill => {
                                        const domikType = domikTypes.find(x => x.id === skill.domikTypeId);
                                        if (domikType == null) {
                                            return null;
                                        }

                                        return (
                                            <span key={skill.domikTypeId} className="worker-skill" title={`${skill.uses} завершённых работ`}>
                                                {domikType.name} +{skill.bonusPercent} %
                                            </span>
                                        );
                                    })}
                                </span>
                            }
                        </div>
                    );
                })}
            </div>
        </section>
    );
};

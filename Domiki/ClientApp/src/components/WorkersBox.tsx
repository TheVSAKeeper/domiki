import type { DomikTypeDto, WorkerDto } from '../types/api';

interface WorkersBoxProps {
    workers: WorkerDto[];
    domikTypes: DomikTypeDto[];
}

export const WorkersBox = ({ workers, domikTypes }: WorkersBoxProps) => {
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
                    const visibleSkills = worker.skills.filter(skill => skill.bonusPercent > 0);
                    return (
                        <div key={worker.id} className={'worker-chip' + (worker.manufactureId == null ? '' : ' worker-busy')}>
                            <span className="worker-name">{worker.name}</span>
                            <span className="worker-trait">{worker.traitName}{effect}</span>
                            <span className="worker-state">{worker.manufactureId == null ? 'свободен' : 'работает'}</span>
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

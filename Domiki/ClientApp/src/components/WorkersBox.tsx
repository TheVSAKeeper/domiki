import type { WorkerDto } from '../types/api';

interface WorkersBoxProps {
    workers: WorkerDto[];
}

export const WorkersBox = ({ workers }: WorkersBoxProps) => {
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
                    return (
                        <div key={worker.id} className={'worker-chip' + (worker.manufactureId == null ? '' : ' worker-busy')}>
                            <span className="worker-name">{worker.name}</span>
                            <span className="worker-trait">{worker.traitName}{effect}</span>
                            <span className="worker-state">{worker.manufactureId == null ? 'свободен' : 'работает'}</span>
                        </div>
                    );
                })}
            </div>
        </section>
    );
};

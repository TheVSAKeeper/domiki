import CheckIcon from 'pixelarticons/svg/check.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import NoteIcon from 'pixelarticons/svg/note.svg?react';
import UserIcon from 'pixelarticons/svg/user.svg?react';
import type { BlueprintDto } from '../types/api';
import { ProgressBar } from './ProgressBar';

interface BlueprintsBoxProps {
    blueprints: BlueprintDto[];
}

export const BlueprintsBox = ({ blueprints }: BlueprintsBoxProps) => {
    if (blueprints.length === 0) {
        return null;
    }

    return (
        <section className="blueprints-panel pixel-panel">
            <div className="blueprints-head">
                <h2 className="panel-title">Чертежи</h2>
                <NoteIcon className="blueprint-title-ico" aria-hidden="true" />
            </div>
            <div className="blueprints-list">
                {blueprints.map(blueprint => {
                    const progress = Math.min(blueprint.currentReputation, blueprint.reputationThreshold);
                    return (
                        <div key={blueprint.id} className={'blueprint-row' + (blueprint.owned ? ' blueprint-owned' : '')}>
                            <div className="blueprint-main">
                                <span className="blueprint-name">{blueprint.name}</span>
                                <span className="blueprint-source">
                                    <UserIcon className="stat-chip-ico" aria-hidden="true" />
                                    {blueprint.neighborName}
                                </span>
                                <ProgressBar value={progress} max={blueprint.reputationThreshold}
                                    label={blueprint.owned ? 'Открыт' : `${progress}/${blueprint.reputationThreshold}`}
                                    done={blueprint.owned} />
                            </div>
                            {blueprint.owned
                                ? <CheckIcon className="blueprint-state" aria-label="Открыт" />
                                : <LockIcon className="blueprint-state" aria-label="Закрыт" />}
                        </div>
                    );
                })}
            </div>
        </section>
    );
};

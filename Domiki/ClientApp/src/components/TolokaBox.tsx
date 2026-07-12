import { useState } from 'react';
import HandIcon from 'pixelarticons/svg/hand.svg?react';
import type { ResourceDto, ResourceTypeDto, TolokaStateDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { NumberStepper } from './NumberStepper';
import { ResourcesBox } from './ResourcesBox';
import { MechanicSprite, ResourceSprite, TolokaSprite } from './sprites';

interface TolokaBoxProps {
    toloka: TolokaStateDto | null;
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    now: number;
    onContribute: (amount: number) => Promise<void>;
}

export const TolokaBox = ({ toloka, resourceTypes, resources, now, onContribute }: TolokaBoxProps) => {
    const [amount, setAmount] = useState(10);

    if (toloka == null) {
        return null;
    }

    const active = toloka.active;
    const resourceType = resourceTypes.find(x => x.id === active.resourceTypeId);
    const cost = [{ typeId: active.resourceTypeId, value: amount }];
    const progress = Math.min(100, Math.floor(active.collected * 100 / active.goal));
    const canAfford = amount > 0 && hasResourcesFor(cost, resources);
    const buildStage = 1 + Math.floor(active.collected * 4 / active.goal);

    const submit = async () => {
        await onContribute(amount);
    };

    return (
        <section className="toloka-panel pixel-panel">
            <div className="toloka-head">
                <h3 className="panel-title mech-title"><MechanicSprite logicName="toloka" size={24} className="panel-title-ico" aria-hidden="true" />Толока</h3>
                <span className="reputation-chip" title="Длительность баффа Сходни">
                    бафф: {toloka.buffHours}ч{toloka.nextBuffHours != null ? ` → ${toloka.nextBuffHours}ч` : ''}
                </span>
                {toloka.activeBuffs.map(buff => {
                    const left = remainingSeconds(buff.buffUntil, now);
                    return left > 0 ? (
                        <span key={buff.logicName} className="reputation-chip">+{buff.percent} % {buff.label}: {formatDuration(left)}</span>
                    ) : null;
                })}
            </div>
            <div className="toloka-card">
                <TolokaSprite className="toloka-sprite" logicName={active.logicName} level={buildStage} aria-hidden="true" />
                <div className="toloka-topline">
                    <span className="toloka-name">{active.name}</span>
                    {resourceType != null &&
                        <span className="resource-box" title={resourceType.name}>
                            <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />
                            <span className="resource-value">{active.collected}/{active.goal}</span>
                        </span>
                    }
                </div>
                <progress max={100} value={progress} data-label={`${active.collected} / ${active.goal}`}></progress>
                <div className="toloka-row">
                    <span className="panel-label">мой вклад: {toloka.myContribution}</span>
                </div>
                <div className="toloka-form">
                    <NumberStepper value={amount} onChange={setAmount} />
                    <ResourcesBox resources={cost} resourceTypes={resourceTypes} have={resources} />
                    <button className="btn-game" disabled={!canAfford} title={canAfford ? undefined : 'Не хватает ресурсов'} onClick={submit}>
                        <HandIcon className="btn-ico" aria-hidden="true" />
                        Вложить
                    </button>
                </div>
            </div>
        </section>
    );
};

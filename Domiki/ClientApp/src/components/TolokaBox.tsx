import { useState } from 'react';
import BuildingCommunityIcon from 'pixelarticons/svg/building-community.svg?react';
import HandIcon from 'pixelarticons/svg/hand.svg?react';
import type { ResourceDto, ResourceTypeDto, TolokaStateDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { NumberStepper } from './NumberStepper';
import { ResourcesBox } from './ResourcesBox';
import { TolokaSprite } from './sprites';

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
    const buffLeft = toloka.buffUntil == null ? 0 : remainingSeconds(toloka.buffUntil, now);
    const buildStage = 1 + Math.floor(active.collected * 4 / active.goal);

    const submit = async () => {
        await onContribute(amount);
    };

    return (
        <section className="toloka-panel pixel-panel">
            <div className="toloka-head">
                <div className="toloka-title-row">
                    <BuildingCommunityIcon className="toloka-title-ico" aria-hidden="true" />
                    <h3 className="panel-title">Толока</h3>
                </div>
                <span className="reputation-chip" title="Длительность баффа Сходни">
                    бафф: {toloka.buffHours}ч{toloka.nextBuffHours != null ? ` → ${toloka.nextBuffHours}ч` : ''}
                </span>
                {toloka.buffActive && toloka.buffUntil != null && buffLeft > 0 &&
                    <span className="reputation-chip">+{toloka.buffPercent} % выход: {formatDuration(buffLeft)}</span>
                }
            </div>
            <div className="toloka-card">
                <TolokaSprite className="toloka-sprite" logicName={active.logicName} level={buildStage} aria-hidden="true" />
                <div className="toloka-topline">
                    <span className="toloka-name">{active.name}</span>
                    {resourceType != null &&
                        <span className="resource-box" title={resourceType.name}>
                            <img src={'/images/resourceTypes/' + resourceType.logicName + '.png'} alt={resourceType.name} />
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

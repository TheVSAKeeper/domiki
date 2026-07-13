import { useState } from 'react';
import HandIcon from 'pixelarticons/svg/hand.svg?react';
import type { ResourceDto, ResourceTypeDto, TolokaStateDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { NumberStepper } from './NumberStepper';
import { ProgressBar } from './ProgressBar';
import { ResourcesBox } from './ResourcesBox';
import { StatChip } from './StatChip';
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
    const done = active.collected >= active.goal;
    const canAfford = amount > 0 && hasResourcesFor(cost, resources);
    const buildStage = Math.min(4, 1 + Math.floor(active.collected * 4 / active.goal));

    const submit = async () => {
        await onContribute(amount);
    };

    return (
        <section className="toloka-panel pixel-panel">
            <div className="toloka-hero">
                <div className="toloka-hero-emblem">
                    <MechanicSprite logicName="toloka" size={40} aria-hidden="true" />
                </div>
                <div className="toloka-hero-text">
                    <h3 className="panel-title toloka-hero-title">Толока</h3>
                    <p className="toloka-hero-sub">Всем миром строим «{active.name}» – скидываемся в общий котёл. Достроим – по всей округе праздник, и всякая работа спорится веселее.</p>
                </div>
                <div className="toloka-hero-stat" title="Праздник толоки за твой вклад">
                    <span className="toloka-hero-stat-num">{toloka.buffHours}ч</span>
                    <span className="toloka-hero-stat-label">{toloka.nextBuffHours != null ? `вложись – ${toloka.nextBuffHours}ч` : 'праздник толоки'}</span>
                </div>
            </div>

            <div className={'toloka-site' + (done ? ' toloka-site-done' : '')}>
                <div className="toloka-scene">
                    <TolokaSprite className="toloka-sprite" logicName={active.logicName} level={buildStage} aria-hidden="true" />
                    <span className="toloka-stage">{done ? 'достроили – праздник!' : `стройка идёт · веха ${buildStage} из 4`}</span>
                </div>
                <div className="toloka-pot">
                    <div className="toloka-pot-head">
                        <span className="panel-label">общий котёл</span>
                        {resourceType != null &&
                            <span className="resource-box" title={resourceType.name}>
                                <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />
                                <span className="resource-value">{active.collected} / {active.goal}</span>
                            </span>}
                    </div>
                    <ProgressBar value={active.collected} max={active.goal} label={`${active.collected} / ${active.goal}`} done={done} />
                    <div className="toloka-pot-foot">
                        <span className="toloka-pot-hint">собрано всем миром</span>
                        <StatChip tone="gold"
                            title="Твой вклад в общий котёл"
                            icon={resourceType != null
                                ? <ResourceSprite logicName={resourceType.logicName} className="stat-chip-ico" aria-hidden="true" />
                                : null}>
                            мой вклад: {toloka.myContribution}
                        </StatChip>
                    </div>
                </div>
            </div>

            <div className="toloka-form">
                <NumberStepper value={amount} onChange={setAmount} />
                <ResourcesBox resources={cost} resourceTypes={resourceTypes} have={resources} />
                <button className="btn-game" disabled={!canAfford} title={canAfford ? undefined : 'Не хватает ресурсов'} onClick={submit}>
                    <HandIcon className="btn-ico" aria-hidden="true" />
                    Вложить
                </button>
            </div>

            {toloka.activeBuffs.length > 0 &&
                <div className="toloka-buffs">
                    {toloka.activeBuffs.map(buff => {
                        const left = remainingSeconds(buff.buffUntil, now);
                        return left > 0 ? (
                            <StatChip key={buff.logicName} tone="gold"
                                title="Идёт праздник толоки"
                                icon={<MechanicSprite logicName="toloka" className="stat-chip-ico" aria-hidden="true" />}>
                                праздник · +{buff.percent} % {buff.label} · {formatDuration(left)}
                            </StatChip>
                        ) : null;
                    })}
                </div>}
        </section>
    );
};

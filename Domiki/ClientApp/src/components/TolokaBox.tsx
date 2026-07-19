import { useState } from 'react';
import HandIcon from 'pixelarticons/svg/hand.svg?react';
import type { ResourceDto, ResourceTypeDto, TolokaPositionDto, TolokaStateDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { NumberStepper } from './NumberStepper';
import { ProgressBar } from './ProgressBar';
import { ActionButton } from './ActionButton';
import { StatChip } from './StatChip';
import { MechanicSprite, ResourceSprite, TolokaSprite } from './sprites';

interface TolokaBoxProps {
    toloka: TolokaStateDto | null;
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    now: number;
    onContribute: (resourceTypeId: number, amount: number) => Promise<void>;
    onVote: (tolokaTypeId: number) => Promise<void>;
}

export const TolokaBox = ({ toloka, resourceTypes, resources, now, onContribute, onVote }: TolokaBoxProps) => {
    if (toloka == null) {
        return null;
    }

    const active = toloka.active;
    const collected = active.positions.reduce((sum, p) => sum + p.collected, 0);
    const goal = active.positions.reduce((sum, p) => sum + p.goal, 0);
    const done = active.positions.every(p => p.collected >= p.goal);
    const buildStage = goal > 0 ? Math.min(4, 1 + Math.floor(collected * 4 / goal)) : 1;

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
                <div className="toloka-basket">
                    <span className="panel-label">общий котёл</span>
                    {active.positions.map(position => (
                        <TolokaPositionRow key={position.resourceTypeId} position={position}
                            resourceTypes={resourceTypes} resources={resources} onContribute={onContribute} />
                    ))}
                </div>
            </div>

            {toloka.candidates.length > 0 &&
                <div className="toloka-vote">
                    <span className="panel-label">за какую толоку взяться дальше</span>
                    <div className="toloka-vote-chips">
                        {toloka.candidates.map(candidate => {
                            const mine = candidate.tolokaTypeId === toloka.myVoteTolokaTypeId;
                            return (
                                <button key={candidate.tolokaTypeId} type="button"
                                    className={'toloka-vote-chip' + (mine ? ' toloka-vote-chip-mine' : '')}
                                    aria-pressed={mine}
                                    title={mine ? 'Твой голос' : `Отдать голос за «${candidate.name}»`}
                                    onClick={() => { void onVote(candidate.tolokaTypeId); }}>
                                    <TolokaSprite logicName={candidate.logicName} level={4} className="toloka-vote-ico" aria-hidden="true" />
                                    <span className="toloka-vote-name">{candidate.name}</span>
                                    <span className="toloka-vote-count" title="голосов">{candidate.votes}</span>
                                </button>
                            );
                        })}
                    </div>
                </div>}

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

interface TolokaPositionRowProps {
    position: TolokaPositionDto;
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    onContribute: (resourceTypeId: number, amount: number) => Promise<void>;
}

const TolokaPositionRow = ({ position, resourceTypes, resources, onContribute }: TolokaPositionRowProps) => {
    const remaining = position.goal - position.collected;
    const [amount, setAmount] = useState(Math.min(10, Math.max(1, remaining)));

    const resourceType = resourceTypes.find(x => x.id === position.resourceTypeId);
    const done = remaining <= 0;
    const cost = [{ typeId: position.resourceTypeId, value: amount }];
    const canAfford = !done && amount > 0 && hasResourcesFor(cost, resources);

    const submit = async () => {
        await onContribute(position.resourceTypeId, amount);
    };

    return (
        <div className={'toloka-pos' + (done ? ' toloka-pos-done' : '')}>
            <div className="toloka-pos-head">
                <span className="toloka-pos-res" title={resourceType?.name}>
                    {resourceType != null && <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />}
                    {resourceType?.name}
                </span>
                <span className="toloka-pos-val">{position.collected} / {position.goal}</span>
            </div>
            <ProgressBar value={position.collected} max={position.goal} label={`${position.collected} / ${position.goal}`} done={done} />
            <div className="toloka-pos-form">
                <StatChip tone="gold" title="Твой вклад в эту позицию"
                    icon={resourceType != null
                        ? <ResourceSprite logicName={resourceType.logicName} className="stat-chip-ico" aria-hidden="true" />
                        : null}>
                    мой вклад: {position.myContribution}
                </StatChip>
                {done
                    ? <span className="toloka-pos-done-badge">собрано</span>
                    : (
                        <>
                            <NumberStepper value={amount} onChange={setAmount} max={remaining} />
                            <ActionButton className="btn-game" disabled={!canAfford} title={canAfford ? undefined : 'Не хватает ресурсов'} onClick={submit}>
                                <HandIcon className="btn-ico" aria-hidden="true" />
                                Вложить
                            </ActionButton>
                        </>
                    )}
            </div>
        </div>
    );
};

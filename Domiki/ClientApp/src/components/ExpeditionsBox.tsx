import { useState, type ReactNode } from 'react';
import BackpackIcon from 'pixelarticons/svg/backpack.svg?react';
import MapPinIcon from 'pixelarticons/svg/map-pin.svg?react';
import GiftIcon from 'pixelarticons/svg/gift.svg?react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import CoinsIcon from 'pixelarticons/svg/coins.svg?react';
import UsersIcon from 'pixelarticons/svg/users.svg?react';
import type { DecorTypeDto, ExpeditionStateDto, ResourceDto, ResourceTypeDto, WorkerDto } from '../types/api';
import { EXPEDITION_LOOT_KIND_BLUEPRINT, EXPEDITION_LOOT_KIND_DECOR, EXPEDITION_LOOT_KIND_RESOURCE, EXPEDITION_LOOT_KIND_TRAIT_UPGRADE, GOLD_RESOURCE_TYPE_ID, hasResourcesFor, isWorkerFree } from '../utils/game';
import { isSkilledWorker } from '../utils/worker';
import { formatDuration, remainingSeconds } from '../utils/time';
import { ResourceChip } from './ResourceChip';
import { StatChip } from './StatChip';
import { ProgressBar } from './ProgressBar';
import { ActionButton } from './ActionButton';
import { AbstractSprite, DecorSprite, MechanicSprite, ResourceSprite, WorkerSprite } from './sprites';

const durationBetween = (startDate: string, finishDate: string) =>
    Math.max(1, Math.round((new Date(finishDate).getTime() - new Date(startDate).getTime()) / 1000));

interface ExpeditionsBoxProps {
    expeditions: ExpeditionStateDto | null;
    resourceTypes: ResourceTypeDto[];
    decorTypes: DecorTypeDto[];
    resources: ResourceDto[];
    workers: WorkerDto[];
    now: number;
    onStart: (expeditionTypeId: number, workerIds?: number[], provisions?: boolean) => void;
}

const EXPEDITION_EMBLEM: Record<string, string> = {
    short_scout: 'near_sortie',
    long_journey: 'long_expedition',
    foot_scout: 'walking_sortie',
};

const ExpeditionEmblem = ({ logicName, size = 40, className }: { logicName: string | undefined; size?: 24 | 32 | 40; className?: string }) => {
    const emblem = logicName == null ? undefined : EXPEDITION_EMBLEM[logicName];
    return emblem == null
        ? <BackpackIcon className={className} aria-hidden="true" />
        : <AbstractSprite logicName={emblem} size={size} className={className} aria-hidden="true" />;
};

const EXPEDITION_FLAVOR: Record<string, string> = {
    short_scout: 'Разведка у околицы – отряд вернётся к вечеру с полными котомками.',
    long_journey: 'Долгий путь за холмы: дорого и небыстро, зато добыча щедрая и редкая.',
    foot_scout: 'Быстрая вылазка налегке – без снаряжения, для одного трудяги.',
};

const EXPEDITION_TONE: Record<string, string> = {
    short_scout: 'near',
    long_journey: 'far',
    foot_scout: 'quick',
};

const RareFind = ({ icon, label, title }: { icon: ReactNode; label: string; title: string }) => (
    <span className="rare-find" title={title}>
        <span className="rare-find-ico">{icon}</span>
        <span className="rare-find-label">{label}</span>
    </span>
);

export const ExpeditionsBox = ({ expeditions, resourceTypes, decorTypes, resources, workers, now, onStart }: ExpeditionsBoxProps) => {
    const [manualMode, setManualMode] = useState<Record<number, boolean>>({});
    const [picks, setPicks] = useState<Record<number, number[]>>({});
    const [provisions, setProvisions] = useState<Record<number, boolean>>({});

    if (expeditions == null) {
        return null;
    }

    const freeWorkers = workers.filter(worker => isWorkerFree(worker, now));
    const untilPity = Math.max(0, expeditions.pityThreshold - expeditions.expeditionsSincePity);
    const goldType = resourceTypes.find(x => x.id === GOLD_RESOURCE_TYPE_ID);
    const allOut = expeditions.active.length >= expeditions.maxActive;

    const toggleManual = (typeId: number) => setManualMode(mode => ({ ...mode, [typeId]: !mode[typeId] }));
    const toggleProvisions = (typeId: number) => setProvisions(current => ({ ...current, [typeId]: !current[typeId] }));
    const toggleWorker = (typeId: number, workerId: number, max: number) => setPicks(prev => {
        const current = prev[typeId] ?? [];
        if (current.includes(workerId)) {
            return { ...prev, [typeId]: current.filter(id => id !== workerId) };
        }
        if (current.length >= max) {
            return prev;
        }
        return { ...prev, [typeId]: [...current, workerId] };
    });

    return (
        <section className="expeditions-panel pixel-panel">
            <div className="expeditions-hero">
                <div className="expeditions-hero-emblem">
                    <MechanicSprite logicName="expeditions" size={40} aria-hidden="true" />
                </div>
                <div className="expeditions-hero-text">
                    <h3 className="panel-title expeditions-hero-title">Экспедиции</h3>
                    <p className="expeditions-hero-sub">Снаряжайте отряды в дорогу – трудяги приносят ресурсы, декор и редкие находки.</p>
                </div>
                <div className="expeditions-hero-stat" title="Отрядов в походе из максимума">
                    <span className="expeditions-hero-stat-num">{expeditions.active.length}/{expeditions.maxActive}</span>
                    <span className="expeditions-hero-stat-label">отрядов в дороге</span>
                </div>
            </div>
            {expeditions.pityThreshold > 0 &&
                <div className="expeditions-trail" title="Чем дольше без редкой находки, тем ближе гарантированная редкость">
                    <span className="expeditions-trail-label">тропа<br />до находки</span>
                    <div className="expeditions-trail-track" aria-hidden="true">
                        {Array.from({ length: expeditions.pityThreshold }, (_, index) =>
                            <span key={index} className={'trail-pip' + (index < expeditions.expeditionsSincePity ? ' trail-pip-done' : '')} />)}
                        <span className="expeditions-trail-goal"><GiftIcon aria-hidden="true" /></span>
                    </div>
                    <span className="expeditions-trail-count">ещё {untilPity}</span>
                </div>}
            {expeditions.types.some(t => t.equipment.length > 0) &&
                <p className="expeditions-hint hint">Снаряжение готовят постройки-переделы (кузница, лесопилка). Нет нужного ресурса – сначала наладьте производство.</p>}
            <div className="expeditions-grid">
                {expeditions.types.map(type => {
                    const canAffordGold = hasResourcesFor([{ typeId: GOLD_RESOURCE_TYPE_ID, value: type.goldCost }], resources);
                    const equipment = type.equipment.filter(entry => !entry.isOptional);
                    const provisionEquipment = type.equipment.filter(entry => entry.isOptional);
                    const useProvisions = provisions[type.id] ?? false;
                    const equipmentReqs = equipment.map(e => ({ typeId: e.resourceTypeId, value: e.value }));
                    const canAffordEquipment = hasResourcesFor(equipmentReqs, resources);
                    const hasWorkers = freeWorkers.length >= type.workerCount;
                    const isManual = manualMode[type.id] ?? false;
                    const picked = (picks[type.id] ?? []).filter(id => freeWorkers.some(worker => worker.id === id));
                    const manualReady = picked.length === type.workerCount;
                    const canStart = !allOut && canAffordGold && canAffordEquipment && hasWorkers && (!isManual || manualReady);
                    const blockedTitle = allOut ? 'Все отряды в походе'
                        : !hasWorkers ? 'Не хватает свободных трудяг'
                            : !canAffordGold ? 'Не хватает золота'
                                : !canAffordEquipment ? 'Не хватает снаряжения'
                                    : isManual && !manualReady ? `Выберите ${type.workerCount} трудяг`
                                        : undefined;
                    const commonLoot = type.loot.filter(entry => entry.kind === EXPEDITION_LOOT_KIND_RESOURCE && !entry.isRare);
                    const rareLoot = type.loot.filter(entry => !(entry.kind === EXPEDITION_LOOT_KIND_RESOURCE && !entry.isRare));
                    return (
                        <div key={type.id} className="expedition-card">
                            <div className={'expedition-topline expedition-topline-' + (EXPEDITION_TONE[type.logicName] ?? 'near')}>
                                <span className="expedition-emblem"><ExpeditionEmblem logicName={type.logicName} /></span>
                                <span className="expedition-name">{type.name}</span>
                            </div>
                            {EXPEDITION_FLAVOR[type.logicName] != null &&
                                <p className="expedition-flavor">{EXPEDITION_FLAVOR[type.logicName]}</p>}
                            <div className="expedition-meta">
                                <StatChip icon={<ClockIcon className="stat-chip-ico" aria-hidden="true" />} title="Длительность">
                                    {formatDuration(type.durationSeconds)}
                                </StatChip>
                                <StatChip icon={<img className="stat-chip-ico" src="/images/modificatorTypes/plodder.png" alt="" />} title="Трудяг в походе">
                                    ×{type.workerCount}
                                </StatChip>
                                {goldType != null
                                    ? <ResourceChip resourceType={goldType} value={type.goldCost} />
                                    : <StatChip icon={<CoinsIcon className="stat-chip-ico" aria-hidden="true" />} tone="gold" title="Золото">{type.goldCost}</StatChip>}
                            </div>
                            {equipment.length > 0 &&
                                <div className={'expedition-req' + (canAffordEquipment ? '' : ' expedition-req-short')}>
                                    <span className="panel-label">снаряжение</span>
                                    <div className="expedition-chips">
                                        {equipment.map(entry => {
                                            const resourceType = resourceTypes.find(x => x.id === entry.resourceTypeId);
                                            if (resourceType == null) {
                                                return null;
                                            }

                                            return <ResourceChip key={entry.resourceTypeId} resourceType={resourceType} value={entry.value} />;
                                        })}
                                    </div>
                                </div>}
                            {provisionEquipment.length > 0 &&
                                <>
                                    <label className="receipt-optional expedition-manual-toggle">
                                        <input type="checkbox" checked={useProvisions} onChange={() => toggleProvisions(type.id)} />
                                        Провизия в дорогу
                                    </label>
                                    {useProvisions &&
                                        <div className="expedition-req">
                                            <span className="panel-label">провизия</span>
                                            <div className="expedition-chips">
                                                {provisionEquipment.map(entry => {
                                                    const resourceType = resourceTypes.find(x => x.id === entry.resourceTypeId);
                                                    return resourceType == null ? null : <ResourceChip key={entry.resourceTypeId} resourceType={resourceType} value={entry.value} />;
                                                })}
                                            </div>
                                        </div>}
                                </>}
                            <div className="expedition-reward">
                                <span className="panel-label">добыча</span>
                                {commonLoot.length > 0 &&
                                    <div className="loot-tier">
                                        <span className="loot-tier-label">всегда в котомке</span>
                                        <div className="loot-common-row">
                                            {commonLoot.map(entry => {
                                                const resourceType = resourceTypes.find(x => x.id === entry.resourceTypeId);
                                                return resourceType == null ? null : (
                                                    <span key={entry.resourceTypeId} className="loot-common-item" title={resourceType.name}>
                                                        <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />
                                                        <span className="loot-common-range">{entry.minValue}–{entry.maxValue}</span>
                                                    </span>
                                                );
                                            })}
                                        </div>
                                    </div>}
                                {rareLoot.length > 0 &&
                                    <div className="loot-tier loot-tier-rare">
                                        <span className="loot-tier-label loot-tier-label-rare">
                                            <AbstractSprite logicName="rare_expedition_find" size={24} aria-hidden="true" />
                                            редкие находки
                                            <span className="loot-tier-chance">· если повезёт</span>
                                        </span>
                                        <div className="rare-finds-row">
                                            {rareLoot.map((entry, index) => {
                                                if (entry.kind === EXPEDITION_LOOT_KIND_DECOR) {
                                                    const decorType = decorTypes.find(x => x.id === entry.decorTypeId);
                                                    return <RareFind key={`decor-${entry.decorTypeId}`}
                                                        icon={<DecorSprite logicName={decorType?.logicName ?? 'generic'} aria-hidden="true" />}
                                                        label={decorType?.name ?? 'Декор'} title={`Трофей в деревню: ${decorType?.name ?? 'декор'}`} />;
                                                }
                                                if (entry.kind === EXPEDITION_LOOT_KIND_TRAIT_UPGRADE) {
                                                    return <RareFind key="trait-upgrade" icon={<AbstractSprite logicName="expedition_hardening" size={32} aria-hidden="true" />}
                                                        label="Закалка похода" title="Трудяга вернётся крепче – прокачка черты" />;
                                                }
                                                if (entry.kind === EXPEDITION_LOOT_KIND_BLUEPRINT) {
                                                    return <RareFind key="blueprint" icon={<AbstractSprite logicName="blueprint" size={32} aria-hidden="true" />}
                                                        label="Чертёж" title="Случайный чертёж, которого у вас ещё нет" />;
                                                }

                                                const resourceType = resourceTypes.find(x => x.id === entry.resourceTypeId);
                                                return resourceType == null ? null : (
                                                    <RareFind key={`rare-${entry.resourceTypeId}-${index}`}
                                                        icon={<ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />}
                                                        label={`${entry.minValue}–${entry.maxValue}`} title={`Редкий ресурс: ${resourceType.name}`} />
                                                );
                                            })}
                                        </div>
                                    </div>}
                            </div>
                            <label className="receipt-optional expedition-manual-toggle">
                                <input type="checkbox" checked={isManual} disabled={freeWorkers.length === 0}
                                    onChange={() => toggleManual(type.id)} />
                                Выбрать трудяг вручную
                                {isManual && <span className="expedition-manual-count">{picked.length} / {type.workerCount}</span>}
                            </label>
                            {isManual &&
                                <div className="worker-picker">
                                    {freeWorkers.length === 0 && <span className="hint">Нет свободных трудяг</span>}
                                    {freeWorkers.map(worker => (
                                        <button key={worker.id} type="button"
                                            className={'worker-chip worker-chip-pick' + (picked.includes(worker.id) ? ' worker-chip-selected' : '')}
                                            onClick={() => toggleWorker(type.id, worker.id, type.workerCount)}>
                                            <WorkerSprite name={worker.name} skilled={isSkilledWorker(worker)} className="worker-avatar" aria-hidden="true" />
                                            <span className="worker-name">{worker.name}</span>
                                        </button>
                                    ))}
                                </div>
                            }
                            <ActionButton className="btn-game" disabled={!canStart} title={blockedTitle}
                                onClick={() => onStart(type.id, isManual ? picked : undefined, useProvisions)}>
                                <ExpeditionEmblem logicName={type.logicName} size={24} className="btn-ico" />
                                Отправить
                            </ActionButton>
                        </div>
                    );
                })}
            </div>
            {expeditions.active.length > 0 &&
                <div className="expeditions-active">
                    <div className="expeditions-active-head">
                        <MapPinIcon className="expeditions-active-ico" aria-hidden="true" />
                        <span className="panel-label expeditions-active-title">В пути</span>
                    </div>
                    <div className="expeditions-grid">
                        {expeditions.active.map(expedition => {
                            const type = expeditions.types.find(x => x.id === expedition.expeditionTypeId);
                            const crew = workers.filter(worker => worker.expeditionId === expedition.id);
                            const total = durationBetween(expedition.startDate, expedition.finishDate);
                            const left = remainingSeconds(expedition.finishDate, now);
                            return (
                                <div key={expedition.id} className="expedition-card expedition-card-active">
                                    <div className={'expedition-topline expedition-topline-' + (type == null ? 'near' : EXPEDITION_TONE[type.logicName] ?? 'near')}>
                                        <span className="expedition-emblem"><ExpeditionEmblem logicName={type?.logicName} /></span>
                                        <span className="expedition-name">{expedition.expeditionName}</span>
                                    </div>
                                    <ProgressBar value={total - left} max={total} label={formatDuration(left)} />
                                    <div className="expedition-crew">
                                        {crew.map(worker => (
                                            <span key={worker.id} className="expedition-crew-member" title={worker.name}>
                                                <WorkerSprite name={worker.name} state="working" skilled={isSkilledWorker(worker)} className="expedition-crew-face" aria-hidden="true" />
                                                <span className="worker-name">{worker.name}</span>
                                            </span>
                                        ))}
                                        {crew.length === 0 && type != null &&
                                            <StatChip icon={<UsersIcon className="stat-chip-ico" aria-hidden="true" />} title="Трудяг в походе">
                                                ×{type.workerCount}
                                            </StatChip>}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </div>
            }
        </section>
    );
};

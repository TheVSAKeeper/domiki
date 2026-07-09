import BackpackIcon from 'pixelarticons/svg/backpack.svg?react';
import MapPinIcon from 'pixelarticons/svg/map-pin.svg?react';
import FlagIcon from 'pixelarticons/svg/flag.svg?react';
import TargetIcon from 'pixelarticons/svg/target.svg?react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import CoinsIcon from 'pixelarticons/svg/coins.svg?react';
import UserIcon from 'pixelarticons/svg/user.svg?react';
import UsersIcon from 'pixelarticons/svg/users.svg?react';
import type { ExpeditionStateDto, ResourceDto, ResourceTypeDto, WorkerDto } from '../types/api';
import { GOLD_RESOURCE_TYPE_ID, hasResourcesFor, isWorkerFree } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { ResourceChip } from './ResourceChip';
import { StatChip } from './StatChip';
import { ProgressBar } from './ProgressBar';

const durationBetween = (startDate: string, finishDate: string) =>
    Math.max(1, Math.round((new Date(finishDate).getTime() - new Date(startDate).getTime()) / 1000));

interface ExpeditionsBoxProps {
    expeditions: ExpeditionStateDto | null;
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    workers: WorkerDto[];
    now: number;
    onStart: (expeditionTypeId: number) => void;
}

const EXPEDITION_ICONS: Record<string, typeof MapPinIcon> = {
    short_scout: MapPinIcon,
    long_journey: FlagIcon,
};

export const ExpeditionsBox = ({ expeditions, resourceTypes, resources, workers, now, onStart }: ExpeditionsBoxProps) => {
    if (expeditions == null) {
        return null;
    }

    const freeWorkerCount = workers.filter(worker => isWorkerFree(worker, now)).length;
    const untilPity = Math.max(0, expeditions.pityThreshold - expeditions.expeditionsSincePity);
    const goldType = resourceTypes.find(x => x.id === GOLD_RESOURCE_TYPE_ID);

    return (
        <section className="expeditions-panel pixel-panel">
            <div className="expeditions-head">
                <div className="expeditions-title-row">
                    <BackpackIcon className="expedition-title-ico" aria-hidden="true" />
                    <h3 className="panel-title">Экспедиции</h3>
                </div>
                <span className="reputation-chip" title="Экспедиций без редкой находки">
                    <TargetIcon className="pity-ico" aria-hidden="true" />
                    до находки: {untilPity}
                </span>
            </div>
            <div className="expeditions-grid">
                {expeditions.types.map(type => {
                    const TypeIcon = EXPEDITION_ICONS[type.logicName] ?? BackpackIcon;
                    const canAffordGold = hasResourcesFor([{ typeId: GOLD_RESOURCE_TYPE_ID, value: type.goldCost }], resources);
                    const hasWorkers = freeWorkerCount >= type.workerCount;
                    const canStart = canAffordGold && hasWorkers;
                    const blockedTitle = !hasWorkers ? 'Не хватает свободных трудяг' : !canAffordGold ? 'Не хватает золота' : undefined;
                    return (
                        <div key={type.id} className="expedition-card">
                            <div className="expedition-topline">
                                <TypeIcon className="expedition-card-ico" aria-hidden="true" />
                                <span className="expedition-name">{type.name}</span>
                            </div>
                            <div className="expedition-row">
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
                            <div className="expedition-loot">
                                {type.loot.map(entry => {
                                    const resourceType = resourceTypes.find(x => x.id === entry.resourceTypeId);
                                    if (resourceType == null) {
                                        return null;
                                    }

                                    return (
                                        <ResourceChip key={entry.resourceTypeId} resourceType={resourceType}
                                            min={entry.minValue} max={entry.maxValue} rare={entry.isRare} />
                                    );
                                })}
                            </div>
                            <button className="btn-game" disabled={!canStart} title={blockedTitle} onClick={() => onStart(type.id)}>
                                <TypeIcon className="btn-ico" aria-hidden="true" />
                                Отправить
                            </button>
                        </div>
                    );
                })}
            </div>
            {expeditions.active.length > 0 &&
                <div className="expeditions-active">
                    <span className="panel-label">В пути</span>
                    <div className="expeditions-grid">
                        {expeditions.active.map(expedition => {
                            const type = expeditions.types.find(x => x.id === expedition.expeditionTypeId);
                            const TypeIcon = type == null ? BackpackIcon : EXPEDITION_ICONS[type.logicName] ?? BackpackIcon;
                            const crew = workers.filter(worker => worker.expeditionId === expedition.id);
                            const total = durationBetween(expedition.startDate, expedition.finishDate);
                            const left = remainingSeconds(expedition.finishDate, now);
                            return (
                                <div key={expedition.id} className="expedition-card expedition-card-active">
                                    <div className="expedition-topline">
                                        <TypeIcon className="expedition-card-ico" aria-hidden="true" />
                                        <span className="expedition-name">{expedition.expeditionName}</span>
                                    </div>
                                    <ProgressBar value={total - left} max={total} label={formatDuration(left)} />
                                    <div className="expedition-crew">
                                        {crew.map(worker => (
                                            <StatChip key={worker.id} icon={<UserIcon className="stat-chip-ico" aria-hidden="true" />}>
                                                {worker.name}
                                            </StatChip>
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

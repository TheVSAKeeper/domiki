import BackpackIcon from 'pixelarticons/svg/backpack.svg?react';
import MapPinIcon from 'pixelarticons/svg/map-pin.svg?react';
import FlagIcon from 'pixelarticons/svg/flag.svg?react';
import TargetIcon from 'pixelarticons/svg/target.svg?react';
import type { ExpeditionStateDto, ResourceDto, ResourceTypeDto, WorkerDto } from '../types/api';
import { GOLD_RESOURCE_TYPE_ID, hasResourcesFor, isWorkerFree } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';

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
                                <span className="panel-label">{formatDuration(type.durationSeconds)}</span>
                                <span className="panel-label">{type.workerCount} трудяг</span>
                                <span className="panel-label">{type.goldCost} золота</span>
                            </div>
                            <div className="expedition-loot">
                                {type.loot.map(entry => {
                                    const resourceType = resourceTypes.find(x => x.id === entry.resourceTypeId);
                                    if (resourceType == null) {
                                        return null;
                                    }

                                    return (
                                        <span key={entry.resourceTypeId}
                                            className={'expedition-loot-chip' + (entry.isRare ? ' expedition-loot-rare' : '')}
                                            title={resourceType.name}>
                                            <img src={'/images/resourceTypes/' + resourceType.logicName + '.png'} alt={resourceType.name} />
                                            {entry.minValue}–{entry.maxValue}
                                        </span>
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
                            return (
                                <div key={expedition.id} className="expedition-card expedition-card-active">
                                    <div className="expedition-topline">
                                        <TypeIcon className="expedition-card-ico" aria-hidden="true" />
                                        <span className="expedition-name">{expedition.expeditionName}</span>
                                    </div>
                                    <div className="expedition-row">
                                        <span className="timer">{formatDuration(remainingSeconds(expedition.finishDate, now))}</span>
                                        {type != null && <span className="panel-label">{type.workerCount} трудяг</span>}
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

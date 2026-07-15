import PlusIcon from 'pixelarticons/svg/plus-box.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import type { DecorStateDto, DecorTypeDto, NeighborReputationDto, PlayerDecorDto, ResourceDto, ResourceTypeDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { ProgressBar } from './ProgressBar';
import { ResourcesBox } from './ResourcesBox';
import { ActionButton } from './ActionButton';
import { DecorSprite, MechanicSprite, NeighborSprite } from './sprites';

interface DecorBoxProps {
    decor: DecorStateDto | null;
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    reputations: NeighborReputationDto[];
    onBuy: (decorTypeId: number) => void;
}

export const DecorBox =({ decor, resourceTypes, resources, reputations, onBuy }: DecorBoxProps) => {
    if (decor == null) {
        return null;
    }

    const purchasable = decor.types.filter(x => x.isPurchasable);
    const exclusiveOwned = decor.owned
        .map(owned => ({ owned, type: decor.types.find(x => x.id === owned.decorTypeId) }))
        .filter((entry): entry is { owned: PlayerDecorDto; type: DecorTypeDto } => entry.type != null && !entry.type.isPurchasable);

    return (
        <section className="decor-panel pixel-panel">
            <div className="decor-hero">
                <div className="decor-hero-emblem">
                    <MechanicSprite logicName="decor" size={40} aria-hidden="true" />
                </div>
                <div className="decor-hero-text">
                    <h3 className="panel-title decor-hero-title">Декор</h3>
                    <p className="decor-hero-sub">Обустраивайте деревню – уют ускоряет отдых трудяг и растит обжитость.</p>
                </div>
                <div className="decor-hero-comfort">
                    <span className="decor-hero-comfort-num">{decor.comfort}</span>
                    <span className="decor-hero-comfort-label">уюта в деревне</span>
                </div>
            </div>
            <div className="decor-grid">
                {purchasable.map(type => {
                    const owned = decor.owned.find(x => x.decorTypeId === type.id)?.count ?? 0;
                    const rep = type.neighborId == null ? null : reputations.find(x => x.neighborId === type.neighborId);
                    const points = type.neighborId == null ? null : (rep?.points ?? 0);
                    const locked = points != null && points < type.reputationThreshold;
                    const canBuy = !locked && hasResourcesFor(type.cost, resources);
                    return (
                        <div key={type.id} className={'decor-card' + (locked ? ' decor-card-locked' : '') + (owned > 0 ? ' decor-card-owned' : '')}>
                            <div className="decor-shelf">
                                <span className="decor-comfort">+{type.comfortPoints} уюта</span>
                                {owned > 0 && <span className="decor-owned-tag" aria-label={`в деревне: ${owned}`}>×{owned}</span>}
                                <DecorSprite logicName={type.logicName} className="decor-card-ico" aria-hidden="true" />
                                {locked && <span className="decor-lock"><LockIcon className="decor-lock-ico" aria-label="Закрыто" /></span>}
                            </div>
                            <span className="decor-name">{type.name}</span>
                            {locked
                                ? <div className="decor-gate">
                                    <span className="decor-gate-neighbor">
                                        <NeighborSprite logicName={rep?.neighborLogicName ?? 'generic'} size={24} className="decor-gate-ico" aria-hidden="true" />
                                        {type.neighborName}
                                    </span>
                                    <ProgressBar value={points} max={type.reputationThreshold} label={`${points}/${type.reputationThreshold}`} />
                                </div>
                                : <>
                                    <ResourcesBox resources={type.cost} resourceTypes={resourceTypes} have={resources} />
                                    <ActionButton className="btn-game" disabled={!canBuy}
                                        title={canBuy ? undefined : 'Не хватает ресурсов'}
                                        onClick={() => onBuy(type.id)}>
                                        <PlusIcon className="btn-ico" aria-hidden="true" />
                                        Поставить
                                    </ActionButton>
                                </>}
                        </div>
                    );
                })}
            </div>
            {exclusiveOwned.length > 0 &&
                <div className="decor-exclusive">
                    <span className="panel-label">Трофеи экспедиций</span>
                    <div className="decor-grid">
                        {exclusiveOwned.map(({ owned, type }) => {
                            return (
                                <div key={type.id} className="decor-card decor-card-exclusive">
                                    <div className="decor-shelf decor-shelf-gold">
                                        <span className="decor-comfort">+{type.comfortPoints} уюта</span>
                                        <span className="decor-owned-tag" aria-label={`в деревне: ${owned.count}`}>×{owned.count}</span>
                                        <DecorSprite logicName={type.logicName} className="decor-card-ico" aria-hidden="true" />
                                    </div>
                                    <span className="decor-name">{type.name}</span>
                                    <span className="decor-exclusive-badge">Награда экспедиций</span>
                                </div>
                            );
                        })}
                    </div>
                </div>
            }
        </section>
    );
};

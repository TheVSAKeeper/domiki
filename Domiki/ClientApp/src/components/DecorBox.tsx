import PlusIcon from 'pixelarticons/svg/plus-box.svg?react';
import type { DecorStateDto, DecorTypeDto, NeighborReputationDto, PlayerDecorDto, ResourceDto, ResourceTypeDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { ResourcesBox } from './ResourcesBox';
import { DecorSprite, MechanicSprite } from './sprites';

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
            <div className="decor-head">
                <h3 className="panel-title mech-title"><MechanicSprite logicName="decor" size={24} className="panel-title-ico" aria-hidden="true" />Декор</h3>
                <span className="reputation-chip">Уют деревни: {decor.comfort}</span>
            </div>
            <div className="decor-grid">
                {purchasable.map(type => {
                    const owned = decor.owned.find(x => x.decorTypeId === type.id)?.count ?? 0;
                    const points = type.neighborId == null ? null : (reputations.find(x => x.neighborId === type.neighborId)?.points ?? 0);
                    const locked = type.neighborId != null && points != null && points < type.reputationThreshold;
                    const canBuy = !locked && hasResourcesFor(type.cost, resources);
                    return (
                        <div key={type.id} className="decor-card">
                            <div className="decor-topline">
                                <DecorSprite logicName={type.logicName} className="decor-card-ico" aria-hidden="true" />
                                <span className="decor-name">{type.name}</span>
                            </div>
                            <div className="decor-row">
                                <span className="decor-comfort">+{type.comfortPoints} уюта</span>
                                <span className="decor-owned">в наличии: {owned}</span>
                            </div>
                            <ResourcesBox resources={type.cost} resourceTypes={resourceTypes} have={resources} />
                            <button className="btn-game" disabled={!canBuy}
                                title={locked ? `Откроется за репутацию: ${type.neighborName ?? ''} ${points}/${type.reputationThreshold}` : canBuy ? undefined : 'Не хватает ресурсов'}
                                onClick={() => onBuy(type.id)}>
                                <PlusIcon className="btn-ico" aria-hidden="true" />
                                Поставить
                            </button>
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
                                    <div className="decor-topline">
                                        <DecorSprite logicName={type.logicName} className="decor-card-ico" aria-hidden="true" />
                                        <span className="decor-name">{type.name}</span>
                                    </div>
                                    <div className="decor-row">
                                        <span className="decor-comfort">+{type.comfortPoints} уюта</span>
                                        <span className="decor-owned">в наличии: {owned.count}</span>
                                    </div>
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

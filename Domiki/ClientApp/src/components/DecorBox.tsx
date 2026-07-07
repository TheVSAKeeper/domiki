import FlowerIcon from 'pixelarticons/svg/heart.svg?react';
import FenceIcon from 'pixelarticons/svg/grid-3x2.svg?react';
import FountainIcon from 'pixelarticons/svg/home.svg?react';
import GardenIcon from 'pixelarticons/svg/tree.svg?react';
import PlusIcon from 'pixelarticons/svg/plus-box.svg?react';
import type { DecorStateDto, ResourceDto, ResourceTypeDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { ResourcesBox } from './ResourcesBox';

interface DecorBoxProps {
    decor: DecorStateDto | null;
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    onBuy: (decorTypeId: number) => void;
}

const DECOR_ICONS: Record<string, typeof FenceIcon> = {
    fence: FenceIcon,
    flowerbed: FlowerIcon,
    garden: GardenIcon,
    fountain: FountainIcon,
};

export const DecorBox = ({ decor, resourceTypes, resources, onBuy }: DecorBoxProps) => {
    if (decor == null) {
        return null;
    }

    return (
        <section className="decor-panel pixel-panel">
            <div className="decor-head">
                <div className="decor-title-row">
                    <GardenIcon className="decor-title-ico" aria-hidden="true" />
                    <h3 className="panel-title">Декор</h3>
                </div>
                <span className="reputation-chip">Уют деревни: {decor.comfort}</span>
            </div>
            <div className="decor-grid">
                {decor.types.map(type => {
                    const TypeIcon = DECOR_ICONS[type.logicName] ?? PlusIcon;
                    const owned = decor.owned.find(x => x.decorTypeId === type.id)?.count ?? 0;
                    const canBuy = hasResourcesFor(type.cost, resources);
                    return (
                        <div key={type.id} className="decor-card">
                            <div className="decor-topline">
                                <TypeIcon className="decor-card-ico" aria-hidden="true" />
                                <span className="decor-name">{type.name}</span>
                            </div>
                            <div className="decor-row">
                                <span className="panel-label">+{type.comfortPoints} уюта</span>
                                <span className="panel-label">в наличии: {owned}</span>
                            </div>
                            <ResourcesBox resources={type.cost} resourceTypes={resourceTypes} have={resources} />
                            <button className="btn-game" disabled={!canBuy} title={canBuy ? undefined : 'Не хватает ресурсов'} onClick={() => onBuy(type.id)}>
                                <PlusIcon className="btn-ico" aria-hidden="true" />
                                Поставить
                            </button>
                        </div>
                    );
                })}
            </div>
        </section>
    );
};

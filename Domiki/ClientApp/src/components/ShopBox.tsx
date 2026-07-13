import { useMemo } from 'react';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import type { BlueprintDto, DomikTypeDto, ReceiptDto, ResourceDto, ResourceTypeDto, VillageLevelDto } from '../types/api';
import { hasResourcesFor, resourceShortfall, resourceSourceMap } from '../utils/game';
import { resourceLore } from '../utils/resourceLore';
import { DomikSprite, ResourceSprite } from './sprites';
import { ResourcesBox } from './ResourcesBox';

interface ShopBoxProps {
    purchaseDomikTypes: DomikTypeDto[];
    domikTypes: DomikTypeDto[];
    receipts: ReceiptDto[];
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    blueprints: BlueprintDto[];
    villageLevel: VillageLevelDto | null;
    onBuy: (typeId: number) => void;
    onClose: () => void;
}

export const ShopBox = ({ purchaseDomikTypes, domikTypes, receipts, resourceTypes, resources, blueprints, villageLevel, onBuy, onClose }: ShopBoxProps) => {
    const sources = useMemo(() => resourceSourceMap(domikTypes, receipts), [domikTypes, receipts]);

    return (
        <section className="shop pixel-panel" aria-label="Плотницкий двор">
            <header className="shop-head">
                <div className="shop-title">
                    <StoreIcon className="shop-title-ico" aria-hidden="true" />
                    <div>
                        <h2 className="panel-title">Плотницкий двор</h2>
                        <p className="shop-sub">Выберите, что построить в деревне</p>
                    </div>
                </div>
                <button type="button" className="shop-close" title="Закрыть" onClick={onClose}>
                    <CloseIcon className="btn-ico" aria-hidden="true" />
                </button>
            </header>
            <div className="shop-grid">
                {purchaseDomikTypes.length === 0 &&
                    <p className="hint shop-empty">Пока строить нечего – новые постройки открываются с ростом обжитости.</p>
                }
                {purchaseDomikTypes.map(domikType => {
                    const cost = domikType.levels.find(x => x.value === 1)?.resources ?? [];
                    const levelLocked = villageLevel != null && domikType.unlockLevel > villageLevel.level;
                    const blueprint = domikType.blueprintId == null ? null : blueprints.find(x => x.id === domikType.blueprintId) ?? null;
                    const blueprintLocked = blueprint != null && !blueprint.owned;
                    const countGateLocked = domikType.availableCount === 0 && domikType.nextCountGateLevel != null;
                    const isLocked = levelLocked || blueprintLocked || countGateLocked;
                    const affordable = hasResourcesFor(cost, resources);
                    const canBuild = !isLocked && affordable;
                    const missing = isLocked ? [] : resourceShortfall(cost, resources);
                    const lockTitle = levelLocked
                        ? `Откроется при обжитости ${domikType.unlockLevel}`
                        : blueprintLocked
                            ? `Нужен чертёж соседа ${blueprint.neighborName}`
                            : countGateLocked
                                ? `Ещё один – при обжитости ${domikType.nextCountGateLevel}`
                                : undefined;
                    const lockHint = blueprintLocked
                        ? `Репутация ${blueprint.reputationThreshold}: бери заказы соседа или ищи чертёж в экспедиции`
                        : null;
                    const buyTitle = isLocked
                        ? (lockHint != null ? `${lockTitle ?? ''} – ${lockHint}` : lockTitle)
                        : affordable ? undefined : 'Не хватает ресурсов';
                    const cardClass = 'shop-card'
                        + (isLocked ? ' shop-card--locked' : canBuild ? ' shop-card--ready' : ' shop-card--poor');

                    return (
                        <article key={domikType.id} className={cardClass}>
                            <span className={'shop-card-badge' + (isLocked ? ' shop-card-badge--locked' : '')}
                                title={isLocked ? lockTitle : 'Свободно построек'}>
                                {isLocked
                                    ? <LockIcon aria-hidden="true" />
                                    : <><BuildingIcon aria-hidden="true" />{domikType.availableCount}/{domikType.maxCount}</>}
                            </span>
                            <DomikSprite className="shop-card-sprite" logicName={domikType.logicName} />
                            <h3 className="shop-card-name">{domikType.name}</h3>
                            {isLocked
                                ? <div className="shop-card-locknote">
                                    <p className="shop-card-lock">{lockTitle}</p>
                                    {lockHint != null && <p className="shop-card-hint">{lockHint}</p>}
                                </div>
                                : <ResourcesBox resources={cost} resourceTypes={resourceTypes} have={resources} />}
                            {!isLocked && missing.length > 0 &&
                                <div className="shop-need">
                                    <span className="shop-need-head">
                                        <img src="/images/upgrade_no_resources.png" alt="" aria-hidden="true" />
                                        Не хватает – где взять
                                    </span>
                                    {missing.map(item => {
                                        const resourceType = resourceTypes.find(x => x.id === item.typeId);
                                        if (resourceType == null) {
                                            return null;
                                        }

                                        const from = sources.get(item.typeId) ?? [];
                                        const fallback = resourceLore[resourceType.logicName]?.source ?? 'из экспедиций или на ярмарке';
                                        return (
                                            <div key={item.typeId} className="shop-need-row">
                                                <ResourceSprite logicName={resourceType.logicName} className="shop-need-ico" aria-hidden="true" />
                                                <span className="shop-need-name">{resourceType.name}</span>
                                                <span className="shop-need-qty">×{item.value}</span>
                                                <span className="shop-need-arrow" aria-hidden="true">→</span>
                                                {from.length > 0
                                                    ? <span className="shop-need-where">
                                                        {from.slice(0, 2).map(source =>
                                                            <span key={source.logicName} className="shop-need-src">
                                                                <DomikSprite logicName={source.logicName} className="shop-need-src-ico" />
                                                                {source.name}
                                                            </span>)}
                                                    </span>
                                                    : <span className="shop-need-where shop-need-where-text">{fallback}</span>}
                                            </div>
                                        );
                                    })}
                                </div>}
                            <button type="button" className="btn-game shop-card-buy"
                                disabled={!canBuild} title={buyTitle} onClick={() => onBuy(domikType.id)}>
                                {isLocked
                                    ? <LockIcon className="btn-ico" aria-hidden="true" />
                                    : <BuildingIcon className="btn-ico" aria-hidden="true" />}
                                Построить
                            </button>
                        </article>
                    );
                })}
            </div>
        </section>
    );
};

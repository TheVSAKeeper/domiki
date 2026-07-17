import { useLayoutEffect, useMemo, useRef } from 'react';
import BackpackIcon from 'pixelarticons/svg/backpack.svg?react';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import BuildingCommunityIcon from 'pixelarticons/svg/building-community.svg?react';
import GridIcon from 'pixelarticons/svg/grid-3x3.svg?react';
import GiftIcon from 'pixelarticons/svg/gift.svg?react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import type { DecorTypeDto, DomikTypeDto, ExpeditionTypeDto, NeighborReputationDto, ResourceTypeDto, TolokaStateDto } from '../types/api';
import type { RecapView } from '../utils/recap';
import { lootEntryKey } from '../utils/recap';
import { EXPEDITION_LOOT_KIND_DECOR, EXPEDITION_LOOT_KIND_TRAIT_UPGRADE } from '../utils/game';
import { withStableKeys } from '../utils/keys';
import { formatDuration } from '../utils/time';
import { pluralRu } from '../utils/plural';
import { genderForm, traitLabel } from '../utils/gender';
import { pickGiftText } from '../utils/giftTexts';
import { DomikSprite } from './sprites';
import { ResourceChip } from './ResourceChip';
import { GiftVisitDots } from './GiftVisitDots';

interface RecapModalProps {
    awaySeconds: number;
    view: RecapView;
    resourceTypes: ResourceTypeDto[];
    domikTypes: DomikTypeDto[];
    decorTypes: DecorTypeDto[];
    expeditionTypes: ExpeditionTypeDto[];
    neighbors: NeighborReputationDto[];
    toloka: TolokaStateDto | null;
    onClose: () => void;
}

export const RecapModal = ({ awaySeconds, view, resourceTypes, domikTypes, decorTypes, expeditionTypes, neighbors, toloka, onClose }: RecapModalProps) => {
    const dialogRef = useRef<HTMLDialogElement>(null);

    useLayoutEffect(() => {
        const dialog = dialogRef.current;
        dialog?.showModal();
        return () => { dialog?.close(); };
    }, []);

    const expeditions = withStableKeys(view.expeditions, e => String(e.expeditionTypeId));
    const market = withStableKeys(view.market, e => `${e.kind}-${e.give.typeId}`);
    const upgrades = withStableKeys(view.upgrades, e => `${e.domikTypeId}-${e.level}`);
    const tolokaEntries = withStableKeys(view.toloka, e => String(e.tolokaTypeId));
    const gifts = withStableKeys(view.gifts, gift => `${gift.neighborId}-${gift.date}`);

    const trophies = useMemo(() => {
        const producedTotal = view.produced.reduce((sum, resource) => sum + resource.value, 0);
        return [
            { key: 'prod', Icon: GridIcon, num: producedTotal, cap: pluralRu(producedTotal, 'ресурс', 'ресурса', 'ресурсов') },
            { key: 'build', Icon: BuildingIcon, num: view.upgrades.length, cap: pluralRu(view.upgrades.length, 'постройка', 'постройки', 'построек') },
            { key: 'exp', Icon: BackpackIcon, num: view.expeditions.length, cap: pluralRu(view.expeditions.length, 'экспедиция', 'экспедиции', 'экспедиций') },
            { key: 'market', Icon: StoreIcon, num: view.market.length, cap: pluralRu(view.market.length, 'сделка', 'сделки', 'сделок') },
            { key: 'toloka', Icon: BuildingCommunityIcon, num: view.toloka.length, cap: pluralRu(view.toloka.length, 'толока', 'толоки', 'толок') },
            { key: 'gift', Icon: GiftIcon, num: view.gifts.length, cap: pluralRu(view.gifts.length, 'гостинец', 'гостинца', 'гостинцев') },
        ].filter(trophy => trophy.num > 0);
    }, [view]);

    return (
        <dialog ref={dialogRef} className="recap-modal pixel-panel" aria-label="Пока вас не было" onClose={onClose}>
            <div className="recap-hero">
                <div className="recap-ribbon" aria-hidden="true">
                    {Array.from({ length: 16 }, (_, index) => <i key={index} />)}
                </div>
                <div className="recap-hero-head">
                    <div>
                        <h2 className="recap-title">С возвращением!</h2>
                        <span className="recap-since">Вас не было {formatDuration(awaySeconds)}</span>
                    </div>
                    <button type="button" className="recap-close" title="Закрыть" onClick={onClose}>
                        <CloseIcon aria-hidden="true" />
                    </button>
                </div>
                <p className="recap-subtitle">Деревня трудилась без устали – вот что успели ваши домовята.</p>
            </div>
            {trophies.length > 0 &&
                <div className="recap-trophies">
                    {trophies.map(trophy => (
                        <span key={trophy.key} className="recap-trophy" data-tone={trophy.key}>
                            <span className="tr-ico"><trophy.Icon aria-hidden="true" /></span>
                            <span className="tr-body">
                                <span className="tr-num">{trophy.num}</span>
                                <span className="tr-cap">{trophy.cap}</span>
                            </span>
                        </span>
                    ))}
                </div>
            }
            {gifts.length > 0 &&
                <div className="recap-section" data-tone="gift">
                    <div className="recap-section-head">
                        <span className="recap-section-badge"><GiftIcon aria-hidden="true" /></span>
                        <h3 className="recap-section-title">Гостинец от соседей</h3>
                        <span className="recap-section-count">{gifts.length}</span>
                    </div>
                    {gifts.map(({ key, item: gift }) => {
                        const neighbor = neighbors.find(item => item.neighborId === gift.neighborId);
                        const neighborName = neighbor?.neighborName ?? `Сосед #${gift.neighborId}`;
                        const decorName = gift.decorTypeId == null ? 'Декор не указан' : decorTypes.find(decorType => decorType.id === gift.decorTypeId)?.name ?? `Декор #${gift.decorTypeId}`;
                        return (
                            <div key={key} className="recap-row gift-row">
                                <div className="gift-head">
                                    <GiftIcon className="recap-row-ico" aria-hidden="true" />
                                    <span className={neighbor == null ? 'recap-fallback' : 'recap-line'}>{neighborName}</span>
                                </div>
                                <p className="gift-note">{pickGiftText(gift.neighborId, gift.big, gift.date)}</p>
                                {gift.big
                                    ? <span className="gift-decor">{decorName}</span>
                                    : <div className="recap-chips">
                                        {withStableKeys(gift.resources, resource => String(resource.resourceTypeId)).map(({ key: resourceKey, item: resource }) => {
                                            const type = resourceTypes.find(resourceType => resourceType.id === resource.resourceTypeId);
                                            return type == null
                                                ? <span key={resourceKey} className="recap-fallback">Ресурс #{resource.resourceTypeId} ×{resource.value}</span>
                                                : <ResourceChip key={resourceKey} resourceType={type} value={resource.value} />;
                                        })}
                                    </div>
                                }
                                <div className="gift-visits">
                                    <GiftVisitDots visitIndex={gift.visitIndex} big={gift.big} />
                                    <span className="gift-visit-label">{gift.big ? 'Большой гостинец!' : 'Большой гостинец – каждый 7-й визит'}</span>
                                </div>
                            </div>
                        );
                    })}
                </div>
            }
            {expeditions.length > 0 &&
                <div className="recap-section" data-tone="exp">
                    <div className="recap-section-head">
                        <span className="recap-section-badge"><BackpackIcon aria-hidden="true" /></span>
                        <h3 className="recap-section-title">Экспедиции</h3>
                        <span className="recap-section-count">{expeditions.length}</span>
                    </div>
                    {expeditions.map(({ key, item: event }) => {
                        const name = expeditionTypes.find(type => type.id === event.expeditionTypeId)?.name ?? `Экспедиция #${event.expeditionTypeId}`;
                        return (
                            <div key={key} className="recap-row">
                                <BackpackIcon className="recap-row-ico" aria-hidden="true" />
                                <span className="recap-line">{name}</span>
                                <div className="recap-chips">
                                    {withStableKeys(event.loot, lootEntryKey).map(({ key: lootKey, item: loot }) => {
                                        if (loot.kind === EXPEDITION_LOOT_KIND_DECOR) {
                                            const decorType = decorTypes.find(x => x.id === loot.decorTypeId);
                                            return <span key={lootKey} className="recap-fallback">Нашли {decorType?.name ?? 'декор'}</span>;
                                        }
                                        if (loot.kind === EXPEDITION_LOOT_KIND_TRAIT_UPGRADE) {
                                            return <span key={lootKey} className="recap-fallback">{loot.workerName} {genderForm(loot.workerGender, 'закалился', 'закалилась')}: {traitLabel(loot.newTraitLogicName ?? '', loot.newTrait ?? '', loot.workerGender)}</span>;
                                        }
                                        const type = resourceTypes.find(resourceType => resourceType.id === loot.typeId);
                                        return type == null
                                            ? <span key={lootKey} className="recap-fallback">Ресурс #{loot.typeId} ×{loot.value}</span>
                                            : <ResourceChip key={lootKey} resourceType={type} value={loot.value ?? 0} rare={loot.isRare} />;
                                    })}
                                </div>
                            </div>
                        );
                    })}
                </div>
            }
            {market.length > 0 &&
                <div className="recap-section" data-tone="market">
                    <div className="recap-section-head">
                        <span className="recap-section-badge"><StoreIcon aria-hidden="true" /></span>
                        <h3 className="recap-section-title">Ярмарка</h3>
                        <span className="recap-section-count">{market.length}</span>
                    </div>
                    {market.map(({ key, item: event }) => {
                        const give = resourceTypes.find(type => type.id === event.give.typeId);
                        const want = event.want == null ? null : resourceTypes.find(type => type.id === event.want?.typeId);
                        return (
                            <div key={key} className="recap-row">
                                <StoreIcon className="recap-row-ico" aria-hidden="true" />
                                <span className="recap-line">{event.kind === 'sold' ? 'Продано' : 'Лот истёк –'}</span>
                                {give == null ? <span className="recap-fallback">Ресурс #{event.give.typeId} ×{event.give.value}</span> : <ResourceChip resourceType={give} value={event.give.value} />}
                                {event.kind === 'sold' && event.want != null &&
                                    <>
                                        <span className="recap-arrow">→ получено</span>
                                        {want == null ? <span className="recap-fallback">Ресурс #{event.want.typeId} ×{event.want.value}</span> : <ResourceChip resourceType={want} value={event.want.value} />}
                                    </>
                                }
                                {event.kind === 'expired' && <span className="recap-line">возвращён</span>}
                            </div>
                        );
                    })}
                </div>
            }
            {view.produced.length > 0 &&
                <div className="recap-section" data-tone="prod">
                    <div className="recap-section-head">
                        <span className="recap-section-badge"><GridIcon aria-hidden="true" /></span>
                        <h3 className="recap-section-title">Произведено</h3>
                        <span className="recap-section-count">{view.produced.length}</span>
                    </div>
                    <div className="recap-chips">
                        {view.produced.map(resource => {
                            const type = resourceTypes.find(resourceType => resourceType.id === resource.typeId);
                            return type == null
                                ? <span key={resource.typeId} className="recap-fallback">Ресурс #{resource.typeId} ×{resource.value}</span>
                                : <ResourceChip key={resource.typeId} resourceType={type} value={resource.value} />;
                        })}
                    </div>
                </div>
            }
            {upgrades.length > 0 &&
                <div className="recap-section" data-tone="build">
                    <div className="recap-section-head">
                        <span className="recap-section-badge"><BuildingIcon aria-hidden="true" /></span>
                        <h3 className="recap-section-title">Постройки улучшены</h3>
                        <span className="recap-section-count">{upgrades.length}</span>
                    </div>
                    {upgrades.map(({ key, item: event }) => {
                        const type = domikTypes.find(domikType => domikType.id === event.domikTypeId);
                        return (
                            <div key={key} className="recap-row">
                                {type != null && <DomikSprite className="recap-domik-sprite" logicName={type.logicName} level={event.level} />}
                                <span className="recap-line">{type?.name ?? `Постройка #${event.domikTypeId}`} → ур. {event.level}</span>
                            </div>
                        );
                    })}
                </div>
            }
            {tolokaEntries.length > 0 &&
                <div className="recap-section" data-tone="toloka">
                    <div className="recap-section-head">
                        <span className="recap-section-badge"><BuildingCommunityIcon aria-hidden="true" /></span>
                        <h3 className="recap-section-title">Толока завершена</h3>
                        <span className="recap-section-count">{tolokaEntries.length}</span>
                    </div>
                    {tolokaEntries.map(({ key, item: event }) => {
                        const name = toloka?.active.tolokaTypeId === event.tolokaTypeId ? toloka.active.name : `Толока #${event.tolokaTypeId}`;
                        return <span key={key} className="recap-line">{name}</span>;
                    })}
                </div>
            }
        </dialog>
    );
};

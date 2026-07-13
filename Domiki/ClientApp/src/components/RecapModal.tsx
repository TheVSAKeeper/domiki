import { useMemo } from 'react';
import BackpackIcon from 'pixelarticons/svg/backpack.svg?react';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import BuildingCommunityIcon from 'pixelarticons/svg/building-community.svg?react';
import GridIcon from 'pixelarticons/svg/grid-3x3.svg?react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import type { DecorTypeDto, DomikTypeDto, ExpeditionTypeDto, ResourceTypeDto, TolokaStateDto } from '../types/api';
import type { RecapView } from '../utils/recap';
import { EXPEDITION_LOOT_KIND_DECOR, EXPEDITION_LOOT_KIND_TRAIT_UPGRADE } from '../utils/game';
import { formatDuration } from '../utils/time';
import { DomikSprite } from './sprites';
import { ResourceChip } from './ResourceChip';

interface RecapModalProps {
    awaySeconds: number;
    view: RecapView;
    resourceTypes: ResourceTypeDto[];
    domikTypes: DomikTypeDto[];
    decorTypes: DecorTypeDto[];
    expeditionTypes: ExpeditionTypeDto[];
    toloka: TolokaStateDto | null;
    onClose: () => void;
}

const pluralRu = (n: number, one: string, few: string, many: string) => {
    const mod10 = n % 10;
    const mod100 = n % 100;
    if (mod10 === 1 && mod100 !== 11) {
        return one;
    }
    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) {
        return few;
    }
    return many;
};

export const RecapModal = ({ awaySeconds, view, resourceTypes, domikTypes, decorTypes, expeditionTypes, toloka, onClose }: RecapModalProps) => {
    const trophies = useMemo(() => {
        const producedTotal = view.produced.reduce((sum, resource) => sum + resource.value, 0);
        return [
            { key: 'prod', Icon: GridIcon, num: producedTotal, cap: pluralRu(producedTotal, 'ресурс', 'ресурса', 'ресурсов') },
            { key: 'build', Icon: BuildingIcon, num: view.upgrades.length, cap: pluralRu(view.upgrades.length, 'постройка', 'постройки', 'построек') },
            { key: 'exp', Icon: BackpackIcon, num: view.expeditions.length, cap: pluralRu(view.expeditions.length, 'экспедиция', 'экспедиции', 'экспедиций') },
            { key: 'market', Icon: StoreIcon, num: view.market.length, cap: pluralRu(view.market.length, 'сделка', 'сделки', 'сделок') },
            { key: 'toloka', Icon: BuildingCommunityIcon, num: view.toloka.length, cap: pluralRu(view.toloka.length, 'толока', 'толоки', 'толок') },
        ].filter(trophy => trophy.num > 0);
    }, [view]);

    return (
        <div className="modal-backdrop" role="presentation">
            <section className="recap-modal pixel-panel" role="dialog" aria-modal="true" aria-label="Пока вас не было">
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
                {view.expeditions.length > 0 &&
                    <div className="recap-section" data-tone="exp">
                        <div className="recap-section-head">
                            <span className="recap-section-badge"><BackpackIcon aria-hidden="true" /></span>
                            <h3 className="recap-section-title">Экспедиции</h3>
                            <span className="recap-section-count">{view.expeditions.length}</span>
                        </div>
                        {view.expeditions.map((event, index) => {
                            const name = expeditionTypes.find(type => type.id === event.expeditionTypeId)?.name ?? `Экспедиция #${event.expeditionTypeId}`;
                            return (
                                <div key={`${event.expeditionTypeId}-${index}`} className="recap-row">
                                    <BackpackIcon className="recap-row-ico" aria-hidden="true" />
                                    <span className="recap-line">{name}</span>
                                    <div className="recap-chips">
                                        {event.loot.map((loot, lootIndex) => {
                                            if (loot.kind === EXPEDITION_LOOT_KIND_DECOR) {
                                                const decorType = decorTypes.find(x => x.id === loot.decorTypeId);
                                                return <span key={lootIndex} className="recap-fallback">Нашли {decorType?.name ?? 'декор'}</span>;
                                            }
                                            if (loot.kind === EXPEDITION_LOOT_KIND_TRAIT_UPGRADE) {
                                                return <span key={lootIndex} className="recap-fallback">{loot.workerName} закалился: {loot.newTrait}</span>;
                                            }
                                            const type = resourceTypes.find(resourceType => resourceType.id === loot.typeId);
                                            return type == null
                                                ? <span key={lootIndex} className="recap-fallback">Ресурс #{loot.typeId} ×{loot.value}</span>
                                                : <ResourceChip key={lootIndex} resourceType={type} value={loot.value ?? 0} rare={loot.isRare} />;
                                        })}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                }
                {view.market.length > 0 &&
                    <div className="recap-section" data-tone="market">
                        <div className="recap-section-head">
                            <span className="recap-section-badge"><StoreIcon aria-hidden="true" /></span>
                            <h3 className="recap-section-title">Ярмарка</h3>
                            <span className="recap-section-count">{view.market.length}</span>
                        </div>
                        {view.market.map((event, index) => {
                            const give = resourceTypes.find(type => type.id === event.give.typeId);
                            const want = event.want == null ? null : resourceTypes.find(type => type.id === event.want?.typeId);
                            return (
                                <div key={`${event.kind}-${index}`} className="recap-row">
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
                {view.upgrades.length > 0 &&
                    <div className="recap-section" data-tone="build">
                        <div className="recap-section-head">
                            <span className="recap-section-badge"><BuildingIcon aria-hidden="true" /></span>
                            <h3 className="recap-section-title">Постройки улучшены</h3>
                            <span className="recap-section-count">{view.upgrades.length}</span>
                        </div>
                        {view.upgrades.map((event, index) => {
                            const type = domikTypes.find(domikType => domikType.id === event.domikTypeId);
                            return (
                                <div key={`${event.domikTypeId}-${index}`} className="recap-row">
                                    {type != null && <DomikSprite className="recap-domik-sprite" logicName={type.logicName} level={event.level} />}
                                    <span className="recap-line">{type?.name ?? `Постройка #${event.domikTypeId}`} → ур. {event.level}</span>
                                </div>
                            );
                        })}
                    </div>
                }
                {view.toloka.length > 0 &&
                    <div className="recap-section" data-tone="toloka">
                        <div className="recap-section-head">
                            <span className="recap-section-badge"><BuildingCommunityIcon aria-hidden="true" /></span>
                            <h3 className="recap-section-title">Толока завершена</h3>
                            <span className="recap-section-count">{view.toloka.length}</span>
                        </div>
                        {view.toloka.map((event, index) => {
                            const name = toloka?.active.tolokaTypeId === event.tolokaTypeId ? toloka.active.name : `Толока #${event.tolokaTypeId}`;
                            return <span key={`${event.tolokaTypeId}-${index}`} className="recap-line">{name}</span>;
                        })}
                    </div>
                }
            </section>
        </div>
    );
};

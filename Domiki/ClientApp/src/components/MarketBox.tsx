import { useMemo, useState } from 'react';
import ArrowsIcon from 'pixelarticons/svg/arrows-horizontal.svg?react';
import CoinsIcon from 'pixelarticons/svg/coins.svg?react';
import HandIcon from 'pixelarticons/svg/hand.svg?react';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import TrashIcon from 'pixelarticons/svg/trash.svg?react';
import type { MarketStateDto, ResourceDto, ResourceTypeDto, TradeLotDto } from '../types/api';
import { DEFAULT_VILLAGE_ICON, VILLAGE_CREST_COLORS, VILLAGE_CREST_ICONS } from '../constants/village';
import { MechanicSprite, ResourceSprite } from './sprites';
import { hasResourcesFor } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { NumberStepper } from './NumberStepper';
import { ResourcesBox } from './ResourcesBox';

interface MarketBoxProps {
    market: MarketStateDto | null;
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    now: number;
    onPost: (giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number) => Promise<void>;
    onAccept: (lotId: number) => Promise<void>;
    onCancel: (lotId: number) => Promise<void>;
}

const mergeResources = (resources: ResourceDto[]): ResourceDto[] => {
    const values = new Map<number, number>();
    resources.forEach(resource => values.set(resource.typeId, (values.get(resource.typeId) ?? 0) + resource.value));
    return [...values].map(([typeId, value]) => ({ typeId, value }));
};

const getResourceName = (resourceTypes: ResourceTypeDto[], resourceTypeId: number) =>
    resourceTypes.find(x => x.id === resourceTypeId)?.name ?? 'Ресурс';

const formatPercent = (rate: number) => (rate * 100).toLocaleString('ru-RU', { maximumFractionDigits: 1 }) + '%';

const LotResources = ({ lot, resourceTypes }: { lot: TradeLotDto; resourceTypes: ResourceTypeDto[] }) => (
    <div className="market-lot-resources">
        <div className="market-side">
            <span className="panel-label">даёт</span>
            <ResourcesBox resources={[{ typeId: lot.giveResourceTypeId, value: lot.giveValue }]} resourceTypes={resourceTypes} />
        </div>
        <ArrowsIcon className="market-arrow" aria-hidden="true" />
        <div className="market-side">
            <span className="panel-label">хочет</span>
            <ResourcesBox resources={[{ typeId: lot.wantResourceTypeId, value: lot.wantValue }]} resourceTypes={resourceTypes} />
        </div>
    </div>
);

const SellerBadge = ({ lot }: { lot: TradeLotDto }) => {
    const CrestIcon = VILLAGE_CREST_ICONS[lot.sellerCrestIcon] ?? DEFAULT_VILLAGE_ICON;
    const crestColor = VILLAGE_CREST_COLORS[lot.sellerCrestColor] ?? VILLAGE_CREST_COLORS[0];
    return (
        <div className="market-seller">
            <span className="crest-badge crest-badge-small" style={{ backgroundColor: crestColor }}>
                <CrestIcon className="crest-ico" aria-hidden="true" />
            </span>
            <span className="market-village-name">{lot.sellerVillageName ?? 'Безымянная деревня'}</span>
        </div>
    );
};

const ResourcePicker = ({ resourceTypes, selectedId, onSelect, label }: { resourceTypes: ResourceTypeDto[]; selectedId: number; onSelect: (id: number) => void; label: string }) => (
    <div className="resource-picker" role="radiogroup" aria-label={label}>
        {resourceTypes.map(type => (
            <button key={type.id} type="button" role="radio" aria-checked={type.id === selectedId}
                className={'resource-option' + (type.id === selectedId ? ' resource-option-selected' : '')}
                title={type.name} onClick={() => onSelect(type.id)}>
                <ResourceSprite logicName={type.logicName} aria-hidden="true" />
            </button>
        ))}
    </div>
);

export const MarketBox = ({ market, resourceTypes, resources, now, onPost, onAccept, onCancel }: MarketBoxProps) => {
    const firstTypeId = resourceTypes[0]?.id ?? 1;
    const secondTypeId = resourceTypes.find(x => x.id !== firstTypeId)?.id ?? firstTypeId;
    const [giveResourceTypeId, setGiveResourceTypeId] = useState(firstTypeId);
    const [wantResourceTypeId, setWantResourceTypeId] = useState(secondTypeId);
    const [giveValue, setGiveValue] = useState(10);
    const [wantValue, setWantValue] = useState(1);

    const commissionFee = useMemo(() => {
        if (market == null) {
            return 0;
        }

        const marketValue = resourceTypes.find(x => x.id === giveResourceTypeId)?.marketValue ?? 0;
        return Math.max(market.commissionMin, Math.round(marketValue * giveValue * market.commissionRate));
    }, [giveResourceTypeId, giveValue, market, resourceTypes]);

    const postCost = useMemo(() => market == null ? [] : mergeResources([
        { typeId: giveResourceTypeId, value: giveValue },
        { typeId: 1, value: commissionFee },
    ]), [commissionFee, giveResourceTypeId, giveValue, market]);

    if (market == null) {
        return null;
    }

    const invalidPair = giveResourceTypeId === wantResourceTypeId;
    const lotsFull = market.myLots.length >= market.maxLots;
    const canAffordPost = giveValue > 0 && wantValue > 0 && !invalidPair && hasResourcesFor(postCost, resources);
    const canPost = canAffordPost && !lotsFull;

    const submitPost = async () => {
        await onPost(giveResourceTypeId, giveValue, wantResourceTypeId, wantValue);
    };

    return (
        <section className="market-panel pixel-panel">
            <div className="market-head">
                <h3 className="panel-title mech-title"><MechanicSprite logicName="market" size={24} className="panel-title-ico" aria-hidden="true" />Ярмарка</h3>
                <span className="reputation-chip" title="Занято мест на прилавке из максимума">
                    мест на прилавке: {market.myLots.length}/{market.maxLots}
                </span>
                <span className="reputation-chip commission-chip" title="Ставка зависит от уровня Торгового двора – качайте, чтобы платить меньше">
                    Комиссия – {formatPercent(market.commissionRate)}
                    {market.nextCommissionRate != null && (
                        <span className="chip-sub"> · ур.{market.buildingLevel + 1} → {formatPercent(market.nextCommissionRate)}</span>
                    )}
                </span>
            </div>
            <div className="market-layout">
                <form className="market-card market-form" onSubmit={event => { event.preventDefault(); void submitPost(); }}>
                    <div className="market-card-title">
                        <ArrowsIcon className="btn-ico" aria-hidden="true" />
                        Выставить лот
                    </div>
                    <div className="market-field">
                        <span className="panel-label">даю</span>
                        <ResourcePicker resourceTypes={resourceTypes} selectedId={giveResourceTypeId} onSelect={setGiveResourceTypeId} label="Ресурс, который даю" />
                        <NumberStepper value={giveValue} onChange={setGiveValue} />
                    </div>
                    <div className="market-field">
                        <span className="panel-label">хочу</span>
                        <ResourcePicker resourceTypes={resourceTypes} selectedId={wantResourceTypeId} onSelect={setWantResourceTypeId} label="Ресурс, который хочу" />
                        <NumberStepper value={wantValue} onChange={setWantValue} />
                    </div>
                    <div className="market-commission">
                        <CoinsIcon className="btn-ico" aria-hidden="true" />
                        Комиссия за лот: {commissionFee} монет
                    </div>
                    <ResourcesBox resources={postCost} resourceTypes={resourceTypes} have={resources} />
                    {invalidPair && <p className="note-warn">Нужны разные ресурсы</p>}
                    {lotsFull && <p className="note-warn">Все места на прилавке заняты – качайте Торговый двор</p>}
                    <button className="btn-game" disabled={!canPost}
                        title={lotsFull ? 'Все места на прилавке заняты' : canAffordPost ? undefined : 'Не хватает ресурсов'}>
                        <StoreIcon className="btn-ico" aria-hidden="true" />
                        Выставить
                    </button>
                </form>
                <div className="market-column">
                    <div className="market-column-head">Чужие лоты</div>
                    {market.lots.length === 0 && <p className="hint">На доске пока пусто.</p>}
                    <div className="market-list">
                        {market.lots.map(lot => {
                            const left = remainingSeconds(lot.expireDate, now);
                            const wantCost = [{ typeId: lot.wantResourceTypeId, value: lot.wantValue }];
                            const canAccept = left > 0 && hasResourcesFor(wantCost, resources);
                            return (
                                <div key={lot.id} className="market-card">
                                    <SellerBadge lot={lot} />
                                    <LotResources lot={lot} resourceTypes={resourceTypes} />
                                    <span className="timer">{formatDuration(left)}</span>
                                    <button className="btn-game" disabled={!canAccept}
                                        title={hasResourcesFor(wantCost, resources) ? undefined : 'Не хватает ' + getResourceName(resourceTypes, lot.wantResourceTypeId)}
                                        onClick={() => { void onAccept(lot.id); }}>
                                        <HandIcon className="btn-ico" aria-hidden="true" />
                                        Принять
                                    </button>
                                </div>
                            );
                        })}
                    </div>
                </div>
                <div className="market-column">
                    <div className="market-column-head">Мои лоты</div>
                    {market.myLots.length === 0 && <p className="hint">Активных лотов нет.</p>}
                    <div className="market-list">
                        {market.myLots.map(lot => (
                            <div key={lot.id} className="market-card">
                                <LotResources lot={lot} resourceTypes={resourceTypes} />
                                <span className="timer">{formatDuration(remainingSeconds(lot.expireDate, now))}</span>
                                <button className="btn-game" onClick={() => { void onCancel(lot.id); }}>
                                    <TrashIcon className="btn-ico" aria-hidden="true" />
                                    Отменить
                                </button>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </section>
    );
};

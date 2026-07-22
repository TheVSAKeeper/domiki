import { useMemo, useState } from 'react';
import type { FC, SVGProps } from 'react';
import ArrowsIcon from 'pixelarticons/svg/arrows-horizontal.svg?react';
import CheckIcon from 'pixelarticons/svg/check.svg?react';
import HandIcon from 'pixelarticons/svg/hand.svg?react';
import TrashIcon from 'pixelarticons/svg/trash.svg?react';
import type { MarketStateDto, ResourceDto, ResourceTypeDto, TradeLotDto } from '../types/api';
import { Crest } from './Crest';
import { MechanicSprite, ResourceSprite } from './sprites';
import { hasResourcesFor, resourceShortfall, tradeDeal, tradeRatio, type TradeDeal } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { NumberStepper } from './NumberStepper';
import { ResourcesBox } from './ResourcesBox';
import { ActionButton } from './ActionButton';

interface MarketBoxProps {
    market: MarketStateDto | null;
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    now: number;
    onPost: (kind: number, giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number) => Promise<void>;
    onAccept: (lotId: number) => Promise<void>;
    onCancel: (lotId: number) => Promise<void>;
}

const LOT_SELL = 1;
const LOT_BUY = 2;
type LotMode = 'sell' | 'buy';
type LotFilter = 'all' | 'sell' | 'buy';

interface OtherLotView {
    lot: TradeLotDto;
    affordable: boolean;
    shortfall: ResourceDto[];
    deal: TradeDeal;
}

const DEAL_LABEL: Record<TradeDeal, string> = { good: 'выгодно', fair: 'по-честному', bad: 'дорого' };

const mergeResources = (resources: ResourceDto[]): ResourceDto[] => {
    const values = new Map<number, number>();
    resources.forEach(resource => values.set(resource.typeId, (values.get(resource.typeId) ?? 0) + resource.value));
    return [...values].map(([typeId, value]) => ({ typeId, value }));
};

const getResourceName = (resourceTypes: ResourceTypeDto[], resourceTypeId: number) =>
    resourceTypes.find(x => x.id === resourceTypeId)?.name ?? 'Ресурс';

const formatPercent = (rate: number) => (rate * 100).toLocaleString('ru-RU', { maximumFractionDigits: 1 }) + '%';

const TradeMeta = ({ giveValue, wantValue, deal }: { giveValue: number; wantValue: number; deal: TradeDeal }) => {
    const [give, want] = tradeRatio(giveValue, wantValue);
    return (
        <div className="trade-meta">
            <span className="trade-rate" title="Сколько отдаёшь за сколько получаешь">курс {give}:{want}</span>
            <span className={'trade-deal trade-deal-' + deal}>{DEAL_LABEL[deal]}</span>
        </div>
    );
};

const LotResources = ({ lot, resourceTypes, have }: { lot: TradeLotDto; resourceTypes: ResourceTypeDto[]; have?: ResourceDto[] }) => (
    <div className="market-lot-resources">
        <div className="market-side">
            <span className="panel-label">даёт</span>
            <ResourcesBox resources={[{ typeId: lot.giveResourceTypeId, value: lot.giveValue }]} resourceTypes={resourceTypes} />
        </div>
        <ArrowsIcon className="market-arrow" aria-hidden="true" />
        <div className="market-side">
            <span className="panel-label">хочет</span>
            <ResourcesBox resources={[{ typeId: lot.wantResourceTypeId, value: lot.wantValue }]} resourceTypes={resourceTypes} {...(have ? { have } : {})} />
        </div>
    </div>
);

const LotKindBadge = ({ lot }: { lot: TradeLotDto }) =>
    lot.kind === LOT_BUY ? (
        <span className="market-lot-badge market-lot-badge-buy">
            <MechanicSprite logicName="market" size={24} className="btn-ico" aria-hidden="true" />
            Куплю
        </span>
    ) : (
        <span className="market-lot-badge market-lot-badge-sell">
            <MechanicSprite logicName="market" size={24} className="btn-ico" aria-hidden="true" />
            Продажа
        </span>
    );

const SellerBadge = ({ lot }: { lot: TradeLotDto }) => {
    return (
        <div className="market-seller">
            <Crest icon={lot.sellerCrestIcon} color={lot.sellerCrestColor} className="crest-badge-small" />
            <span className="market-village-name">{lot.sellerVillageName ?? 'Безымянная деревня'}</span>
        </div>
    );
};

const MarketEmpty = ({ children }: { children: string }) => (
    <div className="market-empty">
        <MechanicSprite logicName="market" size={40} className="market-empty-ico" aria-hidden="true" />
        <p className="hint">{children}</p>
    </div>
);

const ResourcePicker = ({ resourceTypes, selectedId, onSelect, label }: { resourceTypes: ResourceTypeDto[]; selectedId: number; onSelect: (id: number) => void; label: string }) => (
    <div className="resource-picker" role="radiogroup" aria-label={label}>
        {resourceTypes.map(type => (
            <button key={type.id} type="button" role="radio" aria-checked={type.id === selectedId}
                aria-label={type.name}
                className={'resource-option' + (type.id === selectedId ? ' resource-option-selected' : '')}
                title={type.name} onClick={() => onSelect(type.id)}>
                <ResourceSprite logicName={type.logicName} aria-hidden="true" />
            </button>
        ))}
    </div>
);

const MarketModeIcon: FC<SVGProps<SVGSVGElement>> = props => <MechanicSprite logicName="market" size={24} {...props} />;

const LOT_MODE_OPTIONS: { mode: LotMode; label: string; Icon: FC<SVGProps<SVGSVGElement>> }[] = [
    { mode: 'sell', label: 'Продаю', Icon: MarketModeIcon },
    { mode: 'buy', label: 'Куплю', Icon: MarketModeIcon },
];

const ModeToggle = ({ mode, onSelect }: { mode: LotMode; onSelect: (mode: LotMode) => void }) => (
    <div className="market-mode-toggle" role="radiogroup" aria-label="Тип лота">
        {LOT_MODE_OPTIONS.map(option => (
            <button key={option.mode} type="button" role="radio" aria-checked={option.mode === mode}
                className={'market-mode-option' + (option.mode === mode ? ' market-mode-option-active' : '')}
                onClick={() => onSelect(option.mode)}>
                <option.Icon className="btn-ico" aria-hidden="true" />
                {option.label}
            </button>
        ))}
    </div>
);

const LOT_FILTER_OPTIONS: { filter: LotFilter; label: string }[] = [
    { filter: 'all', label: 'Все' },
    { filter: 'sell', label: 'Продажа' },
    { filter: 'buy', label: 'Покупка' },
];

const LotFilterToggle = ({ filter, onSelect }: { filter: LotFilter; onSelect: (filter: LotFilter) => void }) => (
    <div className="market-filter" role="radiogroup" aria-label="Фильтр лотов">
        {LOT_FILTER_OPTIONS.map(option => (
            <button key={option.filter} type="button" role="radio" aria-checked={option.filter === filter}
                className={'market-filter-option' + (option.filter === filter ? ' market-filter-option-active' : '')}
                onClick={() => onSelect(option.filter)}>
                {option.label}
            </button>
        ))}
    </div>
);

const matchesLotFilter = (filter: LotFilter, kind: number) =>
    filter === 'all' || (filter === 'buy' ? kind === LOT_BUY : kind !== LOT_BUY);

export const MarketBox = ({ market, resourceTypes, resources, now, onPost, onAccept, onCancel }: MarketBoxProps) => {
    const firstTypeId = resourceTypes[0]?.id ?? 1;
    const secondTypeId = resourceTypes.find(x => x.id !== firstTypeId)?.id ?? firstTypeId;
    const [giveResourceTypeId, setGiveResourceTypeId] = useState(firstTypeId);
    const [wantResourceTypeId, setWantResourceTypeId] = useState(secondTypeId);
    const [giveValue, setGiveValue] = useState(10);
    const [wantValue, setWantValue] = useState(1);
    const [mode, setMode] = useState<LotMode>('sell');
    const [lotFilter, setLotFilter] = useState<LotFilter>('all');

    const goldTypeId = resourceTypes.find(x => x.logicName === 'gold')?.id ?? 5;
    const buyableTypes = useMemo(() => resourceTypes.filter(x => x.logicName !== 'gold' && x.logicName !== 'coin'), [resourceTypes]);

    const changeMode = (next: LotMode) => {
        if (next === 'buy') {
            setGiveResourceTypeId(goldTypeId);
            setWantResourceTypeId(prev => buyableTypes.some(x => x.id === prev) ? prev : (buyableTypes[0]?.id ?? prev));
        }
        setMode(next);
    };

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

    const dealFor = (giveId: number, giveAmount: number, wantId: number, wantAmount: number): TradeDeal =>
        tradeDeal(giveAmount, resourceTypes.find(x => x.id === giveId)?.marketValue ?? 0, wantAmount, resourceTypes.find(x => x.id === wantId)?.marketValue ?? 0);

    const otherLots = useMemo<OtherLotView[]>(() => {
        const marketValueOf = (typeId: number) => resourceTypes.find(x => x.id === typeId)?.marketValue ?? 0;
        const enriched = (market?.lots ?? []).map(lot => {
            const wantCost = [{ typeId: lot.wantResourceTypeId, value: lot.wantValue }];
            const affordable = hasResourcesFor(wantCost, resources);
            return {
                lot,
                affordable,
                shortfall: affordable ? [] : resourceShortfall(wantCost, resources),
                deal: tradeDeal(lot.giveValue, marketValueOf(lot.giveResourceTypeId), lot.wantValue, marketValueOf(lot.wantResourceTypeId)),
            };
        });
        return enriched.sort((a, b) => Number(b.affordable) - Number(a.affordable));
    }, [market, resources, resourceTypes]);

    const filteredOtherLots = otherLots.filter(x => matchesLotFilter(lotFilter, x.lot.kind));
    const affordableCount = filteredOtherLots.filter(x => x.affordable).length;

    if (market == null) {
        return null;
    }

    const invalidPair = giveResourceTypeId === wantResourceTypeId;
    const lotsFull = market.myLots.length >= market.maxLots;
    const canAffordPost = giveValue > 0 && wantValue > 0 && !invalidPair && hasResourcesFor(postCost, resources);
    const canPost = canAffordPost && !lotsFull;

    const submitPost = async () => {
        await onPost(mode === 'buy' ? LOT_BUY : LOT_SELL, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue);
    };

    return (
        <section className="market-panel pixel-panel">
            <div className="market-awning" aria-hidden="true" />
            <div className="market-head">
                <h3 className="panel-title mech-title"><MechanicSprite logicName="market" size={24} className="panel-title-ico" aria-hidden="true" />Ярмарка</h3>
                <div className="market-chips">
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
            </div>
            <div className="market-layout">
                <form className="market-card market-stall market-form" onSubmit={event => { event.preventDefault(); void submitPost(); }}>
                    <div className="market-card-title">
                        <ArrowsIcon className="btn-ico" aria-hidden="true" />
                        Твой прилавок
                    </div>
                    <ModeToggle mode={mode} onSelect={changeMode} />
                    {mode === 'buy' && <p className="hint">Заявки и продажи делят места на прилавке.</p>}
                    <div className="market-field">
                        <span className="panel-label">даю</span>
                        {mode === 'buy' ? (
                            <div className="market-pay-gold">
                                <ResourceSprite logicName="gold" aria-hidden="true" />
                                <span>плачу золотом</span>
                            </div>
                        ) : (
                            <ResourcePicker resourceTypes={resourceTypes} selectedId={giveResourceTypeId} onSelect={setGiveResourceTypeId} label="Ресурс, который даю" />
                        )}
                        <NumberStepper value={giveValue} onChange={setGiveValue} />
                    </div>
                    <div className="market-field">
                        <span className="panel-label">{mode === 'buy' ? 'покупаю' : 'хочу'}</span>
                        <ResourcePicker resourceTypes={mode === 'buy' ? buyableTypes : resourceTypes} selectedId={wantResourceTypeId} onSelect={setWantResourceTypeId}
                            label={mode === 'buy' ? 'Ресурс, который покупаю' : 'Ресурс, который хочу'} />
                        <NumberStepper value={wantValue} onChange={setWantValue} />
                    </div>
                    {!invalidPair && <TradeMeta giveValue={giveValue} wantValue={wantValue} deal={dealFor(giveResourceTypeId, giveValue, wantResourceTypeId, wantValue)} />}
                    <div className="market-commission">
                        <ResourceSprite logicName="coin" size={24} className="btn-ico" aria-hidden="true" />
                        Комиссия за лот: {commissionFee} монет
                    </div>
                    <div className="market-cost">
                        <span className="panel-label">спишется</span>
                        <ResourcesBox resources={postCost} resourceTypes={resourceTypes} have={resources} />
                    </div>
                    {invalidPair && <p className="note-warn">Нужны разные ресурсы</p>}
                    {lotsFull && <p className="note-warn">Все места на прилавке заняты – качайте Торговый двор</p>}
                    <ActionButton className="btn-game" disabled={!canPost}
                        title={lotsFull ? 'Все места на прилавке заняты' : canAffordPost ? undefined : 'Не хватает ресурсов'}
                        onClick={submitPost}>
                        <MechanicSprite logicName="market" size={24} className="btn-ico" aria-hidden="true" />
                        Выставить лот
                    </ActionButton>
                </form>
                <div className="market-column">
                    <div className="market-column-head">
                        <span>Чужие лоты</span>
                        <LotFilterToggle filter={lotFilter} onSelect={setLotFilter} />
                        {filteredOtherLots.length > 0 && (
                            <span className={'market-count' + (affordableCount > 0 ? ' market-count-live' : '')}>
                                по карману {affordableCount}/{filteredOtherLots.length}
                            </span>
                        )}
                    </div>
                    {filteredOtherLots.length === 0 && (
                        <MarketEmpty>
                            {otherLots.length === 0
                                ? 'Прилавки соседей пусты – загляните позже, торг ещё будет.'
                                : lotFilter === 'buy' ? 'Заявок на покупку пока нет.' : 'Продаж пока нет.'}
                        </MarketEmpty>
                    )}
                    <div className="market-list">
                        {filteredOtherLots.map(({ lot, affordable, shortfall, deal }) => {
                            const left = remainingSeconds(lot.expireDate, now);
                            const canAccept = left > 0 && affordable;
                            return (
                                <div key={lot.id} className={'market-card market-stall' + (affordable ? '' : ' market-stall-locked')}>
                                    <div className="market-stall-head">
                                        <LotKindBadge lot={lot} />
                                        <SellerBadge lot={lot} />
                                    </div>
                                    <LotResources lot={lot} resourceTypes={resourceTypes} have={resources} />
                                    <div className="market-stall-foot">
                                        <TradeMeta giveValue={lot.giveValue} wantValue={lot.wantValue} deal={deal} />
                                        <span className="timer">{formatDuration(left)}</span>
                                    </div>
                                    {affordable ? (
                                        <p className="market-afford"><CheckIcon className="btn-ico" aria-hidden="true" />по карману</p>
                                    ) : (
                                        <p className="note-warn market-shortfall">
                                            не хватает
                                            <ResourcesBox resources={shortfall} resourceTypes={resourceTypes} />
                                        </p>
                                    )}
                                    <ActionButton className="btn-game" disabled={!canAccept}
                                        title={affordable ? undefined : 'Не хватает ' + getResourceName(resourceTypes, lot.wantResourceTypeId)}
                                        onClick={() => onAccept(lot.id)}>
                                        <HandIcon className="btn-ico" aria-hidden="true" />
                                        Принять
                                    </ActionButton>
                                </div>
                            );
                        })}
                    </div>
                </div>
                <div className="market-column">
                    <div className="market-column-head"><span>Мои лоты</span></div>
                    {market.myLots.length === 0 && <MarketEmpty>Вы пока ничего не выставили. Первый лот – слева, на своём прилавке.</MarketEmpty>}
                    <div className="market-list">
                        {market.myLots.map(lot => (
                            <div key={lot.id} className="market-card market-stall">
                                <LotKindBadge lot={lot} />
                                <LotResources lot={lot} resourceTypes={resourceTypes} />
                                <div className="market-stall-foot">
                                    <TradeMeta giveValue={lot.giveValue} wantValue={lot.wantValue}
                                        deal={dealFor(lot.giveResourceTypeId, lot.giveValue, lot.wantResourceTypeId, lot.wantValue)} />
                                    <span className="timer">{formatDuration(remainingSeconds(lot.expireDate, now))}</span>
                                </div>
                                <ActionButton className="btn-game btn-ghost" onClick={() => onCancel(lot.id)}>
                                    <TrashIcon className="btn-ico" aria-hidden="true" />
                                    Отменить
                                </ActionButton>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </section>
    );
};

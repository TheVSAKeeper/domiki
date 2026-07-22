import { useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import type { NeighborReputationDto } from '../types/api';
import { Link } from 'react-router-dom';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import SettingsIcon from 'pixelarticons/svg/settings-cog.svg?react';
import EarthIcon from 'pixelarticons/svg/earth.svg?react';
import BookOpenIcon from 'pixelarticons/svg/book-open.svg?react';
import { acceptErrand as acceptErrandApi, apiPost, ApiError, cancelErrand as cancelErrandApi, cancelOrder as cancelOrderApi, completeOrder as completeOrderApi, setFriendNeighbor as setFriendNeighborApi, startIncidentSearch as startIncidentSearchApi } from '../services/api';
import { useToast } from '../services/toastContext';
import { useGameData } from '../hooks/useGameData';
import { GOLD_RESOURCE_TYPE_ID, computeSelectedDomikView, isWorkerFree } from '../utils/game';
import type { DomikSortMode } from '../utils/game';
import { buildDomikNamer } from '../utils/domikNames';
import { PushToggle } from './PushToggle';
import { GameTabsNav } from './GameTabsNav';
import { VillageIdentityModal } from './VillageIdentityModal';
import { VillageHud } from './VillageHud';
import { DomikGridSection, DomikSortMenu } from './DomikGridSection';
import { VillageYard } from './VillageYard';
import { SelectedDomikPanel } from './SelectedDomikPanel';
import { ActionButton, ActionBusyProvider } from './ActionButton';
import { OrdersBox } from './OrdersBox';
import { GoalCard } from './GoalCard';
import { IncidentCard } from './IncidentCard';
import { DomikIncidentCard } from './DomikIncidentCard';
import { WorkersBox } from './WorkersBox';
import { BlueprintsBox } from './BlueprintsBox';
import { ExpeditionsBox } from './ExpeditionsBox';
import { DecorBox } from './DecorBox';
import { TolokaBox } from './TolokaBox';
import { MarketBox } from './MarketBox';
import { JournalBox } from './JournalBox';
import { GuestbookBox } from './GuestbookBox';
import { ShopBox } from './ShopBox';
import { RecapModal } from './RecapModal';
import { AbstractSprite, MechanicSprite } from './sprites';
import { PixelLoader } from './PixelLoader';
import { ResourceInfoProvider } from './ResourceInfo';
import { DEFAULT_VILLAGE_ICON, VILLAGE_CREST_COLORS, VILLAGE_CREST_ICONS } from '../constants/village';
import { buildRecapView } from '../utils/recap';


const MECHANIC_TAB: Record<string, string> = {
    market_yard: 'market',
    gathering: 'toloka',
    scout_hut: 'expeditions',
};

interface GameTab {
    key: string;
    label: string;
    icon: ReactNode;
    visible: boolean;
    node: ReactNode;
}

export const DomikiPage = () => {
    const toast = useToast();
    const { domiks, domikTypes, resourceTypes, receipts, resources, orders, errand, incident, domikIncident, reputation, blueprints, village, villageLevel, weather, expeditions, decor, toloka, market, convoys, goals, workers, cloaks, sickTypes, purchaseDomikTypes, now, loading, scheduleReload, refreshPurchaseTypes, setVillage, setFeedWorkers, hurryManufacture, setManufactureAutoRepeat, hurryDomik, startExpedition, buyDecor, contributeToloka, voteToloka, postLot, acceptLot, cancelLot, buyFromConvoy, recap, clearRecap, events } =
        useGameData();

    const [shopVisible, setShopVisible] = useState(false);
    const [recapOpen, setRecapOpen] = useState(false);
    const [selectedDomikId, setSelectedDomikId] = useState<number | null>(null);
    const [activeTab, setActiveTab] = useState('');
    const [identity, setIdentity] = useState<'auto' | 'open' | 'dismissed'>('auto');
    const gameTabPanelRef = useRef<HTMLDivElement>(null);
    const [hudStickyOffset, setHudStickyOffset] = useState(76);
    const [sortMode, setSortMode] = useState<DomikSortMode>(() => {
        const saved = localStorage.getItem('domik-sort-mode');
        return saved === 'attention' || saved === 'level' ? saved : 'type';
    });
    const changeSortMode = (mode: DomikSortMode) => {
        setSortMode(mode);
        localStorage.setItem('domik-sort-mode', mode);
    };

    const plodder = useMemo(() => ({
        max: workers.length,
        free: workers.filter(worker => isWorkerFree(worker, now)).length,
    }), [workers, now]);
    const selected = useMemo(
        () => computeSelectedDomikView(selectedDomikId, domiks, domikTypes, receipts, resources, now),
        [selectedDomikId, domiks, domikTypes, receipts, resources, now],
    );
    const domikDisplayName = useMemo(() => buildDomikNamer(domiks), [domiks]);
    const currentWeather = weather?.current ?? null;
    const goldValue = resources.find(x => x.typeId === GOLD_RESOURCE_TYPE_ID)?.value ?? 0;
    const goldType = resourceTypes.find(x => x.id === GOLD_RESOURCE_TYPE_ID);
    const recapView = useMemo(() => buildRecapView(recap?.events ?? []), [recap]);
    const recapPending = recap != null && recap.events.length > 0;
    const recapVisible = recap != null && recap.events.length > 0 && (recap.awaySeconds >= 1800 || recapOpen);
    const friendNeighbor = useMemo(() => {
        const friendIds = new Set(blueprints.filter(b => b.currentReputation >= b.reputationThreshold).map(b => b.neighborId));
        const top = reputation
            .filter(r => friendIds.has(r.neighborId))
            .reduce<NeighborReputationDto | null>((best, r) => best == null || r.points > best.points ? r : best, null);
        return top == null ? null : { logicName: top.neighborLogicName, name: top.neighborName };
    }, [blueprints, reputation]);

    const currentCrestIcon = village?.crestIcon ?? 0;
    const currentCrestColor = village?.crestColor ?? 0;
    const VillageIcon = VILLAGE_CREST_ICONS[currentCrestIcon] ?? DEFAULT_VILLAGE_ICON;
    const villageColor = VILLAGE_CREST_COLORS[currentCrestColor] ?? VILLAGE_CREST_COLORS[0];
    const villageName = village?.villageName ?? 'Безымянная деревня';
    const identityVisible = identity === 'open' || (identity === 'auto' && village?.villageName === null);

    const openIdentity = () => setIdentity('open');

    const closeIdentity = () => setIdentity('dismissed');

    const actionBusy = useRef(false);
    const runAction = async (action: () => Promise<void>, successMessage?: string): Promise<boolean> => {
        if (actionBusy.current) return false;
        actionBusy.current = true;
        try {
            await action();
            if (successMessage != null) {
                toast.success(successMessage);
            }
            return true;
        } catch (err) {
            if (err instanceof ApiError) {
                toast.error(err.message);
                return false;
            }
            throw err;
        } finally {
            actionBusy.current = false;
        }
    };

    const buy = (typeId: number) => {
        const domikType = domikTypes.find(type => type.id === typeId);
        return runAction(async () => {
            await apiPost(`Domiki/BuyDomik/${typeId}`);
            scheduleReload();
        }, domikType == null ? 'Домик построен' : `«${domikType.name}» построен`);
    };

    const upgrade = (id: number) => runAction(async () => {
        await apiPost(`Domiki/UpgradeDomik/${id}`);
        scheduleReload();
    }, 'Улучшение запущено');

    const startManufacture = (domikId: number, receiptId: number, useOptional: boolean, autoRepeat: boolean, workerIds?: number[]) => runAction(async () => {
        const workerIdsQuery = (workerIds ?? []).map(id => `&workerIds=${id}`).join('');
        await apiPost(`Domiki/StartManufacture/${domikId}/${receiptId}?useOptional=${String(useOptional)}&autoRepeat=${String(autoRepeat)}${workerIdsQuery}`);
        scheduleReload();
    }, 'Производство запущено');

    const completeOrder = (orderId: number) => runAction(async () => {
        await completeOrderApi(orderId);
        scheduleReload();
    }, 'Заказ выполнен');

    const cancelOrder = (orderId: number) => runAction(async () => {
        await cancelOrderApi(orderId);
        scheduleReload();
    }, 'Заказ уступили – новый спрос подойдёт со временем.');

    const setFriendNeighborAction = (neighborId: number | null) => {
        const neighborName = neighborId == null ? null : reputation.find(item => item.neighborId === neighborId)?.neighborName ?? null;
        const successMessage = neighborId == null
            ? undefined
            : neighborName != null
                ? `Теперь водим дружбу с «${neighborName}» – их заказы будут заглядывать чаще.`
                : 'Теперь водим дружбу – её заказы будут заглядывать чаще.';
        return runAction(async () => {
            await setFriendNeighborApi(neighborId);
            scheduleReload();
        }, successMessage);
    };

    const acceptErrandAction = (errandId: number, clueId: number, workerIds: number[]) => runAction(async () => {
        await acceptErrandApi(errandId, clueId, workerIds);
        scheduleReload();
    }, 'Поручение принято');

    const cancelErrandAction = (errandId: number) => runAction(async () => {
        await cancelErrandApi(errandId);
        scheduleReload();
    }, errand?.acceptDate == null ? 'Поручение отклонено' : 'Поручение отозвано');

    const startIncidentSearchAction = (incidentId: number, clueId: number, workerIds: number[]) => runAction(async () => {
        await startIncidentSearchApi(incidentId, clueId, workerIds);
        scheduleReload();
    }, 'Поиски начались');

    const hurryManufactureAction = (manufactureId: number) => runAction(() => hurryManufacture(manufactureId), 'Производство ускорено');

    const toggleManufactureAutoRepeat = (manufactureId: number, next: boolean) => runAction(
        () => setManufactureAutoRepeat(manufactureId, next),
        next ? 'Автоповтор включён' : 'Повторы остановлены',
    );

    const hurryDomikAction = (domikId: number) => runAction(() => hurryDomik(domikId), 'Улучшение ускорено');

    const startExpeditionAction = (expeditionTypeId: number, workerIds?: number[], provisions?: boolean) => runAction(() => startExpedition(expeditionTypeId, workerIds, provisions), 'Экспедиция отправлена');

    const toggleFeedWorkers = (enabled: boolean) => runAction(() => setFeedWorkers(enabled));

    const buyDecorAction = (decorTypeId: number) => runAction(() => buyDecor(decorTypeId), 'Декор куплен');

    const buyFromConvoyAction = (neighborId: number, resourceTypeId: number, count: number) =>
        runAction(() => buyFromConvoy(neighborId, resourceTypeId, count), 'Товар куплен у обоза');

    const contributeTolokaAction = async (resourceTypeId: number, amount: number) => {
        await runAction(() => contributeToloka(resourceTypeId, amount), 'Вклад принят');
    };

    const voteTolokaAction = async (tolokaTypeId: number) => {
        await runAction(() => voteToloka(tolokaTypeId), 'Голос учтён');
    };

    const postLotAction = async (kind: number, giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number) => {
        await runAction(() => postLot(kind, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue), 'Лот выставлен');
    };

    const acceptLotAction = async (lotId: number) => {
        await runAction(() => acceptLot(lotId), 'Сделка совершена');
    };

    const cancelLotAction = async (lotId: number) => {
        await runAction(() => cancelLot(lotId), 'Лот снят');
    };

    const saveIdentity = async (name: string, crestIcon: number, crestColor: number) => {
        await runAction(async () => {
            await setVillage(name, crestIcon, crestColor);
            setIdentity('dismissed');
        });
    };

    const toggleShop = async () => {
        await runAction(async () => {
            const willShow = !shopVisible;
            setShopVisible(willShow);
            if (willShow && purchaseDomikTypes == null) {
                await refreshPurchaseTypes();
            }
        });
    };

    const selectDomik = (id: number, logicName: string) => {
        setSelectedDomikId(id);
        const mechTab = MECHANIC_TAB[logicName];
        if (mechTab && mechTab !== activeTab) {
            setActiveTab(mechTab);
        }
    };

    const gameTabs: GameTab[] = [
        {
            key: 'orders', label: 'Заказы', icon: <MechanicSprite logicName="orders" size={24} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: <OrdersBox orders={orders} errand={errand} workers={workers} reputation={reputation} convoys={convoys} resourceTypes={resourceTypes} resources={resources} now={now}
                onComplete={completeOrder} onCancel={cancelOrder} onAcceptErrand={acceptErrandAction} onCancelErrand={cancelErrandAction}
                onBuyFromConvoy={buyFromConvoyAction} onSetFriend={setFriendNeighborAction} />,
        },
        {
            key: 'blueprints', label: 'Вехи соседей', icon: <MechanicSprite logicName="blueprints" size={24} className="game-tab-ico" aria-hidden="true" />, visible: blueprints.length > 0 || (decor?.types ?? []).some(x => x.neighborId != null),
            node: <BlueprintsBox blueprints={blueprints} domikTypes={domikTypes} decorTypes={decor?.types ?? []} reputations={reputation} />,
        },
        {
            key: 'expeditions', label: 'Экспедиции', icon: <MechanicSprite logicName="expeditions" size={24} className="game-tab-ico" aria-hidden="true" />, visible: expeditions != null,
            node: <ExpeditionsBox expeditions={expeditions} resourceTypes={resourceTypes} decorTypes={decor?.types ?? []} resources={resources} workers={workers} now={now} onStart={startExpeditionAction} />,
        },
        {
            key: 'decor', label: 'Декор', icon: <MechanicSprite logicName="decor" size={24} className="game-tab-ico" aria-hidden="true" />, visible: decor != null,
            node: <DecorBox decor={decor} resourceTypes={resourceTypes} resources={resources} reputations={reputation} onBuy={buyDecorAction} />,
        },
        {
            key: 'toloka', label: 'Толока', icon: <MechanicSprite logicName="toloka" size={24} className="game-tab-ico" aria-hidden="true" />, visible: toloka != null,
            node: <TolokaBox toloka={toloka} resourceTypes={resourceTypes} resources={resources} now={now} onContribute={contributeTolokaAction} onVote={voteTolokaAction} />,
        },
        {
            key: 'market', label: 'Ярмарка', icon: <MechanicSprite logicName="market" size={24} className="game-tab-ico" aria-hidden="true" />, visible: market != null,
            node: <MarketBox market={market} resourceTypes={resourceTypes} resources={resources} now={now}
                onPost={postLotAction} onAccept={acceptLotAction} onCancel={cancelLotAction} />,
        },
        {
            key: 'workers', label: 'Трудяги', icon: <MechanicSprite logicName="workers" size={24} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: <WorkersBox workers={workers} domikTypes={domikTypes} domiks={domiks} expeditions={expeditions} errand={errand} incident={incident} domikIncident={domikIncident} cloaks={cloaks} sickTypes={sickTypes} feedWorkers={village?.feedWorkers ?? false} now={now} onToggleFeedWorkers={toggleFeedWorkers} />,
        },
        {
            key: 'journal', label: 'Журнал', icon: <AbstractSprite logicName="journal" size={24} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: <JournalBox events={events} resourceTypes={resourceTypes} domikTypes={domikTypes} decorTypes={decor?.types ?? []} now={now} />,
        },
        {
            key: 'guestbook', label: 'Гости', icon: <BookOpenIcon className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: <GuestbookBox now={now} />,
        },
    ];
    const visibleGameTabs = gameTabs.filter(tab => tab.visible);
    const activeGameTab = visibleGameTabs.find(tab => tab.key === activeTab) ?? visibleGameTabs[0];

    const scrollToGameTabPanel = () => {
        if (window.matchMedia('(max-width: 900px)').matches) {
            gameTabPanelRef.current?.scrollIntoView({ block: 'start' });
        }
    };

    return (
        <ResourceInfoProvider resourceTypes={resourceTypes} domikTypes={domikTypes} receipts={receipts}>
        <ActionBusyProvider>
        <div className="game" style={{ '--hud-sticky-offset': `${hudStickyOffset}px` } as React.CSSProperties}>
            {loading &&
                <div className="game-loading">
                    <PixelLoader label="Загрузка деревни…" />
                </div>
            }
            <VillageHud resources={resources} resourceTypes={resourceTypes} domikTypes={domikTypes} plodder={plodder}
                villageLevel={villageLevel} weather={weather} now={now} onStickyOffsetChange={setHudStickyOffset} />
            <GoalCard goals={goals} resourceTypes={resourceTypes} />
            {incident != null && <IncidentCard incident={incident} workers={workers} now={now} onStartSearch={startIncidentSearchAction} />}
            {domikIncident != null && <DomikIncidentCard incident={domikIncident} workers={workers} domikTypes={domikTypes} now={now} onStartSearch={startIncidentSearchAction} />}
            {identityVisible &&
                <VillageIdentityModal village={village} onSave={saveIdentity} onClose={closeIdentity} />
            }
            {recapVisible &&
                <RecapModal
                    awaySeconds={recap.awaySeconds}
                    view={recapView}
                    resourceTypes={resourceTypes}
                    domikTypes={domikTypes}
                    decorTypes={decor?.types ?? []}
                    expeditionTypes={expeditions?.types ?? []}
                    neighbors={reputation}
                    toloka={toloka}
                    onClose={clearRecap}
                />
            }
            <div className="village-header">
                <div className="village-identity">
                    <span className="crest-badge" style={{ backgroundColor: villageColor }}>
                        <VillageIcon className="crest-ico" aria-hidden="true" />
                    </span>
                    <h2 className="section-title village-name">{villageName}</h2>
                    <button type="button" className="identity-button" title="Настроить деревню" onClick={openIdentity}>
                        <SettingsIcon className="btn-ico" aria-hidden="true" />
                    </button>
                </div>
                <div className="village-header-actions">
                    {domiks.length > 1 && <DomikSortMenu value={sortMode} onChange={changeSortMode} />}
                    <PushToggle />
                    <Link className="btn-game" to="/world">
                        <EarthIcon className="btn-ico" aria-hidden="true" />
                        Мир
                    </Link>
                    {purchaseDomikTypes != null &&
                        <ActionButton className="btn-game" onClick={() => toggleShop()}>
                            <StoreIcon className="btn-ico" aria-hidden="true" />
                            {shopVisible ? 'Закрыть' : 'Плотник'}
                        </ActionButton>
                    }
                </div>
            </div>
            <div className="workspace">
                <section className="village">
                    <VillageYard domiks={domiks} domikTypes={domikTypes} decor={decor} workers={workers}
                        villageLevel={villageLevel} currentWeather={currentWeather} selectedDomikId={selectedDomikId}
                        displayName={domik => {
                            const domikType = domikTypes.find(type => type.id === domik.typeId);
                            return domikType == null ? '' : domikDisplayName(domik.typeId, domik.id, domikType.name, domikType.logicName);
                        }}
                        onSelect={id => {
                            const domik = domiks.find(item => item.id === id);
                            const domikType = domik == null ? undefined : domikTypes.find(type => type.id === domik.typeId);
                            selectDomik(id, domikType?.logicName ?? '');
                        }}
                        recapPending={recapPending}
                        onOpenRecap={() => { setRecapOpen(true); }}
                        activeExpeditionNames={(expeditions?.active ?? []).map(e => e.expeditionName)}
                        friendNeighbor={friendNeighbor} />
                    {shopVisible && purchaseDomikTypes != null &&
                        <ShopBox purchaseDomikTypes={purchaseDomikTypes} domikTypes={domikTypes} receipts={receipts}
                            resourceTypes={resourceTypes} resources={resources} blueprints={blueprints} villageLevel={villageLevel}
                            onBuy={buy} onClose={() => setShopVisible(false)} />
                    }
                    <DomikGridSection domiks={domiks} domikTypes={domikTypes} receipts={receipts} resources={resources}
                        resourceTypes={resourceTypes} currentWeather={currentWeather} now={now} sortMode={sortMode}
                        selectedDomikId={selectedDomikId} displayName={domikDisplayName} onSelect={selectDomik} workers={workers} />
                </section>
                {selected != null && <div className="actions-scrim" role="presentation" onClick={() => { setSelectedDomikId(null); }} />}
                <SelectedDomikPanel selected={selected} resources={resources} resourceTypes={resourceTypes} receipts={receipts}
                    workers={workers} goals={goals} villageLevel={villageLevel} currentWeather={currentWeather} now={now}
                    goldValue={goldValue} goldType={goldType} plodderFree={plodder.free} displayName={domikDisplayName}
                    onClose={() => setSelectedDomikId(null)} onUpgrade={upgrade} onHurryDomik={hurryDomikAction}
                    onStartManufacture={startManufacture} onHurryManufacture={hurryManufactureAction}
                    onToggleManufactureRepeat={toggleManufactureAutoRepeat} />
            </div>
            <GameTabsNav tabs={visibleGameTabs} activeKey={activeGameTab?.key} onSelect={setActiveTab} onScrollToPanel={scrollToGameTabPanel} />
            <div className="game-tab-panel" ref={gameTabPanelRef} id="game-tab-panel" role="tabpanel"
                aria-labelledby={activeGameTab == null ? undefined : `game-tab-${activeGameTab.key}`} tabIndex={0}>
                {activeGameTab?.node}
            </div>
        </div>
        </ActionBusyProvider>
        </ResourceInfoProvider>
    );
};

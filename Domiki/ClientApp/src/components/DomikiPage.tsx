import { useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { ReactNode } from 'react';
import type { NeighborReputationDto } from '../types/api';
import { Navigate, useNavigate, useParams } from 'react-router-dom';
import { acceptErrand as acceptErrandApi, ApiError, cancelErrand as cancelErrandApi, cancelOrder as cancelOrderApi, enqueueCommand, setFriendNeighbor as setFriendNeighborApi, setVillageProfile as setVillageProfileApi, startIncidentSearch as startIncidentSearchApi } from '../services/api';
import type { GameCommand } from '../services/api';
import { useToast } from '../services/toastContext';
import { useGameData } from '../hooks/useGameData';
import { GOLD_RESOURCE_TYPE_ID, computeSelectedDomikView, isWorkerFree } from '../utils/game';
import { buildAssignTarget, buildAssignTargets } from '../utils/assign';
import { computeHudDigest } from '../utils/hud';
import type { DomikSortMode, GoldVeinContext } from '../utils/game';
import { useWorkerAssign } from '../hooks/useWorkerAssign';
import type { AssignPoint } from '../hooks/useWorkerAssign';
import { WorkerRail } from './WorkerRail';
import { StockRail } from './StockRail';
import { AssignGhost } from './AssignGhost';
import { PerfZone } from './PerfZone';
import { perfCommitProbe } from '../utils/perf';
import { ReceiptDropMenu } from './ReceiptDropMenu';
import { buildDomikNamer } from '../utils/domikNames';
import { neighborPrepositionalName, profileGenitiveName } from '../utils/profileLore';
import { reputationTierAhead } from '../utils/reputationTiers';
import { GameTabsNav } from './GameTabsNav';
import { BOARD_TAB_KEY, tabPath } from '../utils/gameTabs';
import { VillageIdentityModal } from './VillageIdentityModal';
import { VillageHud } from './VillageHud';
import { DomikGridSection } from './DomikGridSection';
import { HouseholdBox } from './HouseholdBox';
import { VillageYard } from './VillageYard';
import { SelectedDomikPanel } from './SelectedDomikPanel';
import { ActionBusyProvider } from './ActionButton';
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
import { RelocationBox } from './RelocationBox';
import { ShopBox } from './ShopBox';
import { RecapModal } from './RecapModal';
import { AbstractSprite, MechanicSprite } from './sprites';
import { OfflineBanner } from './OfflineBanner';
import { PixelLoader } from './PixelLoader';
import { ResourceInfoProvider } from './ResourceInfo';
import { Crest } from './Crest';
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
    node: () => ReactNode;
}

export const DomikiPage = () => {
    useEffect(() => { perfCommitProbe(); });

    const toast = useToast();
    const { domiks, domikTypes, resourceTypes, receipts, resources, orders, orderBoardSize, orderFreeConcession, errands, incident, domikIncident, reputation, blueprints, village, villageLevel, goldMinedToday, villageProfiles, relocation, weather, expeditions, decor, toloka, market, convoys, goals, workers, cloaks, larder, ledger, reserves, sickTypes, purchaseDomikTypes, now, loading, staleSince, scheduleReload, refreshPurchaseTypes, setVillage, hurryManufacture, setManufactureAutoRepeat, setManufactureMeasure, setResourceReserve, hurryDomik, startExpedition, buyDecor, setFoodRule, contributeToloka, voteToloka, postLot, acceptLot, cancelLot, buyFromConvoy, relocate, buyPerk, recap, clearRecap, events } =
        useGameData();

    const [recapOpen, setRecapOpen] = useState(false);
    const [selectedDomikId, setSelectedDomikId] = useState<number | null>(null);
    const [assignMenu, setAssignMenu] = useState<{ workerId: number; domikId: number; point: AssignPoint } | null>(null);
    const [identity, setIdentity] = useState<'auto' | 'open' | 'dismissed'>('auto');
    const { tab: routeTab } = useParams();
    const navigate = useNavigate();
    const openTab = (key: string) => { void navigate(tabPath(key)); };
    useEffect(() => { window.scrollTo({ top: 0 }); }, [routeTab]);
    const shopAsked = useRef(false);
    useEffect(() => {
        if (routeTab !== 'shop' || purchaseDomikTypes != null || shopAsked.current) {
            return;
        }
        shopAsked.current = true;
        refreshPurchaseTypes().catch((err: unknown) => {
            shopAsked.current = false;
            toast.error(err instanceof ApiError ? err.message : 'Плотник не отозвался – загляните позже');
        });
    }, [routeTab, purchaseDomikTypes, refreshPurchaseTypes, toast]);
    const selectedDomikPanelRef = useRef<HTMLElement>(null);
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
    const hudDigest = useMemo(
        () => computeHudDigest(domiks, domikTypes, receipts, resources, orders, expeditions, workers, now),
        [domiks, domikTypes, receipts, resources, orders, expeditions, workers, now],
    );
    const selected = useMemo(
        () => computeSelectedDomikView(selectedDomikId, domiks, domikTypes, receipts, resources, now),
        [selectedDomikId, domiks, domikTypes, receipts, resources, now],
    );
    const domikDisplayName = useMemo(() => buildDomikNamer(domiks), [domiks]);
    const tavernLevel = useMemo(() => Math.max(0, ...domiks
        .filter(domik => domikTypes.find(type => type.id === domik.typeId)?.logicName === 'tavern')
        .map(domik => domik.level)), [domiks, domikTypes]);
    const currentWeather = weather?.current ?? null;
    const goldValue = resources.find(x => x.typeId === GOLD_RESOURCE_TYPE_ID)?.value ?? 0;
    const goldType = resourceTypes.find(x => x.id === GOLD_RESOURCE_TYPE_ID);
    const goldVeinContext = useMemo<GoldVeinContext>(() => ({ domiks, receipts, goldMinedToday, now }), [domiks, receipts, goldMinedToday, now]);
    const recapView = useMemo(() => buildRecapView(recap?.events ?? []), [recap]);
    const recapPending = recap != null && (recap.events.length > 0 || toloka?.progress != null);
    const recapVisible = recapPending && (recap.awaySeconds >= 1800 || recapOpen);
    const friendNeighbor = useMemo(() => {
        const friendIds = new Set(blueprints.filter(b => b.currentReputation >= b.reputationThreshold).map(b => b.neighborId));
        const top = reputation
            .filter(r => friendIds.has(r.neighborId))
            .reduce<NeighborReputationDto | null>((best, r) => best == null || r.points > best.points ? r : best, null);
        return top == null ? null : { logicName: top.neighborLogicName, name: top.neighborName };
    }, [blueprints, reputation]);
    const villageProfile = useMemo(() => {
        if (village?.profileNeighborId == null) {
            return null;
        }
        const neighbor = reputation.find(r => r.neighborId === village.profileNeighborId);
        if (neighbor == null) {
            return null;
        }
        const buildings = villageProfiles
            .filter(effect => effect.neighborId === village.profileNeighborId)
            .map(effect => domikTypes.find(type => type.id === effect.domikTypeId)?.name)
            .filter((name): name is string => name != null);
        return {
            logicName: neighbor.neighborLogicName,
            name: profileGenitiveName[neighbor.neighborLogicName] ?? neighbor.neighborName,
            buildings,
        };
    }, [village, reputation, villageProfiles, domikTypes]);

    const currentCrestIcon = village?.crestIcon ?? 0;
    const currentCrestColor = village?.crestColor ?? 0;
    const villageName = village?.villageName ?? 'Безымянная деревня';
    const identityVisible = identity === 'open' || (identity === 'auto' && village?.villageName === null);

    const villageSlot = document.getElementById('village-slot');

    const openIdentity = () => setIdentity('open');

    const closeIdentity = () => setIdentity('dismissed');

    const actionBusy = useRef(false);
    const reportAction = async (action: () => Promise<void>, successMessage?: string): Promise<boolean> => {
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
        }
    };

    const runAction = async (action: () => Promise<void>, successMessage?: string): Promise<boolean> => {
        if (actionBusy.current) return false;
        actionBusy.current = true;
        try {
            return await reportAction(action, successMessage);
        } finally {
            actionBusy.current = false;
        }
    };

    const runCommand = (command: GameCommand, successMessage?: string): Promise<boolean> => reportAction(async () => {
        await enqueueCommand(command);
        scheduleReload();
    }, successMessage);

    const buy = (typeId: number) => {
        const domikType = domikTypes.find(type => type.id === typeId);
        return runCommand({ kind: 'BuyDomik', args: { typeId } }, domikType == null ? 'Домик построен' : `«${domikType.name}» построен`);
    };

    const upgrade = (id: number) => runCommand({ kind: 'UpgradeDomik', args: { domikId: id } }, 'Улучшение запущено');

    const startManufacture = (domikId: number, receiptId: number, useOptional: boolean, autoRepeat: boolean, workerIds?: number[]) =>
        runCommand({ kind: 'StartManufacture', args: { domikId, receiptId, useOptional, autoRepeat, workerIds: workerIds ?? [] } }, 'Производство запущено');

    const assignWorker = (workerId: number, domikId: number, point: AssignPoint) => {
        const worker = workers.find(item => item.id === workerId);
        const domik = domiks.find(item => item.id === domikId);
        const domikType = domikTypes.find(type => type.id === domik?.typeId);
        if (worker == null || domik == null || domikType == null) {
            return;
        }

        const freeWorkers = workers.filter(item => isWorkerFree(item, now));
        const target = buildAssignTarget(domik, domikType, receipts, resources, freeWorkers, worker, goldVeinContext);
        const name = domikDisplayName(domik.typeId, domik.id, domikType.name, domikType.logicName);
        if (!target.eligible) {
            toast.error(`«${name}»: ${target.reason ?? 'нечего делать'}`);
            return;
        }

        const runnable = target.options.filter(option => option.canRun);
        const single = runnable.length === 1 ? runnable[0] : null;
        if (single != null) {
            void startManufacture(domikId, single.receipt.id, false, false, single.crew.map(item => item.id));
            return;
        }

        setAssignMenu({ workerId, domikId, point });
    };

    const assign = useWorkerAssign(assignWorker);
    const heldWorker = assign.workerId == null ? null : workers.find(worker => worker.id === assign.workerId) ?? null;
    const assignTargets = useMemo(
        () => heldWorker == null
            ? new Map<number, ReturnType<typeof buildAssignTarget>>()
            : buildAssignTargets(domiks, domikTypes, receipts, resources, workers.filter(item => isWorkerFree(item, now)), heldWorker, goldVeinContext),
        [heldWorker, domiks, domikTypes, receipts, resources, workers, now, goldVeinContext],
    );
    const assignMenuView = useMemo(() => {
        if (assignMenu == null) {
            return null;
        }

        const worker = workers.find(item => item.id === assignMenu.workerId);
        const domik = domiks.find(item => item.id === assignMenu.domikId);
        const domikType = domikTypes.find(type => type.id === domik?.typeId);
        if (worker == null || domik == null || domikType == null) {
            return null;
        }

        return {
            worker,
            domikType,
            name: domikDisplayName(domik.typeId, domik.id, domikType.name, domikType.logicName),
            target: buildAssignTarget(domik, domikType, receipts, resources, workers.filter(item => isWorkerFree(item, now)), worker, goldVeinContext),
        };
    }, [assignMenu, workers, domiks, domikTypes, receipts, resources, now, domikDisplayName, goldVeinContext]);
    const railSkillDomikTypeId = useMemo(() => {
        const domikId = assign.hoverDomikId ?? selectedDomikId;
        return domiks.find(item => item.id === domikId)?.typeId ?? null;
    }, [assign.hoverDomikId, selectedDomikId, domiks]);
    const stockFocusTypeIds = useMemo(
        () => selected == null ? [] : [...new Set(selected.receipts.flatMap(receipt => receipt.inputResources.map(item => item.typeId)))],
        [selected],
    );

    const completeOrder = (orderId: number) => {
        const order = orders.find(item => item.id === orderId);
        const neighbor = order == null ? null : reputation.find(item => item.neighborId === order.neighborId) ?? null;
        const tierAhead = order == null || neighbor == null ? null : reputationTierAhead(neighbor.points, order.rewardReputation);
        const successMessage = tierAhead == null || neighbor == null
            ? 'Заказ выполнен'
            : `В ${neighborPrepositionalName[neighbor.neighborLogicName] ?? neighbor.neighborName} ты теперь ${tierAhead.name}`;
        return runCommand({ kind: 'CompleteOrder', args: { orderId } }, successMessage);
    };

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

    const setVillageProfileAction = (neighborId: number) => {
        const neighbor = reputation.find(item => item.neighborId === neighborId);
        const successMessage = neighbor == null
            ? 'Уклад деревни принят'
            : `Деревня переняла уклад «${profileGenitiveName[neighbor.neighborLogicName] ?? neighbor.neighborName}»`;
        return runAction(async () => {
            await setVillageProfileApi(neighborId);
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
    }, errands.find(item => item.id === errandId)?.acceptDate == null ? 'Поручение отклонено' : 'Поручение отозвано');

    const startIncidentSearchAction = (incidentId: number, clueId: number, workerIds: number[]) => runAction(async () => {
        await startIncidentSearchApi(incidentId, clueId, workerIds);
        scheduleReload();
    }, 'Поиски начались');

    const hurryManufactureAction = (manufactureId: number) => runAction(() => hurryManufacture(manufactureId), 'Производство ускорено');

    const toggleManufactureAutoRepeat = (manufactureId: number, next: boolean) => runAction(
        () => setManufactureAutoRepeat(manufactureId, next),
        next ? 'Наряд поставлен' : 'Наряда нет',
    );

    const hurryDomikAction = (domikId: number) => runAction(() => hurryDomik(domikId), 'Улучшение ускорено');

    const startExpeditionAction = (expeditionTypeId: number, workerIds?: number[], provisions?: boolean) => runAction(() => startExpedition(expeditionTypeId, workerIds, provisions), 'Экспедиция отправлена');

    const buyDecorAction = (decorTypeId: number) => runAction(() => buyDecor(decorTypeId), 'Декор куплен');

    const relocateAction = (valleyId: number, newVillageName: string | null, valleyName: string) => {
        return runAction(
            () => relocate(valleyId, newVillageName),
            `Обоз тронулся. «${villageName}» осталась на памятном столбе, впереди – ${valleyName}.`,
        );
    };

    const buyPerkAction = (perkType: number) => runAction(() => buyPerk(perkType), 'Узелок развязан – память пригодилась');

    const setFoodRuleAction = (resourceTypeId: number, reserve: number, forbidden: boolean) => runAction(() => setFoodRule(resourceTypeId, reserve, forbidden));

    const setManufactureMeasureAction = (manufactureId: number, resourceTypeId: number | null, value: number | null) => runAction(
        () => setManufactureMeasure(manufactureId, resourceTypeId, value),
        resourceTypeId == null ? 'Мера снята' : 'Мера назначена',
    );

    const setResourceReserveAction = (resourceTypeId: number, reserve: number) => runAction(() => setResourceReserve(resourceTypeId, reserve));

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

    const selectDomik = (id: number) => {
        setSelectedDomikId(id);
    };

    const scrollToSelectedDomikPanel = () => {
        const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const panel = selectedDomikPanelRef.current;
        panel?.scrollIntoView({ block: 'start', behavior: reduce ? 'auto' : 'smooth' });
        if (panel != null) {
            panel.tabIndex = -1;
            panel.focus({ preventScroll: true });
        }
    };

    const selectDomikFromBoard = (id: number) => {
        selectDomik(id);
        scrollToSelectedDomikPanel();
    };

    const gameTabs: GameTab[] = [
        {
            key: 'shop', label: 'Лавка', icon: <MechanicSprite logicName="shop" size={32} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: () => purchaseDomikTypes == null
                ? <PixelLoader label="Плотник раскладывает чертежи…" />
                : <ShopBox purchaseDomikTypes={purchaseDomikTypes} domikTypes={domikTypes} receipts={receipts}
                    resourceTypes={resourceTypes} resources={resources} blueprints={blueprints} villageLevel={villageLevel}
                    onBuy={buy} onClose={() => { openTab(BOARD_TAB_KEY); }} />,
        },
        {
            key: 'household', label: 'Хозяйство', icon: <AbstractSprite logicName="household" size={32} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: () => <HouseholdBox digest={hudDigest} resourceTypes={resourceTypes} resources={resources} reserves={reserves} ledger={ledger} now={now}
                onSetReserve={setResourceReserveAction} onSelectDomik={selectDomikFromBoard} onOpenTab={openTab}
                onToggleRepeat={toggleManufactureAutoRepeat} />,
        },
        {
            key: 'orders', label: 'Заказы', icon: <MechanicSprite logicName="orders" size={32} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: () => <OrdersBox orders={orders} errands={errands} boardSize={orderBoardSize} freeConcession={orderFreeConcession} workers={workers} reputation={reputation} convoys={convoys} resourceTypes={resourceTypes} resources={resources} now={now}
                domikTypes={domikTypes} villageProfiles={villageProfiles} village={village} villageLevel={villageLevel}
                onComplete={completeOrder} onCancel={cancelOrder} onAcceptErrand={acceptErrandAction} onCancelErrand={cancelErrandAction}
                onBuyFromConvoy={buyFromConvoyAction} onSetFriend={setFriendNeighborAction} onSetVillageProfile={setVillageProfileAction} />,
        },
        {
            key: 'blueprints', label: 'Вехи соседей', icon: <MechanicSprite logicName="blueprints" size={32} className="game-tab-ico" aria-hidden="true" />, visible: blueprints.length > 0 || (decor?.types ?? []).some(x => x.neighborId != null),
            node: () => <BlueprintsBox blueprints={blueprints} domikTypes={domikTypes} decorTypes={decor?.types ?? []} reputations={reputation} receipts={receipts} resourceTypes={resourceTypes} />,
        },
        {
            key: 'expeditions', label: 'Экспедиции', icon: <MechanicSprite logicName="expeditions" size={32} className="game-tab-ico" aria-hidden="true" />, visible: expeditions != null,
            node: () => <ExpeditionsBox expeditions={expeditions} resourceTypes={resourceTypes} decorTypes={decor?.types ?? []} resources={resources} workers={workers} tavernLevel={tavernLevel} now={now} onStart={startExpeditionAction} />,
        },
        {
            key: 'decor', label: 'Декор', icon: <MechanicSprite logicName="decor" size={32} className="game-tab-ico" aria-hidden="true" />, visible: decor != null,
            node: () => <DecorBox decor={decor} resourceTypes={resourceTypes} resources={resources} reputations={reputation} onBuy={buyDecorAction} />,
        },
        {
            key: 'toloka', label: 'Толока', icon: <MechanicSprite logicName="toloka" size={32} className="game-tab-ico" aria-hidden="true" />, visible: toloka != null,
            node: () => <TolokaBox toloka={toloka} resourceTypes={resourceTypes} resources={resources} now={now} onContribute={contributeTolokaAction} onVote={voteTolokaAction} />,
        },
        {
            key: 'market', label: 'Ярмарка', icon: <MechanicSprite logicName="market" size={32} className="game-tab-ico" aria-hidden="true" />, visible: market != null,
            node: () => <MarketBox market={market} resourceTypes={resourceTypes} resources={resources} now={now}
                onPost={postLotAction} onAccept={acceptLotAction} onCancel={cancelLotAction} />,
        },
        {
            key: 'workers', label: 'Трудяги', icon: <MechanicSprite logicName="workers" size={32} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: () => <WorkersBox workers={workers} domikTypes={domikTypes} domiks={domiks} receipts={receipts} expeditions={expeditions} errands={errands} incident={incident} domikIncident={domikIncident} cloaks={cloaks} sickTypes={sickTypes} resourceTypes={resourceTypes} resources={resources} villageLevel={villageLevel} tavernLevel={tavernLevel} larder={larder} onSetFoodRule={setFoodRuleAction} now={now} />,
        },
        {
            key: 'journal', label: 'Журнал', icon: <AbstractSprite logicName="journal" size={32} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: () => <JournalBox events={events} resourceTypes={resourceTypes} domikTypes={domikTypes} decorTypes={decor?.types ?? []} neighbors={reputation} now={now} />,
        },
        {
            key: 'guestbook', label: 'Гости', icon: <MechanicSprite logicName="guestbook" size={32} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: () => <GuestbookBox now={now} />,
        },
        {
            key: 'memory', label: 'Память', icon: <AbstractSprite logicName="prestige_new_valley" size={32} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: () => <RelocationBox relocation={relocation} villageName={villageName} onRelocate={relocateAction} onBuyPerk={buyPerkAction} />,
        },
    ];
    const visibleGameTabs = gameTabs.filter(tab => tab.visible);
    const activeGameTab = routeTab == null ? undefined : visibleGameTabs.find(tab => tab.key === routeTab);
    const onBoard = routeTab == null;
    const strayTab = routeTab != null && activeGameTab == null && !loading && domikTypes.length > 0;
    const mechanicTab = selected == null ? undefined : visibleGameTabs.find(tab => tab.key === MECHANIC_TAB[selected.domikType.logicName]);

    return (
        <ResourceInfoProvider resourceTypes={resourceTypes} domikTypes={domikTypes} receipts={receipts}>
        <ActionBusyProvider>
        <div className="game" style={{ '--hud-sticky-offset': `${hudStickyOffset}px` } as React.CSSProperties}>
            {loading &&
                <div className="game-loading">
                    <PixelLoader label="Загрузка деревни…" />
                </div>
            }
            <OfflineBanner staleSince={staleSince} />
            {villageSlot != null && createPortal(
                <h1 className="village-title">
                    <button type="button" className="village-identity" title="Настроить деревню" onClick={openIdentity}>
                        <Crest icon={currentCrestIcon} color={currentCrestColor} />
                        <span className="section-title village-name">{villageName}</span>
                    </button>
                </h1>,
                villageSlot)}
            <PerfZone id="шапка">
            <VillageHud resources={resources} resourceTypes={resourceTypes} domikTypes={domikTypes} plodder={plodder} digest={hudDigest}
                villageLevel={villageLevel} weather={weather} now={now} onStickyOffsetChange={setHudStickyOffset} villageProfile={villageProfile}
                onOpenTab={openTab} compact={!onBoard} />
            </PerfZone>
            {strayTab && <Navigate to="/domiki-page" replace />}
            <GameTabsNav tabs={visibleGameTabs} activeKey={activeGameTab?.key ?? BOARD_TAB_KEY} />
            {onBoard && <>
                <GoalCard goals={goals} resourceTypes={resourceTypes} />
                {incident != null && <IncidentCard incident={incident} workers={workers} now={now} onStartSearch={startIncidentSearchAction} />}
                {domikIncident != null && <DomikIncidentCard incident={domikIncident} workers={workers} domikTypes={domikTypes} now={now} onStartSearch={startIncidentSearchAction} />}
            </>}
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
            {onBoard && <>
            <PerfZone id="двор">
            <VillageYard domiks={domiks} domikTypes={domikTypes} decor={decor} workers={workers}
                villageLevel={villageLevel} currentWeather={currentWeather} selectedDomikId={selectedDomikId}
                displayName={domik => {
                    const domikType = domikTypes.find(type => type.id === domik.typeId);
                    return domikType == null ? '' : domikDisplayName(domik.typeId, domik.id, domikType.name, domikType.logicName);
                }}
                onSelect={selectDomik}
                recapPending={recapPending}
                onOpenRecap={() => { setRecapOpen(true); }}
                activeExpeditionNames={(expeditions?.active ?? []).map(e => e.expeditionName)}
                friendNeighbor={friendNeighbor} />
            </PerfZone>
            <div className="workspace">
                <div className="worker-rail-slot">
                    <div className="worker-rail-float">
                        <div className="yard-rail">
                            <PerfZone id="рельс">
                                <WorkerRail workers={workers} domikTypes={domikTypes} now={now} skillDomikTypeId={railSkillDomikTypeId}
                                    heldWorkerId={assign.workerId} onGrab={assign.grab} onCancel={assign.cancel} />
                            </PerfZone>
                            <PerfZone id="закрома">
                                <StockRail resources={resources} resourceTypes={resourceTypes} digest={hudDigest}
                                    ledger={ledger} reserves={reserves} focusTypeIds={stockFocusTypeIds} />
                            </PerfZone>
                        </div>
                    </div>
                </div>
                <section className="village">
                    <PerfZone id="сетка">
                        <DomikGridSection domiks={domiks} domikTypes={domikTypes} receipts={receipts} resources={resources}
                            resourceTypes={resourceTypes} currentWeather={currentWeather} now={now} sortMode={sortMode}
                            onSortChange={changeSortMode} selectedDomikId={selectedDomikId} displayName={domikDisplayName}
                            onSelect={selectDomik} workers={workers}
                            assign={{ active: assign.workerId != null, dragging: assign.dragging, targets: assignTargets, hoverDomikId: assign.hoverDomikId, onDrop: assign.drop }} />
                    </PerfZone>
                </section>
                {selected != null && <div className="actions-scrim" role="presentation" onClick={() => { setSelectedDomikId(null); }} />}
                <PerfZone id="карточка">
                    <SelectedDomikPanel ref={selectedDomikPanelRef} selected={selected} resources={resources} resourceTypes={resourceTypes} receipts={receipts} blueprints={blueprints}
                        workers={workers} goals={goals} villageLevel={villageLevel} currentWeather={currentWeather} sickTypes={sickTypes} now={now}
                        goldValue={goldValue} goldType={goldType} goldVein={goldVeinContext} plodderFree={plodder.free} displayName={domikDisplayName}
                        mechanicTab={mechanicTab == null ? null : { key: mechanicTab.key, label: mechanicTab.label }} onOpenTab={openTab}
                        onClose={() => setSelectedDomikId(null)} onUpgrade={upgrade} onHurryDomik={hurryDomikAction}
                        onStartManufacture={startManufacture} onHurryManufacture={hurryManufactureAction}
                        elderHouseLevel={ledger?.level ?? 0}
                        onToggleManufactureRepeat={toggleManufactureAutoRepeat} onSetManufactureMeasure={setManufactureMeasureAction} />
                </PerfZone>
            </div>
            {assign.dragging && heldWorker != null &&
                <AssignGhost ghost={assign.ghost} name={heldWorker.name} />
            }
            {assignMenu != null && assignMenuView != null &&
                <ReceiptDropMenu target={assignMenuView.target} domikName={assignMenuView.name}
                    domikTypeId={assignMenuView.domikType.id} worker={assignMenuView.worker} point={assignMenu.point}
                    resourceTypes={resourceTypes}
                    onPick={(receiptId, workerIds) => {
                        setAssignMenu(null);
                        void startManufacture(assignMenu.domikId, receiptId, false, false, workerIds);
                    }}
                    onClose={() => { setAssignMenu(null); }} />
            }
            </>}
            {!onBoard && activeGameTab != null &&
                <section className="game-tab-panel" aria-label={activeGameTab.label}>
                    <PerfZone id="вкладка">{activeGameTab.node()}</PerfZone>
                </section>
            }
        </div>
        </ActionBusyProvider>
        </ResourceInfoProvider>
    );
};

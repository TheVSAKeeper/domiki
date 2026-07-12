import { useEffect, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { Link } from 'react-router-dom';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import ArrowUpIcon from 'pixelarticons/svg/arrow-up.svg?react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import PlayIcon from 'pixelarticons/svg/play.svg?react';
import SettingsIcon from 'pixelarticons/svg/settings-cog.svg?react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import SaveIcon from 'pixelarticons/svg/save.svg?react';
import ZapIcon from 'pixelarticons/svg/zap.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import EarthIcon from 'pixelarticons/svg/earth.svg?react';
import ClipboardIcon from 'pixelarticons/svg/clipboard.svg?react';
import NoteIcon from 'pixelarticons/svg/note.svg?react';
import BackpackIcon from 'pixelarticons/svg/backpack.svg?react';
import GardenIcon from 'pixelarticons/svg/tree.svg?react';
import BuildingCommunityIcon from 'pixelarticons/svg/building-community.svg?react';
import UsersIcon from 'pixelarticons/svg/users.svg?react';
import BellIcon from 'pixelarticons/svg/bell.svg?react';
import BellOffIcon from 'pixelarticons/svg/bell-off.svg?react';
import GridIcon from 'pixelarticons/svg/grid-3x3.svg?react';
import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import JournalIcon from 'pixelarticons/svg/article.svg?react';
import { apiPost, ApiError, completeOrder as completeOrderApi } from '../services/api';
import { useToast } from '../services/toast';
import { disablePush, enablePush, getPushState } from '../services/push';
import type { PushState } from '../services/push';
import { useGameData } from '../hooks/useGameData';
import { COIN_RESOURCE_TYPE_ID, EXPEDITION_LOOT_KIND_DECOR, EXPEDITION_LOOT_KIND_TRAIT_UPGRADE, GOLD_RESOURCE_TYPE_ID, canAffordUpgrade, canInstaFinish, computeReceiptView, computeSelectedDomikView, instaFinishCost, isWorkerFree, progressPercent, sortDomiks, workerFitness } from '../utils/game';
import type { DomikSortMode } from '../utils/game';
import { domikThemedName } from '../utils/domikNames';
import { formatDuration, remainingSeconds } from '../utils/time';
import { domikLore } from '../utils/domikLore';
import { ManufactureBox } from './ManufactureBox';
import { ProgressBar } from './ProgressBar';
import { ResourcesBox } from './ResourcesBox';
import { UpgradeBox } from './UpgradeBox';
import { OrdersBox } from './OrdersBox';
import { GoalCard } from './GoalCard';
import { WorkersBox } from './WorkersBox';
import { BlueprintsBox } from './BlueprintsBox';
import { ExpeditionsBox } from './ExpeditionsBox';
import { DecorBox } from './DecorBox';
import { TolokaBox } from './TolokaBox';
import { MarketBox } from './MarketBox';
import { ResourceChip } from './ResourceChip';
import { JournalBox } from './JournalBox';
import { AbstractSprite, DomikSprite, MechanicSprite, ResourceSprite, WeatherSprite, WorkerSprite } from './sprites';
import { isSkilledWorker } from '../utils/worker';
import { AnimatedDomikSprite } from './AnimatedDomikSprite';
import { HudResource } from './HudResource';
import { DEFAULT_VILLAGE_ICON, VILLAGE_CREST_COLORS, VILLAGE_CREST_ICONS } from '../constants/village';
import { buildRecapView } from '../utils/recap';


const MECHANIC_TAB: Record<string, string> = {
    market_yard: 'market',
    gathering: 'toloka',
    scout_hut: 'expeditions',
};

type SortModeEntry = { mode: DomikSortMode; label: string; Icon: typeof StoreIcon };

const SORT_MODES: readonly [SortModeEntry, ...SortModeEntry[]] = [
    { mode: 'attention', label: 'Внимание', Icon: BellIcon },
    { mode: 'type', label: 'Тип', Icon: GridIcon },
    { mode: 'level', label: 'Уровень', Icon: ChevronUpIcon },
];

interface GameTab {
    key: string;
    label: string;
    Icon: typeof StoreIcon;
    visible: boolean;
    node: ReactNode;
}

export const DomikiPage = () => {
    const toast = useToast();
    const { domiks, domikTypes, resourceTypes, receipts, resources, orders, reputation, blueprints, village, villageLevel, weather, expeditions, decor, toloka, market, goals, workers, purchaseDomikTypes, now, reload, refreshPurchaseTypes, setVillage, setFeedWorkers, hurryManufacture, setManufactureAutoRepeat, hurryDomik, startExpedition, buyDecor, contributeToloka, postLot, acceptLot, cancelLot, recap, clearRecap, events } =
        useGameData();

    const [shopVisible, setShopVisible] = useState(false);
    const [selectedDomikId, setSelectedDomikId] = useState<number | null>(null);
    const [activeTab, setActiveTab] = useState('');
    const [expandedReceiptId, setExpandedReceiptId] = useState<number | null>(null);
    const [optionalReceiptIds, setOptionalReceiptIds] = useState<ReadonlySet<number>>(new Set());
    const [autoRepeatReceiptIds, setAutoRepeatReceiptIds] = useState<ReadonlySet<number>>(new Set());
    const [manualReceiptIds, setManualReceiptIds] = useState<ReadonlySet<number>>(new Set());
    const [selectedWorkerIdsByReceipt, setSelectedWorkerIdsByReceipt] = useState<Record<number, number[]>>({});
    const [identityOpen, setIdentityOpen] = useState(false);
    const [levelFlyout, setLevelFlyout] = useState<{ top: number; left: number; width: number } | null>(null);
    const villageLevelRef = useRef<HTMLDivElement>(null);
    const openLevelFlyout = () => {
        const rect = villageLevelRef.current?.getBoundingClientRect();
        if (rect != null) {
            setLevelFlyout({ top: rect.bottom + 6, left: rect.left, width: rect.width });
        }
    };
    const closeLevelFlyout = () => setLevelFlyout(null);
    const gameTabsRef = useRef<HTMLDivElement>(null);
    const gameTabPanelRef = useRef<HTMLDivElement>(null);
    const tabAnchorReady = useRef(false);
    const scrollTabsPending = useRef(false);
    const hudSentinelRef = useRef<HTMLDivElement>(null);
    const [hudAway, setHudAway] = useState(false);
    const [hudPinnedOpen, setHudPinnedOpen] = useState(false);
    const [identityDismissed, setIdentityDismissed] = useState(false);
    const [draftVillageName, setDraftVillageName] = useState('');
    const [draftCrestIcon, setDraftCrestIcon] = useState(0);
    const [draftCrestColor, setDraftCrestColor] = useState(0);
    const [sortMode, setSortMode] = useState<DomikSortMode>(() => {
        const saved = localStorage.getItem('domik-sort-mode');
        return saved === 'type' || saved === 'level' ? saved : 'attention';
    });
    const changeSortMode = (mode: DomikSortMode) => {
        setSortMode(mode);
        localStorage.setItem('domik-sort-mode', mode);
    };
    const [sortOpen, setSortOpen] = useState(false);
    const sortRef = useRef<HTMLDivElement>(null);
    const activeSort = SORT_MODES.find(item => item.mode === sortMode) ?? SORT_MODES[0];
    const [pushState, setPushState] = useState<PushState>('unsupported');
    const [pushBusy, setPushBusy] = useState(false);

    useEffect(() => {
        void getPushState().then(setPushState);
    }, []);

    const togglePush = async () => {
        if (pushState === 'denied') {
            toast.error('Уведомления заблокированы в настройках браузера');
            return;
        }

        setPushBusy(true);
        try {
            if (pushState === 'on') {
                await disablePush();
                toast.success('Уведомления выключены');
            } else {
                await enablePush();
                toast.success('Уведомления включены');
            }
        } catch (err) {
            toast.error(err instanceof Error ? err.message : 'Не удалось изменить настройку уведомлений');
        } finally {
            setPushState(await getPushState());
            setPushBusy(false);
        }
    };

    useEffect(() => {
        if (!sortOpen) {
            return;
        }

        const onDown = (event: MouseEvent) => {
            if (sortRef.current != null && !sortRef.current.contains(event.target as Node)) {
                setSortOpen(false);
            }
        };

        document.addEventListener('mousedown', onDown);
        return () => { document.removeEventListener('mousedown', onDown); };
    }, [sortOpen]);

    useEffect(() => {
        if (!tabAnchorReady.current) {
            tabAnchorReady.current = true;
            return;
        }

        if (!scrollTabsPending.current) {
            return;
        }

        scrollTabsPending.current = false;
        gameTabsRef.current?.scrollIntoView({ behavior: 'auto', block: 'start' });
    }, [activeTab]);

    useEffect(() => {
        const sentinel = hudSentinelRef.current;
        if (sentinel == null) {
            return;
        }

        const observer = new IntersectionObserver(entries => {
            const entry = entries[0];
            if (entry == null) {
                return;
            }

            setHudAway(!entry.isIntersecting);
            if (entry.isIntersecting) {
                setHudPinnedOpen(false);
            }
        });

        observer.observe(sentinel);
        return () => { observer.disconnect(); };
    }, []);

    const toggleExpand = (receiptId: number) =>
        setExpandedReceiptId(prev => (prev === receiptId ? null : receiptId));

    const toggleOptional = (receiptId: number) => setOptionalReceiptIds(prev => {
        const next = new Set(prev);
        if (next.has(receiptId)) {
            next.delete(receiptId);
        } else {
            next.add(receiptId);
        }
        return next;
    });

    const toggleAutoRepeat = (receiptId: number) => setAutoRepeatReceiptIds(prev => {
        const next = new Set(prev);
        if (next.has(receiptId)) {
            next.delete(receiptId);
        } else {
            next.add(receiptId);
        }
        return next;
    });

    const toggleManual = (receiptId: number) => {
        setManualReceiptIds(prev => {
            const next = new Set(prev);
            if (next.has(receiptId)) {
                next.delete(receiptId);
            } else {
                next.add(receiptId);
            }
            return next;
        });
        setSelectedWorkerIdsByReceipt(prev => ({ ...prev, [receiptId]: [] }));
    };

    const toggleSelectedWorker = (receiptId: number, workerId: number, maxCount: number) => {
        setSelectedWorkerIdsByReceipt(prev => {
            const current = prev[receiptId] ?? [];
            if (current.includes(workerId)) {
                return { ...prev, [receiptId]: current.filter(id => id !== workerId) };
            }

            if (current.length >= maxCount) {
                return prev;
            }

            return { ...prev, [receiptId]: [...current, workerId] };
        });
    };

    const plodder = useMemo(() => ({
        max: workers.length,
        free: workers.filter(worker => isWorkerFree(worker, now)).length,
    }), [workers, now]);
    const selected = useMemo(
        () => computeSelectedDomikView(selectedDomikId, domiks, domikTypes, receipts, resources, now),
        [selectedDomikId, domiks, domikTypes, receipts, resources, now],
    );
    const sortedDomiks = useMemo(
        () => sortDomiks(domiks, domikTypes, resources, sortMode),
        [domiks, domikTypes, resources, sortMode],
    );
    const domikOrdinals = useMemo(() => {
        const idsByType = new Map<number, number[]>();
        for (const domik of domiks) {
            const ids = idsByType.get(domik.typeId) ?? [];
            ids.push(domik.id);
            idsByType.set(domik.typeId, ids);
        }

        const ordinalById = new Map<number, number>();
        const countByType = new Map<number, number>();
        for (const [typeId, ids] of idsByType) {
            const sortedIds = [...ids].sort((a, b) => a - b);
            countByType.set(typeId, sortedIds.length);
            sortedIds.forEach((id, index) => ordinalById.set(id, index + 1));
        }

        return { ordinalById, countByType };
    }, [domiks]);
    const domikDisplayName = (typeId: number, id: number, name: string) =>
        (domikOrdinals.countByType.get(typeId) ?? 1) > 1
            ? domikThemedName(name, typeId, domikOrdinals.ordinalById.get(id) ?? 1)
            : name;
    const currentWeather = weather?.current ?? null;
    const hudCollapsed = hudAway && !hudPinnedOpen;
    const coinType = resourceTypes.find(t => t.id === COIN_RESOURCE_TYPE_ID);
    const coinValue = resources.find(r => r.typeId === COIN_RESOURCE_TYPE_ID)?.value;
    const weatherEffect = selected == null
        ? null
        : currentWeather?.effects.find(effect => effect.domikTypeId === selected.domikType.id) ?? null;
    const goldValue = resources.find(x => x.typeId === GOLD_RESOURCE_TYPE_ID)?.value ?? 0;
    const goldType = resourceTypes.find(x => x.id === GOLD_RESOURCE_TYPE_ID);
    const recapView = useMemo(() => buildRecapView(recap?.events ?? []), [recap]);
    const recapVisible = recap != null && recap.events.length > 0 && recap.awaySeconds >= 1800;

    const currentCrestIcon = village?.crestIcon ?? 0;
    const currentCrestColor = village?.crestColor ?? 0;
    const VillageIcon = VILLAGE_CREST_ICONS[currentCrestIcon] ?? DEFAULT_VILLAGE_ICON;
    const villageColor = VILLAGE_CREST_COLORS[currentCrestColor] ?? VILLAGE_CREST_COLORS[0];
    const villageName = village?.villageName ?? 'Безымянная деревня';
    const identityVisible = identityOpen || village?.villageName === null && !identityDismissed;

    const openIdentity = () => {
        setDraftVillageName(village?.villageName ?? '');
        setDraftCrestIcon(village?.crestIcon ?? 0);
        setDraftCrestColor(village?.crestColor ?? 0);
        setIdentityDismissed(false);
        setIdentityOpen(true);
    };

    const closeIdentity = () => {
        setIdentityDismissed(true);
        setIdentityOpen(false);
    };

    const runAction = async (action: () => Promise<void>, successMessage?: string) => {
        try {
            await action();
            if (successMessage != null) {
                toast.success(successMessage);
            }
        } catch (err) {
            if (err instanceof ApiError) {
                toast.error(err.message);
                return;
            }
            throw err;
        }
    };

    const buy = (typeId: number) => {
        const domikType = domikTypes.find(type => type.id === typeId);
        return runAction(async () => {
            await apiPost(`Domiki/BuyDomik/${typeId}`);
            await reload();
            await refreshPurchaseTypes();
        }, domikType == null ? 'Домик построен' : `«${domikType.name}» построен`);
    };

    const upgrade = (id: number) => runAction(async () => {
        await apiPost(`Domiki/UpgradeDomik/${id}`);
        await reload();
    }, 'Улучшение запущено');

    const startManufacture = (domikId: number, receiptId: number, useOptional: boolean, autoRepeat: boolean, workerIds?: number[]) => runAction(async () => {
        const workerIdsQuery = (workerIds ?? []).map(id => `&workerIds=${id}`).join('');
        await apiPost(`Domiki/StartManufacture/${domikId}/${receiptId}?useOptional=${String(useOptional)}&autoRepeat=${String(autoRepeat)}${workerIdsQuery}`);
        setSelectedWorkerIdsByReceipt(prev => ({ ...prev, [receiptId]: [] }));
        await reload();
    }, 'Производство запущено');

    const completeOrder = (orderId: number) => runAction(async () => {
        await completeOrderApi(orderId);
        await reload();
        if (shopVisible) {
            await refreshPurchaseTypes();
        }
    }, 'Заказ выполнен');

    const hurryManufactureAction = (manufactureId: number) => runAction(() => hurryManufacture(manufactureId), 'Производство ускорено');

    const toggleManufactureAutoRepeat = (manufactureId: number, next: boolean) => runAction(() => setManufactureAutoRepeat(manufactureId, next));

    const hurryDomikAction = (domikId: number) => runAction(() => hurryDomik(domikId), 'Улучшение ускорено');

    const startExpeditionAction = (expeditionTypeId: number, workerIds?: number[], provisions?: boolean) => runAction(() => startExpedition(expeditionTypeId, workerIds, provisions), 'Экспедиция отправлена');

    const toggleFeedWorkers = (enabled: boolean) => runAction(() => setFeedWorkers(enabled));

    const buyDecorAction = (decorTypeId: number) => runAction(() => buyDecor(decorTypeId), 'Декор куплен');

    const contributeTolokaAction = (amount: number) => runAction(() => contributeToloka(amount), 'Вклад принят');

    const postLotAction = (giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number) =>
        runAction(() => postLot(giveResourceTypeId, giveValue, wantResourceTypeId, wantValue), 'Лот выставлен');

    const acceptLotAction = (lotId: number) => runAction(() => acceptLot(lotId), 'Сделка совершена');

    const cancelLotAction = (lotId: number) => runAction(() => cancelLot(lotId), 'Лот снят');

    const saveIdentity = () => runAction(async () => {
        await setVillage(draftVillageName, draftCrestIcon, draftCrestColor);
        setIdentityDismissed(true);
        setIdentityOpen(false);
    });

    const toggleShop = () => runAction(async () => {
        const willShow = !shopVisible;
        setShopVisible(willShow);
        if (willShow && purchaseDomikTypes == null) {
            await refreshPurchaseTypes();
        }
    });

    const selectDomik = (id: number, logicName: string) => {
        setSelectedDomikId(id);
        const mechTab = MECHANIC_TAB[logicName];
        if (mechTab && mechTab !== activeTab) {
            scrollTabsPending.current = true;
            setActiveTab(mechTab);
        }
    };

    const gameTabs: GameTab[] = [
        {
            key: 'orders', label: 'Заказы', Icon: ClipboardIcon, visible: true,
            node: <><GoalCard goals={goals} resourceTypes={resourceTypes} /><OrdersBox orders={orders} reputation={reputation} resourceTypes={resourceTypes} resources={resources} now={now} onComplete={completeOrder} /></>,
        },
        {
            key: 'blueprints', label: 'Вехи соседей', Icon: NoteIcon, visible: blueprints.length > 0 || (decor?.types ?? []).some(x => x.neighborId != null),
            node: <BlueprintsBox blueprints={blueprints} domikTypes={domikTypes} decorTypes={decor?.types ?? []} reputations={reputation} />,
        },
        {
            key: 'expeditions', label: 'Экспедиции', Icon: BackpackIcon, visible: expeditions != null,
            node: <ExpeditionsBox expeditions={expeditions} resourceTypes={resourceTypes} decorTypes={decor?.types ?? []} resources={resources} workers={workers} now={now} onStart={startExpeditionAction} />,
        },
        {
            key: 'decor', label: 'Декор', Icon: GardenIcon, visible: decor != null,
            node: <DecorBox decor={decor} resourceTypes={resourceTypes} resources={resources} reputations={reputation} onBuy={buyDecorAction} />,
        },
        {
            key: 'toloka', label: 'Толока', Icon: BuildingCommunityIcon, visible: toloka != null,
            node: <TolokaBox toloka={toloka} resourceTypes={resourceTypes} resources={resources} now={now} onContribute={contributeTolokaAction} />,
        },
        {
            key: 'market', label: 'Ярмарка', Icon: StoreIcon, visible: market != null,
            node: <MarketBox market={market} resourceTypes={resourceTypes} resources={resources} now={now}
                onPost={postLotAction} onAccept={acceptLotAction} onCancel={cancelLotAction} />,
        },
        {
            key: 'workers', label: 'Трудяги', Icon: UsersIcon, visible: true,
            node: <WorkersBox workers={workers} domikTypes={domikTypes} domiks={domiks} expeditions={expeditions} feedWorkers={village?.feedWorkers ?? false} now={now} onToggleFeedWorkers={toggleFeedWorkers} />,
        },
        {
            key: 'journal', label: 'Журнал', Icon: JournalIcon, visible: true,
            node: <JournalBox events={events} resourceTypes={resourceTypes} domikTypes={domikTypes} decorTypes={decor?.types ?? []} now={now} />,
        },
    ];
    const visibleGameTabs = gameTabs.filter(tab => tab.visible);
    const activeGameTab = visibleGameTabs.find(tab => tab.key === activeTab) ?? visibleGameTabs[0];
    const nextGoal = villageLevel?.upcomingUnlocks.find((unlock): unlock is typeof unlock & { level: number } => unlock.level != null);

    return (
        <div className="game">
            <div className="hud-sentinel" ref={hudSentinelRef} aria-hidden="true" />
            <header className={'hud pixel-panel' + (hudCollapsed ? ' hud-collapsed' : '')}>
                <button type="button" className="hud-compact" onClick={() => { setHudPinnedOpen(true); }} title="Развернуть панель">
                    {coinType != null && coinValue != null && <HudResource resourceType={coinType} value={coinValue} />}
                    <div className="resource-box">
                        <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                        <span className="resource-value">{plodder.free}/{plodder.max}</span>
                    </div>
                    {villageLevel != null &&
                        <span className="resource-box">
                            <MechanicSprite logicName="obzhitost" size={24} className="village-level-ico" aria-hidden="true" />
                            <span className="resource-value">{villageLevel.level}</span>
                        </span>}
                    {currentWeather != null && <WeatherSprite logicName={currentWeather.logicName} className="weather-ico" aria-hidden="true" />}
                    <ChevronDownIcon className="btn-ico hud-compact-caret" aria-hidden="true" />
                </button>
                <div className="resources">
                    {resourceTypes.length > 0 &&
                        resources.map(resource => {
                            const resourceType = resourceTypes.find(x => x.id === resource.typeId);
                            if (resourceType == null) {
                                return null;
                            }

                            return <HudResource key={resource.typeId} resourceType={resourceType} value={resource.value} />;
                        })
                    }
                </div>
                {domikTypes.length > 0 &&
                    <div className="resource-box hud-plodder" title="Трудяги">
                        <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                        <span className="resource-value">{plodder.free}/{plodder.max}</span>
                    </div>
                }
                {villageLevel != null &&
                    <>
                        <div className="village-level" ref={villageLevelRef}
                            onMouseEnter={openLevelFlyout} onMouseLeave={closeLevelFlyout}
                            onFocus={openLevelFlyout} onBlur={closeLevelFlyout}>
                            <button type="button" className="village-level-box"
                                title={`Постройки ${villageLevel.buildings}, жители ${villageLevel.residents}, репутация ${villageLevel.reputation}, уют ${villageLevel.comfort}`}>
                                <MechanicSprite logicName="obzhitost" size={24} className="village-level-ico" aria-hidden="true" />
                                <span className="village-level-label">Обжитость</span>
                                <span className="village-level-value">{villageLevel.level}</span>
                            </button>
                        </div>
                        {levelFlyout != null && createPortal(
                            <div className="village-level-flyout" style={{ top: levelFlyout.top, left: levelFlyout.left, width: levelFlyout.width }}>
                                <span>Постройки: {villageLevel.buildings}</span>
                                <span>Жители: {villageLevel.residents}</span>
                                <span>Репутация: {villageLevel.reputation}</span>
                                <span>Уют: {villageLevel.comfort}</span>
                                {[
                                    ...villageLevel.upcomingUnlocks.filter(unlock => unlock.level != null).slice(0, 3),
                                    ...villageLevel.upcomingUnlocks.filter(unlock => unlock.level == null),
                                ].map(unlock => (
                                    <span key={`${unlock.label}-${unlock.level ?? unlock.requirement}`} className="village-level-next">
                                        {unlock.label}: {unlock.level != null ? unlock.level : unlock.requirement}
                                    </span>
                                ))}
                            </div>,
                            document.body)}
                        {nextGoal != null &&
                            <div className="hud-goal"
                                title={`Откроется при обжитости ${nextGoal.level}: ${nextGoal.label}`}>
                                <LockIcon className="hud-goal-ico" aria-hidden="true" />
                                <span className="hud-goal-label">{nextGoal.label}</span>
                                <ProgressBar value={villageLevel.level} max={nextGoal.level}
                                    label={`${villageLevel.level}/${nextGoal.level}`} />
                            </div>}
                    </>
                }
                {weather != null && currentWeather != null &&
                    <div className="weather-strip" title={currentWeather.weatherName}>
                        <WeatherSprite logicName={currentWeather.logicName} className="weather-ico" aria-hidden="true" />
                        <span className="weather-name">{currentWeather.weatherName}</span>
                        {currentWeather.effects.some(effect => effect.outputPercent !== 100) &&
                            <div className="weather-effects">
                                {currentWeather.effects.filter(effect => effect.outputPercent !== 100).map(effect => {
                                    const domikType = domikTypes.find(type => type.id === effect.domikTypeId);
                                    if (domikType == null) {
                                        return null;
                                    }

                                    const delta = effect.outputPercent - 100;
                                    const buff = delta > 0;
                                    return (
                                        <span key={effect.domikTypeId}
                                            className={'weather-effect' + (buff ? ' weather-effect-buff' : ' weather-effect-nerf')}
                                            title={`${domikType.name}: ${buff ? "+" : ""}${delta}% выход`}>
                                            <DomikSprite className="weather-effect-ico" logicName={domikType.logicName} />
                                            {buff ? '+' : ''}{delta}%
                                        </span>
                                    );
                                })}
                            </div>
                        }
                        {weather.forecast.length > 0 &&
                        <div className="weather-forecast">
                            <span className="weather-forecast-label">далее</span>
                            {weather.forecast.map(period => {
                                const hoursAhead = Math.max(1, Math.round(remainingSeconds(period.startDate, now) / 3600));
                                const hint = period.effects
                                    .filter(effect => effect.outputPercent !== 100)
                                    .flatMap(effect => {
                                        const domikType = domikTypes.find(type => type.id === effect.domikTypeId);
                                        return domikType != null ? [{ delta: effect.outputPercent - 100, domikType }] : [];
                                    })
                                    .sort((a, b) => Math.abs(b.delta) - Math.abs(a.delta))[0];
                                return (
                                    <span key={period.startDate} className="weather-chip" title={period.weatherName}>
                                        <WeatherSprite logicName={period.logicName} size={24} className="weather-chip-ico" aria-hidden="true" />
                                        через {hoursAhead}ч
                                        {hint != null &&
                                            <span className={'weather-effect' + (hint.delta > 0 ? ' weather-effect-buff' : ' weather-effect-nerf')}
                                                title={`${hint.domikType.name}: ${hint.delta > 0 ? '+' : ''}${hint.delta}% выход`}>
                                                <DomikSprite className="weather-effect-ico" logicName={hint.domikType.logicName} />
                                                {hint.delta > 0 ? '+' : ''}{hint.delta}%
                                            </span>}
                                    </span>
                                );
                            })}
                        </div>
                        }
                    </div>
                }
                {hudAway && hudPinnedOpen &&
                    <button type="button" className="hud-fold" onClick={() => { setHudPinnedOpen(false); }} title="Свернуть панель">
                        <ChevronUpIcon className="btn-ico" aria-hidden="true" />
                    </button>}
            </header>
            {identityVisible &&
                <div className="modal-backdrop" role="presentation">
                    <form className="identity-modal pixel-panel" onSubmit={event => { event.preventDefault(); void saveIdentity(); }}>
                        <div className="identity-modal-head">
                            <h2 className="panel-title">Деревня</h2>
                            <button type="button" className="identity-button" title="Закрыть" onClick={closeIdentity}>
                                <CloseIcon className="btn-ico" aria-hidden="true" />
                            </button>
                        </div>
                        <label className="identity-field">
                            <span className="panel-label">Название деревни</span>
                            <input value={draftVillageName} maxLength={24} onChange={event => setDraftVillageName(event.target.value)} />
                        </label>
                        <div className="identity-field">
                            <span className="panel-label">Герб</span>
                            <div className="crest-options">
                                {VILLAGE_CREST_ICONS.map((Icon, index) =>
                                    <button key={index} type="button"
                                        className={'crest-option' + (draftCrestIcon === index ? ' crest-option-selected' : '')}
                                        onClick={() => setDraftCrestIcon(index)}>
                                        <Icon className="crest-ico" aria-hidden="true" />
                                    </button>,
                                )}
                            </div>
                        </div>
                        <div className="identity-field">
                            <span className="panel-label">Цвет</span>
                            <div className="color-options">
                                {VILLAGE_CREST_COLORS.map((color, index) =>
                                    <button key={color} type="button"
                                        className={'color-option' + (draftCrestColor === index ? ' color-option-selected' : '')}
                                        style={{ backgroundColor: color }}
                                        aria-label={`Цвет ${index + 1}`}
                                        onClick={() => setDraftCrestColor(index)} />,
                                )}
                            </div>
                        </div>
                        <button className="btn-game" type="submit">
                            <SaveIcon className="btn-ico" aria-hidden="true" />
                            Сохранить
                        </button>
                    </form>
                </div>
            }
            {recapVisible &&
                <div className="modal-backdrop" role="presentation">
                    <section className="recap-modal pixel-panel" role="dialog" aria-modal="true" aria-label="Пока вас не было">
                        <div className="recap-modal-head">
                            <h2 className="panel-title">Пока вас не было – {formatDuration(recap.awaySeconds)}</h2>
                            <button type="button" className="identity-button" title="Закрыть" onClick={clearRecap}>
                                <CloseIcon className="btn-ico" aria-hidden="true" />
                            </button>
                        </div>
                        {recapView.toloka.length > 0 &&
                            <div className="recap-section">
                                <span className="panel-label">Толока завершена</span>
                                {recapView.toloka.map((event, index) => {
                                    const name = toloka?.active.tolokaTypeId === event.tolokaTypeId ? toloka.active.name : `Толока #${event.tolokaTypeId}`;
                                    return <span key={`${event.tolokaTypeId}-${index}`} className="recap-line">{name}</span>;
                                })}
                            </div>
                        }
                        {recapView.expeditions.length > 0 &&
                            <div className="recap-section">
                                <span className="panel-label">Экспедиции</span>
                                {recapView.expeditions.map((event, index) => {
                                    const name = expeditions?.types.find(type => type.id === event.expeditionTypeId)?.name ?? `Экспедиция #${event.expeditionTypeId}`;
                                    return (
                                        <div key={`${event.expeditionTypeId}-${index}`} className="recap-row">
                                            <BackpackIcon className="recap-row-ico" aria-hidden="true" />
                                            <span className="recap-line">{name}</span>
                                            <div className="recap-chips">
                                                {event.loot.map((loot, lootIndex) => {
                                                    if (loot.kind === EXPEDITION_LOOT_KIND_DECOR) {
                                                        const decorType = decor?.types.find(x => x.id === loot.decorTypeId);
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
                        {recapView.market.length > 0 &&
                            <div className="recap-section">
                                <span className="panel-label">Ярмарка</span>
                                {recapView.market.map((event, index) => {
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
                        {recapView.produced.length > 0 &&
                            <div className="recap-section">
                                <span className="panel-label">Произведено</span>
                                <div className="recap-chips">
                                    {recapView.produced.map(resource => {
                                        const type = resourceTypes.find(resourceType => resourceType.id === resource.typeId);
                                        return type == null
                                            ? <span key={resource.typeId} className="recap-fallback">Ресурс #{resource.typeId} ×{resource.value}</span>
                                            : <ResourceChip key={resource.typeId} resourceType={type} value={resource.value} />;
                                    })}
                                </div>
                            </div>
                        }
                        {recapView.upgrades.length > 0 &&
                            <div className="recap-section">
                                <span className="panel-label">Постройки улучшены</span>
                                {recapView.upgrades.map((event, index) => {
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
                    </section>
                </div>
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
                    {domiks.length > 1 &&
                        <div className="domik-sort-menu" ref={sortRef}>
                            <button type="button" className="btn-game btn-ghost" aria-expanded={sortOpen}
                                onClick={() => setSortOpen(prev => !prev)}>
                                <activeSort.Icon className="btn-ico" aria-hidden="true" />
                                {activeSort.label}
                                <ChevronDownIcon className="btn-ico" aria-hidden="true" />
                            </button>
                            {sortOpen &&
                                <div className="domik-sort-pop">
                                    {SORT_MODES.map(item =>
                                        <button key={item.mode} type="button"
                                            className={'domik-sort-option' + (item.mode === sortMode ? ' domik-sort-option-active' : '')}
                                            onClick={() => { changeSortMode(item.mode); setSortOpen(false); }}>
                                            <item.Icon className="game-tab-ico" aria-hidden="true" />
                                            {item.label}
                                        </button>,
                                    )}
                                </div>
                            }
                        </div>
                    }
                    {pushState !== 'unsupported' &&
                        <button type="button" className="btn-game btn-ghost" title="Уведомления" aria-label="Уведомления"
                            disabled={pushBusy} onClick={() => void togglePush()}>
                            {pushState === 'on'
                                ? <BellIcon className="btn-ico" aria-hidden="true" />
                                : <BellOffIcon className="btn-ico" aria-hidden="true" />}
                        </button>
                    }
                    <Link className="btn-game" to="/world">
                        <EarthIcon className="btn-ico" aria-hidden="true" />
                        Мир
                    </Link>
                    {purchaseDomikTypes != null &&
                        <button className="btn-game" onClick={() => toggleShop()}>
                            <StoreIcon className="btn-ico" aria-hidden="true" />
                            {shopVisible ? 'Закрыть магазин' : 'Магазин'}
                        </button>
                    }
                </div>
            </div>
            <div className="workspace">
                <section className="village">
                    {shopVisible && purchaseDomikTypes != null &&
                        <div className="purchase-box">
                            {purchaseDomikTypes.length === 0 &&
                                <span className="hint">Магазин пуст</span>
                            }
                            {purchaseDomikTypes.map(purchaseDomikType => {
                                const firstLevel = purchaseDomikType.levels[0];
                                const levelLocked = villageLevel != null && purchaseDomikType.unlockLevel > villageLevel.level;
                                const blueprint = purchaseDomikType.blueprintId == null ? null : blueprints.find(x => x.id === purchaseDomikType.blueprintId) ?? null;
                                const blueprintLocked = blueprint != null && !blueprint.owned;
                                const countGateLocked = purchaseDomikType.availableCount === 0 && purchaseDomikType.nextCountGateLevel != null;
                                const isLocked = levelLocked || blueprintLocked || countGateLocked;
                                const lockTitle = levelLocked
                                    ? `Откроется при обжитости ${purchaseDomikType.unlockLevel}`
                                    : blueprintLocked
                                        ? `Нужен чертёж (репутация ${blueprint.neighborName} ${blueprint.reputationThreshold})`
                                        : countGateLocked
                                            ? `Ещё один – при обжитости ${purchaseDomikType.nextCountGateLevel}`
                                            : undefined;
                                return (
                                    <div key={purchaseDomikType.id} className={'plot plot-shop' + (isLocked ? ' plot-locked' : '')} title={isLocked ? lockTitle : undefined}>
                                        <DomikSprite className="plot-sprite" logicName={purchaseDomikType.logicName} />
                                        <span className="plot-name">{purchaseDomikType.name}</span>
                                        <span className="plot-status">
                                            {isLocked ? lockTitle : `Доступно: ${purchaseDomikType.availableCount}/${purchaseDomikType.maxCount}`}
                                        </span>
                                        <ResourcesBox resources={firstLevel?.resources ?? []} resourceTypes={resourceTypes} />
                                        <button className="btn-game" disabled={isLocked} title={lockTitle} onClick={() => buy(purchaseDomikType.id)}>
                                            {blueprintLocked || countGateLocked ? <LockIcon className="btn-ico" aria-hidden="true" /> : <BuildingIcon className="btn-ico" aria-hidden="true" />}
                                            Купить
                                        </button>
                                    </div>
                                );
                            })
                            }
                        </div>
                    }
                    <div className="domiks">
                        {domikTypes.length > 0 &&
                            sortedDomiks.map(domik => {
                                const domikType = domikTypes.find(x => x.id === domik.typeId);
                                if (domikType == null) {
                                    return null;
                                }

                                const hasManufacture = domik.manufactures != null && domik.manufactures.length > 0;
                                const durationSecondsText = domik.finishDate != null
                                    ? formatDuration(remainingSeconds(domik.finishDate, now))
                                    : null;
                                const cardWeather = currentWeather?.effects.find(
                                    effect => effect.domikTypeId === domik.typeId && effect.outputPercent !== 100) ?? null;
                                return (
                                    <button key={domik.id}
                                        className={'plot' + (selectedDomikId === domik.id ? ' plot-selected' : '')}
                                        onClick={() => selectDomik(domik.id, domikType.logicName)}>
                                        {cardWeather != null &&
                                            <span className={'plot-weather' + (cardWeather.outputPercent > 100 ? ' plot-weather-buff' : ' plot-weather-nerf')}
                                                title={`Погода: ${cardWeather.outputPercent > 100 ? "+" : ""}${cardWeather.outputPercent - 100}% выход`}>
                                                {cardWeather.outputPercent > 100 ? '+' : ''}{cardWeather.outputPercent - 100}%
                                            </span>
                                        }
                                        <AnimatedDomikSprite mode="levelup" className="plot-sprite" logicName={domikType.logicName} level={domik.level} working={hasManufacture} />
                                        <span className="plot-name">{domikDisplayName(domik.typeId, domik.id, domikType.name)}</span>
                                        <UpgradeBox durationSeconds={durationSecondsText} level={domik.level} />
                                        <span className="plot-status">
                                            {canAffordUpgrade(domik, domikType, resources) &&
                                                <img className="status-icon" src="/images/upgrade_available.png" alt="Доступно улучшение" title="Доступно улучшение" />
                                            }
                                            {domik.finishDate != null &&
                                                <img className="status-icon icon-busy" src="/images/upgrade_in_process.png" alt="Идёт улучшение" title="Идёт улучшение" />
                                            }
                                            {hasManufacture &&
                                                <AbstractSprite logicName="production_recipe" size={24} className="status-icon" aria-label="Идёт производство" />
                                            }
                                        </span>
                                    </button>
                                );
                            })
                        }
                    </div>
                </section>
                {selected != null && <div className="actions-scrim" role="presentation" onClick={() => { setSelectedDomikId(null); }} />}
                <aside className={'actions pixel-panel' + (selected == null ? ' actions--empty' : '')}>
                    <button type="button" className="actions-close" title="Закрыть" onClick={() => setSelectedDomikId(null)}>
                        <CloseIcon className="btn-ico" aria-hidden="true" />
                    </button>
                    {selected == null &&
                        <p className="hint">Выберите домик в деревне – здесь появятся улучшение и производство.</p>
                    }
                    {selected != null &&
                        <div>
                            <h3 className="panel-title">{domikDisplayName(selected.domik.typeId, selected.domik.id, selected.domikType.name)}</h3>
                            <span className="domik-level">ур. {selected.domik.level}</span>
                            {domikLore[selected.domikType.logicName] != null &&
                                <p className="domik-lore">{domikLore[selected.domikType.logicName]}</p>
                            }
                            {selected.upgrade != null &&
                                <div className="panel-block">
                                    <div className="upgrade-row">
                                        <span className="panel-label">Улучшение до ур. {selected.upgrade.nextLevel}</span>
                                        <ResourcesBox resources={selected.upgrade.resources} resourceTypes={resourceTypes} have={resources} />
                                    </div>
                                    <button className="btn-game"
                                        disabled={!selected.upgrade.hasResources}
                                        title={selected.upgrade.hasResources ? undefined : 'Не хватает ресурсов'}
                                        onClick={() => upgrade(selected.domik.id)}>
                                        <ArrowUpIcon className="btn-ico" aria-hidden="true" />
                                        Улучшить
                                    </button>
                                </div>
                            }
                            {selected.domik.finishDate != null &&
                                <div className="panel-block">
                                    {(() => {
                                        const hurryCost = instaFinishCost(selected.domik.finishDate, now);
                                        const tooFar = !canInstaFinish(selected.domik.finishDate, now);
                                        const notEnoughGold = goldValue < hurryCost;
                                        const hurryTitle = tooFar ? 'До конца слишком далеко' : notEnoughGold ? 'Не хватает золота' : undefined;

                                        return (
                                            <>
                                                <ProgressBar value={progressPercent(selected.domik.finishDate, selected.domik.upgradeSeconds ?? 0, now)} max={100} label={selected.remainingText ?? ''} />
                                                <button type="button" className="btn-game"
                                                    disabled={tooFar || notEnoughGold}
                                                    title={hurryTitle}
                                                    onClick={() => hurryDomikAction(selected.domik.id)}>
                                                    <ZapIcon className="btn-ico" aria-hidden="true" />
                                                    Поторопить – {Math.max(1, hurryCost)}
                                                    {goldType != null &&
                                                        <ResourceSprite logicName={goldType.logicName} className="hurry-cost-ico" aria-hidden="true" />
                                                    }
                                                </button>
                                            </>
                                        );
                                    })()}
                                </div>
                            }
                            {selected.receipts.length > 0 &&
                                <div className="panel-block">
                                    <span className="panel-label">Запустить производство</span>
                                    <div className="receipt-list">
                                        {selected.receipts.map(receipt => {
                                            const hasOptional = receipt.optionalInputResources.length > 0;
                                            const useOptional = optionalReceiptIds.has(receipt.id);
                                            const autoRepeat = autoRepeatReceiptIds.has(receipt.id);
                                            const view = computeReceiptView(receipt, resources, plodder.free, hasOptional && useOptional, goals?.zealCharges, selected.domikType);
                                            const expanded = expandedReceiptId === receipt.id;
                                            const isManual = manualReceiptIds.has(receipt.id);
                                            const selectedWorkerIds = selectedWorkerIdsByReceipt[receipt.id] ?? [];
                                            const freeWorkersForType = workers
                                                .filter(worker => isWorkerFree(worker, now))
                                                .map(worker => ({ worker, fitness: workerFitness(worker, selected.domikType.id) }))
                                                .sort((a, b) => b.fitness - a.fitness);
                                            const freeIdsForType = new Set(freeWorkersForType.map(({ worker }) => worker.id));
                                            const validSelectedIds = selectedWorkerIds.filter(id => freeIdsForType.has(id));
                                            const canRun = isManual
                                                ? view.hasResources && validSelectedIds.length === receipt.plodderCount
                                                : view.canRun;
                                            return (
                                                <div key={receipt.id}
                                                    className={'receipt-row' + (expanded ? ' receipt-open' : '') + (view.canRun ? '' : ' receipt-blocked')}>
                                                    <button type="button" className="receipt-head"
                                                        aria-expanded={expanded}
                                                        onClick={() => toggleExpand(receipt.id)}>
                                                        <span className="receipt-name">{receipt.name}</span>
                                                        <span className="receipt-cost">
                                                            {!view.canRun &&
                                                                <img className="receipt-warn" src="/images/upgrade_no_resources.png"
                                                                    alt="" title={!view.hasPlodders ? 'Нет свободных трудяг' : 'Не хватает ресурсов'} />
                                                            }
                                                            <span className="resource-box" title="Трудяги">
                                                                <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                                                                <span className="resource-value">{receipt.plodderCount}</span>
                                                            </span>
                                                            <span className="timer">{formatDuration(view.effectiveDurationSeconds)}</span>
                                                            {view.zealMultiplier > 1 && <span className="receipt-zeal">×{view.zealMultiplier}</span>}
                                                            <ChevronDownIcon className="receipt-caret" aria-hidden="true" />
                                                        </span>
                                                    </button>
                                                    {expanded &&
                                                        <div className="receipt-body">
                                                            <div className="receipt-io">
                                                                {view.inputs.length > 0 &&
                                                                    <div className="receipt-io-row">
                                                                        <span className="receipt-io-label">Нужно</span>
                                                                        <ResourcesBox resources={view.inputs} resourceTypes={resourceTypes} have={resources} />
                                                                    </div>
                                                                }
                                                                {receipt.outputResources.length > 0 &&
                                                                    <div className="receipt-io-row">
                                                                        <span className="receipt-io-label">Даёт</span>
                                                                        <ResourcesBox resources={receipt.outputResources} resourceTypes={resourceTypes} />
                                                                    </div>
                                                                }
                                                            </div>
                                                            {hasOptional &&
                                                                <label className="receipt-optional">
                                                                    <input type="checkbox" checked={useOptional}
                                                                        onChange={() => toggleOptional(receipt.id)} />
                                                                    с инструментом (+{receipt.outputBonusPercent}% выхода)
                                                                </label>
                                                            }
                                                            <label className="receipt-optional">
                                                                <input type="checkbox" checked={autoRepeat}
                                                                    onChange={() => toggleAutoRepeat(receipt.id)} />
                                                                Повторять
                                                            </label>
                                                            {weatherEffect != null &&
                                                                <p className="weather-modifier">
                                                                    Погода: {weatherEffect.outputPercent >= 100 ? '+' : ''}{weatherEffect.outputPercent - 100} % выход
                                                                </p>
                                                            }
                                                            <div className="receipt-mode">
                                                                <label className="receipt-optional">
                                                                    <input type="checkbox" checked={isManual}
                                                                        onChange={() => toggleManual(receipt.id)} />
                                                                    Выбрать трудяг вручную
                                                                </label>
                                                                {isManual &&
                                                                    <span className="receipt-mode-count">
                                                                        выбрано {validSelectedIds.length} / {receipt.plodderCount}
                                                                    </span>
                                                                }
                                                            </div>
                                                            {isManual &&
                                                                <div className="worker-picker">
                                                                    {freeWorkersForType.length === 0 &&
                                                                        <span className="hint">Нет свободных трудяг</span>
                                                                    }
                                                                    {freeWorkersForType.map(({ worker, fitness }) => {
                                                                        const isSelected = selectedWorkerIds.includes(worker.id);
                                                                        return (
                                                                            <button key={worker.id} type="button"
                                                                                className={'worker-chip worker-chip-pick' + (isSelected ? ' worker-chip-selected' : '')}
                                                                                onClick={() => toggleSelectedWorker(receipt.id, worker.id, receipt.plodderCount)}>
                                                                                <WorkerSprite name={worker.name} skilled={isSkilledWorker(worker)} className="worker-avatar" aria-hidden="true" />
                                                                                <span className="worker-name">{worker.name}</span>
                                                                                <span className="worker-effect">{fitness >= 0 ? '+' : ''}{fitness} %</span>
                                                                            </button>
                                                                        );
                                                                    })}
                                                                </div>
                                                            }
                                                            <button className="btn-game"
                                                                disabled={!canRun}
                                                                onClick={() => startManufacture(selected.domik.id, receipt.id, hasOptional && useOptional, autoRepeat, isManual ? validSelectedIds : undefined)}>
                                                                <PlayIcon className="btn-ico" aria-hidden="true" />
                                                                Запустить
                                                            </button>
                                                            {!canRun &&
                                                                <p className="note-warn">
                                                                    <img src="/images/upgrade_no_resources.png" alt="" />
                                                                    {isManual
                                                                        ? !view.hasResources ? 'Не хватает ресурсов' : `Выберите ровно ${receipt.plodderCount} трудяг`
                                                                        : !view.hasPlodders ? 'Нет свободных трудяг' : 'Не хватает ресурсов'}
                                                                </p>
                                                            }
                                                        </div>
                                                    }
                                                </div>
                                            );
                                        })}
                                    </div>
                                </div>
                            }
                            {selected.domik.manufactures != null && selected.domik.manufactures.length > 0 &&
                                <div className="panel-block">
                                    <span className="panel-label">Сейчас производится</span>
                                    {selected.domik.manufactures.map(manufacture => {
                                        const receipt = receipts.find(x => x.id === manufacture.receiptId);
                                        if (receipt == null) {
                                            return null;
                                        }

                                        return (
                                            <ManufactureBox key={manufacture.id} manufacture={manufacture} receipt={receipt}
                                                now={now} remainingText={formatDuration(remainingSeconds(manufacture.finishDate, now))}
                                                goldValue={goldValue} goldType={goldType} onHurry={hurryManufactureAction}
                                                onToggleAutoRepeat={toggleManufactureAutoRepeat} />
                                        );
                                    })}
                                </div>
                            }
                        </div>
                    }
                </aside>
            </div>
            <div className="game-tabs" ref={gameTabsRef}>
                <button type="button" className="game-tab game-tab-home" onClick={() => { window.scrollTo({ top: 0 }); }}>
                    <BuildingIcon className="game-tab-ico" aria-hidden="true" />
                    Домики
                </button>
                {visibleGameTabs.map(tab => (
                    <button type="button" key={tab.key}
                        className={'game-tab' + (tab.key === activeGameTab?.key ? ' game-tab-active' : '')}
                        onClick={() => {
                            setActiveTab(tab.key);
                            if (window.matchMedia('(max-width: 900px)').matches) {
                                gameTabPanelRef.current?.scrollIntoView({ block: 'start' });
                            }
                        }}>
                        <tab.Icon className="game-tab-ico" aria-hidden="true" />
                        {tab.label}
                    </button>
                ))}
            </div>
            <div className="game-tab-panel" ref={gameTabPanelRef}>
                {activeGameTab?.node}
            </div>
        </div>
    );
};

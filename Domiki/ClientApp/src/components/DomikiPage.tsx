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
import BellIcon from 'pixelarticons/svg/bell.svg?react';
import BellOffIcon from 'pixelarticons/svg/bell-off.svg?react';
import GridIcon from 'pixelarticons/svg/grid-3x3.svg?react';
import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import ChevronLeftIcon from 'pixelarticons/svg/chevron-left.svg?react';
import ChevronRightIcon from 'pixelarticons/svg/chevron-right.svg?react';
import RepeatIcon from 'pixelarticons/svg/repeat.svg?react';
import { apiPost, ApiError, completeOrder as completeOrderApi } from '../services/api';
import { useToast } from '../services/toast';
import { disablePush, enablePush, getPushState } from '../services/push';
import type { PushState } from '../services/push';
import { useGameData } from '../hooks/useGameData';
import { COIN_RESOURCE_TYPE_ID, GOLD_RESOURCE_TYPE_ID, canAffordUpgrade, canInstaFinish, computeReceiptView, computeSelectedDomikView, instaFinishCost, isWorkerFree, progressPercent, resourceShortfall, sortDomiks, workerFitness } from '../utils/game';
import type { DomikSortMode } from '../utils/game';
import { buildDomikNamer } from '../utils/domikNames';
import { formatDuration, remainingSeconds } from '../utils/time';
import { domikLore } from '../utils/domikLore';
import { pluralRu } from '../utils/plural';
import { ManufactureBox } from './ManufactureBox';
import { StatChip } from './StatChip';
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
import { JournalBox } from './JournalBox';
import { ShopBox } from './ShopBox';
import { RecapModal } from './RecapModal';
import { AbstractSprite, DomikSprite, MechanicSprite, ResourceSprite, WeatherSprite, WorkerSprite } from './sprites';
import { isSkilledWorker } from '../utils/worker';
import { AnimatedDomikSprite } from './AnimatedDomikSprite';
import { HudResource } from './HudResource';
import { PixelLoader } from './PixelLoader';
import { ResourceInfoProvider } from './ResourceInfo';
import { DEFAULT_VILLAGE_ICON, VILLAGE_CREST_COLORS, VILLAGE_CREST_ICONS } from '../constants/village';
import { buildRecapView } from '../utils/recap';


const MECHANIC_TAB: Record<string, string> = {
    market_yard: 'market',
    gathering: 'toloka',
    scout_hut: 'expeditions',
};

type SortModeEntry = { mode: DomikSortMode; label: string; Icon: typeof StoreIcon };

const SORT_MODES: readonly [SortModeEntry, ...SortModeEntry[]] = [
    { mode: 'attention', label: 'По важности', Icon: BellIcon },
    { mode: 'type', label: 'По типу', Icon: GridIcon },
    { mode: 'level', label: 'По уровню', Icon: ChevronUpIcon },
];

type RowsPerPage = 2 | 3 | 5 | 'all';

const ROWS_PER_PAGE_OPTIONS: { value: RowsPerPage; label: string }[] = [
    { value: 2, label: '2 ряда' },
    { value: 3, label: '3 ряда' },
    { value: 5, label: '5 рядов' },
    { value: 'all', label: 'Все' },
];

interface GameTab {
    key: string;
    label: string;
    icon: ReactNode;
    visible: boolean;
    node: ReactNode;
}

export const DomikiPage = () => {
    const toast = useToast();
    const { domiks, domikTypes, resourceTypes, receipts, resources, orders, reputation, blueprints, village, villageLevel, weather, expeditions, decor, toloka, market, goals, workers, purchaseDomikTypes, now, loading, scheduleReload, refreshPurchaseTypes, setVillage, setFeedWorkers, hurryManufacture, setManufactureAutoRepeat, hurryDomik, startExpedition, buyDecor, contributeToloka, postLot, acceptLot, cancelLot, recap, clearRecap, events } =
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
    const hudRef = useRef<HTMLElement>(null);
    const [hudStickyOffset, setHudStickyOffset] = useState(76);
    const [tabsOverflow, setTabsOverflow] = useState({ left: false, right: false });
    const hudSentinelRef = useRef<HTMLDivElement>(null);
    const [hudAway, setHudAway] = useState(false);
    const [hudPinnedOpen, setHudPinnedOpen] = useState(false);
    const [identityDismissed, setIdentityDismissed] = useState(false);
    const [draftVillageName, setDraftVillageName] = useState('');
    const [draftCrestIcon, setDraftCrestIcon] = useState(0);
    const [draftCrestColor, setDraftCrestColor] = useState(0);
    const [sortMode, setSortMode] = useState<DomikSortMode>(() => {
        const saved = localStorage.getItem('domik-sort-mode');
        return saved === 'attention' || saved === 'level' ? saved : 'type';
    });
    const changeSortMode = (mode: DomikSortMode) => {
        setSortMode(mode);
        setPage(1);
        localStorage.setItem('domik-sort-mode', mode);
    };
    const [sortOpen, setSortOpen] = useState(false);
    const sortRef = useRef<HTMLDivElement>(null);
    const [rowsPerPage, setRowsPerPage] = useState<RowsPerPage>(() => {
        const saved = localStorage.getItem('domik-page-size');
        if (saved === '2') return 2;
        if (saved === '5') return 5;
        if (saved === 'all') return 'all';
        return 3;
    });
    const [page, setPage] = useState(1);
    const [pageSizeOpen, setPageSizeOpen] = useState(false);
    const pageSizeRef = useRef<HTMLDivElement>(null);
    const domiksRef = useRef<HTMLDivElement>(null);
    const [columns, setColumns] = useState(1);
    const changeRowsPerPage = (rows: RowsPerPage) => {
        setRowsPerPage(rows);
        setPage(1);
        localStorage.setItem('domik-page-size', String(rows));
    };
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
        if (!pageSizeOpen) {
            return;
        }

        const onDown = (event: MouseEvent) => {
            if (pageSizeRef.current != null && !pageSizeRef.current.contains(event.target as Node)) {
                setPageSizeOpen(false);
            }
        };

        document.addEventListener('mousedown', onDown);
        return () => { document.removeEventListener('mousedown', onDown); };
    }, [pageSizeOpen]);

    useEffect(() => {
        const grid = domiksRef.current;
        if (grid == null) {
            return;
        }

        const measure = () => {
            const count = getComputedStyle(grid).gridTemplateColumns.split(' ').filter(Boolean).length;
            setColumns(Math.max(1, count));
        };
        measure();
        const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(measure);
        observer?.observe(grid);
        return () => { observer?.disconnect(); };
    }, []);

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

    useEffect(() => {
        const hud = hudRef.current;
        if (hud == null) {
            return;
        }
        const updateOffset = () => setHudStickyOffset(hud.offsetHeight + 16);
        updateOffset();
        const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(updateOffset);
        observer?.observe(hud);
        return () => { observer?.disconnect(); };
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
    const perPage = rowsPerPage === 'all' ? Math.max(1, sortedDomiks.length) : Math.max(1, rowsPerPage * columns);
    const totalPages = Math.max(1, Math.ceil(sortedDomiks.length / perPage));
    const safePage = Math.min(page, totalPages);
    const pagedDomiks = sortedDomiks.slice((safePage - 1) * perPage, safePage * perPage);
    const domikDisplayName = useMemo(() => buildDomikNamer(domiks), [domiks]);
    const upgradeBenefits = selected?.upgrade == null
        ? null
        : (() => {
            const currentLevel = selected.domikType.levels.find(level => level.value === selected.domik.level);
            const nextLevel = selected.domikType.levels.find(level => level.value === selected.upgrade?.nextLevel);
            if (currentLevel == null || nextLevel == null) {
                return null;
            }

            const plodderDelta = (nextLevel.modificators.find(modificator => modificator.typeId === 1)?.value ?? 0)
                - (currentLevel.modificators.find(modificator => modificator.typeId === 1)?.value ?? 0);
            const manufactureDelta = nextLevel.maxManufactureCount - currentLevel.maxManufactureCount;
            const newReceiptNames = nextLevel.receiptIds
                .filter(receiptId => !currentLevel.receiptIds.includes(receiptId))
                .map(receiptId => receipts.find(receipt => receipt.id === receiptId)?.name)
                .filter((name): name is string => name != null);

            return plodderDelta > 0 || manufactureDelta > 0 || newReceiptNames.length > 0
                ? { plodderDelta, manufactureDelta, newReceiptNames }
                : null;
        })();
    const currentWeather = weather?.current ?? null;
    const hudCollapsed = hudAway && !hudPinnedOpen;
    const coinType = resourceTypes.find(t => t.id === COIN_RESOURCE_TYPE_ID);
    const coinValue = resources.find(r => r.typeId === COIN_RESOURCE_TYPE_ID)?.value;
    const weatherEffect = selected == null
        ? null
        : currentWeather?.effects.find(effect => effect.domikTypeId === selected.domikType.id) ?? null;
    const goldValue = resources.find(x => x.typeId === GOLD_RESOURCE_TYPE_ID)?.value ?? 0;
    const goldType = resourceTypes.find(x => x.id === GOLD_RESOURCE_TYPE_ID);
    const formatShortfall = (cost: { typeId: number; value: number }[]) => resourceShortfall(cost, resources)
        .map(item => `${resourceTypes.find(type => type.id === item.typeId)?.name ?? `ресурс #${item.typeId}`} ×${item.value}`)
        .join(', ');
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
        setSelectedWorkerIdsByReceipt(prev => ({ ...prev, [receiptId]: [] }));
        scheduleReload();
    }, 'Производство запущено');

    const completeOrder = (orderId: number) => runAction(async () => {
        await completeOrderApi(orderId);
        scheduleReload();
    }, 'Заказ выполнен');

    const hurryManufactureAction = (manufactureId: number) => runAction(() => hurryManufacture(manufactureId), 'Производство ускорено');

    const toggleManufactureAutoRepeat = (manufactureId: number, next: boolean) => runAction(
        () => setManufactureAutoRepeat(manufactureId, next),
        next ? 'Автоповтор включён' : 'Повторы остановлены',
    );

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
            setActiveTab(mechTab);
        }
    };

    const gameTabs: GameTab[] = [
        {
            key: 'orders', label: 'Заказы', icon: <MechanicSprite logicName="orders" size={24} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: <OrdersBox orders={orders} reputation={reputation} resourceTypes={resourceTypes} resources={resources} now={now} onComplete={completeOrder} />,
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
            node: <TolokaBox toloka={toloka} resourceTypes={resourceTypes} resources={resources} now={now} onContribute={contributeTolokaAction} />,
        },
        {
            key: 'market', label: 'Ярмарка', icon: <MechanicSprite logicName="market" size={24} className="game-tab-ico" aria-hidden="true" />, visible: market != null,
            node: <MarketBox market={market} resourceTypes={resourceTypes} resources={resources} now={now}
                onPost={postLotAction} onAccept={acceptLotAction} onCancel={cancelLotAction} />,
        },
        {
            key: 'workers', label: 'Трудяги', icon: <MechanicSprite logicName="workers" size={24} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: <WorkersBox workers={workers} domikTypes={domikTypes} domiks={domiks} expeditions={expeditions} feedWorkers={village?.feedWorkers ?? false} now={now} onToggleFeedWorkers={toggleFeedWorkers} />,
        },
        {
            key: 'journal', label: 'Журнал', icon: <AbstractSprite logicName="journal" size={24} className="game-tab-ico" aria-hidden="true" />, visible: true,
            node: <JournalBox events={events} resourceTypes={resourceTypes} domikTypes={domikTypes} decorTypes={decor?.types ?? []} now={now} />,
        },
    ];
    const visibleGameTabs = gameTabs.filter(tab => tab.visible);
    const activeGameTab = visibleGameTabs.find(tab => tab.key === activeTab) ?? visibleGameTabs[0];
    const activeGameTabKey = activeGameTab?.key;
    const nextGoal = villageLevel?.upcomingUnlocks.find((unlock): unlock is typeof unlock & { level: number } => unlock.level != null);

    useEffect(() => {
        const tabs = gameTabsRef.current;
        if (tabs == null) {
            return;
        }

        const updateOverflow = () => {
            const max = tabs.scrollWidth - tabs.clientWidth;
            setTabsOverflow({ left: tabs.scrollLeft > 2, right: tabs.scrollLeft < max - 2 });
        };
        updateOverflow();
        tabs.addEventListener('scroll', updateOverflow, { passive: true });
        const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(updateOverflow);
        observer?.observe(tabs);
        return () => {
            tabs.removeEventListener('scroll', updateOverflow);
            observer?.disconnect();
        };
    }, [visibleGameTabs.length]);

    useEffect(() => {
        const tabs = gameTabsRef.current;
        const active = activeGameTabKey == null ? null : tabs?.querySelector<HTMLElement>(`#game-tab-${activeGameTabKey}`);
        if (tabs == null || active == null) {
            return;
        }

        const left = active.offsetLeft;
        const right = left + active.offsetWidth;
        if (left < tabs.scrollLeft || right > tabs.scrollLeft + tabs.clientWidth) {
            tabs.scrollTo({ left: Math.max(0, left - 12), behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
        }
    }, [activeGameTabKey]);

    const activateTabByKeyboard = (event: React.KeyboardEvent<HTMLButtonElement>, index: number) => {
        if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
            return;
        }
        event.preventDefault();
        const nextIndex = event.key === 'Home'
            ? 0
            : event.key === 'End'
                ? visibleGameTabs.length - 1
                : (index + (event.key === 'ArrowRight' ? 1 : -1) + visibleGameTabs.length) % visibleGameTabs.length;
        const next = visibleGameTabs[nextIndex];
        if (next != null) {
            setActiveTab(next.key);
            requestAnimationFrame(() => document.getElementById(`game-tab-${next.key}`)?.focus());
        }
    };

    return (
        <ResourceInfoProvider resourceTypes={resourceTypes} domikTypes={domikTypes} receipts={receipts}>
        <div className="game" style={{ '--hud-sticky-offset': `${hudStickyOffset}px` } as React.CSSProperties}>
            {loading &&
                <div className="game-loading">
                    <PixelLoader label="Загрузка деревни…" />
                </div>
            }
            <div className="hud-sentinel" ref={hudSentinelRef} aria-hidden="true" />
            <header ref={hudRef} className={'hud pixel-panel' + (hudCollapsed ? ' hud-collapsed' : '')}>
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
            <GoalCard goals={goals} resourceTypes={resourceTypes} />
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
                <RecapModal
                    awaySeconds={recap.awaySeconds}
                    view={recapView}
                    resourceTypes={resourceTypes}
                    domikTypes={domikTypes}
                    decorTypes={decor?.types ?? []}
                    expeditionTypes={expeditions?.types ?? []}
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
                        <button type="button" className={`btn-game btn-ghost btn-icon push-toggle push-toggle-${pushState}`}
                            title={pushState === 'on' ? 'Push-уведомления включены' : pushState === 'denied' ? 'Push-уведомления заблокированы браузером' : 'Push-уведомления выключены'}
                            aria-label={pushState === 'on' ? 'Выключить push-уведомления' : 'Включить push-уведомления'}
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
                            {shopVisible ? 'Закрыть' : 'Плотник'}
                        </button>
                    }
                </div>
            </div>
            <div className="workspace">
                <section className="village">
                    {shopVisible && purchaseDomikTypes != null &&
                        <ShopBox purchaseDomikTypes={purchaseDomikTypes} domikTypes={domikTypes} receipts={receipts}
                            resourceTypes={resourceTypes} resources={resources} blueprints={blueprints} villageLevel={villageLevel}
                            onBuy={buy} onClose={() => setShopVisible(false)} />
                    }
                    <div className="domiks" ref={domiksRef}>
                        {domikTypes.length > 0 &&
                            pagedDomiks.map(domik => {
                                const domikType = domikTypes.find(x => x.id === domik.typeId);
                                if (domikType == null) {
                                    return null;
                                }

                                const hasManufacture = domik.manufactures != null && domik.manufactures.length > 0;
                                const repeatedRecipeNames = (domik.manufactures ?? [])
                                    .filter(manufacture => manufacture.autoRepeat)
                                    .map(manufacture => receipts.find(receipt => receipt.id === manufacture.receiptId)?.name)
                                    .filter((name): name is string => name != null);
                                const repeatTitle = repeatedRecipeNames.length > 0
                                    ? `Автоповтор: ${repeatedRecipeNames.join(', ')}`
                                    : null;
                                const durationSecondsText = domik.finishDate != null
                                    ? formatDuration(remainingSeconds(domik.finishDate, now))
                                    : null;
                                const cardWeather = currentWeather?.effects.find(
                                    effect => effect.domikTypeId === domik.typeId && effect.outputPercent !== 100) ?? null;
                                const displayName = domikDisplayName(domik.typeId, domik.id, domikType.name, domikType.logicName);
                                const upgradeAvailable = canAffordUpgrade(domik, domikType, resources);
                                const cardStatus = domik.finishDate != null
                                        ? 'идёт улучшение'
                                    : hasManufacture
                                        ? `идёт производство${repeatTitle == null ? '' : `, ${repeatTitle.toLocaleLowerCase()}`}`
                                        : upgradeAvailable
                                            ? 'доступно улучшение'
                                            : 'готов к работе';
                                return (
                                    <button key={domik.id}
                                        className={'plot' + (selectedDomikId === domik.id ? ' plot-selected' : '')}
                                        aria-label={`${displayName}, уровень ${domik.level}, ${cardStatus}`}
                                        aria-pressed={selectedDomikId === domik.id}
                                        onClick={() => selectDomik(domik.id, domikType.logicName)}>
                                        {cardWeather != null &&
                                            <span className={'plot-weather' + (cardWeather.outputPercent > 100 ? ' plot-weather-buff' : ' plot-weather-nerf')}
                                                title={`Погода: ${cardWeather.outputPercent > 100 ? "+" : ""}${cardWeather.outputPercent - 100}% выход`}>
                                                {cardWeather.outputPercent > 100 ? '+' : ''}{cardWeather.outputPercent - 100}%
                                            </span>
                                        }
                                        <AnimatedDomikSprite mode="levelup" className="plot-sprite" logicName={domikType.logicName} level={domik.level} working={hasManufacture} />
                                        <span className="plot-name">{displayName}</span>
                                        <UpgradeBox durationSeconds={durationSecondsText} level={domik.level} />
                                        <span className="plot-status">
                                            {upgradeAvailable &&
                                                <img className="status-icon" src="/images/upgrade_available.png" alt="" aria-hidden="true" title="Доступно улучшение" />
                                            }
                                            {domik.finishDate != null &&
                                                <img className="status-icon icon-busy" src="/images/upgrade_in_process.png" alt="" aria-hidden="true" title="Идёт улучшение" />
                                            }
                                            {hasManufacture &&
                                                <AbstractSprite logicName="production_recipe" size={24} className="status-icon" aria-hidden="true" />
                                            }
                                            {repeatTitle != null &&
                                                <RepeatIcon className="status-icon status-repeat" aria-hidden="true" title={repeatTitle} />
                                            }
                                        </span>
                                    </button>
                                );
                            })
                        }
                    </div>
                    {(totalPages > 1 || domiks.length > 2 * columns) &&
                        <div className="domik-pager">
                            {totalPages > 1 &&
                                <div className="domik-pager-nav">
                                    <button type="button" className="btn-game btn-ghost btn-icon" disabled={safePage <= 1}
                                        onClick={() => setPage(safePage - 1)} aria-label="Предыдущая страница">
                                        <ChevronLeftIcon className="btn-ico" aria-hidden="true" />
                                    </button>
                                    <span className="domik-pager-status">Стр. {safePage} из {totalPages}</span>
                                    <button type="button" className="btn-game btn-ghost btn-icon" disabled={safePage >= totalPages}
                                        onClick={() => setPage(safePage + 1)} aria-label="Следующая страница">
                                        <ChevronRightIcon className="btn-ico" aria-hidden="true" />
                                    </button>
                                </div>
                            }
                            {domiks.length > 2 * columns &&
                                <div className="domik-sort-menu domik-page-size" ref={pageSizeRef}>
                                    <button type="button" className="btn-game btn-ghost" aria-expanded={pageSizeOpen}
                                        title="Рядов домиков на странице"
                                        onClick={() => setPageSizeOpen(prev => !prev)}>
                                        <BuildingIcon className="btn-ico" aria-hidden="true" />
                                        {ROWS_PER_PAGE_OPTIONS.find(opt => opt.value === rowsPerPage)?.label ?? '3 ряда'}
                                        <ChevronDownIcon className="btn-ico" aria-hidden="true" />
                                    </button>
                                    {pageSizeOpen &&
                                        <div className="domik-sort-pop domik-page-size-pop">
                                            {ROWS_PER_PAGE_OPTIONS.map(opt =>
                                                <button key={String(opt.value)} type="button"
                                                    className={'domik-sort-option' + (rowsPerPage === opt.value ? ' domik-sort-option-active' : '')}
                                                    onClick={() => { changeRowsPerPage(opt.value); setPageSizeOpen(false); }}>
                                                    {opt.label}
                                                </button>,
                                            )}
                                        </div>
                                    }
                                </div>
                            }
                        </div>
                    }
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
                            <div className="actions-heading">
                                <h3 className="panel-title">{domikDisplayName(selected.domik.typeId, selected.domik.id, selected.domikType.name, selected.domikType.logicName)}</h3>
                                <span className="domik-level">ур. {selected.domik.level}</span>
                            </div>
                            {domikLore[selected.domikType.logicName] != null &&
                                <p className="domik-lore">{domikLore[selected.domikType.logicName]}</p>
                            }
                            {selected.upgrade != null &&
                                <div className="panel-block">
                                    <div className="upgrade-row">
                                        <span className="panel-label">Улучшение до ур. {selected.upgrade.nextLevel}</span>
                                        <ResourcesBox resources={selected.upgrade.resources} resourceTypes={resourceTypes} have={resources} />
                                    </div>
                                    {upgradeBenefits != null &&
                                        <div className="upgrade-benefits">
                                            <span className="panel-label">Что даст ур. {selected.upgrade.nextLevel}</span>
                                            <div className="upgrade-benefits-chips">
                                                {upgradeBenefits.plodderDelta > 0 &&
                                                    <StatChip icon={<img className="stat-chip-ico" src="/images/modificatorTypes/plodder.png" alt="" />} title="Вместимость трудяг">
                                                        +{upgradeBenefits.plodderDelta} {pluralRu(upgradeBenefits.plodderDelta, 'трудяга', 'трудяги', 'трудяг')}
                                                    </StatChip>}
                                                {upgradeBenefits.manufactureDelta > 0 &&
                                                    <StatChip icon={<GridIcon className="stat-chip-ico" aria-hidden="true" />} title="Одновременные производства">
                                                        +{upgradeBenefits.manufactureDelta} {pluralRu(upgradeBenefits.manufactureDelta, 'производство', 'производства', 'производств')}
                                                    </StatChip>}
                                                {upgradeBenefits.newReceiptNames.slice(0, 3).map((name, index) =>
                                                    <StatChip key={`${name}-${index}`} icon={<AbstractSprite logicName="production_recipe" size={24} className="stat-chip-ico" aria-hidden="true" />} title="Новый рецепт">
                                                        {name}
                                                    </StatChip>)}
                                                {upgradeBenefits.newReceiptNames.length > 3 &&
                                                    <StatChip icon={<AbstractSprite logicName="production_recipe" size={24} className="stat-chip-ico" aria-hidden="true" />} title={upgradeBenefits.newReceiptNames.slice(3).join(', ')}>
                                                        +{upgradeBenefits.newReceiptNames.length - 3} ещё
                                                    </StatChip>}
                                            </div>
                                        </div>
                                    }
                                    <button className="btn-game"
                                        disabled={!selected.upgrade.hasResources}
                                        title={selected.upgrade.hasResources ? undefined : `Не хватает: ${formatShortfall(selected.upgrade.resources)}`}
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
                                        const hurryTitle = tooFar
                                            ? `До конца ${selected.remainingText ?? ''}; ускорение доступно в последние 6 ч`
                                            : notEnoughGold ? `Не хватает золота: ${hurryCost - goldValue}` : undefined;

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
                                            const missingResources = resourceShortfall(view.inputs, resources);
                                            const missingResourcesText = formatShortfall(view.inputs);
                                            const automaticWorkerShortfall = Math.max(0, receipt.plodderCount - plodder.free);
                                            const canRun = isManual
                                                ? view.hasResources && validSelectedIds.length === receipt.plodderCount
                                                : view.canRun;
                                            const workerBlockReason = isManual
                                                ? validSelectedIds.length !== receipt.plodderCount
                                                    ? `Выберите ровно ${receipt.plodderCount} трудяг (сейчас ${validSelectedIds.length})`
                                                    : null
                                                : !view.hasPlodders ? `Не хватает свободных трудяг: ${automaticWorkerShortfall}` : null;
                                            const blockTitle = [
                                                !view.hasResources ? `Не хватает: ${missingResourcesText}` : null,
                                                workerBlockReason,
                                            ].filter(reason => reason != null).join('; ');
                                            const summaryBlockTitle = [
                                                !view.hasResources ? `Не хватает: ${missingResourcesText}` : null,
                                                !view.hasPlodders ? `Не хватает свободных трудяг: ${automaticWorkerShortfall}` : null,
                                            ].filter(reason => reason != null).join('; ');
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
                                                                    alt="" title={summaryBlockTitle} />
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
                                                                Повторять смену автоматически
                                                            </label>
                                                            {autoRepeat &&
                                                                <p className="receipt-repeat-hint">
                                                                    После каждой смены этот рецепт запустится снова, пока хватает ресурсов и трудяги могут работать.
                                                                </p>
                                                            }
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
                                                                                onClick={() => receipt.plodderCount === 1 && view.hasResources
                                                                                    ? startManufacture(selected.domik.id, receipt.id, hasOptional && useOptional, autoRepeat, [worker.id])
                                                                                    : toggleSelectedWorker(receipt.id, worker.id, receipt.plodderCount)}>
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
                                                                title={!canRun ? blockTitle : undefined}
                                                                onClick={() => startManufacture(selected.domik.id, receipt.id, hasOptional && useOptional, autoRepeat, isManual ? validSelectedIds : undefined)}>
                                                                <PlayIcon className="btn-ico" aria-hidden="true" />
                                                                Запустить
                                                            </button>
                                                             {!canRun &&
                                                                <div className="note-warn resource-shortfall">
                                                                    <img src="/images/upgrade_no_resources.png" alt="" />
                                                                    {!view.hasResources
                                                                        ? <><span>Не хватает</span><ResourcesBox resources={missingResources} resourceTypes={resourceTypes} showNames /></>
                                                                        : null}
                                                                    {workerBlockReason != null && <span>{workerBlockReason}</span>}
                                                                </div>
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
            <nav className={'game-tabs' + (tabsOverflow.left ? ' game-tabs-overflow-left' : '') + (tabsOverflow.right ? ' game-tabs-overflow-right' : '')}
                ref={gameTabsRef} aria-label="Разделы деревни">
                <button type="button" className="game-tab game-tab-home" onClick={() => { window.scrollTo({ top: 0 }); }}>
                    <BuildingIcon className="game-tab-ico" aria-hidden="true" />
                    Домики
                </button>
                <div className="game-tabs-list" role="tablist" aria-label="Игровые разделы">
                    {visibleGameTabs.map((tab, index) => {
                        const active = tab.key === activeGameTab?.key;
                        return (
                            <button type="button" role="tab" key={tab.key} id={`game-tab-${tab.key}`}
                                data-game-tab={tab.key}
                                aria-selected={active}
                                aria-controls="game-tab-panel"
                                tabIndex={active ? 0 : -1}
                                className={'game-tab' + (active ? ' game-tab-active' : '')}
                                onKeyDown={event => activateTabByKeyboard(event, index)}
                                onClick={() => {
                                    setActiveTab(tab.key);
                                    if (window.matchMedia('(max-width: 900px)').matches) {
                                        gameTabPanelRef.current?.scrollIntoView({ block: 'start' });
                                    }
                                }}>
                                {tab.icon}
                                {tab.label}
                            </button>
                        );
                    })}
                </div>
                <span className="game-tabs-affordance" aria-hidden="true">›</span>
            </nav>
            <div className="game-tab-panel" ref={gameTabPanelRef} id="game-tab-panel" role="tabpanel"
                aria-labelledby={activeGameTab == null ? undefined : `game-tab-${activeGameTab.key}`} tabIndex={0}>
                {activeGameTab?.node}
            </div>
        </div>
        </ResourceInfoProvider>
    );
};

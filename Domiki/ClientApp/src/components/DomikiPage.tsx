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
import CloudSunIcon from 'pixelarticons/svg/cloud-sun.svg?react';
import CloudIcon from 'pixelarticons/svg/cloud.svg?react';
import FireIcon from 'pixelarticons/svg/fire.svg?react';
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
import GridIcon from 'pixelarticons/svg/grid-3x3.svg?react';
import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import JournalIcon from 'pixelarticons/svg/article.svg?react';
import { apiPost, ApiError, completeOrder as completeOrderApi } from '../services/api';
import { useToast } from '../services/toast';
import { useGameData } from '../hooks/useGameData';
import { GOLD_RESOURCE_TYPE_ID, canAffordUpgrade, canInstaFinish, computeReceiptView, computeSelectedDomikView, instaFinishCost, isWorkerFree, progressPercent, sortDomiks, workerFitness } from '../utils/game';
import type { DomikSortMode } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { domikLore } from '../utils/domikLore';
import { ManufactureBox } from './ManufactureBox';
import { ProgressBar } from './ProgressBar';
import { ResourcesBox } from './ResourcesBox';
import { UpgradeBox } from './UpgradeBox';
import { OrdersBox } from './OrdersBox';
import { WorkersBox } from './WorkersBox';
import { BlueprintsBox } from './BlueprintsBox';
import { ExpeditionsBox } from './ExpeditionsBox';
import { DecorBox } from './DecorBox';
import { TolokaBox } from './TolokaBox';
import { MarketBox } from './MarketBox';
import { ResourceChip } from './ResourceChip';
import { JournalBox } from './JournalBox';
import { DomikSprite, WorkerSprite } from './sprites';
import { AnimatedDomikSprite } from './AnimatedDomikSprite';
import { HudResource } from './HudResource';
import { DEFAULT_VILLAGE_ICON, VILLAGE_CREST_COLORS, VILLAGE_CREST_ICONS } from '../constants/village';
import { buildRecapView } from '../utils/recap';

const WEATHER_ICONS: Record<string, typeof CloudSunIcon> = {
    clear: CloudSunIcon,
    rain: CloudIcon,
    drought: FireIcon,
};

const MECHANIC_TAB: Record<string, string> = {
    market_yard: 'market',
    gathering: 'toloka',
    scout_hut: 'expeditions',
};

interface GameTab {
    key: string;
    label: string;
    Icon: typeof StoreIcon;
    visible: boolean;
    node: ReactNode;
}

export const DomikiPage = () => {
    const toast = useToast();
    const { domiks, domikTypes, resourceTypes, receipts, resources, orders, reputation, blueprints, village, villageLevel, weather, expeditions, decor, toloka, market, workers, purchaseDomikTypes, now, reload, refreshPurchaseTypes, setVillage, hurryManufacture, setManufactureAutoRepeat, hurryDomik, startExpedition, buyDecor, contributeToloka, postLot, acceptLot, cancelLot, recap, clearRecap, events } =
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
    const [villageLevelOpen, setVillageLevelOpen] = useState(false);
    const [villageLevelPos, setVillageLevelPos] = useState<{ top: number; right: number; width: number } | null>(null);
    const villageLevelBtnRef = useRef<HTMLButtonElement>(null);
    const gameTabsRef = useRef<HTMLDivElement>(null);
    const tabAnchorReady = useRef(false);
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

    useEffect(() => {
        if (!villageLevelOpen) {
            return;
        }

        const reposition = () => {
            const rect = villageLevelBtnRef.current?.getBoundingClientRect();
            if (rect != null) {
                setVillageLevelPos({ top: rect.bottom + 8, right: window.innerWidth - rect.right, width: rect.width });
            }
        };

        window.addEventListener('scroll', reposition, { capture: true, passive: true });
        window.addEventListener('resize', reposition);
        return () => {
            window.removeEventListener('scroll', reposition, { capture: true });
            window.removeEventListener('resize', reposition);
        };
    }, [villageLevelOpen]);

    useEffect(() => {
        if (!tabAnchorReady.current) {
            tabAnchorReady.current = true;
            return;
        }

        gameTabsRef.current?.scrollIntoView({ behavior: 'auto', block: 'nearest' });
    }, [activeTab]);

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
    const currentWeather = weather?.current ?? null;
    const weatherEffect = selected == null
        ? null
        : currentWeather?.effects.find(effect => effect.domikTypeId === selected.domikType.id) ?? null;
    const CurrentWeatherIcon = currentWeather == null ? null : WEATHER_ICONS[currentWeather.logicName] ?? CloudSunIcon;
    const goldValue = resources.find(x => x.typeId === GOLD_RESOURCE_TYPE_ID)?.value ?? 0;
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

    const startExpeditionAction = (expeditionTypeId: number, workerIds?: number[]) => runAction(() => startExpedition(expeditionTypeId, workerIds), 'Экспедиция отправлена');

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
        if (mechTab) {
            setActiveTab(mechTab);
        }
    };

    const gameTabs: GameTab[] = [
        {
            key: 'orders', label: 'Заказы', Icon: ClipboardIcon, visible: true,
            node: <OrdersBox orders={orders} reputation={reputation} resourceTypes={resourceTypes} resources={resources} now={now} onComplete={completeOrder} />,
        },
        {
            key: 'blueprints', label: 'Чертежи', Icon: NoteIcon, visible: blueprints.length > 0,
            node: <BlueprintsBox blueprints={blueprints} domikTypes={domikTypes} />,
        },
        {
            key: 'expeditions', label: 'Экспедиции', Icon: BackpackIcon, visible: expeditions != null,
            node: <ExpeditionsBox expeditions={expeditions} resourceTypes={resourceTypes} resources={resources} workers={workers} now={now} onStart={startExpeditionAction} />,
        },
        {
            key: 'decor', label: 'Декор', Icon: GardenIcon, visible: decor != null,
            node: <DecorBox decor={decor} resourceTypes={resourceTypes} resources={resources} onBuy={buyDecorAction} />,
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
            node: <WorkersBox workers={workers} domikTypes={domikTypes} domiks={domiks} expeditions={expeditions} now={now} />,
        },
        {
            key: 'journal', label: 'Журнал', Icon: JournalIcon, visible: true,
            node: <JournalBox events={events} resourceTypes={resourceTypes} domikTypes={domikTypes} now={now} />,
        },
    ];
    const visibleGameTabs = gameTabs.filter(tab => tab.visible);
    const activeGameTab = visibleGameTabs.find(tab => tab.key === activeTab) ?? visibleGameTabs[0];

    return (
        <div className="game">
            <header className="hud pixel-panel">
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
                        <button type="button" className="village-level-box"
                            ref={villageLevelBtnRef}
                            title={`Постройки ${villageLevel.buildings}, жители ${villageLevel.residents}, репутация ${villageLevel.reputation}, уют ${villageLevel.comfort}`}
                            aria-expanded={villageLevelOpen}
                            onClick={() => {
                                const rect = villageLevelBtnRef.current?.getBoundingClientRect();
                                if (rect != null) {
                                    setVillageLevelPos({ top: rect.bottom + 8, right: window.innerWidth - rect.right, width: rect.width });
                                }
                                setVillageLevelOpen(prev => !prev);
                            }}>
                            <span className="village-level-label">Обжитость</span>
                            <span className="village-level-value">{villageLevel.level}</span>
                        </button>
                        {villageLevelOpen && villageLevelPos != null && createPortal(
                            <div className="village-level-popover" style={{ top: villageLevelPos.top, right: villageLevelPos.right, minWidth: villageLevelPos.width }}>
                                <span>Постройки: {villageLevel.buildings}</span>
                                <span>Жители: {villageLevel.residents}</span>
                                <span>Репутация: {villageLevel.reputation}</span>
                                <span>Уют: {villageLevel.comfort}</span>
                                {villageLevel.upcomingUnlocks.slice(0, 3).map(unlock => (
                                    <span key={`${unlock.level}-${unlock.label}`} className="village-level-next">
                                        {unlock.label}: {unlock.level}
                                    </span>
                                ))}
                            </div>,
                            document.body)}
                        {villageLevel.upcomingUnlocks[0] != null &&
                            <div className="hud-goal"
                                title={`Откроется при обжитости ${villageLevel.upcomingUnlocks[0].level}: ${villageLevel.upcomingUnlocks[0].label}`}>
                                <LockIcon className="hud-goal-ico" aria-hidden="true" />
                                <span className="hud-goal-label">{villageLevel.upcomingUnlocks[0].label}</span>
                                <ProgressBar value={villageLevel.level} max={villageLevel.upcomingUnlocks[0].level}
                                    label={`${villageLevel.level}/${villageLevel.upcomingUnlocks[0].level}`} />
                            </div>}
                    </>
                }
                {weather != null && currentWeather != null && CurrentWeatherIcon != null &&
                    <div className="weather-strip" title={currentWeather.weatherName}>
                        <CurrentWeatherIcon className="weather-ico" aria-hidden="true" />
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
                        <div className="weather-forecast">
                            {weather.forecast.map(period => {
                                const ForecastIcon = WEATHER_ICONS[period.logicName] ?? CloudSunIcon;
                                const hoursAhead = Math.max(1, Math.round(remainingSeconds(period.startDate, now) / 3600));
                                return (
                                    <span key={period.startDate} className="weather-chip" title={period.weatherName}>
                                        <ForecastIcon className="weather-chip-ico" aria-hidden="true" />
                                        через {hoursAhead}ч
                                    </span>
                                );
                            })}
                        </div>
                    </div>
                }
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
                                                    const type = resourceTypes.find(resourceType => resourceType.id === loot.typeId);
                                                    return type == null
                                                        ? <span key={`${loot.typeId}-${lootIndex}`} className="recap-fallback">Ресурс #{loot.typeId} ×{loot.value}</span>
                                                        : <ResourceChip key={`${loot.typeId}-${lootIndex}`} resourceType={type} value={loot.value} rare={loot.isRare} />;
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
                                const isLocked = levelLocked || blueprintLocked;
                                const lockTitle = levelLocked
                                    ? `Откроется при обжитости ${purchaseDomikType.unlockLevel}`
                                    : blueprintLocked
                                        ? `Нужен чертёж (репутация ${blueprint.neighborName} ${blueprint.reputationThreshold})`
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
                                            {blueprintLocked ? <LockIcon className="btn-ico" aria-hidden="true" /> : <BuildingIcon className="btn-ico" aria-hidden="true" />}
                                            Купить
                                        </button>
                                    </div>
                                );
                            })
                            }
                        </div>
                    }
                    {domiks.length > 1 &&
                        <div className="domik-sort">
                            <button type="button" className={'game-tab' + (sortMode === 'attention' ? ' game-tab-active' : '')}
                                onClick={() => changeSortMode('attention')}>
                                <BellIcon className="game-tab-ico" aria-hidden="true" />
                                Внимание
                            </button>
                            <button type="button" className={'game-tab' + (sortMode === 'type' ? ' game-tab-active' : '')}
                                onClick={() => changeSortMode('type')}>
                                <GridIcon className="game-tab-ico" aria-hidden="true" />
                                Тип
                            </button>
                            <button type="button" className={'game-tab' + (sortMode === 'level' ? ' game-tab-active' : '')}
                                onClick={() => changeSortMode('level')}>
                                <ChevronUpIcon className="game-tab-ico" aria-hidden="true" />
                                Уровень
                            </button>
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
                                        <span className="plot-name">{domikType.name}</span>
                                        <UpgradeBox durationSeconds={durationSecondsText} level={domik.level} />
                                        <span className="plot-status">
                                            {canAffordUpgrade(domik, domikType, resources) &&
                                                <img className="status-icon" src="/images/upgrade_available.png" alt="Доступно улучшение" title="Доступно улучшение" />
                                            }
                                            {domik.finishDate != null &&
                                                <img className="status-icon icon-busy" src="/images/upgrade_in_process.png" alt="Идёт улучшение" title="Идёт улучшение" />
                                            }
                                            {hasManufacture &&
                                                <img className="status-icon" src="/images/manufacture.png" alt="Идёт производство" title="Идёт производство" />
                                            }
                                        </span>
                                    </button>
                                );
                            })
                        }
                    </div>
                </section>
                <aside className={'actions pixel-panel' + (selected == null ? ' actions--empty' : '')}>
                    <button type="button" className="actions-close" title="Закрыть" onClick={() => setSelectedDomikId(null)}>
                        <CloseIcon className="btn-ico" aria-hidden="true" />
                    </button>
                    {selected == null &&
                        <p className="hint">Выберите домик в деревне – здесь появятся улучшение и производство.</p>
                    }
                    {selected != null &&
                        <div>
                            <h3 className="panel-title">{selected.domikType.name}</h3>
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
                                                    Поторопить ({Math.max(1, hurryCost)} золота)
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
                                            const view = computeReceiptView(receipt, resources, plodder.free, hasOptional && useOptional);
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
                                                            <span className="timer">{formatDuration(view.durationSeconds)}</span>
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
                                                                    с инструментом (−{receipt.speedupPercent}%)
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
                                                                                <WorkerSprite name={worker.name} className="worker-avatar" aria-hidden="true" />
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
                                                goldValue={goldValue} onHurry={hurryManufactureAction}
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
                {visibleGameTabs.map(tab => (
                    <button type="button" key={tab.key}
                        className={'game-tab' + (tab.key === activeGameTab?.key ? ' game-tab-active' : '')}
                        onClick={() => { setActiveTab(tab.key); }}>
                        <tab.Icon className="game-tab-ico" aria-hidden="true" />
                        {tab.label}
                    </button>
                ))}
            </div>
            {activeGameTab?.node}
        </div>
    );
};

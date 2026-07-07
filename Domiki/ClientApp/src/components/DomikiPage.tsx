import { useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
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
import { apiPost, ApiError, completeOrder as completeOrderApi } from '../services/api';
import { useToast } from '../services/toast';
import { useGameData } from '../hooks/useGameData';
import { GOLD_RESOURCE_TYPE_ID, canAffordUpgrade, canInstaFinish, computeReceiptView, computeSelectedDomikView, instaFinishCost, isWorkerFree, workerFitness } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { ManufactureBox } from './ManufactureBox';
import { ResourcesBox } from './ResourcesBox';
import { UpgradeBox } from './UpgradeBox';
import { OrdersBox } from './OrdersBox';
import { WorkersBox } from './WorkersBox';
import { BlueprintsBox } from './BlueprintsBox';
import { ExpeditionsBox } from './ExpeditionsBox';
import { DecorBox } from './DecorBox';
import { DEFAULT_VILLAGE_ICON, VILLAGE_CREST_COLORS, VILLAGE_CREST_ICONS } from '../constants/village';

const WEATHER_ICONS: Record<string, typeof CloudSunIcon> = {
    clear: CloudSunIcon,
    rain: CloudIcon,
    drought: FireIcon,
};

export const DomikiPage = () => {
    const toast = useToast();
    const { domiks, domikTypes, resourceTypes, receipts, resources, orders, reputation, blueprints, village, villageLevel, weather, expeditions, decor, workers, purchaseDomikTypes, now, reload, refreshPurchaseTypes, setVillage, hurryManufacture, hurryDomik, startExpedition, buyDecor } =
        useGameData();

    const [shopVisible, setShopVisible] = useState(false);
    const [selectedDomikId, setSelectedDomikId] = useState<number | null>(null);
    const [expandedReceiptId, setExpandedReceiptId] = useState<number | null>(null);
    const [optionalReceiptIds, setOptionalReceiptIds] = useState<ReadonlySet<number>>(new Set());
    const [manualReceiptIds, setManualReceiptIds] = useState<ReadonlySet<number>>(new Set());
    const [selectedWorkerIdsByReceipt, setSelectedWorkerIdsByReceipt] = useState<Record<number, number[]>>({});
    const [identityOpen, setIdentityOpen] = useState(false);
    const [villageLevelOpen, setVillageLevelOpen] = useState(false);
    const [villageLevelPos, setVillageLevelPos] = useState<{ top: number; right: number } | null>(null);
    const villageLevelBtnRef = useRef<HTMLButtonElement>(null);
    const [identityDismissed, setIdentityDismissed] = useState(false);
    const [draftVillageName, setDraftVillageName] = useState('');
    const [draftCrestIcon, setDraftCrestIcon] = useState(0);
    const [draftCrestColor, setDraftCrestColor] = useState(0);

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
    const currentWeather = weather?.current ?? null;
    const weatherEffect = selected == null
        ? null
        : currentWeather?.effects.find(effect => effect.domikTypeId === selected.domikType.id) ?? null;
    const CurrentWeatherIcon = currentWeather == null ? null : WEATHER_ICONS[currentWeather.logicName] ?? CloudSunIcon;
    const goldValue = resources.find(x => x.typeId === GOLD_RESOURCE_TYPE_ID)?.value ?? 0;

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

    const runAction = async (action: () => Promise<void>) => {
        try {
            await action();
        } catch (err) {
            if (err instanceof ApiError) {
                toast.error(err.message);
                return;
            }
            throw err;
        }
    };

    const buy = (typeId: number) => runAction(async () => {
        await apiPost(`Domiki/BuyDomik/${typeId}`);
        await reload();
        await refreshPurchaseTypes();
    });

    const upgrade = (id: number) => runAction(async () => {
        await apiPost(`Domiki/UpgradeDomik/${id}`);
        await reload();
    });

    const startManufacture = (domikId: number, receiptId: number, useOptional: boolean, workerIds?: number[]) => runAction(async () => {
        const workerIdsQuery = (workerIds ?? []).map(id => `&workerIds=${id}`).join('');
        await apiPost(`Domiki/StartManufacture/${domikId}/${receiptId}?useOptional=${String(useOptional)}${workerIdsQuery}`);
        setSelectedWorkerIdsByReceipt(prev => ({ ...prev, [receiptId]: [] }));
        await reload();
    });

    const completeOrder = (orderId: number) => runAction(async () => {
        await completeOrderApi(orderId);
        await reload();
        if (shopVisible) {
            await refreshPurchaseTypes();
        }
    });

    const hurryManufactureAction = (manufactureId: number) => runAction(() => hurryManufacture(manufactureId));

    const hurryDomikAction = (domikId: number) => runAction(() => hurryDomik(domikId));

    const startExpeditionAction = (expeditionTypeId: number) => runAction(() => startExpedition(expeditionTypeId));

    const buyDecorAction = (decorTypeId: number) => runAction(() => buyDecor(decorTypeId));

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

    const selectDomik = (id: number) => setSelectedDomikId(id);

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

                            const image = '/images/resourceTypes/' + resourceType.logicName + '.png';
                            return (
                                <div key={resource.typeId} className="resource-box" title={resourceType.name}>
                                    <img src={image} alt={resourceType.name} />
                                    <span className="resource-value">{resource.value}</span>
                                </div>
                            );
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
                                    setVillageLevelPos({ top: rect.bottom + 8, right: window.innerWidth - rect.right });
                                }
                                setVillageLevelOpen(prev => !prev);
                            }}>
                            <span className="village-level-label">Обжитость</span>
                            <span className="village-level-value">{villageLevel.level}</span>
                        </button>
                        {villageLevelOpen && villageLevelPos != null && createPortal(
                            <div className="village-level-popover" style={{ top: villageLevelPos.top, right: villageLevelPos.right }}>
                                <span>Постройки: {villageLevel.buildings}</span>
                                <span>Жители: {villageLevel.residents}</span>
                                <span>Репутация: {villageLevel.reputation}</span>
                                <span>Уют: {villageLevel.comfort}</span>
                                {villageLevel.upcomingUnlocks[0] != null &&
                                    <span className="village-level-next">
                                        {villageLevel.upcomingUnlocks[0].label}: {villageLevel.upcomingUnlocks[0].level}
                                    </span>
                                }
                            </div>,
                            document.body)}
                    </>
                }
                {weather != null && currentWeather != null && CurrentWeatherIcon != null &&
                    <div className="weather-strip" title={currentWeather.weatherName}>
                        <CurrentWeatherIcon className="weather-ico" aria-hidden="true" />
                        <span className="weather-name">{currentWeather.weatherName}</span>
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
            <OrdersBox orders={orders} reputation={reputation} resourceTypes={resourceTypes}
                resources={resources} now={now} onComplete={completeOrder} />
            <BlueprintsBox blueprints={blueprints} />
            <ExpeditionsBox expeditions={expeditions} resourceTypes={resourceTypes} resources={resources} workers={workers} now={now} onStart={startExpeditionAction} />
            <DecorBox decor={decor} resourceTypes={resourceTypes} resources={resources} onBuy={buyDecorAction} />
            <WorkersBox workers={workers} domikTypes={domikTypes} now={now} />
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
                {purchaseDomikTypes != null &&
                    <button className="btn-game" onClick={() => toggleShop()}>
                        <StoreIcon className="btn-ico" aria-hidden="true" />
                        {shopVisible ? 'Закрыть магазин' : 'Магазин'}
                    </button>
                }
            </div>
            <div className="workspace">
                <section className="village">
                    {shopVisible && purchaseDomikTypes != null &&
                        <div className="purchase-box">
                            {purchaseDomikTypes.length === 0 &&
                                <span className="hint">Магазин пуст</span>
                            }
                            {purchaseDomikTypes.map(purchaseDomikType => {
                                const image = '/images/domikTypes/' + purchaseDomikType.logicName + '.png';
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
                                        <img className="plot-sprite" src={image} alt={purchaseDomikType.name} />
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
                    <div className="domiks">
                        {domikTypes.length > 0 &&
                            domiks.map(domik => {
                                const domikType = domikTypes.find(x => x.id === domik.typeId);
                                if (domikType == null) {
                                    return null;
                                }

                                const image = '/images/domikTypes/' + domikType.logicName + '.png';
                                const hasManufacture = domik.manufactures != null && domik.manufactures.length > 0;
                                const durationSecondsText = domik.finishDate != null
                                    ? formatDuration(remainingSeconds(domik.finishDate, now))
                                    : null;
                                return (
                                    <button key={domik.id}
                                        className={'plot' + (selectedDomikId === domik.id ? ' plot-selected' : '')}
                                        onClick={() => selectDomik(domik.id)}>
                                        <img className="plot-sprite" src={image} alt={domikType.name} />
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
                <aside className="actions pixel-panel">
                    {selected == null &&
                        <p className="hint">Выберите домик в деревне – здесь появятся улучшение и производство.</p>
                    }
                    {selected != null &&
                        <div>
                            <h3 className="panel-title">{selected.domikType.name}</h3>
                            <span className="domik-level">ур. {selected.domik.level}</span>
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
                                                <span className="panel-label">Строится</span>
                                                <span className="timer">{selected.remainingText}</span>
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
                                                                                <span className="worker-name">{worker.name}</span>
                                                                                <span className="worker-effect">{fitness >= 0 ? '+' : ''}{fitness} %</span>
                                                                            </button>
                                                                        );
                                                                    })}
                                                                </div>
                                                            }
                                                            <button className="btn-game"
                                                                disabled={!canRun}
                                                                onClick={() => startManufacture(selected.domik.id, receipt.id, hasOptional && useOptional, isManual ? validSelectedIds : undefined)}>
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
                                                goldValue={goldValue} onHurry={hurryManufactureAction} />
                                        );
                                    })}
                                </div>
                            }
                        </div>
                    }
                </aside>
            </div>
        </div>
    );
};

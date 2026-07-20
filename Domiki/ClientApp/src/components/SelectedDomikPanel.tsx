import { useReducer, useState } from 'react';
import type { Dispatch, KeyboardEvent } from 'react';
import ArrowUpIcon from 'pixelarticons/svg/arrow-up.svg?react';
import BriefcaseIcon from 'pixelarticons/svg/briefcase.svg?react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import GridIcon from 'pixelarticons/svg/grid-3x3.svg?react';
import InfoBoxIcon from 'pixelarticons/svg/info-box.svg?react';
import PlayIcon from 'pixelarticons/svg/play.svg?react';
import ZapIcon from 'pixelarticons/svg/zap.svg?react';
import type { DomikTypeDto, GoalsStateDto, ReceiptDto, ResourceDto, ResourceTypeDto, SelectedDomikView, VillageLevelDto, WeatherEffectDto, WeatherPeriodDto, WorkerDto } from '../types/api';
import type { DomikNamer } from '../utils/domikNames';
import { SICK_CHANCE_PERCENT, SICK_MIN_VILLAGE_LEVEL, canInstaFinish, computeReceiptView, instaFinishCost, isWorkerFree, progressPercent, resourceShortfall, workerFitness } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { domikLore } from '../utils/domikLore';
import { pluralRu } from '../utils/plural';
import { isSkilledWorker } from '../utils/worker';
import { ManufactureBox } from './ManufactureBox';
import { ActionButton } from './ActionButton';
import { StatChip } from './StatChip';
import { ProgressBar } from './ProgressBar';
import { ResourcesBox } from './ResourcesBox';
import { AbstractSprite, ResourceSprite, WorkerSprite } from './sprites';

interface ReceiptUiState {
    expandedId: number | null;
    optionalIds: ReadonlySet<number>;
    autoRepeatIds: ReadonlySet<number>;
    manualIds: ReadonlySet<number>;
    workersByReceipt: Record<number, number[]>;
}

type ReceiptUiAction =
    | { type: 'toggleExpand'; id: number }
    | { type: 'toggleOptional'; id: number }
    | { type: 'toggleAutoRepeat'; id: number }
    | { type: 'toggleManual'; id: number }
    | { type: 'toggleWorker'; id: number; workerId: number; maxCount: number }
    | { type: 'clearWorkers'; id: number };

const initialReceiptUiState: ReceiptUiState = {
    expandedId: null,
    optionalIds: new Set(),
    autoRepeatIds: new Set(),
    manualIds: new Set(),
    workersByReceipt: {},
};

const toggledSet = (set: ReadonlySet<number>, id: number): ReadonlySet<number> => {
    const next = new Set(set);
    if (next.has(id)) {
        next.delete(id);
    } else {
        next.add(id);
    }
    return next;
};

const receiptUiReducer = (state: ReceiptUiState, action: ReceiptUiAction): ReceiptUiState => {
    switch (action.type) {
        case 'toggleExpand':
            return { ...state, expandedId: state.expandedId === action.id ? null : action.id };
        case 'toggleOptional':
            return { ...state, optionalIds: toggledSet(state.optionalIds, action.id) };
        case 'toggleAutoRepeat':
            return { ...state, autoRepeatIds: toggledSet(state.autoRepeatIds, action.id) };
        case 'toggleManual':
            return { ...state, manualIds: toggledSet(state.manualIds, action.id), workersByReceipt: { ...state.workersByReceipt, [action.id]: [] } };
        case 'toggleWorker': {
            const current = state.workersByReceipt[action.id] ?? [];
            const next = current.includes(action.workerId)
                ? current.filter(id => id !== action.workerId)
                : current.length >= action.maxCount ? current : [...current, action.workerId];
            return { ...state, workersByReceipt: { ...state.workersByReceipt, [action.id]: next } };
        }
        case 'clearWorkers':
            return { ...state, workersByReceipt: { ...state.workersByReceipt, [action.id]: [] } };
        default:
            return state;
    }
};

interface ReceiptRowProps {
    receipt: ReceiptDto;
    domikId: number;
    domikType: DomikTypeDto;
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
    workers: WorkerDto[];
    goals: GoalsStateDto | null;
    villageLevel: VillageLevelDto | null;
    weatherEffect: WeatherEffectDto | null;
    now: number;
    plodderFree: number;
    atManufactureCap: boolean;
    runningManufactures: number;
    maxManufactures: number;
    ui: { expanded: boolean; useOptional: boolean; autoRepeat: boolean; isManual: boolean; selectedWorkerIds: number[] };
    dispatch: Dispatch<ReceiptUiAction>;
    onStart: (domikId: number, receiptId: number, useOptional: boolean, autoRepeat: boolean, workerIds?: number[]) => Promise<boolean>;
    formatShortfall: (cost: { typeId: number; value: number }[]) => string;
}

const ReceiptRow = ({ receipt, domikId, domikType, resources, resourceTypes, workers, goals, villageLevel, weatherEffect, now, plodderFree, atManufactureCap, runningManufactures, maxManufactures, ui, dispatch, onStart, formatShortfall }: ReceiptRowProps) => {
    const { expanded, useOptional, autoRepeat, isManual, selectedWorkerIds } = ui;
    const hasOptional = receipt.optionalInputResources.length > 0;
    const view = computeReceiptView(receipt, resources, plodderFree, hasOptional && useOptional, goals?.zealCharges, domikType);
    const freeWorkersForType = workers
        .flatMap(worker => isWorkerFree(worker, now) ? [{ worker, fitness: workerFitness(worker, domikType.id) }] : [])
        .sort((a, b) => b.fitness - a.fitness);
    const freeIdsForType = new Set(freeWorkersForType.map(({ worker }) => worker.id));
    const selectedIdSet = new Set(selectedWorkerIds);
    const validSelectedIds = selectedWorkerIds.filter(id => freeIdsForType.has(id));
    const missingResources = resourceShortfall(view.inputs, resources);
    const missingResourcesText = formatShortfall(view.inputs);
    const automaticWorkerShortfall = Math.max(0, receipt.plodderCount - plodderFree);
    const capReason = atManufactureCap ? `Все слоты заняты: ${runningManufactures} / ${maxManufactures}` : null;
    const canRun = (isManual
        ? view.hasResources && validSelectedIds.length === receipt.plodderCount
        : view.canRun) && !atManufactureCap;
    const workerBlockReason = isManual
        ? validSelectedIds.length !== receipt.plodderCount
            ? `Выберите ровно ${receipt.plodderCount} трудяг (сейчас ${validSelectedIds.length})`
            : null
        : !view.hasPlodders ? `Не хватает свободных трудяг: ${automaticWorkerShortfall}` : null;
    const blockTitle = [
        capReason,
        !view.hasResources ? `Не хватает: ${missingResourcesText}` : null,
        workerBlockReason,
    ].filter(reason => reason != null).join('; ');
    const summaryBlockTitle = [
        !view.hasResources ? `Не хватает: ${missingResourcesText}` : null,
        !view.hasPlodders ? `Не хватает свободных трудяг: ${automaticWorkerShortfall}` : null,
    ].filter(reason => reason != null).join('; ');

    const startAndClear = (workerIds?: number[]) =>
        onStart(domikId, receipt.id, hasOptional && useOptional, autoRepeat, workerIds).then(ok => {
            if (ok) {
                dispatch({ type: 'clearWorkers', id: receipt.id });
            }
        });

    return (
        <div className={'receipt-row' + (expanded ? ' receipt-open' : '') + (view.canRun ? '' : ' receipt-blocked')}>
            <button type="button" className="receipt-head"
                aria-expanded={expanded}
                onClick={() => dispatch({ type: 'toggleExpand', id: receipt.id })}>
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
                                onChange={() => dispatch({ type: 'toggleOptional', id: receipt.id })} />
                            с инструментом (+{receipt.outputBonusPercent}% выхода)
                        </label>
                    }
                    <label className="receipt-optional">
                        <input type="checkbox" checked={autoRepeat}
                            onChange={() => dispatch({ type: 'toggleAutoRepeat', id: receipt.id })} />
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
                    {weatherEffect != null && weatherEffect.outputPercent > 100 && (villageLevel?.level ?? 0) >= SICK_MIN_VILLAGE_LEVEL &&
                        <p className="weather-modifier weather-modifier--risk">
                            Риск простуды {SICK_CHANCE_PERCENT} %
                        </p>
                    }
                    <div className="receipt-mode">
                        <label className="receipt-optional">
                            <input type="checkbox" checked={isManual}
                                onChange={() => dispatch({ type: 'toggleManual', id: receipt.id })} />
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
                                const isSelected = selectedIdSet.has(worker.id);
                                return (
                                    <button key={worker.id} type="button"
                                        className={'worker-chip worker-chip-pick' + (isSelected ? ' worker-chip-selected' : '')}
                                        onClick={() => receipt.plodderCount === 1 && view.hasResources && !atManufactureCap
                                            ? startAndClear([worker.id])
                                            : dispatch({ type: 'toggleWorker', id: receipt.id, workerId: worker.id, maxCount: receipt.plodderCount })}>
                                        <WorkerSprite name={worker.name} skilled={isSkilledWorker(worker)} className="worker-avatar" aria-hidden="true" />
                                        <span className="worker-name">{worker.name}</span>
                                        <span className="worker-effect">{fitness >= 0 ? '+' : ''}{fitness} %</span>
                                    </button>
                                );
                            })}
                        </div>
                    }
                    <ActionButton className="btn-game"
                        disabled={!canRun}
                        title={!canRun ? blockTitle : undefined}
                        onClick={() => startAndClear(isManual ? validSelectedIds : undefined)}>
                        <PlayIcon className="btn-ico" aria-hidden="true" />
                        Запустить
                    </ActionButton>
                     {!canRun &&
                        <div className="note-warn resource-shortfall">
                            <img src="/images/upgrade_no_resources.png" alt="" />
                            {capReason != null && <span>{capReason}</span>}
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
};

type PanelView = 'work' | 'grow';
type GrowPip = 'none' | 'available' | 'affordable' | 'building';

interface PanelTabsProps {
    active: PanelView;
    onSelect: (view: PanelView) => void;
    workPip: boolean;
    growPip: GrowPip;
}

const growPipLabel: Record<GrowPip, string> = {
    none: '',
    available: ', есть улучшение',
    affordable: ', улучшение по карману',
    building: ', идёт улучшение',
};

const PanelTabs = ({ active, onSelect, workPip, growPip }: PanelTabsProps) => {
    const order: PanelView[] = ['work', 'grow'];
    const onKey = (event: KeyboardEvent<HTMLButtonElement>, view: PanelView) => {
        if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
            return;
        }
        event.preventDefault();
        const other: PanelView = view === 'work' ? 'grow' : 'work';
        const next: PanelView = event.key === 'Home' ? 'work' : event.key === 'End' ? 'grow' : other;
        onSelect(next);
        requestAnimationFrame(() => document.getElementById(`panel-tab-${next}`)?.focus());
    };

    return (
        <div className="panel-tabs" role="tablist" aria-label="Разделы постройки">
            {order.map(view => {
                const isActive = view === active;
                const isWork = view === 'work';
                const showPip = !isActive && (isWork ? workPip : growPip !== 'none');
                const pipClass = isWork ? 'panel-tab-pip--idle' : `panel-tab-pip--${growPip}`;
                const label = isWork ? 'Дела' : 'Рост';
                const ariaLabel = isWork
                    ? workPip ? 'Дела, есть свободный слот' : undefined
                    : growPip === 'none' ? undefined : `Рост${growPipLabel[growPip]}`;
                return (
                    <button key={view} type="button" role="tab" id={`panel-tab-${view}`}
                        aria-selected={isActive}
                        aria-controls="panel-view"
                        aria-label={ariaLabel}
                        tabIndex={isActive ? 0 : -1}
                        className={'panel-tab' + (isActive ? ' panel-tab-active' : '')}
                        onKeyDown={event => onKey(event, view)}
                        onClick={() => onSelect(view)}>
                        {isWork
                            ? <BriefcaseIcon className="panel-tab-ico" aria-hidden="true" />
                            : <ArrowUpIcon className="panel-tab-ico" aria-hidden="true" />}
                        {label}
                        {showPip && <span className={'panel-tab-pip ' + pipClass} aria-hidden="true" />}
                    </button>
                );
            })}
        </div>
    );
};

interface UpgradeBenefits {
    plodderDelta: number;
    manufactureDelta: number;
    newReceipts: ReceiptDto[];
}

interface SelectedDomikPanelProps {
    selected: SelectedDomikView | null;
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
    workers: WorkerDto[];
    goals: GoalsStateDto | null;
    villageLevel: VillageLevelDto | null;
    currentWeather: WeatherPeriodDto | null;
    now: number;
    goldValue: number;
    goldType: ResourceTypeDto | undefined;
    plodderFree: number;
    displayName: DomikNamer;
    onClose: () => void;
    onUpgrade: (id: number) => void;
    onHurryDomik: (id: number) => void;
    onStartManufacture: (domikId: number, receiptId: number, useOptional: boolean, autoRepeat: boolean, workerIds?: number[]) => Promise<boolean>;
    onHurryManufacture: (manufactureId: number) => void;
    onToggleManufactureRepeat: (manufactureId: number, next: boolean) => void;
}

export const SelectedDomikPanel = ({ selected, resources, resourceTypes, receipts, workers, goals, villageLevel, currentWeather, now, goldValue, goldType, plodderFree, displayName, onClose, onUpgrade, onHurryDomik, onStartManufacture, onHurryManufacture, onToggleManufactureRepeat }: SelectedDomikPanelProps) => {
    const [ui, dispatch] = useReducer(receiptUiReducer, initialReceiptUiState);
    const [tab, setTab] = useState<PanelView>('work');
    const [tabbedDomikId, setTabbedDomikId] = useState(selected?.domik.id);
    if (selected?.domik.id !== tabbedDomikId) {
        setTabbedDomikId(selected?.domik.id);
        setTab('work');
    }

    const upgradeBenefits: UpgradeBenefits | null = selected?.upgrade == null
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
            const currentReceiptIds = new Set(currentLevel.receiptIds);
            const newReceipts: ReceiptDto[] = nextLevel.receiptIds.flatMap(id => {
                if (currentReceiptIds.has(id)) {
                    return [];
                }
                const receipt = receipts.find(r => r.id === id);
                return receipt == null ? [] : [receipt];
            });

            return plodderDelta > 0 || manufactureDelta > 0 || newReceipts.length > 0
                ? { plodderDelta, manufactureDelta, newReceipts }
                : null;
        })();
    const maxManufactures = selected?.domikType.levels.find(level => level.value === selected.domik.level)?.maxManufactureCount ?? 0;
    const runningManufactures = selected?.domik.manufactures?.length ?? 0;
    const atManufactureCap = maxManufactures > 0 && runningManufactures >= maxManufactures;
    const weatherEffect = selected == null
        ? null
        : currentWeather?.effects.find(effect => effect.domikTypeId === selected.domikType.id) ?? null;
    const formatShortfall = (cost: { typeId: number; value: number }[]) => resourceShortfall(cost, resources)
        .map(item => `${resourceTypes.find(type => type.id === item.typeId)?.name ?? `ресурс #${item.typeId}`} ×${item.value}`)
        .join(', ');

    const isBuilding = selected?.domik.finishDate != null;
    const hasGrow = selected?.upgrade != null || isBuilding;
    const hasWork = selected != null && (selected.receipts.length > 0 || runningManufactures > 0);
    const showTabs = hasWork && hasGrow;
    const activeView: PanelView = showTabs ? tab : hasWork ? 'work' : 'grow';
    const idlePip = hasWork && maxManufactures > 0 && runningManufactures < maxManufactures && selected.receipts.length > 0;
    const growPip: GrowPip = isBuilding
        ? 'building'
        : selected?.upgrade != null
            ? selected.upgrade.hasResources ? 'affordable' : 'available'
            : 'none';
    const runningTimers = (selected?.domik.manufactures ?? [])
        .map(manufacture => remainingSeconds(manufacture.finishDate, now))
        .filter(seconds => seconds > 0);
    const soonestManufacture = runningTimers.length > 0 ? Math.min(...runningTimers) : null;
    const statusTimer = isBuilding
        ? selected.remainingText ?? null
        : soonestManufacture != null ? formatDuration(soonestManufacture) : null;

    return (
        <aside className={'actions pixel-panel' + (selected == null ? ' actions--empty' : '')}>
            <button type="button" className="actions-close" title="Закрыть" onClick={onClose}>
                <CloseIcon className="btn-ico" aria-hidden="true" />
            </button>
            {selected == null &&
                <p className="hint">Выберите домик в деревне – здесь появятся улучшение и производство.</p>
            }
            {selected != null &&
                <div>
                    <div className="actions-heading">
                        <h3 className="panel-title">
                            {displayName(selected.domik.typeId, selected.domik.id, selected.domikType.name, selected.domikType.logicName)}
                            <span className="domik-level">ур. {selected.domik.level}</span>
                            {domikLore[selected.domikType.logicName] != null &&
                                <span className="lore-tip" tabIndex={0} aria-label="Описание постройки">
                                    <InfoBoxIcon className="lore-tip-ico" aria-hidden="true" />
                                    <span className="lore-tip-pop" role="tooltip">{domikLore[selected.domikType.logicName]}</span>
                                </span>
                            }
                        </h3>
                        {(maxManufactures > 0 || statusTimer != null) &&
                            <div className="panel-status">
                                {maxManufactures > 0 &&
                                    <span className={'panel-status-item' + (atManufactureCap ? ' panel-status-item--full' : '')}
                                        title="Занятые слоты производства">
                                        <GridIcon className="panel-status-ico" aria-hidden="true" />
                                        {runningManufactures} / {maxManufactures}
                                    </span>
                                }
                                {statusTimer != null &&
                                    <span className="panel-status-item"
                                        title={isBuilding ? 'До конца улучшения' : 'До ближайшей готовой смены'}>
                                        {isBuilding
                                            ? <ArrowUpIcon className="panel-status-ico" aria-hidden="true" />
                                            : <ClockIcon className="panel-status-ico" aria-hidden="true" />}
                                        {statusTimer}
                                    </span>
                                }
                            </div>
                        }
                        {showTabs &&
                            <PanelTabs active={activeView} onSelect={setTab} workPip={idlePip} growPip={growPip} />
                        }
                    </div>
                    <div className="panel-view" id="panel-view" role={showTabs ? 'tabpanel' : undefined}>
                    {activeView === 'grow' && selected.upgrade != null &&
                        <div className="panel-block">
                            <div className="upgrade-row">
                                <span className="panel-label">Улучшение до ур. {selected.upgrade.nextLevel}</span>
                                <ResourcesBox resources={selected.upgrade.resources} resourceTypes={resourceTypes} have={resources} />
                            </div>
                            {upgradeBenefits != null &&
                                <div className="upgrade-benefits">
                                    <div className="upgrade-benefits-chips">
                                        {upgradeBenefits.plodderDelta > 0 &&
                                            <StatChip icon={<img className="stat-chip-ico" src="/images/modificatorTypes/plodder.png" alt="" />} title="Вместимость трудяг">
                                                +{upgradeBenefits.plodderDelta} {pluralRu(upgradeBenefits.plodderDelta, 'трудяга', 'трудяги', 'трудяг')}
                                            </StatChip>}
                                        {upgradeBenefits.manufactureDelta > 0 &&
                                            <StatChip icon={<GridIcon className="stat-chip-ico" aria-hidden="true" />} title="Одновременные производства">
                                                +{upgradeBenefits.manufactureDelta} {pluralRu(upgradeBenefits.manufactureDelta, 'производство', 'производства', 'производств')}
                                            </StatChip>}
                                        {upgradeBenefits.newReceipts.slice(0, 3).map(receipt =>
                                            <StatChip key={receipt.id} icon={<AbstractSprite logicName="production_recipe" size={24} className="stat-chip-ico" aria-hidden="true" />} title="Новый рецепт">
                                                {receipt.name}
                                            </StatChip>)}
                                        {upgradeBenefits.newReceipts.length > 3 &&
                                            <StatChip icon={<AbstractSprite logicName="production_recipe" size={24} className="stat-chip-ico" aria-hidden="true" />} title={upgradeBenefits.newReceipts.slice(3).map(receipt => receipt.name).join(', ')}>
                                                +{upgradeBenefits.newReceipts.length - 3} ещё
                                            </StatChip>}
                                    </div>
                                </div>
                            }
                            <ActionButton className="btn-game"
                                disabled={!selected.upgrade.hasResources}
                                title={selected.upgrade.hasResources ? undefined : `Не хватает: ${formatShortfall(selected.upgrade.resources)}`}
                                onClick={() => onUpgrade(selected.domik.id)}>
                                <ArrowUpIcon className="btn-ico" aria-hidden="true" />
                                Улучшить
                            </ActionButton>
                        </div>
                    }
                    {activeView === 'grow' && selected.domik.finishDate != null &&
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
                                        <ActionButton className="btn-game"
                                            disabled={tooFar || notEnoughGold}
                                            title={hurryTitle}
                                            onClick={() => onHurryDomik(selected.domik.id)}>
                                            <ZapIcon className="btn-ico" aria-hidden="true" />
                                            Поторопить – {Math.max(1, hurryCost)}
                                            {goldType != null &&
                                                <ResourceSprite logicName={goldType.logicName} className="hurry-cost-ico" aria-hidden="true" />
                                            }
                                        </ActionButton>
                                    </>
                                );
                            })()}
                        </div>
                    }
                    {activeView === 'work' && selected.domik.manufactures != null && selected.domik.manufactures.length > 0 &&
                        <div className="panel-block">
                            <span className="panel-label">Идёт сейчас</span>
                            {selected.domik.manufactures.map(manufacture => {
                                const receipt = receipts.find(x => x.id === manufacture.receiptId);
                                if (receipt == null) {
                                    return null;
                                }

                                return (
                                    <ManufactureBox key={manufacture.id} manufacture={manufacture} receipt={receipt}
                                        now={now} remainingText={formatDuration(remainingSeconds(manufacture.finishDate, now))}
                                        goldValue={goldValue} goldType={goldType} onHurry={onHurryManufacture}
                                        onToggleAutoRepeat={onToggleManufactureRepeat} />
                                );
                            })}
                        </div>
                    }
                    {activeView === 'work' && selected.receipts.length > 0 &&
                        <div className="panel-block">
                            <span className="panel-label">Запустить производство</span>
                            <div className="receipt-list">
                                {selected.receipts.map(receipt =>
                                    <ReceiptRow key={receipt.id}
                                        receipt={receipt}
                                        domikId={selected.domik.id}
                                        domikType={selected.domikType}
                                        resources={resources}
                                        resourceTypes={resourceTypes}
                                        workers={workers}
                                        goals={goals}
                                        villageLevel={villageLevel}
                                        weatherEffect={weatherEffect}
                                        now={now}
                                        plodderFree={plodderFree}
                                        atManufactureCap={atManufactureCap}
                                        runningManufactures={runningManufactures}
                                        maxManufactures={maxManufactures}
                                        ui={{
                                            expanded: ui.expandedId === receipt.id,
                                            useOptional: ui.optionalIds.has(receipt.id),
                                            autoRepeat: ui.autoRepeatIds.has(receipt.id),
                                            isManual: ui.manualIds.has(receipt.id),
                                            selectedWorkerIds: ui.workersByReceipt[receipt.id] ?? [],
                                        }}
                                        dispatch={dispatch}
                                        onStart={onStartManufacture}
                                        formatShortfall={formatShortfall} />,
                                )}
                            </div>
                        </div>
                    }
                    </div>
                </div>
            }
        </aside>
    );
};

import { useReducer, useState } from 'react';
import type { Dispatch, KeyboardEvent, Ref } from 'react';
import ArrowUpIcon from 'pixelarticons/svg/arrow-up.svg?react';
import BriefcaseIcon from 'pixelarticons/svg/briefcase.svg?react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import InfoBoxIcon from 'pixelarticons/svg/info-box.svg?react';
import PlayIcon from 'pixelarticons/svg/play.svg?react';
import type { BlueprintDto, DomikTypeDto, GoalsStateDto, ReceiptDto, ResourceDto, ResourceTypeDto, SelectedDomikView, SickTypeDto, VillageLevelDto, WeatherEffectDto, WeatherPeriodDto, WorkerDto } from '../types/api';
import type { DomikNamer } from '../utils/domikNames';
import { PLODDER_MODIFICATOR_TYPE_ID, SICK_MIN_VILLAGE_LEVEL, computeReceiptView, goldVeinView, isWorkerFree, keyResourceTypeIds, progressPercent, residentsGain, resourceShortfall, workIntensity, workerFitness, type GoldVeinContext, type GoldVeinView } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { sickRiskPercent, sickTypeForWeather, weatherMark, weatherOutputChange } from '../utils/weather';
import { domikLore } from '../utils/domikLore';
import { pluralRu } from '../utils/plural';
import { isSkilledWorker } from '../utils/worker';
import { ManufactureBox } from './ManufactureBox';
import { ActionButton } from './ActionButton';
import { HurryButton } from './HurryButton';
import { StatChip } from './StatChip';
import { ProgressBar } from './ProgressBar';
import { ResourcesBox } from './ResourcesBox';
import { WeatherMark } from './WeatherMark';
import { AbstractSprite, DomikSprite, ResourceSprite, WorkerSprite } from './sprites';

const MEASURE_MIN_LEVEL = 2;
const SHOWN_OUTPUTS = 2;
const GOLD_VEIN_LORE = 'Рудник за сутки отдаёт золота по своей ступени: ступень 3 – три крупицы. Золото не разменять – им торопят стройку, снаряжают походы да доплачивают за новые постройки, потому и копится по крупице. Углубишь рудник – жила станет щедрее.';

interface ReceiptUiState {
    expandedIds: ReadonlySet<number>;
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
    expandedIds: new Set(),
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
            return { ...state, expandedIds: toggledSet(state.expandedIds, action.id) };
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
    blueprintLock: string | null;
    domikId: number;
    domikType: DomikTypeDto;
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
    workers: WorkerDto[];
    goals: GoalsStateDto | null;
    villageLevel: VillageLevelDto | null;
    weatherEffect: WeatherEffectDto | null;
    sickName: string | null;
    now: number;
    plodderFree: number;
    atManufactureCap: boolean;
    runningManufactures: number;
    maxManufactures: number;
    goldVein: GoldVeinView | null;
    ui: { expanded: boolean; useOptional: boolean; autoRepeat: boolean; isManual: boolean; selectedWorkerIds: number[] };
    dispatch: Dispatch<ReceiptUiAction>;
    onStart: (domikId: number, receiptId: number, useOptional: boolean, autoRepeat: boolean, workerIds?: number[]) => Promise<boolean>;
    formatShortfall: (cost: { typeId: number; value: number }[]) => string;
}

const ReceiptRow = ({ receipt, blueprintLock, domikId, domikType, resources, resourceTypes, workers, goals, villageLevel, weatherEffect, sickName, now, plodderFree, atManufactureCap, runningManufactures, maxManufactures, goldVein, ui, dispatch, onStart, formatShortfall }: ReceiptRowProps) => {
    const { expanded, useOptional, autoRepeat, isManual, selectedWorkerIds } = ui;
    const hasOptional = receipt.optionalInputResources.length > 0;
    const optionalNames = receipt.optionalInputResources
        .map(item => `${resourceTypes.find(type => type.id === item.typeId)?.name ?? `ресурс #${item.typeId}`} ×${item.value}`)
        .join(', ');
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
    const capReason = atManufactureCap ? `Все места заняты: ${runningManufactures} из ${maxManufactures}` : null;
    const canRun = (isManual
        ? view.hasResources && validSelectedIds.length === receipt.plodderCount
        : view.canRun) && !atManufactureCap && goldVein?.blockReason == null && blueprintLock == null;
    const workerBlockReason = isManual
        ? validSelectedIds.length !== receipt.plodderCount
            ? `Выберите ровно ${receipt.plodderCount} трудяг (сейчас ${validSelectedIds.length})`
            : null
        : !view.hasPlodders ? `Не хватает свободных трудяг: ${automaticWorkerShortfall}` : null;
    const blockTitle = [
        blueprintLock,
        capReason,
        goldVein?.blockReason ?? null,
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

    const lackLabel = !view.hasResources ? 'нет припасов' : !view.hasPlodders ? 'нет трудяг' : null;
    const weatherOutput = weatherEffect == null ? null : weatherOutputChange(receipt.outputResources, weatherEffect.outputPercent);

    return (
        <div className={'receipt-row' + (expanded ? ' receipt-open' : '') + (view.canRun ? '' : ' receipt-blocked')}>
            <button type="button" className="receipt-head"
                aria-expanded={expanded}
                onClick={() => dispatch({ type: 'toggleExpand', id: receipt.id })}>
                {receipt.outputResources.length > 0 &&
                    <span className="receipt-yield">
                        {receipt.outputResources.slice(0, SHOWN_OUTPUTS).map(output => {
                            const outputType = resourceTypes.find(type => type.id === output.typeId);
                            return outputType == null ? null : (
                                <span key={output.typeId} className="receipt-yield-item"
                                    aria-label={`даёт ${outputType.name}: ${output.value}`}>
                                    <ResourceSprite logicName={outputType.logicName} size={24} aria-hidden="true" />
                                    <span className="receipt-yield-value">×{output.value}</span>
                                </span>
                            );
                        })}
                    </span>
                }
                <span className="receipt-main">
                    <span className="receipt-name">{receipt.name}</span>
                    <span className="receipt-meta">
                        <span className="receipt-stat" title="Трудяги на смену">
                            <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                            {receipt.plodderCount}
                        </span>
                        <span className="receipt-stat" title="Длительность смены">
                            <ClockIcon aria-hidden="true" />
                            {formatDuration(view.effectiveDurationSeconds)}
                        </span>
                        {goldVein != null &&
                            <span className={'receipt-vein' + (goldVein.exhausted ? ' receipt-vein--spent' : '')} title={GOLD_VEIN_LORE}>
                                {goldVein.exhausted ? 'жила выбрана' : `жила: ${goldVein.mined} из ${goldVein.cap}`}
                            </span>}
                        {view.zealMultiplier > 1 && <span className="receipt-zeal">×{view.zealMultiplier}</span>}
                        {lackLabel != null && <span className="receipt-lack" title={summaryBlockTitle}>{lackLabel}</span>}
                    </span>
                </span>
                <ChevronDownIcon className="receipt-caret" aria-hidden="true" />
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
                        {receipt.outputResources.length > SHOWN_OUTPUTS &&
                            <div className="receipt-io-row">
                                <span className="receipt-io-label">Даёт</span>
                                <ResourcesBox resources={receipt.outputResources} resourceTypes={resourceTypes} />
                            </div>
                        }
                    </div>
                    {weatherOutput != null && weatherOutput.delta !== 0 &&
                        <p className="weather-modifier">
                            Погода: выход {weatherOutput.base} → {weatherOutput.granted}
                        </p>
                    }
                    {weatherOutput != null && weatherOutput.delta > 0 && sickName != null && (villageLevel?.level ?? 0) >= SICK_MIN_VILLAGE_LEVEL &&
                        <p className="weather-modifier weather-modifier--risk">
                            {sickName}: риск {sickRiskPercent(weatherOutput.delta, receipt.plodderCount)} %
                        </p>
                    }
                    {goldVein != null &&
                        <p className="weather-modifier" title={GOLD_VEIN_LORE}>
                            {goldVein.exhausted
                                ? `Жила на сегодня выбрана: ${goldVein.mined} из ${goldVein.cap} – столько рудник за сутки и отдаёт. Свежая порода подойдёт через ${goldVein.hoursToFresh} ч.`
                                : `Жила на сегодня: намыто ${goldVein.mined} из ${goldVein.cap} – рудник отдаёт за сутки по своей ступени.`}
                        </p>
                    }
                    <div className="receipt-options">
                        {hasOptional &&
                            <label className="receipt-optional">
                                <input type="checkbox" checked={useOptional}
                                    onChange={() => dispatch({ type: 'toggleOptional', id: receipt.id })} />
                                с расходником: {optionalNames} (+{receipt.outputBonusPercent}% выхода)
                            </label>
                        }
                        <label className="receipt-optional">
                            <input type="checkbox" checked={autoRepeat}
                                onChange={() => dispatch({ type: 'toggleAutoRepeat', id: receipt.id })} />
                            Поставить наряд на смену
                        </label>
                        {autoRepeat &&
                            <p className="receipt-repeat-hint">
                                По наряду смена возобновится сама, пока хватает припасов и трудяги могут работать.
                            </p>
                        }
                        <label className="receipt-optional">
                            <input type="checkbox" checked={isManual}
                                onChange={() => dispatch({ type: 'toggleManual', id: receipt.id })} />
                            Выбрать трудяг списком
                            {isManual &&
                                <span className="receipt-mode-count">
                                    выбрано {validSelectedIds.length} / {receipt.plodderCount}
                                </span>
                            }
                        </label>
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
                     {!canRun && (!view.hasResources || workerBlockReason != null || goldVein?.blockReason != null || blueprintLock != null) &&
                        <div className="note-warn resource-shortfall">
                            <img src="/images/upgrade_no_resources.png" alt="" />
                            {!view.hasResources
                                ? <><span>Не хватает</span><ResourcesBox resources={missingResources} resourceTypes={resourceTypes} showNames /></>
                                : null}
                            {workerBlockReason != null && <span>{workerBlockReason}</span>}
                            {goldVein?.blockReason != null && <span>{goldVein.blockReason}</span>}
                            {blueprintLock != null && <span>{blueprintLock}</span>}
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
    available: Record<PanelView, boolean>;
}

const growPipLabel: Record<GrowPip, string> = {
    none: '',
    available: ', есть улучшение',
    affordable: ', улучшение по карману',
    building: ', идёт улучшение',
};

const PanelTabs = ({ active, onSelect, workPip, growPip, available }: PanelTabsProps) => {
    const order: PanelView[] = ['work', 'grow'];
    const onKey = (event: KeyboardEvent<HTMLButtonElement>, view: PanelView) => {
        if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
            return;
        }
        event.preventDefault();
        const other: PanelView = view === 'work' ? 'grow' : 'work';
        const next: PanelView = event.key === 'Home' ? 'work' : event.key === 'End' ? 'grow' : other;
        if (!available[next]) {
            return;
        }
        onSelect(next);
        requestAnimationFrame(() => document.getElementById(`panel-tab-${next}`)?.focus());
    };

    return (
        <div className="panel-tabs" role="tablist" aria-label="Разделы постройки">
            {order.map(view => {
                const isActive = view === active;
                const isWork = view === 'work';
                const isAvailable = available[view];
                const showPip = !isActive && (isWork ? workPip : growPip !== 'none');
                const pipClass = isWork ? 'panel-tab-pip--idle' : `panel-tab-pip--${growPip}`;
                const label = isWork ? 'Дела' : 'Рост';
                const emptyTitle = isWork ? 'Дел на этом дворе пока нет' : 'Расти дальше некуда';
                const ariaLabel = !isAvailable
                    ? `${label}: ${emptyTitle}`
                    : isWork
                        ? workPip ? 'Дела, есть свободное место' : undefined
                        : growPip === 'none' ? undefined : `Рост${growPipLabel[growPip]}`;
                return (
                    <button key={view} type="button" role="tab" id={`panel-tab-${view}`}
                        aria-selected={isActive}
                        aria-controls="panel-view"
                        aria-label={ariaLabel}
                        disabled={!isAvailable}
                        title={isAvailable ? undefined : emptyTitle}
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
    plodderGain: number;
    manufactureDelta: number;
    newReceipts: ReceiptDto[];
}

interface SelectedDomikPanelProps {
    ref?: Ref<HTMLElement>;
    selected: SelectedDomikView | null;
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
    blueprints: BlueprintDto[];
    workers: WorkerDto[];
    goals: GoalsStateDto | null;
    villageLevel: VillageLevelDto | null;
    currentWeather: WeatherPeriodDto | null;
    sickTypes: SickTypeDto[];
    now: number;
    goldValue: number;
    goldType: ResourceTypeDto | undefined;
    goldVein: GoldVeinContext;
    plodderFree: number;
    displayName: DomikNamer;
    onClose: () => void;
    onUpgrade: (id: number) => void;
    onHurryDomik: (id: number) => void;
    onStartManufacture: (domikId: number, receiptId: number, useOptional: boolean, autoRepeat: boolean, workerIds?: number[]) => Promise<boolean>;
    onHurryManufacture: (manufactureId: number) => void;
    onToggleManufactureRepeat: (manufactureId: number, next: boolean) => void;
    elderHouseLevel: number;
    onSetManufactureMeasure: (manufactureId: number, resourceTypeId: number | null, value: number | null) => void;
}

export const SelectedDomikPanel = ({ ref, selected, resources, resourceTypes, receipts, blueprints, workers, goals, villageLevel, currentWeather, sickTypes, now, goldValue, goldType, goldVein, plodderFree, displayName, onClose, onUpgrade, onHurryDomik, onStartManufacture, onHurryManufacture, onToggleManufactureRepeat, elderHouseLevel, onSetManufactureMeasure }: SelectedDomikPanelProps) => {
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

            const plodderDelta = (nextLevel.modificators.find(modificator => modificator.typeId === PLODDER_MODIFICATOR_TYPE_ID)?.value ?? 0)
                - (currentLevel.modificators.find(modificator => modificator.typeId === PLODDER_MODIFICATOR_TYPE_ID)?.value ?? 0);
            const plodderGain = villageLevel == null
                ? plodderDelta
                : residentsGain(villageLevel.residents, villageLevel.residentsCap, plodderDelta);
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
                ? { plodderDelta, plodderGain, manufactureDelta, newReceipts }
                : null;
        })();
    const maxManufactures = selected?.domikType.levels.find(level => level.value === selected.domik.level)?.maxManufactureCount ?? 0;
    const runningManufactures = selected?.domik.manufactures?.length ?? 0;
    const atManufactureCap = maxManufactures > 0 && runningManufactures >= maxManufactures;
    const crestIntensity = selected == null ? 'normal' : workIntensity(selected.domik, selected.domikType);
    const weatherEffect = selected == null
        ? null
        : currentWeather?.effects.find(effect => effect.domikTypeId === selected.domikType.id) ?? null;
    const sickName = sickTypeForWeather(sickTypes, currentWeather?.weatherTypeId)?.name ?? null;
    const crestWeather = selected == null ? null : weatherMark(currentWeather, selected.domikType.id);
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
    const freeSlots = maxManufactures - runningManufactures;
    const slotsText = `${freeSlots}/${maxManufactures} свободно`;
    const keyResourceTypes = keyResourceTypeIds(receipts, blueprints);
    const blueprintLockFor = (receipt: ReceiptDto) => {
        const blueprint = blueprints.find(x => x.receiptId === receipt.id);
        return blueprint == null || blueprint.owned
            ? null
            : `Нужен чертёж «${blueprint.name}»: репутация ${blueprint.neighborName} ${blueprint.currentReputation}/${blueprint.reputationThreshold}`;
    };

    const readyReceipts: ReceiptDto[] = [];
    const blockedReceipts: ReceiptDto[] = [];
    for (const receipt of selected?.receipts ?? []) {
        const veinBlocked = selected != null && goldVeinView(receipt, selected.domik, goldVein)?.blockReason != null;
        const canRun = computeReceiptView(receipt, resources, plodderFree, false, goals?.zealCharges, selected?.domikType).canRun
            && !veinBlocked && blueprintLockFor(receipt) == null;
        (canRun ? readyReceipts : blockedReceipts).push(receipt);
    }

    const renderReceipt = (receipt: ReceiptDto, view: SelectedDomikView) =>
        <ReceiptRow key={receipt.id}
            receipt={receipt}
            blueprintLock={blueprintLockFor(receipt)}
            domikId={view.domik.id}
            domikType={view.domikType}
            resources={resources}
            resourceTypes={resourceTypes}
            workers={workers}
            goals={goals}
            villageLevel={villageLevel}
            weatherEffect={weatherEffect}
            sickName={sickName}
            now={now}
            plodderFree={plodderFree}
            atManufactureCap={atManufactureCap}
            runningManufactures={runningManufactures}
            maxManufactures={maxManufactures}
            goldVein={goldVeinView(receipt, view.domik, goldVein)}
            ui={{
                expanded: ui.expandedIds.has(receipt.id),
                useOptional: ui.optionalIds.has(receipt.id),
                autoRepeat: ui.autoRepeatIds.has(receipt.id),
                isManual: ui.manualIds.has(receipt.id),
                selectedWorkerIds: ui.workersByReceipt[receipt.id] ?? [],
            }}
            dispatch={dispatch}
            onStart={onStartManufacture}
            formatShortfall={formatShortfall} />;

    return (
        <aside ref={ref} className={'actions pixel-panel' + (selected == null ? ' actions--empty' : '')}>
            {selected == null &&
                <p className="hint">Выберите домик в деревне – здесь появятся улучшение и производство.</p>
            }
            {selected != null &&
                <div>
                    <div className="actions-heading">
                        <button type="button" className="actions-close" title="Закрыть" onClick={onClose}>
                            <CloseIcon className="btn-ico" aria-hidden="true" />
                        </button>
                        <DomikSprite className="panel-crest" logicName={selected.domikType.logicName}
                            level={selected.domik.level} working={runningManufactures > 0}
                            data-motion={crestIntensity === 'normal' ? undefined : crestIntensity} aria-hidden="true" />
                        <div className="panel-ident">
                        <h3 className="panel-title">
                            {displayName(selected.domik.typeId, selected.domik.id, selected.domikType.name, selected.domikType.logicName)}
                            {domikLore[selected.domikType.logicName] != null &&
                                <span className="lore-tip" tabIndex={0} aria-label="Описание постройки">
                                    <InfoBoxIcon className="lore-tip-ico" aria-hidden="true" />
                                    <span className="lore-tip-pop" role="tooltip">{domikLore[selected.domikType.logicName]}</span>
                                </span>
                            }
                        </h3>
                        <div className="panel-level" aria-label={`Уровень ${selected.domik.level} из ${selected.domikType.maxLevel}`}>
                            <span className="panel-level-value">ур. {selected.domik.level}</span>
                            <span className="panel-notches" aria-hidden="true">
                                {Array.from({ length: selected.domikType.maxLevel }, (_, index) =>
                                    <span key={index} className={'panel-notch' + (index < selected.domik.level ? ' panel-notch--cut' : '')} />)}
                            </span>
                            {crestWeather != null && <WeatherMark key={crestWeather.weatherLogicName} mark={crestWeather} full />}
                        </div>
                        <div className="panel-status">
                            {maxManufactures > 0 &&
                                <span className={'panel-status-item' + (atManufactureCap ? ' panel-status-item--full' : '')}
                                    title="Места для одновременных смен">
                                    <AbstractSprite logicName="production_recipe" size={24} className="panel-status-ico" aria-hidden="true" />
                                    {slotsText}
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
                        </div>
                        <PanelTabs active={activeView} onSelect={setTab} workPip={idlePip} growPip={growPip}
                            available={{ work: hasWork, grow: hasGrow }} />
                    </div>
                    <div className="panel-view" id="panel-view" role="tabpanel">
                    {!hasWork && !hasGrow &&
                        <p className="hint panel-view-empty">Постройка выросла до предела, и работы для неё нет.</p>
                    }
                    {activeView === 'grow' && selected.upgrade != null &&
                        <div className="panel-block">
                            <div className="upgrade-row">
                                <span className="panel-label">Улучшение до ур. {selected.upgrade.nextLevel}</span>
                                <ResourcesBox resources={selected.upgrade.resources} resourceTypes={resourceTypes} have={resources} />
                            </div>
                            {upgradeBenefits != null &&
                                <div className="upgrade-benefits">
                                    <div className="upgrade-benefits-chips">
                                        {upgradeBenefits.plodderGain > 0 &&
                                            <StatChip icon={<img className="stat-chip-ico" src="/images/modificatorTypes/plodder.png" alt="" />} title="Вместимость трудяг">
                                                +{upgradeBenefits.plodderGain} {pluralRu(upgradeBenefits.plodderGain, 'трудяга', 'трудяги', 'трудяг')}
                                            </StatChip>}
                                        {upgradeBenefits.manufactureDelta > 0 &&
                                            <StatChip icon={<AbstractSprite logicName="production_recipe" size={24} className="stat-chip-ico" aria-hidden="true" />} title="Одновременные производства">
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
                                    {villageLevel != null && upgradeBenefits.plodderDelta > 0 && upgradeBenefits.plodderGain === 0 &&
                                        <p className="hint upgrade-benefits-cap">
                                            Трудяг не прибавится – в артели уже {villageLevel.residentsCap}. Уровень идёт в обжитость.
                                        </p>
                                    }
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
                            <ProgressBar value={progressPercent(selected.domik.finishDate, selected.domik.upgradeSeconds ?? 0, now)} max={100} label={selected.remainingText ?? ''} />
                            <HurryButton finishDate={selected.domik.finishDate} now={now} goldValue={goldValue} goldType={goldType}
                                remainingText={selected.remainingText ?? ''} onHurry={() => { onHurryDomik(selected.domik.id); }} />
                        </div>
                    }
                    {activeView === 'work' && selected.receipts.length > 0 &&
                        <div className="panel-block">
                            <span className="panel-label">Запустить производство</span>
                            <div className="receipt-list">
                                {readyReceipts.map(receipt => renderReceipt(receipt, selected))}
                                {blockedReceipts.length > 0 &&
                                    <p className="receipt-divider">пока не берутся</p>
                                }
                                {blockedReceipts.map(receipt => renderReceipt(receipt, selected))}
                            </div>
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
                                        onToggleAutoRepeat={onToggleManufactureRepeat}
                                        resourceTypes={resourceTypes} measureUnlocked={elderHouseLevel >= MEASURE_MIN_LEVEL}
                                        keyResourceTypeIds={keyResourceTypes}
                                        onSetMeasure={onSetManufactureMeasure} />
                                );
                            })}
                        </div>
                    }
                    </div>
                </div>
            }
        </aside>
    );
};

import { useRef, useState } from 'react';
import type { ReactNode } from 'react';
import ArrowUpIcon from 'pixelarticons/svg/arrow-up.svg?react';
import ChartIcon from 'pixelarticons/svg/chart.svg?react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import ChevronRightIcon from 'pixelarticons/svg/chevron-right.svg?react';
import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import RepeatIcon from 'pixelarticons/svg/repeat.svg?react';
import WarningIcon from 'pixelarticons/svg/warning-diamond.svg?react';
import type { LedgerDto, ResourceDto, ResourceReserveDto, ResourceTypeDto } from '../types/api';
import type { HudBlockedBuilding, HudBuildingRef, HudDigest } from '../utils/hud';
import { formatTimeOfDay } from '../utils/time';
import { pluralRu } from '../utils/plural';
import { SectionHero } from './SectionHero';
import { ActionButton } from './ActionButton';
import { ResourceChip } from './ResourceChip';
import { ResourceNameChip } from './ResourceNameChip';
import { AbstractSprite, DomikSprite, MechanicSprite, NeighborSprite } from './sprites';
import '../styles/household.css';

const CHIP_LIMIT = 5;
const RESERVE_MIN_LEVEL = 3;
const BLOCKED_LIMIT = 3;

interface HouseholdBoxProps {
    digest: HudDigest;
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    reserves: ResourceReserveDto[];
    intentReserves: ResourceReserveDto[];
    ledger: LedgerDto | null;
    now: number;
    onSetReserve: (resourceTypeId: number, reserve: number) => void;
    onSelectDomik: (domikId: number, logicName: string) => void;
    onOpenTab: (tabKey: string) => void;
    onToggleRepeat: (manufactureId: number, next: boolean) => void;
}

interface LedgerBlockProps {
    ledger: LedgerDto;
    resourceTypes: ResourceTypeDto[];
}

const LedgerBlock = ({ ledger, resourceTypes }: LedgerBlockProps) => {
    const resourceName = (typeId: number) => resourceTypes.find(type => type.id === typeId)?.name ?? 'припас';
    const netFlows = ledger.flows
        .map(flow => ({ typeId: flow.resourceTypeId, net: flow.gained - flow.spent }))
        .filter(flow => flow.net !== 0)
        .sort((a, b) => Math.abs(b.net) - Math.abs(a.net));
    const shortage = ledger.shortage;

    return (
        <div className="household-block">
            <span className="panel-label">Счётная книга</span>
            <div className="household-rows">
                <div className="household-row">
                    <ChartIcon className="household-row-ico" aria-hidden="true" />
                    <span className="household-row-text">
                        <span className="household-ledger-label">За сутки</span>
                        {netFlows.length > 0 &&
                            <span className="household-ledger-flows">
                                {netFlows.map(flow => (
                                    <span key={flow.typeId} className="household-ledger-flow" data-sign={flow.net > 0 ? 'gain' : 'spend'}>
                                        {resourceName(flow.typeId).toLocaleLowerCase('ru')} {flow.net > 0 ? '+' : '−'}{Math.abs(flow.net)}
                                    </span>
                                ))}
                            </span>
                        }
                        {netFlows.length === 0 &&
                            <span className="household-ledger-empty">
                                {ledger.hasEntries || ledger.idlePercent === 100 || ledger.idlePercent == null
                                    ? 'За сутки ни прихода, ни расхода – двор стоял.'
                                    : 'Книга только заведена – староста считает с этого часа.'}
                            </span>
                        }
                    </span>
                </div>

                <div className="household-row">
                    <WarningIcon className="household-row-ico" aria-hidden="true" />
                    <span className="household-row-text">
                        <span className="household-ledger-label">Чего не хватит первым</span>
                        <span className={shortage == null ? 'household-ledger-empty' : 'household-ledger-value'}>
                            {shortage == null
                                ? 'Ничего не убывает – припасов хватает на всё, что стоит нарядом.'
                                : shortage.hours <= 0
                                    ? `${resourceName(shortage.resourceTypeId)}: уже на исходе`
                                    : `${resourceName(shortage.resourceTypeId)}: хватит на ${shortage.hours} ч при нынешнем расходе`}
                        </span>
                    </span>
                </div>

                {ledger.idlePercent != null &&
                    <div className="household-row">
                        <ClockIcon className="household-row-ico" aria-hidden="true" />
                        <span className="household-row-text">
                            <span className="household-ledger-label">Двор простоял</span>
                            <span className="household-ledger-value">{ledger.idlePercent} % суток</span>
                        </span>
                    </div>
                }
            </div>
        </div>
    );
};

interface BuildingChipsProps {
    icon: ReactNode;
    label: string;
    buildings: HudBuildingRef[];
    onSelectDomik: (domikId: number, logicName: string) => void;
}

const BuildingChips = ({ icon, label, buildings, onSelectDomik }: BuildingChipsProps) => {
    const [expanded, setExpanded] = useState(false);
    const hiddenCount = buildings.length - CHIP_LIMIT;
    const visible = expanded || hiddenCount <= 0 ? buildings : buildings.slice(0, CHIP_LIMIT);

    return (
        <div className="household-row household-row-group" data-expanded={expanded ? 'true' : 'false'}>
            {icon}
            <span className="household-row-label">
                {label}
                <span className="household-row-count">{buildings.length}</span>
            </span>
            <div className="household-chips">
                {visible.map(building => (
                    <button key={building.domikId} type="button" className="household-building-btn"
                        onClick={() => onSelectDomik(building.domikId, building.logicName)}>
                        <DomikSprite logicName={building.logicName} className="household-building-ico" aria-hidden="true" />
                        <span className="household-building-name" title={building.displayName}>{building.displayName}</span>
                    </button>
                ))}
                {hiddenCount > 0 &&
                    <button type="button" className="household-building-btn household-more-btn"
                        aria-expanded={expanded}
                        onClick={() => { setExpanded(value => !value); }}>
                        {expanded
                            ? <>свернуть <ChevronUpIcon aria-hidden="true" /></>
                            : <>ещё {hiddenCount} <ChevronDownIcon aria-hidden="true" /></>}
                    </button>
                }
            </div>
        </div>
    );
};

interface BlockedRowProps {
    building: HudBlockedBuilding;
    resourceTypes: ResourceTypeDto[];
    onSelectDomik: (domikId: number, logicName: string) => void;
}

const BlockedRow = ({ building, resourceTypes, onSelectDomik }: BlockedRowProps) => {
    const missingNames = building.missing
        .map(item => resourceTypes.find(type => type.id === item.typeId)?.name)
        .filter((name): name is string => name != null)
        .join(', ');

    return (
        <button type="button" className="household-row household-row-button"
            aria-label={`${building.displayName} стоит: не хватает ${missingNames}`}
            onClick={() => onSelectDomik(building.domikId, building.logicName)}>
            <DomikSprite logicName={building.logicName} className="household-row-ico" aria-hidden="true" />
            <span className="household-row-text">{building.displayName} стоит: <span className="household-row-lack">не хватает</span></span>
            <span className="household-row-chips">
                {building.missing.map(item => {
                    const resourceType = resourceTypes.find(type => type.id === item.typeId);
                    return resourceType == null ? null : <ResourceChip key={item.typeId} resourceType={resourceType} value={item.value} />;
                })}
            </span>
            <ChevronRightIcon className="household-row-go" aria-hidden="true" />
        </button>
    );
};

interface ReserveBlockProps {
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    reserves: ResourceReserveDto[];
    intentReserves: ResourceReserveDto[];
    shiftInputTypeIds: number[];
    onSetReserve: (resourceTypeId: number, reserve: number) => void;
}

interface ReserveRowProps {
    resourceType: ResourceTypeDto;
    stock: number;
    reserve: number;
    onSetReserve: (resourceTypeId: number, reserve: number) => void;
}

const ReserveRow = ({ resourceType, stock, reserve, onSetReserve }: ReserveRowProps) => {
    const [input, setInput] = useState(String(reserve));
    const savedRef = useRef(reserve);

    const commit = () => {
        const parsed = Math.trunc(Number(input));
        const next = Number.isFinite(parsed) ? Math.max(0, parsed) : 0;
        setInput(String(next));
        if (next === savedRef.current) {
            return;
        }

        savedRef.current = next;
        onSetReserve(resourceType.id, next);
    };

    return (
        <div className="household-row household-reserve-row">
            <ResourceNameChip resourceType={resourceType} />
            <span className="household-reserve-stock">{stock} на складе</span>
            <label className="household-reserve-field">
                беречь
                <input type="number" min={0} value={input}
                    onChange={event => setInput(event.target.value)}
                    onBlur={commit}
                    onKeyDown={event => { if (event.key === 'Enter') { event.currentTarget.blur(); } }} />
            </label>
            <span className="household-reserve-hint">
                {reserve > 0
                    ? `Наряды остановятся, когда на складе останется ${reserve}.`
                    : 'Не бережём – наряды берут всё, что нужно.'}
            </span>
        </div>
    );
};

const ReserveBlock = ({ resourceTypes, resources, reserves, intentReserves, shiftInputTypeIds, onSetReserve }: ReserveBlockProps) => {
    const rowTypeIds = [...new Set([...reserves.map(item => item.resourceTypeId), ...shiftInputTypeIds])];
    const rows = rowTypeIds
        .flatMap(typeId => {
            const resourceType = resourceTypes.find(type => type.id === typeId);
            return resourceType == null ? [] : [resourceType];
        })
        .sort((a, b) => a.name.localeCompare(b.name, 'ru'));

    return (
        <div className="household-block">
            <span className="panel-label">Заповедный припас</span>
            <p className="household-reserve-lead">Заповеданное наряды не тронут – запас под заказ и толоку остаётся цел.</p>
            {intentReserves.length > 0 &&
                <p className="household-reserve-intent">
                    Под задумку заповедано: {intentReserves
                        .map(item => `${resourceTypes.find(type => type.id === item.resourceTypeId)?.name ?? `ресурс #${item.resourceTypeId}`} ×${item.reserve}`)
                        .join(' · ')}. Наряды это не тронут; заказ, ярмарка и ручная смена берут со склада всё.
                </p>
            }
            {rows.length === 0 && intentReserves.length === 0 &&
                <p className="household-empty">Ничего не заповедано – наряды берут со склада всё, что нужно.</p>
            }
            {rows.length > 0 &&
                <div className="household-rows">
                    {rows.map(resourceType => (
                        <ReserveRow key={resourceType.id} resourceType={resourceType}
                            stock={resources.find(item => item.typeId === resourceType.id)?.value ?? 0}
                            reserve={reserves.find(item => item.resourceTypeId === resourceType.id)?.reserve ?? 0}
                            onSetReserve={onSetReserve} />
                    ))}
                </div>
            }
        </div>
    );
};

export const HouseholdBox = ({ digest, resourceTypes, resources, reserves, intentReserves, ledger, now, onSetReserve, onSelectDomik, onOpenTab, onToggleRepeat }: HouseholdBoxProps) => {
    const [blockedExpanded, setBlockedExpanded] = useState(false);
    const measureType = (typeId: number) => resourceTypes.find(type => type.id === typeId);
    const handsShort = digest.workersFree === 0 && digest.idleBuildings.length > 0;
    const hasNowRows = digest.idleBuildings.length > 0
        || digest.runningShifts > 0
        || digest.blockedBuildings.length > 0
        || digest.soonestOrder != null
        || digest.expeditionsBack > 0
        || digest.workersResting > 0
        || digest.workersSick > 0
        || digest.upgradeableBuildings.length > 0;

    const handsNeeded = (handsShort ? 0 : digest.idleBuildings.length) + digest.blockedBuildings.length + digest.upgradeableBuildings.length;
    const blockedOverflow = digest.blockedBuildings.length - BLOCKED_LIMIT;
    const visibleBlocked = blockedExpanded || blockedOverflow <= 0
        ? digest.blockedBuildings
        : digest.blockedBuildings.slice(0, BLOCKED_LIMIT);

    return (
        <section className="household-panel pixel-panel">
            <SectionHero className="household-hero">
                <span className="household-hero-emblem sec-hero-emblem" aria-hidden="true"><AbstractSprite logicName="household" size={40} /></span>
                <div className="household-hero-text">
                    <h3 className="panel-title household-hero-title">Хозяйство</h3>
                    <p className="household-hero-sub sec-hero-sub">Здесь видно, что в деревне просит рук – а дела правятся на дворе.</p>
                </div>
                <div className="household-hero-stat sec-hero-stat" data-calm={handsNeeded === 0 ? 'true' : 'false'} title="Построек, что просят рук">
                    <span className="household-hero-stat-num sec-hero-stat-num">{handsNeeded}</span>
                    <span className="household-hero-stat-label sec-hero-stat-label">дел на дворе</span>
                </div>
            </SectionHero>

            <div className="household-block">
                <span className="panel-label">Сейчас</span>
                {!hasNowRows &&
                    <p className="household-empty">В деревне всё при деле – староста доволен.</p>
                }
                {hasNowRows &&
                    <div className="household-rows">
                        {handsShort &&
                            <div className="household-row">
                                <AbstractSprite logicName="smart_artel" size={24} className="household-row-ico" aria-hidden="true" />
                                <span className="household-row-text">
                                    Свободных рук нет{digest.handsFreeEarliest != null
                                        ? <>: первый трудяга освободится в {formatTimeOfDay(digest.handsFreeEarliest, now)}</>
                                        : digest.workersAway > 0 && <>: {digest.workersAway} {pluralRu(digest.workersAway, 'трудяга', 'трудяги', 'трудяг')} в отходе – не хватает коек</>}
                                </span>
                            </div>
                        }

                        {!handsShort && digest.idleBuildings.length > 0 &&
                            <BuildingChips icon={<HomeIcon className="household-row-ico" aria-hidden="true" />} label="В простое"
                                buildings={digest.idleBuildings} onSelectDomik={onSelectDomik} />
                        }

                        {visibleBlocked.map(building => (
                            <BlockedRow key={building.domikId} building={building}
                                resourceTypes={resourceTypes} onSelectDomik={onSelectDomik} />
                        ))}

                        {blockedOverflow > 0 &&
                            <button type="button" className="household-row household-more-btn household-more-row"
                                aria-expanded={blockedExpanded}
                                onClick={() => { setBlockedExpanded(value => !value); }}>
                                {blockedExpanded
                                    ? <>свернуть <ChevronUpIcon aria-hidden="true" /></>
                                    : <>ещё {blockedOverflow} {pluralRu(blockedOverflow, 'постройка', 'постройки', 'построек')} {pluralRu(blockedOverflow, 'стоит', 'стоят', 'стоят')} без припасов <ChevronDownIcon aria-hidden="true" /></>}
                            </button>
                        }

                        {digest.soonestOrder != null &&
                            <button type="button" className="household-row household-row-button"
                                onClick={() => onOpenTab('orders')}>
                                <NeighborSprite logicName={digest.soonestOrder.neighborLogicName} size={24} className="household-row-ico" aria-hidden="true" />
                                <span className="household-row-text">
                                    Заказ {digest.soonestOrder.neighborName} истекает {digest.soonestOrder.hours <= 1
                                        ? <span className="household-row-urgent">через {digest.soonestOrder.hours} ч</span>
                                        : `через ${digest.soonestOrder.hours} ч`}
                                </span>
                                <ChevronRightIcon className="household-row-go" aria-hidden="true" />
                            </button>
                        }

                        {digest.expeditionsBack > 0 &&
                            <button type="button" className="household-row household-row-button"
                                onClick={() => onOpenTab('expeditions')}>
                                <MechanicSprite logicName="expeditions" size={24} className="household-row-ico" aria-hidden="true" />
                                <span className="household-row-text">Поход вернулся: добыча не разобрана</span>
                                <ChevronRightIcon className="household-row-go" aria-hidden="true" />
                            </button>
                        }

                        {digest.runningShifts > 0 &&
                            <div className="household-row">
                                <AbstractSprite logicName="production_recipe" size={24} className="household-row-ico" aria-hidden="true" />
                                <span className="household-row-text">
                                    В работе {digest.runningShifts} {pluralRu(digest.runningShifts, 'смена', 'смены', 'смен')}
                                    {digest.runningEarliest != null && <>, ближайшая поспеет в {formatTimeOfDay(digest.runningEarliest, now)}</>}
                                </span>
                            </div>
                        }

                        {digest.workersResting > 0 && digest.restingEarliest != null &&
                            <div className="household-row">
                                <AbstractSprite logicName="fatigue_rest" size={24} className="household-row-ico" aria-hidden="true" />
                                <span className="household-row-text">
                                    {pluralRu(digest.workersResting, 'Отдыхает', 'Отдыхают', 'Отдыхают')} {digest.workersResting} {pluralRu(digest.workersResting, 'трудяга', 'трудяги', 'трудяг')}, первый встанет в {formatTimeOfDay(digest.restingEarliest, now)}
                                </span>
                            </div>
                        }

                        {digest.workersSick > 0 && digest.sickEarliest != null &&
                            <div className="household-row">
                                <AbstractSprite logicName="fatigue_rest" size={24} className="household-row-ico" aria-hidden="true" />
                                <span className="household-row-text">
                                    {pluralRu(digest.workersSick, 'Хворает', 'Хворают', 'Хворают')} {digest.workersSick} {pluralRu(digest.workersSick, 'трудяга', 'трудяги', 'трудяг')}, первый поправится в {formatTimeOfDay(digest.sickEarliest, now)}
                                </span>
                            </div>
                        }

                        {digest.upgradeableBuildings.length > 0 &&
                            <BuildingChips icon={<ArrowUpIcon className="household-row-ico" aria-hidden="true" />} label="Готовы к улучшению"
                                buildings={digest.upgradeableBuildings} onSelectDomik={onSelectDomik} />
                        }
                    </div>
                }
            </div>

            <div className="household-block">
                <span className="panel-label">Наряды</span>
                {digest.standingShifts.length === 0 &&
                    <p className="household-empty">Нарядов не поставлено – каждая смена запускается вручную.</p>
                }
                {digest.standingShifts.length > 0 &&
                    <div className="household-rows">
                        {digest.standingShifts.map(shift => (
                            <div key={shift.manufactureId} className="household-row household-shift-row">
                                <button type="button" className="household-shift-link"
                                    onClick={() => onSelectDomik(shift.domikId, shift.domikLogicName)}>
                                    <RepeatIcon className="household-row-ico" aria-hidden="true" />
                                    <span className="household-row-text">
                                        Наряд: {shift.receiptName} · {shift.domikName}
                                        {(() => {
                                            const measure = shift.measure;
                                            const resourceType = measure == null ? undefined : measureType(measure.resourceTypeId);
                                            return measure == null || resourceType == null
                                                ? null
                                                : <> · мера: <ResourceChip resourceType={resourceType} value={measure.value} /></>;
                                        })()}
                                        {' '}· снова в {formatTimeOfDay(shift.finishDate, now)}
                                        {shift.starving && <span className="household-shift-starving">Припасов на следующий круг нет – и никто их сейчас не делает.</span>}
                                    </span>
                                    <ChevronRightIcon className="household-row-go" aria-hidden="true" />
                                </button>
                                <ActionButton className="btn-game btn-ghost household-shift-action"
                                    onClick={() => onToggleRepeat(shift.manufactureId, false)}>
                                    Снять наряд
                                </ActionButton>
                            </div>
                        ))}
                    </div>
                }
            </div>

            {ledger != null && <LedgerBlock ledger={ledger} resourceTypes={resourceTypes} />}

            {ledger != null && ledger.level >= RESERVE_MIN_LEVEL &&
                <ReserveBlock resourceTypes={resourceTypes} resources={resources} reserves={reserves} intentReserves={intentReserves}
                    shiftInputTypeIds={digest.standingShifts.flatMap(shift => shift.inputTypeIds)}
                    onSetReserve={onSetReserve} />
            }
        </section>
    );
};

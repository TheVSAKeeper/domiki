import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { CSSProperties, FocusEvent, Ref } from 'react';
import { createPortal } from 'react-dom';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import type { CloakStateDto, DomikDto, DomikIncidentDto, DomikTypeDto, ErrandDto, ExpeditionStateDto, FoodRuleDto, IncidentDto, ReceiptDto, ResourceDto, ResourceTypeDto, SickTypeDto, TavernLarderDto, VillageLevelDto, WorkerDto } from '../types/api';
import { buildDomikNamer, type DomikNamer } from '../utils/domikNames';
import { formatDuration, formatDurationShort, formatTimeOfDay, remainingSeconds } from '../utils/time';
import { describeWorker, describeWorkerParts, isSkilledWorker, rankedSkills } from '../utils/worker';
import { AbstractSprite, DomikSprite, MechanicSprite, ResourceSprite, TraitSprite, WorkerSprite } from './sprites';
import { genderForm, traitLabel } from '../utils/gender';
import { pluralRu } from '../utils/plural';
import { flyoutLeft, flyoutWidth, useFlyoutTop } from '../utils/flyout';

type WorkerState = 'expedition' | 'errand' | 'incidentMissing' | 'incidentSearch' | 'domikIncidentSearch' | 'busy' | 'resting' | 'away' | 'free';

interface WorkersBoxProps {
    workers: WorkerDto[];
    domikTypes: DomikTypeDto[];
    domiks: DomikDto[];
    receipts: ReceiptDto[];
    expeditions: ExpeditionStateDto | null;
    errand: ErrandDto | null;
    incident: IncidentDto | null;
    domikIncident: DomikIncidentDto | null;
    cloaks: CloakStateDto;
    sickTypes: SickTypeDto[];
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    villageLevel: VillageLevelDto | null;
    tavernLevel: number;
    larder: TavernLarderDto | null;
    onSetFoodRule: (resourceTypeId: number, reserve: number, forbidden: boolean) => void;
    now: number;
}

const stateLabels: Record<WorkerState, string> = { expedition: 'В экспедиции', errand: 'В поручении', incidentMissing: 'Задержался', incidentSearch: 'В поисках', domikIncidentSearch: 'Разбирается', busy: 'Работает', resting: 'Отдыхает', away: 'В отходе', free: 'Свободен' };
const tallyLabels: Record<WorkerState, string> = { expedition: 'в пути', errand: 'в поручении', incidentMissing: 'задержались', incidentSearch: 'в поисках', domikIncidentSearch: 'разбираются', busy: 'за работой', resting: 'отдыхают', away: 'в отходе', free: 'свободны' };
const tallyOrder: WorkerState[] = ['free', 'busy', 'resting', 'away', 'incidentMissing', 'incidentSearch', 'domikIncidentSearch', 'errand', 'expedition'];
const AWAY_TITLE = 'Койки заняты – трудяга ждёт своей и работы не берёт';
const FATIGUE_THRESHOLD_SECONDS = 28800;

const useShownPortraits = () => {
    const [shown, setShown] = useState<ReadonlySet<number>>(() => new Set());
    const observer = useMemo(() => {
        if (typeof IntersectionObserver === 'undefined') {
            return null;
        }

        const instance = new IntersectionObserver(entries => {
            const ids = entries
                .filter(entry => entry.isIntersecting)
                .map(entry => {
                    instance.unobserve(entry.target);
                    return Number((entry.target as HTMLElement).dataset.workerId);
                });
            if (ids.length > 0) {
                setShown(prev => new Set([...prev, ...ids]));
            }
        }, { rootMargin: '300px' });
        return instance;
    }, []);

    useEffect(() => () => { observer?.disconnect(); }, [observer]);
    const observePortrait = useCallback((node: HTMLSpanElement | null) => {
        if (node != null) {
            observer?.observe(node);
        }
    }, [observer]);

    return { shownPortraits: observer == null ? null : shown, observePortrait };
};

const WorkerDetails = ({ worker, domikTypes, domiks, namer, style, ref }: { worker: WorkerDto; domikTypes: DomikTypeDto[]; domiks: DomikDto[]; namer: DomikNamer; style: CSSProperties; ref?: Ref<HTMLDivElement> }) => {
    const effect = worker.traitDurationPercent === 0 ? '' : ` ${worker.traitDurationPercent} %`;
    const visibleSkills = worker.skills.filter(skill => skill.bonusPercent > 0);
    const workplaceDomik = worker.manufactureId == null
        ? null
        : domiks.find(d => (d.manufactures ?? []).some(m => m.id === worker.manufactureId));
    const workplaceType = workplaceDomik == null ? null : domikTypes.find(t => t.id === workplaceDomik.typeId) ?? null;
    return (
        <div ref={ref} className="worker-details" style={style}>
            {workplaceDomik != null && workplaceType != null &&
                <span className="worker-workplace worker-detail-workplace">
                    <DomikSprite logicName={workplaceType.logicName} className="worker-workplace-ico" aria-hidden="true" />
                    {namer(workplaceType.id, workplaceDomik.id, workplaceType.name, workplaceType.logicName)}
                </span>
            }
            <span className="worker-trait">
                <TraitSprite logicName={worker.traitLogicName} size={24} className="worker-trait-ico" aria-hidden="true" />
                {traitLabel(worker.traitLogicName, worker.traitName, worker.gender)}{effect}
            </span>
            <span className="worker-desc">{describeWorker(worker, domikTypes)}</span>
            {(worker.noFatigue || visibleSkills.length > 0) &&
                <div className="worker-skills">
                    {worker.noFatigue && <span className="worker-flag"><AbstractSprite logicName="fatigue_rest" size={24} className="worker-flag-ico" aria-hidden="true" />не устаёт</span>}
                    {visibleSkills.length > 0 && <AbstractSprite logicName="worker_skill" size={24} className="worker-skill-label" aria-hidden="true" />}
                    {visibleSkills.map(skill => {
                        const domikType = domikTypes.find(x => x.id === skill.domikTypeId);
                        if (domikType == null) {
                            return null;
                        }

                        return (
                            <span
                                key={skill.domikTypeId}
                                className="worker-skill"
                                title={`${domikType.name}: +${skill.bonusPercent} % · ${skill.uses} завершённых работ`}
                            >
                                <DomikSprite logicName={domikType.logicName} className="worker-skill-ico" aria-hidden="true" />
                                +{skill.bonusPercent} %
                            </span>
                        );
                    })}
                </div>
            }
        </div>
    );
};

interface LarderRuleRowProps {
    resourceType: ResourceTypeDto;
    stock: number;
    rule: FoodRuleDto | undefined;
    onSetFoodRule: (resourceTypeId: number, reserve: number, forbidden: boolean) => void;
}

const LarderRuleRow = ({ resourceType, stock, rule, onSetFoodRule }: LarderRuleRowProps) => {
    const reserve = rule?.reserve ?? 0;
    const forbidden = rule?.forbidden ?? false;
    const [reserveInput, setReserveInput] = useState(String(reserve));
    const [forbiddenInput, setForbiddenInput] = useState(forbidden);
    const forbiddenCheckboxRef = useRef<HTMLInputElement>(null);
    const savedRef = useRef({ reserve, forbidden });

    const parseReserveInput = () => {
        const parsed = Math.trunc(Number(reserveInput));
        return Number.isFinite(parsed) ? Math.max(0, parsed) : 0;
    };

    const commit = (nextForbidden: boolean) => {
        const next = parseReserveInput();
        setReserveInput(String(next));
        const saved = savedRef.current;
        if (next === saved.reserve && nextForbidden === saved.forbidden) {
            return;
        }

        savedRef.current = { reserve: next, forbidden: nextForbidden };
        onSetFoodRule(resourceType.id, next, nextForbidden);
    };

    const commitReserve = (event: FocusEvent<HTMLInputElement>) => {
        if (event.relatedTarget === forbiddenCheckboxRef.current) {
            setReserveInput(String(parseReserveInput()));
            return;
        }

        commit(savedRef.current.forbidden);
    };

    const commitForbidden = (nextForbidden: boolean) => {
        setForbiddenInput(nextForbidden);
        commit(nextForbidden);
    };

    return (
        <div className="larder-row">
            <span className="larder-row-name">
                <ResourceSprite logicName={resourceType.logicName} size={24} className="larder-row-ico" aria-hidden="true" />
                {resourceType.name}
                <span className="larder-row-stock">{stock}</span>
            </span>
            <label className="larder-row-reserve" title="Корчмарь берёт только то, что сверх этого запаса – ниже не тронет">
                оставлять
                <input type="number" min={0} value={reserveInput}
                    onChange={event => setReserveInput(event.target.value)}
                    onBlur={commitReserve}
                    onKeyDown={event => { if (event.key === 'Enter') { event.currentTarget.blur(); } }} />
            </label>
            <label className="receipt-optional larder-row-forbidden" title="Корчмарь обойдёт этот припас стороной – ни к обеду, ни в котомки">
                <input ref={forbiddenCheckboxRef} type="checkbox" checked={forbiddenInput}
                    onChange={event => commitForbidden(event.target.checked)}
                    onBlur={() => commit(savedRef.current.forbidden)} />
                не подавать
            </label>
        </div>
    );
};

export const WorkersBox = ({ workers, domikTypes, domiks, receipts, expeditions, errand, incident, domikIncident, cloaks, sickTypes, resourceTypes, resources, villageLevel, tavernLevel, larder, onSetFoodRule, now }: WorkersBoxProps) => {
    const [hover, setHover] = useState<{ worker: WorkerDto; rect: DOMRect } | null>(null);
    const [detailsRef, detailsTop, detailsHidden] = useFlyoutTop<HTMLDivElement>(hover?.rect ?? null);
    const [larderOpen, setLarderOpen] = useState(false);
    const { shownPortraits, observePortrait } = useShownPortraits();
    const clearHover = (id: number) => setHover(prev => (prev?.worker.id === id ? null : prev));
    const namer = useMemo(() => buildDomikNamer(domiks), [domiks]);
    const freeCloaks = Math.max(0, cloaks.stock - cloaks.outOnShifts);
    const hasCloaks = cloaks.stock > 0 || cloaks.outOnShifts > 0 || cloaks.wearPoints > 0;
    const foodStocks = resourceTypes
        .filter(resourceType => resourceType.isFood)
        .map(resourceType => ({
            name: resourceType.name.toLocaleLowerCase('ru-RU'),
            value: resources.find(resource => resource.typeId === resourceType.id)?.value ?? 0,
        }));
    const hasFood = foodStocks.some(food => food.value > 0);
    const filledCap = villageLevel != null && villageLevel.residents >= villageLevel.residentsCap ? villageLevel.residentsCap : null;
    const tavernPerks = ['«котёл»', '«котомки в дорогу»', '«тёплый угол»'].slice(0, tavernLevel).join(' · ');

    const foodTypes = resourceTypes.filter(resourceType => resourceType.isFood);
    const ruleFor = (resourceTypeId: number) => larder?.rules.find(rule => rule.resourceTypeId === resourceTypeId);
    const stockFor = (resourceTypeId: number) => resources.find(resource => resource.typeId === resourceTypeId)?.value ?? 0;
    const allFoodForbidden = foodTypes.length > 0 && foodTypes.every(type => ruleFor(type.id)?.forbidden ?? false);
    const anyFoodSpendable = foodTypes.some(type => {
        const rule = ruleFor(type.id);
        return !(rule?.forbidden ?? false) && stockFor(type.id) > (rule?.reserve ?? 0);
    });
    const larderState = !hasFood
        ? 'В кладовой пусто – уставшие отдыхают полный срок'
        : allFoodForbidden
            ? 'Вся еда заповедана – корчмарь не подаёт, уставшие отдыхают полный срок'
            : !anyFoodSpendable
                ? 'Всё, что есть, – заповедное: обеда не будет, пока запас не подрастёт'
                : null;
    const eatenEntries = foodTypes
        .map(type => ({ name: type.name.toLocaleLowerCase('ru-RU'), eaten: ruleFor(type.id)?.eatenToday ?? 0 }))
        .filter(entry => entry.eaten > 0);
    const eatenText = eatenEntries.length === 0
        ? 'За сутки не съедено ни крошки'
        : `Съедено за сутки: ${eatenEntries.map(entry => `${entry.name} ${entry.eaten}`).join(' · ')}`;

    const stateOf = (worker: WorkerDto): WorkerState => {
        if (worker.incidentId != null) {
            if (worker.id === incident?.missingWorkerId) {
                return 'incidentMissing';
            }
            if (incident?.searchWorkerIds.includes(worker.id)) {
                return 'incidentSearch';
            }
            if (domikIncident?.searchWorkerIds.includes(worker.id)) {
                return 'domikIncidentSearch';
            }
            return 'incidentSearch';
        }
        if (worker.expeditionId != null) {
            return 'expedition';
        }
        if (worker.errandId != null) {
            return 'errand';
        }
        if (worker.manufactureId != null) {
            return 'busy';
        }
        if (worker.isAway) {
            return 'away';
        }
        if (worker.restUntil != null && remainingSeconds(worker.restUntil, now) > 0) {
            return 'resting';
        }
        return 'free';
    };

    const tally = workers.reduce<Record<WorkerState, number>>(
        (acc, worker) => { acc[stateOf(worker)] += 1; return acc; },
        { expedition: 0, errand: 0, incidentMissing: 0, incidentSearch: 0, domikIncidentSearch: 0, busy: 0, resting: 0, away: 0, free: 0 },
    );

    return (
        <section className="workers-panel pixel-panel">
            <div className="workers-head">
                <div className="workers-hero">
                    <span className="workers-hero-emblem"><MechanicSprite logicName="workers" size={40} aria-hidden="true" /></span>
                    <div className="workers-hero-text">
                        <h3 className="panel-title workers-hero-title">Трудяги</h3>
                        {workers.length > 0 &&
                            <div className="workers-tally">
                                <span className="workers-tally-total">{workers.length}</span>
                                {tallyOrder.filter(key => tally[key] > 0).map(key => (
                                    <span key={key} className={`workers-tally-item workers-tally--${key}`}>
                                        <i className="workers-tally-dot" aria-hidden="true" />{tally[key]} {tallyLabels[key]}
                                    </span>
                                ))}
                            </div>
                        }
                    </div>
                </div>
                {filledCap != null &&
                    <div className="workers-cap">
                        Артель полна: {filledCap} {pluralRu(filledCap, 'трудяга', 'трудяги', 'трудяг')} – столько двор и кормит. Новые койки пока постоят пустыми.
                    </div>
                }
                {hasCloaks &&
                    <div className="workers-cloaks" title="Плащи сами уходят на смены с погодным бонусом">
                        <b>Плащи:</b> свободно {freeCloaks} · на сменах {cloaks.outOnShifts} · износ {cloaks.wearPoints}/{cloaks.lifetimeShifts}
                    </div>
                }
                {(tavernLevel > 0 || hasFood) &&
                    <div className="workers-larder" title={tavernLevel > 0 ? `Корчма, ступень ${tavernLevel}: ${tavernPerks}` : undefined}>
                        {tavernLevel === 0
                            ? 'Корчмы нет – уставшие трудяги отдыхают полный срок'
                            : <><b>Корчма:</b> обед из запаса – {foodStocks.map(food => `${food.name} ${food.value}`).join(' · ')}</>}
                    </div>
                }
            </div>
            {tavernLevel > 0 &&
                <div className="workers-larder-panel">
                    <div className="workers-larder-panel-head">
                        <MechanicSprite logicName="tavern" size={24} className="workers-larder-panel-ico" aria-hidden="true" />
                        <div className="workers-larder-panel-text">
                            <span className="workers-larder-panel-title">Кладовая</span>
                            <span className="workers-larder-panel-hint">Что беречь от котла: корчмарь берёт сам, дешёвое первым</span>
                        </div>
                        <button type="button" className="workers-larder-toggle" aria-expanded={larderOpen} onClick={() => setLarderOpen(open => !open)}>
                            {larderOpen ? 'Свернуть' : 'Показать'}
                            <ChevronDownIcon className="workers-larder-toggle-caret" aria-hidden="true" />
                        </button>
                    </div>
                    {larderState != null && <p className="workers-larder-state">{larderState}</p>}
                    {larderOpen &&
                        <div className="workers-larder-rows">
                            {foodTypes.map(type => (
                                <LarderRuleRow key={`${type.id}:${ruleFor(type.id)?.reserve ?? 0}`} resourceType={type} stock={stockFor(type.id)} rule={ruleFor(type.id)} onSetFoodRule={onSetFoodRule} />
                            ))}
                        </div>
                    }
                    <p className="workers-larder-counter">{eatenText}</p>
                </div>
            }
            <div className="workers-list">
                {workers.length === 0 &&
                    <span className="hint">Поставьте артельную избу, чтобы поселить трудяг.</span>
                }
                {workers.map(worker => {
                    const restingSeconds = worker.restUntil == null ? 0 : remainingSeconds(worker.restUntil, now);
                    const isSick = worker.sickUntil != null && remainingSeconds(worker.sickUntil, now) > 0;
                    const sickName = isSick ? sickTypes.find(sickType => sickType.id === worker.sickTypeId)?.name ?? 'Хворает' : '';
                    const stateKey = stateOf(worker);
                    const stateLabel = stateKey === 'free'
                        ? genderForm(worker.gender, 'Свободен', 'Свободна')
                        : stateKey === 'resting' && isSick
                            ? sickName
                            : stateLabels[stateKey];
                    const restTitle = worker.restUntil == null
                        ? undefined
                        : `${isSick ? sickName : 'Отдыхает'} до ${new Date(worker.restUntil).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} (${formatDuration(restingSeconds)})`;
                    const timer = (() => {
                        const build = (verb: string, seconds: number) =>
                            seconds > 0 ? { seconds, full: `${verb} через ${formatDuration(seconds)}` } : null;
                        if (stateKey === 'resting') {
                            return build(isSick ? 'поправится' : 'отдохнёт', restingSeconds);
                        }
                        if (stateKey === 'busy') {
                            const manufacture = domiks.flatMap(d => d.manufactures ?? []).find(m => m.id === worker.manufactureId);
                            return manufacture == null ? null : build('освободится', remainingSeconds(manufacture.finishDate, now));
                        }
                        if (stateKey === 'expedition') {
                            const expedition = expeditions?.active.find(e => e.id === worker.expeditionId);
                            return expedition == null ? null : build('вернётся', remainingSeconds(expedition.finishDate, now));
                        }
                        if (stateKey === 'errand') {
                            return errand?.finishDate == null ? null : build('вернётся', remainingSeconds(errand.finishDate, now));
                        }
                        if (stateKey === 'incidentMissing') {
                            return incident == null ? null : build('вернётся', remainingSeconds(incident.searchEndDate ?? incident.autoReturnDate, now));
                        }
                        if (stateKey === 'incidentSearch') {
                            return incident?.searchEndDate == null ? null : build('вернётся', remainingSeconds(incident.searchEndDate, now));
                        }
                        if (stateKey === 'domikIncidentSearch') {
                            return domikIncident?.searchEndDate == null ? null : build('вернётся', remainingSeconds(domikIncident.searchEndDate, now));
                        }
                        return null;
                    })();
                    const workplace = (() => {
                        if (stateKey !== 'busy') {
                            return null;
                        }
                        const domik = domiks.find(d => (d.manufactures ?? []).some(m => m.id === worker.manufactureId));
                        const domikType = domik == null ? null : domikTypes.find(t => t.id === domik.typeId) ?? null;
                        const manufacture = domik?.manufactures?.find(m => m.id === worker.manufactureId) ?? null;
                        const receipt = manufacture == null ? null : receipts.find(r => r.id === manufacture.receiptId) ?? null;
                        if (domik == null || domikType == null || manufacture == null || receipt == null) {
                            return null;
                        }
                        return {
                            logicName: domikType.logicName,
                            name: namer(domikType.id, domik.id, domikType.name, domikType.logicName),
                            level: domik.level,
                            receiptName: receipt.name,
                            finishTime: formatTimeOfDay(manufacture.finishDate, now),
                            autoRepeat: manufacture.autoRepeat,
                        };
                    })();
                    const portraitState = isSick
                        ? 'sick'
                        : stateKey === 'resting'
                            ? 'resting'
                            : stateKey === 'busy' || stateKey === 'expedition' || stateKey === 'errand' || stateKey === 'incidentMissing' || stateKey === 'incidentSearch' || stateKey === 'domikIncidentSearch'
                                ? 'working'
                                : 'idle';
                    const fatigueFraction = Math.min(worker.workedSeconds / FATIGUE_THRESHOLD_SECONDS, 1);
                    const fatigueLevel = fatigueFraction >= 0.8 ? 'high' : fatigueFraction >= 0.5 ? 'mid' : 'low';
                    const craft = describeWorkerParts(worker, domikTypes);
                    const ranked = rankedSkills(worker);
                    const best = ranked[0];
                    const bestType = best == null ? undefined : domikTypes.find(t => t.id === best.domikTypeId);
                    const extra = ranked.slice(1);
                    const extraTitle = extra
                        .flatMap(skill => {
                            const type = domikTypes.find(t => t.id === skill.domikTypeId);
                            return type == null ? [] : [`${type.name}: +${skill.bonusPercent} %`];
                        })
                        .join(' · ');
                    return (
                        <article key={worker.id} className={`worker-card worker--${stateKey}`} tabIndex={0}
                            onMouseEnter={event => setHover({ worker, rect: event.currentTarget.getBoundingClientRect() })}
                            onMouseLeave={() => clearHover(worker.id)}
                            onFocus={event => setHover({ worker, rect: event.currentTarget.getBoundingClientRect() })}
                            onBlur={() => clearHover(worker.id)}>
                            <div className="worker-topline" title={stateKey === 'resting' ? restTitle : stateKey === 'away' ? AWAY_TITLE : undefined}>
                                <span className="worker-badge">
                                    {stateKey === 'resting' && <AbstractSprite logicName="fatigue_rest" size={24} className="worker-badge-ico" aria-hidden="true" />}
                                    {stateLabel}
                                </span>
                                {workplace?.autoRepeat === true &&
                                    <span className="worker-shift-badge" title="Наряд: работа повторяется сама">наряд</span>
                                }
                                {timer != null &&
                                    <span className="worker-timer" title={timer.full}>
                                        <ClockIcon className="worker-timer-ico" aria-hidden="true" />
                                        {formatDurationShort(timer.seconds)}
                                    </span>
                                }
                            </div>
                            <div className="worker-card-body">
                                <span className="worker-portrait" data-worker-id={worker.id} ref={observePortrait}>
                                    {(shownPortraits == null || shownPortraits.has(worker.id)) &&
                                        <WorkerSprite name={worker.name} state={portraitState} skilled={isSkilledWorker(worker)} className="worker-avatar" aria-hidden="true" />
                                    }
                                    {craft.tier === 'master' && <AbstractSprite logicName="worker_mastery" size={24} className="worker-seal" aria-hidden="true" />}
                                    {!worker.noFatigue && worker.workedSeconds > 0 &&
                                        <span className="worker-fatigue" data-level={fatigueLevel}
                                            title={`Усталость: ${formatDuration(worker.workedSeconds)} из ${formatDuration(FATIGUE_THRESHOLD_SECONDS)}`}>
                                            <span className="worker-fatigue-fill" style={{ width: `${String(Math.round(fatigueFraction * 100))}%` }} />
                                        </span>
                                    }
                                </span>
                                <div className="worker-headings">
                                    <span className="worker-name">{worker.name}</span>
                                    <span className={`worker-title worker-title--${craft.tier}`}>{craft.primaryTitle}</span>
                                    <p className="worker-flavor">{craft.flavor}</p>
                                    {(best != null && bestType != null || worker.noFatigue) &&
                                        <div className="worker-card-tags">
                                            {best != null && bestType != null &&
                                                <span className="worker-skill" title={`${bestType.name}: +${best.bonusPercent} % · ${best.uses} завершённых работ`}>
                                                    <DomikSprite logicName={bestType.logicName} className="worker-skill-ico" aria-hidden="true" />
                                                    +{best.bonusPercent} %
                                                </span>
                                            }
                                            {extra.length > 0 &&
                                                <span className="worker-more" title={extraTitle}>ещё {extra.length}</span>
                                            }
                                            {worker.noFatigue &&
                                                <span className="worker-flag"><AbstractSprite logicName="fatigue_rest" size={24} className="worker-flag-ico" aria-hidden="true" />не устаёт</span>
                                            }
                                        </div>
                                    }
                                </div>
                            </div>
                            {workplace != null &&
                                <div className="worker-shift" title={`«${workplace.name}» (ур. ${String(workplace.level)}) · ${workplace.receiptName} · до ${workplace.finishTime}`}>
                                    <DomikSprite logicName={workplace.logicName} className="worker-shift-ico" aria-hidden="true" />
                                    <span className="worker-shift-text">
                                        <span className="worker-shift-name">{workplace.name}<i className="worker-shift-level">ур. {workplace.level}</i></span>
                                        <span className="worker-shift-task">{workplace.receiptName} · до {workplace.finishTime}</span>
                                    </span>
                                </div>
                            }
                        </article>
                    );
                })}
            </div>
            {hover != null && createPortal(
                <WorkerDetails worker={hover.worker} domikTypes={domikTypes} domiks={domiks} namer={namer}
                    ref={detailsRef}
                    style={{
                        position: 'fixed',
                        top: detailsTop,
                        left: flyoutLeft(hover.rect.left, flyoutWidth(Math.max(hover.rect.width, 240))),
                        width: flyoutWidth(Math.max(hover.rect.width, 240)),
                        visibility: detailsHidden ? 'hidden' : undefined,
                    }} />,
                document.body)}
        </section>
    );
};

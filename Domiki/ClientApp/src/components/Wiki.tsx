import { Fragment, useCallback, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { createPortal } from 'react-dom';
import { ApiError, OfflineError, getWikiState } from '../services/api';
import { loadSnapshot, loadWikiFacts, saveWikiFacts } from '../services/offlineSnapshot';
import { useToast } from '../services/toastContext';
import { formatDuration } from '../utils/time';
import { domikLore } from '../utils/domikLore';
import { unlockLore } from '../utils/unlockLore';
import { resourceLore } from '../utils/resourceLore';
import { flyoutLeft, flyoutWidth, useFlyoutTop } from '../utils/flyout';
import { PLODDER_MODIFICATOR_TYPE_ID, weatherEffects } from '../utils/game';
import { profileGenitiveName, profileLore } from '../utils/profileLore';
import { wikiArticleAnchor, wikiBuildingAnchor } from '../utils/wikiLinks';
import { fillFacts } from '../utils/wikiFacts';
import { wikiFactsFallback } from '../utils/wikiFactsFallback';
import { MECHANICS } from '../utils/wikiTexts';
import type { Mechanic } from '../utils/wikiTexts';
import type { ConvoyDto, DecorStateDto, DomikTypeDto, NeighborReputationDto, ReceiptDto, RelocationDto, ResourceDto, ResourceTypeDto, TolokaStateDto, VillageDto, VillageLevelDto, VillageProfileDto, WeatherStateDto, WikiStateDto } from '../types/api';
import { AbstractSprite, DecorSprite, DomikSprite, MechanicSprite, NeighborSprite, ResourceSprite, WeatherSprite } from './sprites';
import { AnimatedDomikSprite } from './AnimatedDomikSprite';
import { ConvoyTally } from './ConvoyTally';
import { PixelLoader } from './PixelLoader';
import { OfflineBanner } from './OfflineBanner';
import ArrowLeftIcon from 'pixelarticons/svg/arrow-left.svg?react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import CheckIcon from 'pixelarticons/svg/check.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import '../styles/wiki.css';

interface Catalog {
    facts: Readonly<Record<string, string>>;
    domikTypes: DomikTypeDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
    weather: WeatherStateDto;
    decor: DecorStateDto;
    villageLevel: VillageLevelDto;
    convoys: ConvoyDto[];
    toloka: TolokaStateDto | null;
    village: VillageDto;
    villageProfiles: VillageProfileDto[];
    reputation: NeighborReputationDto[];
    relocation: RelocationDto;
}

interface CatalogView {
    catalog: Catalog;
    staleSince: number | null;
}

const toCatalog = (state: Omit<WikiStateDto, 'facts'>, facts: Readonly<Record<string, string>>): Catalog => ({
    facts: { ...wikiFactsFallback, ...facts },
    domikTypes: state.domikTypes,
    resourceTypes: state.resourceTypes,
    receipts: state.receipts,
    weather: state.weather,
    decor: state.decor,
    villageLevel: state.villageLevel,
    convoys: state.convoys,
    toloka: state.toloka,
    village: state.village,
    villageProfiles: state.villageProfiles,
    reputation: state.reputation,
    relocation: state.relocation,
});

const revealArticle = (anchorId: string) => {
    const node = document.getElementById(anchorId);
    if (node == null) {
        return;
    }

    node.scrollIntoView({ block: 'start' });
    node.querySelector('button')?.focus({ preventScroll: true });
};

interface WikiArticleProps {
    mechanic: Mechanic;
    facts: Readonly<Record<string, string>>;
}

const WikiArticle = ({ mechanic, facts }: WikiArticleProps) => {
    const paragraphs = mechanic.description.split('\n\n').map(paragraph => fillFacts(paragraph, facts));
    const [lead, ...sections] = paragraphs;

    return (
        <div className="wiki-article">
            <p className="wiki-article-lead">{lead}</p>
            {sections.length > 0 && (
                <div className="wiki-article-grid">
                    {sections.map((paragraph, index) => (
                        <div key={`${mechanic.key}-${index}`} className="wiki-article-card">
                            <h3>{fillFacts(mechanic.sectionTitles?.[index] ?? 'Подробнее', facts)}</h3>
                            <p>{paragraph}</p>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};

const RES_POP_WIDTH = 260;

const METAL_CHAIN: { logicName: string; name: string; where: string }[] = [
    { logicName: 'ore', name: 'Руда', where: 'Рудник' },
    { logicName: 'iron', name: 'Железо', where: 'Кузница' },
    { logicName: 'tool', name: 'Инструмент', where: 'Кузница + доски' },
    { logicName: 'pick', name: 'Кайло', where: 'Кузница, чертёж Каменки' },
    { logicName: 'tongs', name: 'Клещи', where: 'Кузница, чертёж Глинищ' },
];

interface ResChipsProps {
    items: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
}

const ResChips = ({ items, resourceTypes }: ResChipsProps) => (
    <span className="wiki-chips">
        {items.map(res => {
            const type = resourceTypes.find(x => x.id === res.typeId);
            if (type == null) {
                return null;
            }
            return (
                <span key={res.typeId} className="wiki-chip" title={type.name}>
                    <ResourceSprite logicName={type.logicName} aria-hidden="true" />
                    {res.value}
                </span>
            );
        })}
    </span>
);

interface RecipeCardProps {
    receipt: ReceiptDto;
    resourceTypes: ResourceTypeDto[];
}

const RecipeCard = ({ receipt, resourceTypes }: RecipeCardProps) => (
    <div className="wiki-recipe">
        <div className="wiki-recipe-name">{receipt.name}</div>
        <div className="wiki-recipe-flow">
            <ResChips items={receipt.inputResources} resourceTypes={resourceTypes} />
            <span className="wiki-arrow" aria-hidden="true">→</span>
            <ResChips items={receipt.outputResources} resourceTypes={resourceTypes} />
        </div>
        {receipt.optionalInputResources.length > 0 && (
            <div className="wiki-recipe-opt">
                сверх нормы: <ResChips items={receipt.optionalInputResources} resourceTypes={resourceTypes} />
                {receipt.outputBonusPercent > 0 && <span> (+{receipt.outputBonusPercent}% выхода)</span>}
            </div>
        )}
        <div className="wiki-recipe-meta">
            <span>{formatDuration(receipt.durationSeconds)}</span>
            <span>{receipt.plodderCount} трудяг</span>
        </div>
    </div>
);

interface WikiResourcesSectionProps {
    resourceTypes: ResourceTypeDto[];
}

const WikiResourcesSection = ({ resourceTypes }: WikiResourcesSectionProps) => {
    const [resFlyout, setResFlyout] = useState<{ type: ResourceTypeDto; rect: DOMRect } | null>(null);
    const [popRef, popTop, popHidden] = useFlyoutTop<HTMLDivElement>(resFlyout?.rect ?? null);
    const openResFlyout = (type: ResourceTypeDto, el: HTMLElement) => {
        if (resourceLore[type.logicName] == null) {
            return;
        }
        setResFlyout({ type, rect: el.getBoundingClientRect() });
    };
    const closeResFlyout = () => setResFlyout(null);

    return (
        <section className="wiki-section">
            <h2 className="section-head">Ресурсы</h2>
            <p className="wiki-res-hint">Наведи на ресурс – всплывёт карточка: что это, откуда берётся и зачем нужен.</p>
            <div className="wiki-res-grid">
                {resourceTypes.map(type => (
                    <button
                        key={type.id}
                        type="button"
                        className={'wiki-res-cell pixel-panel' + (resFlyout?.type.id === type.id ? ' active' : '')}
                        onMouseEnter={e => { openResFlyout(type, e.currentTarget); }}
                        onMouseLeave={closeResFlyout}
                        onFocus={e => { openResFlyout(type, e.currentTarget); }}
                        onBlur={closeResFlyout}
                    >
                        <ResourceSprite logicName={type.logicName} aria-hidden="true" />
                        <span>{type.name}</span>
                    </button>
                ))}
            </div>
            {resFlyout != null && (() => {
                const lore = resourceLore[resFlyout.type.logicName];
                if (lore == null) {
                    return null;
                }
                return createPortal(
                    <div ref={popRef} className="wiki-res-pop pixel-panel" role="tooltip"
                        style={{
                            top: popTop,
                            left: flyoutLeft(resFlyout.rect.left, flyoutWidth(RES_POP_WIDTH)),
                            width: flyoutWidth(RES_POP_WIDTH),
                            visibility: popHidden ? 'hidden' : undefined,
                        }}>
                        <div className="wiki-res-pop-head">
                            <ResourceSprite logicName={resFlyout.type.logicName} size={40} aria-hidden="true" />
                            <span className="wiki-res-pop-name">{resFlyout.type.name}</span>
                        </div>
                        <p className="wiki-res-pop-flavor">{lore.flavor}</p>
                        <dl className="wiki-res-facts">
                            <dt>Откуда</dt>
                            <dd>{lore.source}</dd>
                            <dt>Зачем</dt>
                            <dd>{lore.use}</dd>
                        </dl>
                    </div>,
                    document.body);
            })()}
        </section>
    );
};

interface WikiBuildingsSectionProps {
    domikTypes: DomikTypeDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
    openLogicName: string | null;
}

const WikiBuildingsSection = ({ domikTypes, resourceTypes, receipts, openLogicName }: WikiBuildingsSectionProps) => {
    const buildingIdByLogicName = (logicName: string | null) => (logicName == null ? undefined : domikTypes.find(type => type.logicName === logicName)?.id);
    const [openIds, setOpenIds] = useState<ReadonlySet<number>>(() => {
        const target = buildingIdByLogicName(openLogicName);
        return target == null ? new Set() : new Set([target]);
    });
    const toggleBuilding = (id: number) => setOpenIds(prev => {
        const next = new Set(prev);
        if (next.has(id)) {
            next.delete(id);
        } else {
            next.add(id);
        }
        return next;
    });
    const receiptById = (id: number) => receipts.find(x => x.id === id);
    const buildings = [...domikTypes].sort((a, b) => a.unlockLevel - b.unlockLevel || a.id - b.id);

    const [appliedLogicName, setAppliedLogicName] = useState<string | null>(openLogicName);
    if (openLogicName !== appliedLogicName) {
        setAppliedLogicName(openLogicName);
        const target = buildingIdByLogicName(openLogicName);
        if (target != null) {
            setOpenIds(prev => new Set(prev).add(target));
        }
    }

    useEffect(() => {
        if (openLogicName != null) {
            revealArticle(wikiBuildingAnchor(openLogicName));
        }
    }, [openLogicName]);

    return (
        <section className="wiki-section">
            <h2 className="section-head">Постройки</h2>
            <div className="wiki-buildings">
                {buildings.map(type => {
                    const open = openIds.has(type.id);
                    const lore = domikLore[type.logicName];
                    const outputTypeIds = new Set<number>();
                    for (const level of type.levels) {
                        for (const receiptId of level.receiptIds) {
                            for (const output of receiptById(receiptId)?.outputResources ?? []) {
                                outputTypeIds.add(output.typeId);
                            }
                        }
                    }

                    return (
                        <div key={type.id} id={wikiBuildingAnchor(type.logicName)} className={'wiki-building pixel-panel' + (open ? ' receipt-open' : '')}>
                            <button type="button" className="wiki-building-head" aria-expanded={open} onClick={() => toggleBuilding(type.id)}>
                                <AnimatedDomikSprite mode="loop" logicName={type.logicName} maxLevel={type.levels.length} active={open} />
                                <span className="wiki-building-titles">
                                    <span className="wiki-building-name">{type.name}</span>
                                    <span className="wiki-building-meta">
                                        до {type.maxLevel} ур. · макс. {type.maxCount} шт.
                                        {type.unlockLevel > 0 && ` · обжитость ${type.unlockLevel}`}
                                        {type.blueprintId != null && ' · по чертежу'}
                                    </span>
                                </span>
                                <span className="wiki-building-aside">
                                    {outputTypeIds.size > 0 && (
                                        <span className="wiki-building-teaser">
                                            {[...outputTypeIds].map(tid => {
                                                const rt = resourceTypes.find(x => x.id === tid);
                                                if (rt == null) {
                                                    return null;
                                                }
                                                return <ResourceSprite key={tid} logicName={rt.logicName} aria-label={rt.name} />;
                                            })}
                                        </span>
                                    )}
                                    <ChevronDownIcon className="receipt-caret" aria-hidden="true" />
                                </span>
                            </button>
                            {open && (
                                <div className="wiki-levels">
                                    {lore != null && <p className="wiki-building-lore">{lore}</p>}
                                    {type.levels.map(level => {
                                        const levelReceipts = level.receiptIds.map(receiptById).filter((r): r is ReceiptDto => r != null);
                                        // TODO: показан только модификатор вместимости – других типов в ModificatorTypes пока нет; завести общий показ, когда появится второй тип
                                        const plodders = level.modificators.find(modificator => modificator.typeId === PLODDER_MODIFICATOR_TYPE_ID)?.value ?? 0;
                                        if (level.resources.length === 0 && levelReceipts.length === 0 && plodders === 0) {
                                            return null;
                                        }
                                        return (
                                            <div key={level.value} className="wiki-level">
                                                <div className="wiki-level-head">
                                                    <span className="wiki-level-badge">Ур. {level.value}</span>
                                                    {level.resources.length > 0 && (
                                                        <span className="wiki-level-cost">{level.value === 1 ? 'постройка' : 'апгрейд'}: <ResChips items={level.resources} resourceTypes={resourceTypes} /></span>
                                                    )}
                                                    {plodders > 0 && <span className="wiki-level-cost">мест для трудяг: {plodders}</span>}
                                                </div>
                                                {levelReceipts.map(receipt => (
                                                    <RecipeCard key={receipt.id} receipt={receipt} resourceTypes={resourceTypes} />
                                                ))}
                                            </div>
                                        );
                                    })}
                                </div>
                            )}
                        </div>
                    );
                })}
            </div>
        </section>
    );
};

interface WikiMechanicsSectionProps {
    facts: Readonly<Record<string, string>>;
    villageLevel: VillageLevelDto;
    weather: WeatherStateDto;
    decor: DecorStateDto;
    domikTypes: DomikTypeDto[];
    convoys: ConvoyDto[];
    toloka: TolokaStateDto | null;
    resourceTypes: ResourceTypeDto[];
    village: VillageDto;
    villageProfiles: VillageProfileDto[];
    reputation: NeighborReputationDto[];
    relocation: RelocationDto;
    openKey: string | null;
}

const WikiMechanicsSection = ({ facts, villageLevel, weather, decor, domikTypes, convoys, toloka, resourceTypes, village, villageProfiles, reputation, relocation, openKey }: WikiMechanicsSectionProps) => {
    const [openMechanics, setOpenMechanics] = useState<ReadonlySet<string>>(() => (
        openKey != null && MECHANICS.some(mechanic => mechanic.key === openKey) ? new Set([openKey]) : new Set()
    ));
    const unlocks = villageLevel.unlocks;
    const unlocked = unlocks.filter(unlock => unlock.unlocked);
    const upcoming = unlocks.filter(unlock => !unlock.unlocked);
    const gateReached = relocation.level >= relocation.threshold;
    const getUnlockDescription = (unlock: typeof unlocks[number]) => {
        if (unlock.logicName == null) {
            return '';
        }

        return unlock.kind === 'building' ? domikLore[unlock.logicName] ?? '' : unlockLore[unlock.logicName] ?? '';
    };
    const getUnlockIcon = (unlock: typeof unlocks[number]) => {
        if (unlock.kind === 'building' && unlock.logicName != null) {
            return <DomikSprite logicName={unlock.logicName} className="unlock-ico" aria-hidden="true" />;
        }

        if (unlock.kind === 'neighbor') {
            return <NeighborSprite logicName={unlock.logicName ?? ''} size={24} className="unlock-ico" aria-hidden="true" />;
        }

        if (unlock.kind === 'feature') {
            return unlock.logicName === 'smart_artel'
                ? <AbstractSprite logicName="smart_artel" size={24} className="unlock-ico" aria-hidden="true" />
                : <HomeIcon className="unlock-ico" aria-hidden="true" />;
        }

        return null;
    };
    const [appliedKey, setAppliedKey] = useState<string | null>(openKey);
    if (openKey !== appliedKey) {
        setAppliedKey(openKey);
        if (openKey != null && MECHANICS.some(mechanic => mechanic.key === openKey)) {
            setOpenMechanics(prev => new Set(prev).add(openKey));
        }
    }

    useEffect(() => {
        if (openKey != null) {
            revealArticle(wikiArticleAnchor(openKey));
        }
    }, [openKey]);
    const toggleMechanic = (key: string) => setOpenMechanics(prev => {
        const next = new Set(prev);
        if (next.has(key)) {
            next.delete(key);
        } else {
            next.add(key);
        }
        return next;
    });

    return (
        <section className="wiki-section">
            <h2 className="section-head">Механики</h2>
            <div className="wiki-buildings">
                {MECHANICS.map(m => {
                    const open = openMechanics.has(m.key);
                    const effectChips = m.key === 'weather' && weather.current != null
                        ? weather.current.effects.flatMap(effect => {
                            if (effect.outputPercent === 100) {
                                return [];
                            }
                            const domikType = domikTypes.find(t => t.id === effect.domikTypeId);
                            if (domikType == null) {
                                return [];
                            }
                            const buff = effect.outputPercent > 100;
                            const delta = effect.outputPercent - 100;
                            return [
                                <span key={effect.domikTypeId} className={'weather-effect' + (buff ? ' weather-effect-buff' : ' weather-effect-nerf')} title={domikType.logicName}>
                                    <DomikSprite className="weather-effect-ico" logicName={domikType.logicName} />
                                    {buff ? '+' : ''}{delta}%
                                </span>,
                            ];
                        })
                        : [];

                    return (
                        <div key={m.key} id={wikiArticleAnchor(m.key)} className={'wiki-building pixel-panel' + (open ? ' receipt-open' : '')}>
                            <button type="button" className="wiki-building-head" aria-expanded={open} onClick={() => toggleMechanic(m.key)}>
                                {m.domikLogic != null
                                    ? <DomikSprite logicName={m.domikLogic} level={3} className="wiki-mech-ico" aria-hidden="true" />
                                    : m.abstractLogic != null
                                        ? <AbstractSprite logicName={m.abstractLogic} size={24} className="wiki-mech-ico" aria-hidden="true" />
                                        : <MechanicSprite logicName={m.logic} size={24} className="wiki-mech-ico" aria-hidden="true" />}
                                <span className="wiki-building-titles">
                                    <span className="wiki-building-name">{m.name}</span>
                                    <span className="wiki-building-meta">{fillFacts(m.teaser, facts)}</span>
                                </span>
                                <span className="wiki-building-aside">
                                    <ChevronDownIcon className="receipt-caret" aria-hidden="true" />
                                </span>
                            </button>
                            {open && (
                                <div className="wiki-mechanic-body">
                                    <WikiArticle mechanic={m} facts={facts} />
                                    {m.key === 'village' && (
                                        <div className="wiki-mechanic-live">
                                            <span className="wiki-mechanic-live-label wiki-village-level">
                                                <MechanicSprite logicName="obzhitost" size={32} className="weather-chip-ico" aria-hidden="true" />
                                                Текущая обжитость: {villageLevel.level}
                                            </span>
                                            <dl className="wiki-res-facts">
                                                <dt>Постройки</dt>
                                                <dd>{villageLevel.buildings} × 1 = {villageLevel.buildings}</dd>
                                                <dt>Вместимость коек</dt>
                                                <dd>{villageLevel.residents} × 2 = {villageLevel.residents * 2}</dd>
                                                <dt>Вехи доброго имени</dt>
                                                <dd>{villageLevel.reputation} × 5 = {villageLevel.reputation * 5}</dd>
                                                <dt>Уют</dt>
                                                <dd>{Math.min(villageLevel.comfort, 50)} × 1 = {Math.min(villageLevel.comfort, 50)}</dd>
                                                <dt>Итого</dt>
                                                <dd>{villageLevel.level}</dd>
                                            </dl>
                                            {unlocks.length > 0 && (
                                                <div className="unlock-roadmap">
                                                    {unlocked.length > 0 && (
                                                        <>
                                                            <span className="wiki-mechanic-live-label">Уже открыто</span>
                                                            <ul className="unlock-list unlock-list-done">
                                                                {unlocked.map(unlock => {
                                                                    const description = getUnlockDescription(unlock);
                                                                    return (
                                                                        <li key={`${unlock.kind}-${unlock.logicName ?? unlock.label}-${unlock.level ?? unlock.requirement}`} className="unlock-row unlock-row-done">
                                                                            {getUnlockIcon(unlock)}
                                                                            <span className="unlock-body">
                                                                                <span className="unlock-name">{unlock.label}</span>
                                                                                {description !== '' && <span className="unlock-description">{description}</span>}
                                                                            </span>
                                                                            <span className="unlock-badge unlock-badge-done">
                                                                                <CheckIcon aria-hidden="true" />
                                                                                обжитость {unlock.level}
                                                                            </span>
                                                                        </li>
                                                                    );
                                                                })}
                                                            </ul>
                                                        </>
                                                    )}
                                                    <div className="unlock-here">ты здесь: обжитость {villageLevel.level}</div>
                                                    {upcoming.length > 0 && (
                                                        <>
                                                            <span className="wiki-mechanic-live-label">Впереди</span>
                                                            <ul className="unlock-list">
                                                                {upcoming.map(unlock => {
                                                                    const description = getUnlockDescription(unlock);
                                                                    return (
                                                                        <li key={`${unlock.kind}-${unlock.logicName ?? unlock.label}-${unlock.level ?? unlock.requirement}`} className="unlock-row">
                                                                            {getUnlockIcon(unlock)}
                                                                            <span className="unlock-body">
                                                                                <span className="unlock-name">{unlock.label}</span>
                                                                                {description !== '' && <span className="unlock-description">{description}</span>}
                                                                            </span>
                                                                            <span className="unlock-badge">
                                                                                {unlock.level != null ? <><LockIcon aria-hidden="true" />при обжитости {unlock.level}</> : unlock.requirement}
                                                                            </span>
                                                                        </li>
                                                                    );
                                                                })}
                                                            </ul>
                                                        </>
                                                    )}
                                                </div>
                                            )}
                                            <ul className="unlock-list">
                                                <li className={`unlock-row${gateReached ? ' unlock-row-done' : ''}`}>
                                                    <AbstractSprite logicName="prestige_new_valley" size={24} className="unlock-ico" aria-hidden="true" />
                                                    <span className="unlock-body">
                                                        <span className="unlock-name">Твой переезд в новую долину</span>
                                                        <span className="unlock-description">
                                                            переездов позади: {relocation.relocationCount}
                                                            {gateReached && !relocation.canRelocate && relocation.blockReason != null && ` · ${relocation.blockReason}`}
                                                        </span>
                                                    </span>
                                                    <span className={`unlock-badge${gateReached ? ' unlock-badge-done' : ''}`}>
                                                        {gateReached
                                                            ? <><CheckIcon aria-hidden="true" />обжитость {relocation.threshold}</>
                                                            : <><LockIcon aria-hidden="true" />при обжитости {relocation.threshold}</>}
                                                    </span>
                                                </li>
                                            </ul>
                                        </div>
                                    )}
                                    {m.key === 'convoy' && convoys.length > 0 && (
                                        <div className="wiki-mechanic-live">
                                            <span className="wiki-mechanic-live-label">Обозы твоих соседей сейчас</span>
                                            <ul className="wiki-convoy-list">
                                                {convoys.map(convoy => (
                                                    <li key={convoy.neighborId} className={'wiki-convoy-row' + (convoy.isLocked ? ' wiki-convoy-row-locked' : '')}>
                                                        <span className="wiki-convoy-name">
                                                            <NeighborSprite logicName={convoy.neighborLogicName} size={24} className="neighbor-ico" aria-hidden="true" />
                                                            {convoy.neighborName}
                                                        </span>
                                                        {convoy.isLocked
                                                            ? <span className="wiki-convoy-note"><LockIcon aria-hidden="true" />обоз закрыт – мало доверия</span>
                                                            : <>
                                                                <span className="wiki-chips wiki-convoy-items">
                                                                    {convoy.items.map(item => {
                                                                        const resourceType = resourceTypes.find(x => x.id === item.resourceTypeId);
                                                                        if (resourceType == null) {
                                                                            return null;
                                                                        }
                                                                        return (
                                                                            <span key={item.resourceTypeId} className="wiki-chip" title={`${resourceType.name} за ${item.price}`}>
                                                                                <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />
                                                                                <ResourceSprite logicName="coin" aria-hidden="true" />
                                                                                {item.price}
                                                                            </span>
                                                                        );
                                                                    })}
                                                                </span>
                                                                <ConvoyTally remaining={convoy.remaining} limit={convoy.limit} />
                                                            </>}
                                                    </li>
                                                ))}
                                            </ul>
                                        </div>
                                    )}
                                    {m.key === 'weather' && (
                                        <div className="wiki-mechanic-live">
                                            {weather.current != null && (
                                                <>
                                                    <span className="wiki-mechanic-live-label">
                                                        <WeatherSprite logicName={weather.current.logicName} size={24} className="weather-chip-ico" aria-hidden="true" />
                                                        Сейчас: {weather.current.weatherName}
                                                    </span>
                                                    {effectChips.length > 0 && (
                                                        <div className="weather-effects">
                                                            {effectChips}
                                                        </div>
                                                    )}
                                                </>
                                            )}
                                            {weather.forecast.length > 0 && (
                                                <>
                                                    <span className="wiki-mechanic-live-label">Прогноз:</span>
                                                    <div className="weather-effects">
                                                        {weather.forecast.map(period => (
                                                            <span key={period.startDate} className="weather-chip" title={period.weatherName}>
                                                                <WeatherSprite logicName={period.logicName} size={24} className="weather-chip-ico" aria-hidden="true" />
                                                                {period.weatherName}
                                                                {weatherEffects(period.effects, domikTypes).map(row => (
                                                                    <span key={row.domikType.id}
                                                                        className={'weather-effect' + (row.delta > 0 ? ' weather-effect-buff' : ' weather-effect-nerf')}
                                                                        title={`${row.domikType.name}: ${row.delta > 0 ? '+' : ''}${row.delta}% выход`}>
                                                                        <DomikSprite className="weather-effect-ico" logicName={row.domikType.logicName} />
                                                                        {row.delta > 0 ? '+' : ''}{row.delta}%
                                                                    </span>
                                                                ))}
                                                            </span>
                                                        ))}
                                                    </div>
                                                </>
                                            )}
                                        </div>
                                    )}
                                    {m.key === 'profile' && (
                                        <div className="wiki-mechanic-live">
                                            {village.profileNeighborId != null ? (() => {
                                                const activeReputation = reputation.find(r => r.neighborId === village.profileNeighborId);
                                                if (activeReputation == null) {
                                                    return null;
                                                }
                                                const buildings = villageProfiles
                                                    .filter(effect => effect.neighborId === village.profileNeighborId)
                                                    .map(effect => domikTypes.find(type => type.id === effect.domikTypeId))
                                                    .filter((type): type is DomikTypeDto => type != null);
                                                const genitiveName = profileGenitiveName[activeReputation.neighborLogicName] ?? activeReputation.neighborName;
                                                return (
                                                    <>
                                                        <span className="wiki-mechanic-live-label">
                                                            <NeighborSprite logicName={activeReputation.neighborLogicName} size={24} className="weather-chip-ico" aria-hidden="true" />
                                                            Деревня живёт по укладу {genitiveName}
                                                        </span>
                                                        <div className="weather-effects">
                                                            {buildings.map(type => (
                                                                <span key={type.id} className="weather-effect weather-effect-buff" title={type.name}>
                                                                    <DomikSprite className="weather-effect-ico" logicName={type.logicName} />
                                                                    {type.name} −15%
                                                                </span>
                                                            ))}
                                                        </div>
                                                    </>
                                                );
                                            })() : (
                                                <>
                                                    <span className="wiki-mechanic-live-label">Уклады соседей</span>
                                                    <ul className="unlock-list">
                                                        {[...new Set(villageProfiles.map(effect => effect.neighborId))].map(neighborId => {
                                                            const neighborReputation = reputation.find(r => r.neighborId === neighborId);
                                                            if (neighborReputation == null) {
                                                                return null;
                                                            }
                                                            const buildings = villageProfiles
                                                                .filter(effect => effect.neighborId === neighborId)
                                                                .map(effect => domikTypes.find(type => type.id === effect.domikTypeId))
                                                                .filter((type): type is DomikTypeDto => type != null);
                                                            const lore = profileLore[neighborReputation.neighborLogicName];
                                                            return (
                                                                <li key={neighborId} className="unlock-row">
                                                                    <NeighborSprite logicName={neighborReputation.neighborLogicName} size={24} className="unlock-ico" aria-hidden="true" />
                                                                    <span className="unlock-body">
                                                                        <span className="unlock-name">{neighborReputation.neighborName}: {buildings.map(b => b.name).join(' и ')}</span>
                                                                        {lore != null && <span className="unlock-description">{lore}</span>}
                                                                    </span>
                                                                </li>
                                                            );
                                                        })}
                                                    </ul>
                                                </>
                                            )}
                                        </div>
                                    )}
                                    {m.key === 'toloka' && toloka != null && (
                                        <div className="wiki-mechanic-live">
                                            <span className="wiki-mechanic-live-label">Сейчас: {toloka.active.name}</span>
                                            <div className="wiki-toloka-list">
                                                {toloka.active.positions.map(position => {
                                                    const resourceType = resourceTypes.find(type => type.id === position.resourceTypeId);
                                                    const progress = position.goal > 0
                                                        ? Math.min(100, position.collected * 100 / position.goal)
                                                        : 0;
                                                    return (
                                                        <div key={position.resourceTypeId} className="wiki-toloka-row">
                                                            <div className="wiki-toloka-row-head">
                                                                <span className="wiki-toloka-resource">
                                                                    {resourceType != null && <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />}
                                                                    {resourceType?.name ?? 'Ресурс'}
                                                                </span>
                                                                <span>{position.collected} / {position.goal} · мой вклад {position.myContribution}</span>
                                                            </div>
                                                            <div className="wiki-toloka-progress" role="progressbar" aria-label={`${resourceType?.name ?? 'Ресурс'}: ${position.collected} из ${position.goal}`} aria-valuenow={position.collected} aria-valuemin={0} aria-valuemax={position.goal}>
                                                                <span style={{ width: `${progress}%` }} />
                                                            </div>
                                                        </div>
                                                    );
                                                })}
                                            </div>
                                            <span className="wiki-mechanic-live-label">Бафф участнику: {toloka.buffHours} ч{toloka.nextBuffHours != null ? ` · следующий уровень: ${toloka.nextBuffHours} ч` : ''}</span>
                                            {toloka.activeBuffs.length > 0 && (
                                                <div className="weather-effects">
                                                    {toloka.activeBuffs.map(buff => (
                                                        <span key={buff.logicName} className="weather-effect weather-effect-buff">
                                                            +{buff.percent} % {buff.label} · до {new Date(buff.buffUntil).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
                                                        </span>
                                                    ))}
                                                </div>
                                            )}
                                            {toloka.candidates.length > 0 && (
                                                <div className="wiki-toloka-vote">
                                                    <span className="wiki-mechanic-live-label">Голосование за следующий проект</span>
                                                    <div className="wiki-toloka-votes">
                                                        {toloka.candidates.map(candidate => (
                                                            <span key={candidate.tolokaTypeId} className={'wiki-toloka-vote-chip' + (candidate.tolokaTypeId === toloka.myVoteTolokaTypeId ? ' wiki-toloka-vote-chip-mine' : '')}>
                                                                {candidate.name}: {candidate.votes}
                                                                {candidate.tolokaTypeId === toloka.myVoteTolokaTypeId && ' · твой голос'}
                                                            </span>
                                                        ))}
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    )}
                                    {m.key === 'decor' && decor.types.length > 0 && (
                                        <div className="wiki-mechanic-live">
                                            <span className="wiki-mechanic-live-label">Сейчас: уют {decor.comfort} · каталог и владение</span>
                                            <div className="wiki-res-grid">
                                                {decor.types.map(type => {
                                                    const owned = decor.owned.find(item => item.decorTypeId === type.id)?.count ?? 0;
                                                    const reputationPoints = type.neighborId == null
                                                        ? null
                                                        : reputation.find(item => item.neighborId === type.neighborId)?.points ?? 0;
                                                    return (
                                                        <div key={type.id} className="wiki-res-cell wiki-decor-cell pixel-panel" title={type.name}>
                                                            <div className="wiki-decor-head">
                                                                <DecorSprite logicName={type.logicName} size={32} aria-hidden="true" />
                                                                <span>{type.name}</span>
                                                            </div>
                                                            <span className="wiki-decor-meta">{type.comfortPoints === 0 ? 'Витрина' : `уют +${type.comfortPoints}`} · в деревне {owned}</span>
                                                            {type.isPurchasable
                                                                ? <span className="wiki-decor-cost">цена: <ResChips items={type.cost} resourceTypes={resourceTypes} /></span>
                                                                : <span className="wiki-decor-meta">не покупается · трофей экспедиции</span>}
                                                            {type.maxCount != null && <span className="wiki-decor-meta">максимум: {type.maxCount}</span>}
                                                            {type.neighborName != null && <span className="wiki-decor-meta">{type.neighborName}: доброе имя {reputationPoints}/{type.reputationThreshold}</span>}
                                                            {type.requiresDecorName != null && <span className="wiki-decor-meta">сначала: {type.requiresDecorName}</span>}
                                                        </div>
                                                    );
                                                })}
                                            </div>
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>
                    );
                })}
            </div>
        </section>
    );
};

export const Wiki = () => {
    const toast = useToast();
    const [searchParams] = useSearchParams();
    const [view, setView] = useState<CatalogView | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [attempt, setAttempt] = useState(0);

    const retry = useCallback(() => {
        setError(null);
        setAttempt(value => value + 1);
    }, []);

    useEffect(() => {
        const controller = new AbortController();

        void (async () => {
            try {
                const state = await getWikiState(controller.signal);
                void saveWikiFacts(state.facts);
                setView({ catalog: toCatalog(state, state.facts), staleSince: null });
            } catch (err) {
                if (err instanceof OfflineError) {
                    const snapshot = await loadSnapshot();
                    if (snapshot != null && !controller.signal.aborted) {
                        setView({ catalog: toCatalog(snapshot.state, await loadWikiFacts()), staleSince: snapshot.savedAt });
                        return;
                    }
                }
                if (controller.signal.aborted) {
                    return;
                }
                const message = err instanceof ApiError ? err.message : 'Справочник не открылся. Попробуйте ещё раз.';
                toast.error(message);
                setError(message);
            }
        })();

        return () => { controller.abort(); };
    }, [toast, attempt]);

    if (error != null) {
        return (
            <div className="wiki">
                <section className="wiki-intro pixel-panel">
                    <h1 className="wiki-title">Справочник</h1>
                    <p>{error}</p>
                    <p><button type="button" className="btn-game" onClick={retry}>Загрузить ещё раз</button></p>
                </section>
            </div>
        );
    }

    if (view == null) {
        return <div className="wiki"><PixelLoader label="Загрузка справочника…" /></div>;
    }

    const { facts, domikTypes, resourceTypes, receipts, weather, decor, villageLevel, convoys, toloka, village, villageProfiles, reputation, relocation } = view.catalog;
    const openArticle = searchParams.get('article');
    const openBuilding = searchParams.get('building');

    return (
        <div className="wiki">
            <OfflineBanner staleSince={view.staleSince} subject="справочник показан таким, каким был" />
            <section className="wiki-intro pixel-panel">
                <h1 className="wiki-title">Справочник</h1>
                <p>Domiki – уютная idle-деревня. Заходи на пару минут: строй домики, запускай производства, бери заказы соседей. Ресурсы копятся сами, даже с закрытой вкладкой.</p>
                <p>Ниже – ресурсы, постройки, рецепты и обзор механик. Данные загружаются из текущего состояния игры при открытии справочника.</p>
                <Link className="btn-game" to="/domiki-page">
                    <ArrowLeftIcon className="btn-ico" aria-hidden="true" />
                    В игру
                </Link>
            </section>

            <WikiResourcesSection resourceTypes={resourceTypes} />

            <WikiBuildingsSection domikTypes={domikTypes} resourceTypes={resourceTypes} receipts={receipts} openLogicName={openBuilding} />

            <section className="wiki-section">
                <h2 className="section-head">Переделы</h2>
                <div className="wiki-chain pixel-panel">
                    <div className="wiki-chain-flow">
                        {METAL_CHAIN.map((step, i) => (
                            <Fragment key={step.logicName}>
                                {i > 0 && <span className="wiki-chain-arrow" aria-hidden="true">→</span>}
                                <div className="wiki-chain-node">
                                    <ResourceSprite logicName={step.logicName} size={48} aria-hidden="true" />
                                    <span className="wiki-chain-name">{step.name}</span>
                                    <span className="wiki-chain-where">{step.where}</span>
                                </div>
                            </Fragment>
                        ))}
                    </div>
                    <p className="wiki-chain-note">Сырьё сначала перерабатывают: руда груба, а в паре переделов становится добротным инструментом. До обжитости 20 инструмент достаётся только из экспедиций.</p>
                </div>
            </section>

            <WikiMechanicsSection facts={facts} villageLevel={villageLevel} weather={weather} decor={decor} domikTypes={domikTypes} convoys={convoys} toloka={toloka} resourceTypes={resourceTypes} relocation={relocation}
                village={village} villageProfiles={villageProfiles} reputation={reputation} openKey={openArticle} />
        </div>
    );
};

import { Fragment, useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { ApiError, getGameState } from '../services/api';
import { useToast } from '../services/toast';
import { formatDuration } from '../utils/time';
import { domikLore } from '../utils/domikLore';
import { resourceLore } from '../utils/resourceLore';
import type { DecorStateDto, DomikTypeDto, ReceiptDto, ResourceDto, ResourceTypeDto, WeatherStateDto } from '../types/api';
import { DecorSprite, DomikSprite, MechanicSprite, ResourceSprite, WeatherSprite } from './sprites';
import { AnimatedDomikSprite } from './AnimatedDomikSprite';
import { PixelLoader } from './PixelLoader';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';

interface Catalog {
    domikTypes: DomikTypeDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
    weather: WeatherStateDto;
    decor: DecorStateDto;
}

interface Mechanic {
    key: string;
    logic: string;
    name: string;
    teaser: string;
    description: string;
}

const MECHANICS: Mechanic[] = [
    {
        key: 'village',
        logic: 'obzhitost',
        name: 'Обжитость',
        teaser: 'уровень деревни, открывает контент',
        description: 'Уровень деревни – не опыт, а производная от её состояния: сумма уровней построек ×1, жители ×2, вехи репутации ×5, очки уюта. Нафармить дёшево нельзя. Пороги обжитости открывают новые постройки, соседей, приросты и здания механик.',
    },
    {
        key: 'orders',
        logic: 'orders',
        name: 'Заказы',
        teaser: 'спрос соседей, репутация',
        description: 'Доска заказов соседей: «15 кирпичей и 5 досок за 600 монет и 2 золота, истекает через 8 часов». Платят больше магазина, требуют цепочек и срока. Выполненные заказы копят репутацию у конкретного соседа, а она открывает уникальные рецепты, чертежи и декор.',
    },
    {
        key: 'workers',
        logic: 'workers',
        name: 'Трудяги',
        teaser: 'персонажи с чертами и опытом',
        description: 'Трудяги – именованные персонажи, а не счётчик. У каждого одна черта (Проворный, Запасливый, Соня, Везучий), опыт по профессии растёт от работы до +15 %, а после долгой смены нужен отдых в бараке. Кого куда ставить – решаешь ты, по умолчанию авторасстановка.',
    },
    {
        key: 'weather',
        logic: 'weather',
        name: 'Погода',
        teaser: 'глобальные ±% на выход',
        description: 'Погода одна на всю деревню и меняется раз в 6–12 часов, а прогноз виден на сутки вперёд. Дождь, сушь и ясные дни меняют выход разных построек на ±25–50 %. Это планирование, а не рулетка: подстраивай производства под выгодный период.',
    },
    {
        key: 'blueprints',
        logic: 'blueprints',
        name: 'Чертежи',
        teaser: 'открывают новые постройки',
        description: 'Чертежи открывают постройки следующего круга – пекарню, гончарню и дальше. Их нельзя купить: только найти в экспедиции или заслужить репутацией у соседей.',
    },
    {
        key: 'expeditions',
        logic: 'expeditions',
        name: 'Экспедиции',
        teaser: 'поход за редкостями',
        description: 'Отправь трудяг с припасами на 4–24 часа – вернутся с сундуком: редкие ресурсы, чертежи, декор, новые трудяги. Это единственная случайность в игре, с гарантией редкости за несколько походов. Вход – снаряжение из кузницы, а поход утомляет трудяг. Открывается зданием «Сторожка».',
    },
    {
        key: 'market',
        logic: 'market',
        name: 'Ярмарка',
        teaser: 'обмен лотами между деревнями',
        description: 'Ярмарка – асинхронный обмен: выставил лот «20 глины за 3 золота» и вышел, кто-то примет позже. Комиссия убывает с уровнем «Торгового двора». Торгуются только массовые ресурсы, чертежи и трудяги – нет. Открывается зданием «Торговый двор».',
    },
    {
        key: 'toloka',
        logic: 'toloka',
        name: 'Толока',
        teaser: 'общий проект деревни',
        description: 'Толока – общий проект всех игроков: мост, амбар или печь. Каждый вкладывает своё сырьё в общий счётчик; цель растёт с числом вкладчиков. При достижении цели все участники получают временный тематический бафф +40 %: амбар ускоряет добычу дерева и глины, печь – переделы в кузнице и мастерской, мост добавляет монет к наградам заказов. Выбор проекта – решение, а не фон. Кооперация без риска. Открывается зданием «Сборня».',
    },
    {
        key: 'decor',
        logic: 'decor',
        name: 'Декор',
        teaser: 'уют ускоряет отдых',
        description: 'Заборы, фонтаны, сады – сток излишков ресурсов. Каждый предмет даёт очки уюта, а чем уютнее деревня, тем быстрее трудяги отдыхают в бараке.',
    },
];

const RES_POP_WIDTH = 260;

const METAL_CHAIN: { logicName: string; name: string; where: string }[] = [
    { logicName: 'ore', name: 'Руда', where: 'Рудник' },
    { logicName: 'iron', name: 'Железо', where: 'Кузница' },
    { logicName: 'tool', name: 'Инструмент', where: 'Кузница + доски' },
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
                ускорение: <ResChips items={receipt.optionalInputResources} resourceTypes={resourceTypes} />
                {receipt.outputBonusPercent > 0 && <span> (+{receipt.outputBonusPercent}% выхода)</span>}
            </div>
        )}
        <div className="wiki-recipe-meta">
            <span>{formatDuration(receipt.durationSeconds)}</span>
            <span>{receipt.plodderCount} трудяг</span>
        </div>
    </div>
);

export const Wiki = () => {
    const toast = useToast();
    const [catalog, setCatalog] = useState<Catalog | null>(null);
    const [openIds, setOpenIds] = useState<ReadonlySet<number>>(new Set());
    const toggleBuilding = (id: number) => setOpenIds(prev => {
        const next = new Set(prev);
        if (next.has(id)) {
            next.delete(id);
        } else {
            next.add(id);
        }
        return next;
    });
    const [resFlyout, setResFlyout] = useState<{ type: ResourceTypeDto; top: number; left: number } | null>(null);
    const openResFlyout = (type: ResourceTypeDto, el: HTMLElement) => {
        if (resourceLore[type.logicName] == null) {
            return;
        }
        const rect = el.getBoundingClientRect();
        const left = Math.max(12, Math.min(rect.left, window.innerWidth - RES_POP_WIDTH - 12));
        setResFlyout({ type, top: rect.bottom + 6, left });
    };
    const closeResFlyout = () => setResFlyout(null);
    const [openMechanics, setOpenMechanics] = useState<ReadonlySet<string>>(new Set());
    const toggleMechanic = (key: string) => setOpenMechanics(prev => {
        const next = new Set(prev);
        if (next.has(key)) {
            next.delete(key);
        } else {
            next.add(key);
        }
        return next;
    });

    useEffect(() => {
        const controller = new AbortController();

        void (async () => {
            try {
                const state = await getGameState(controller.signal);
                setCatalog({
                    domikTypes: state.domikTypes,
                    resourceTypes: state.resourceTypes,
                    receipts: state.receipts,
                    weather: state.weather,
                    decor: state.decor,
                });
            } catch (err) {
                if (err instanceof DOMException && err.name === 'AbortError') {
                    return;
                }
                if (err instanceof ApiError) {
                    toast.error(err.message);
                }
            }
        })();

        return () => { controller.abort(); };
    }, [toast]);

    if (catalog == null) {
        return <div className="wiki"><PixelLoader label="Загрузка справочника…" /></div>;
    }

    const { domikTypes, resourceTypes, receipts, weather, decor } = catalog;
    const receiptById = (id: number) => receipts.find(x => x.id === id);
    const buildings = [...domikTypes].sort((a, b) => a.unlockLevel - b.unlockLevel || a.id - b.id);

    return (
        <div className="wiki">
            <section className="wiki-intro pixel-panel">
                <h1 className="wiki-title">Справочник</h1>
                <p>Domiki – уютная idle-деревня. Заходи на пару минут: строй домики, запускай производства, бери заказы соседей. Ресурсы копятся сами, даже с закрытой вкладкой.</p>
                <p>Ниже – ресурсы, постройки, рецепты и обзор механик. Данные живые, прямо из игры, так что не устаревают.</p>
            </section>

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
                        <div className="wiki-res-pop pixel-panel" role="tooltip" style={{ top: resFlyout.top, left: resFlyout.left, width: RES_POP_WIDTH }}>
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

            <section className="wiki-section">
                <h2 className="section-head">Постройки</h2>
                <div className="wiki-buildings">
                    {buildings.map(type => {
                        const open = openIds.has(type.id);
                        const lore = domikLore[type.logicName];
                        const outputTypeIds = [...new Set(
                            type.levels.flatMap(l => l.receiptIds)
                                .map(receiptById).filter((r): r is ReceiptDto => r != null)
                                .flatMap(r => r.outputResources.map(o => o.typeId))
                        )];

                        return (
                            <div key={type.id} className={'wiki-building pixel-panel' + (open ? ' receipt-open' : '')}>
                                <button type="button" className="wiki-building-head" aria-expanded={open} onClick={() => toggleBuilding(type.id)}>
                                    <AnimatedDomikSprite mode="loop" logicName={type.logicName} maxLevel={type.levels.length} active={open} />
                                    <span className="wiki-building-titles">
                                        <span className="wiki-building-name">{type.name}</span>
                                        <span className="wiki-building-meta">
                                            до {type.maxLevel} ур. · макс. {type.maxCount} шт.
                                            {type.unlockLevel > 0 && ` · с ${type.unlockLevel} ур. деревни`}
                                            {type.blueprintId != null && ' · по чертежу'}
                                        </span>
                                    </span>
                                    <span className="wiki-building-aside">
                                        {outputTypeIds.length > 0 && (
                                            <span className="wiki-building-teaser">
                                                {outputTypeIds.map(tid => {
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
                                            if (level.resources.length === 0 && levelReceipts.length === 0) {
                                                return null;
                                            }
                                            return (
                                                <div key={level.value} className="wiki-level">
                                                    <div className="wiki-level-head">
                                                        <span className="wiki-level-badge">Ур. {level.value}</span>
                                                        {level.resources.length > 0 && (
                                                            <span className="wiki-level-cost">{level.value === 1 ? 'постройка' : 'апгрейд'}: <ResChips items={level.resources} resourceTypes={resourceTypes} /></span>
                                                        )}
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

            <section className="wiki-section">
                <h2 className="section-head">Механики</h2>
                <div className="wiki-buildings">
                    {MECHANICS.map(m => {
                        const open = openMechanics.has(m.key);

                        return (
                            <div key={m.key} className={'wiki-building pixel-panel' + (open ? ' receipt-open' : '')}>
                                <button type="button" className="wiki-building-head" aria-expanded={open} onClick={() => toggleMechanic(m.key)}>
                                    <MechanicSprite logicName={m.logic} size={24} className="wiki-mech-ico" aria-hidden="true" />
                                    <span className="wiki-building-titles">
                                        <span className="wiki-building-name">{m.name}</span>
                                        <span className="wiki-building-meta">{m.teaser}</span>
                                    </span>
                                    <span className="wiki-building-aside">
                                        <ChevronDownIcon className="receipt-caret" aria-hidden="true" />
                                    </span>
                                </button>
                                {open && (
                                    <div className="wiki-mechanic-body">
                                        <p>{m.description}</p>
                                        {m.key === 'weather' && (
                                            <div className="wiki-mechanic-live">
                                                {weather.current != null && (
                                                    <>
                                                        <span className="wiki-mechanic-live-label">
                                                            <WeatherSprite logicName={weather.current.logicName} size={24} className="weather-chip-ico" aria-hidden="true" />
                                                            Сейчас: {weather.current.weatherName}
                                                        </span>
                                                        {weather.current.effects.some(e => e.outputPercent !== 100) && (
                                                            <div className="weather-effects">
                                                                {weather.current.effects.filter(e => e.outputPercent !== 100).map(e => {
                                                                    const domikType = domikTypes.find(t => t.id === e.domikTypeId);
                                                                    if (domikType == null) {
                                                                        return null;
                                                                    }
                                                                    const buff = e.outputPercent > 100;
                                                                    const delta = e.outputPercent - 100;
                                                                    return (
                                                                        <span key={e.domikTypeId} className={'weather-effect' + (buff ? ' weather-effect-buff' : ' weather-effect-nerf')} title={domikType.logicName}>
                                                                            <DomikSprite className="weather-effect-ico" logicName={domikType.logicName} />
                                                                            {buff ? '+' : ''}{delta}%
                                                                        </span>
                                                                    );
                                                                })}
                                                            </div>
                                                        )}
                                                    </>
                                                )}
                                                {weather.forecast.length > 0 && (
                                                    <>
                                                        <span className="wiki-mechanic-live-label">Прогноз:</span>
                                                        <div className="weather-effects">
                                                            {weather.forecast.map(period => {
                                                                const hint = period.effects
                                                                    .filter(e => e.outputPercent !== 100)
                                                                    .flatMap(e => {
                                                                        const domikType = domikTypes.find(t => t.id === e.domikTypeId);
                                                                        return domikType != null ? [{ delta: e.outputPercent - 100, domikType }] : [];
                                                                    })
                                                                    .sort((a, b) => Math.abs(b.delta) - Math.abs(a.delta))[0];
                                                                return (
                                                                    <span key={period.startDate} className="weather-chip" title={period.weatherName}>
                                                                        <WeatherSprite logicName={period.logicName} size={24} className="weather-chip-ico" aria-hidden="true" />
                                                                        {period.weatherName}
                                                                        {hint != null && (
                                                                            <span className={'weather-effect' + (hint.delta > 0 ? ' weather-effect-buff' : ' weather-effect-nerf')} title={`${hint.domikType.name}: ${hint.delta > 0 ? '+' : ''}${hint.delta}% выход`}>
                                                                                <DomikSprite className="weather-effect-ico" logicName={hint.domikType.logicName} />
                                                                                {hint.delta > 0 ? '+' : ''}{hint.delta}%
                                                                            </span>
                                                                        )}
                                                                    </span>
                                                                );
                                                            })}
                                                        </div>
                                                    </>
                                                )}
                                            </div>
                                        )}
                                        {m.key === 'decor' && decor.types.length > 0 && (
                                            <div className="wiki-res-grid">
                                                {decor.types.map(type => {
                                                    return (
                                                        <div key={type.id} className="wiki-res-cell pixel-panel" title={type.name}>
                                                            <DecorSprite logicName={type.logicName} size={32} aria-hidden="true" />
                                                            <span>{type.name} · уют +{type.comfortPoints}</span>
                                                        </div>
                                                    );
                                                })}
                                            </div>
                                        )}
                                    </div>
                                )}
                            </div>
                        );
                    })}
                </div>
            </section>
        </div>
    );
};

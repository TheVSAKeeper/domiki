import { useEffect, useState } from 'react';
import { ApiError, getGameState } from '../services/api';
import { useToast } from '../services/toast';
import { formatDuration } from '../utils/time';
import { domikLore } from '../utils/domikLore';
import type { DecorStateDto, DomikTypeDto, ReceiptDto, ResourceDto, ResourceTypeDto, WeatherStateDto } from '../types/api';
import { DomikSprite } from './sprites';
import { AnimatedDomikSprite } from './AnimatedDomikSprite';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import CloudSunIcon from 'pixelarticons/svg/cloud-sun.svg?react';
import CloudIcon from 'pixelarticons/svg/cloud.svg?react';
import FireIcon from 'pixelarticons/svg/fire.svg?react';
import BackpackIcon from 'pixelarticons/svg/backpack.svg?react';
import UsersIcon from 'pixelarticons/svg/users.svg?react';
import UserIcon from 'pixelarticons/svg/user.svg?react';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import ClipboardIcon from 'pixelarticons/svg/clipboard.svg?react';
import BuildingCommunityIcon from 'pixelarticons/svg/building-community.svg?react';
import NoteIcon from 'pixelarticons/svg/note.svg?react';
import GardenIcon from 'pixelarticons/svg/tree.svg?react';
import FenceIcon from 'pixelarticons/svg/grid-3x2.svg?react';
import FlowerIcon from 'pixelarticons/svg/heart.svg?react';
import FountainIcon from 'pixelarticons/svg/home.svg?react';

interface Catalog {
    domikTypes: DomikTypeDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
    weather: WeatherStateDto;
    decor: DecorStateDto;
}

const WEATHER_ICONS: Record<string, typeof CloudSunIcon> = {
    clear: CloudSunIcon,
    rain: CloudIcon,
    drought: FireIcon,
};

const DECOR_ICONS: Record<string, typeof FenceIcon> = {
    fence: FenceIcon,
    flowerbed: FlowerIcon,
    garden: GardenIcon,
    fountain: FountainIcon,
};

interface Mechanic {
    key: string;
    name: string;
    Icon: typeof CloudSunIcon;
    teaser: string;
    description: string;
}

const MECHANICS: Mechanic[] = [
    {
        key: 'village',
        Icon: BuildingCommunityIcon,
        name: 'Обжитость',
        teaser: 'уровень деревни, открывает контент',
        description: 'Уровень деревни – не опыт, а производная от её состояния: сумма уровней построек ×1, жители ×2, вехи репутации ×5, очки уюта. Нафармить дёшево нельзя. Пороги обжитости открывают новые постройки, соседей, приросты и здания механик.',
    },
    {
        key: 'orders',
        Icon: ClipboardIcon,
        name: 'Заказы',
        teaser: 'спрос соседей, репутация',
        description: 'Доска заказов соседей: «15 кирпичей и 5 досок за 600 монет и 2 золота, истекает через 8 часов». Платят больше магазина, требуют цепочек и срока. Выполненные заказы копят репутацию у конкретного соседа, а она открывает уникальные рецепты, чертежи и декор.',
    },
    {
        key: 'workers',
        Icon: UserIcon,
        name: 'Трудяги',
        teaser: 'персонажи с чертами и опытом',
        description: 'Трудяги – именованные персонажи, а не счётчик. У каждого одна черта (Проворный, Запасливый, Соня, Везучий), опыт по профессии растёт от работы до +15 %, а после долгой смены нужен отдых в бараке. Кого куда ставить – решаешь ты, по умолчанию авторасстановка.',
    },
    {
        key: 'weather',
        Icon: CloudSunIcon,
        name: 'Погода',
        teaser: 'глобальные ±% на выход',
        description: 'Погода одна на всю деревню и меняется раз в 6–12 часов, а прогноз виден на сутки вперёд. Дождь, сушь и ясные дни меняют выход разных построек на ±25–50 %. Это планирование, а не рулетка: подстраивай производства под выгодный период.',
    },
    {
        key: 'blueprints',
        Icon: NoteIcon,
        name: 'Чертежи',
        teaser: 'открывают новые постройки',
        description: 'Чертежи открывают постройки следующего круга – пекарню, гончарню и дальше. Их нельзя купить: только найти в экспедиции или заслужить репутацией у соседей.',
    },
    {
        key: 'expeditions',
        Icon: BackpackIcon,
        name: 'Экспедиции',
        teaser: 'поход за редкостями',
        description: 'Отправь трудяг с припасами на 4–24 часа – вернутся с сундуком: редкие ресурсы, чертежи, декор, новые трудяги. Это единственная случайность в игре, с гарантией редкости за несколько походов. Вход – снаряжение из кузницы, а поход утомляет трудяг. Открывается зданием «Сторожка».',
    },
    {
        key: 'market',
        Icon: StoreIcon,
        name: 'Ярмарка',
        teaser: 'обмен лотами между деревнями',
        description: 'Ярмарка – асинхронный обмен: выставил лот «20 глины за 3 золота» и вышел, кто-то примет позже. Комиссия убывает с уровнем «Торгового двора». Торгуются только массовые ресурсы, чертежи и трудяги – нет. Открывается зданием «Торговый двор».',
    },
    {
        key: 'toloka',
        Icon: UsersIcon,
        name: 'Толока',
        teaser: 'общий проект деревни',
        description: 'Толока – общий проект всех игроков: «на мост нужно 5000 камня со всех». Каждый вкладывает в общий счётчик, и при достижении цели все участники получают временный бафф и отметку. Кооперация без риска. Открывается зданием «Сходня».',
    },
    {
        key: 'decor',
        Icon: GardenIcon,
        name: 'Декор',
        teaser: 'уют ускоряет отдых',
        description: 'Заборы, фонтаны, сады – сток излишков ресурсов. Каждый предмет даёт очки уюта, а чем уютнее деревня, тем быстрее трудяги отдыхают в бараке.',
    },
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
                    <img src={'/images/resourceTypes/' + type.logicName + '.png'} alt={type.name} />
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
                {receipt.speedupPercent > 0 && <span> (−{receipt.speedupPercent}% времени)</span>}
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
        return <div className="wiki"><p className="wiki-loading">Загрузка справочника…</p></div>;
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
                <div className="wiki-res-grid">
                    {resourceTypes.map(type => (
                        <div key={type.id} className="wiki-res-cell pixel-panel" title={type.name}>
                            <img src={'/images/resourceTypes/' + type.logicName + '.png'} alt={type.name} />
                            <span>{type.name}</span>
                        </div>
                    ))}
                </div>
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
                                                    return <img key={tid} src={'/images/resourceTypes/' + rt.logicName + '.png'} alt={rt.name} title={rt.name} />;
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
                                                            <span className="wiki-level-cost">апгрейд: <ResChips items={level.resources} resourceTypes={resourceTypes} /></span>
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
                <h2 className="section-head">Механики</h2>
                <div className="wiki-buildings">
                    {MECHANICS.map(m => {
                        const open = openMechanics.has(m.key);
                        const Icon = m.Icon;

                        return (
                            <div key={m.key} className={'wiki-building pixel-panel' + (open ? ' receipt-open' : '')}>
                                <button type="button" className="wiki-building-head" aria-expanded={open} onClick={() => toggleMechanic(m.key)}>
                                    <Icon aria-hidden="true" />
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
                                                        <span className="wiki-mechanic-live-label">Сейчас: {weather.current.weatherName}</span>
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
                                                                const ForecastIcon = WEATHER_ICONS[period.logicName] ?? CloudSunIcon;
                                                                const hint = period.effects
                                                                    .filter(e => e.outputPercent !== 100)
                                                                    .flatMap(e => {
                                                                        const domikType = domikTypes.find(t => t.id === e.domikTypeId);
                                                                        return domikType != null ? [{ delta: e.outputPercent - 100, domikType }] : [];
                                                                    })
                                                                    .sort((a, b) => Math.abs(b.delta) - Math.abs(a.delta))[0];
                                                                return (
                                                                    <span key={period.startDate} className="weather-chip" title={period.weatherName}>
                                                                        <ForecastIcon className="weather-chip-ico" aria-hidden="true" />
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
                                                    const DecorIcon = DECOR_ICONS[type.logicName] ?? GardenIcon;
                                                    return (
                                                        <div key={type.id} className="wiki-res-cell pixel-panel" title={type.name}>
                                                            <DecorIcon aria-hidden="true" />
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

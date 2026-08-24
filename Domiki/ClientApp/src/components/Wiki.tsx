import { Fragment, useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { ApiError, getGameState } from '../services/api';
import { useToast } from '../services/toastContext';
import { formatDuration } from '../utils/time';
import { domikLore } from '../utils/domikLore';
import { unlockLore } from '../utils/unlockLore';
import { resourceLore } from '../utils/resourceLore';
import { flyoutLeft, flyoutWidth, useFlyoutTop } from '../utils/flyout';
import { strongestWeatherEffect } from '../utils/game';
import { profileGenitiveName, profileLore } from '../utils/profileLore';
import type { ConvoyDto, DecorStateDto, DomikTypeDto, NeighborReputationDto, ReceiptDto, ResourceDto, ResourceTypeDto, VillageDto, VillageLevelDto, VillageProfileDto, WeatherStateDto } from '../types/api';
import { AbstractSprite, DecorSprite, DomikSprite, MechanicSprite, NeighborSprite, ResourceSprite, WeatherSprite } from './sprites';
import { AnimatedDomikSprite } from './AnimatedDomikSprite';
import { ConvoyTally } from './ConvoyTally';
import { PixelLoader } from './PixelLoader';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import CheckIcon from 'pixelarticons/svg/check.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import '../styles/wiki.css';

interface Catalog {
    domikTypes: DomikTypeDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
    weather: WeatherStateDto;
    decor: DecorStateDto;
    villageLevel: VillageLevelDto;
    convoys: ConvoyDto[];
    village: VillageDto;
    villageProfiles: VillageProfileDto[];
    reputation: NeighborReputationDto[];
}

interface Mechanic {
    key: string;
    logic: string;
    name: string;
    teaser: string;
    description: string;
    /** Постройка, чей спрайт заменяет значок механики: у механик-надстроек своего значка нет. */
    domikLogic?: string;
}

const MECHANICS: Mechanic[] = [
    {
        key: 'village',
        logic: 'obzhitost',
        name: 'Обжитость',
        teaser: 'уровень деревни, открывает контент',
        description: 'Уровень деревни – не опыт, а производная от её состояния: сумма уровней построек ×1, жители ×2, вехи репутации ×5, очки уюта. Дешёвыми хлопотами её не поднять. Ранние пороги укладываются в первые сутки: они открывают, что ставить у плотника, и приводят новых соседей. Дальше обжитость ведёт лестницу коек – шестая артельная изба с 60, седьмая со 110, восьмая со 175; больше 35 трудяг артель не держит. С 350 деревня вправе сняться в новую долину. Ниже – твоя обжитость сейчас и ближайшие открытия.',
    },
    {
        key: 'orders',
        logic: 'orders',
        name: 'Заказы',
        teaser: 'спрос соседей, репутация',
        description: 'Доска заказов соседей: «15 кирпичей и 5 досок за 600 монет и 2 золота, истекает через 8 часов». Платят больше магазина, требуют цепочек и срока. Выполненные заказы копят репутацию у конкретного соседа, а она открывает уникальные рецепты, чертежи и декор.\n\nПросят по силам: сосед смотрит, сколько дворов и рук у тебя занято этим ресурсом, и берёт в расчёт половину твоей выработки за срок заказа – три весточки на доске рассчитаны так, чтобы их можно было закрыть, а не выбирать из них одну. Меньше двух штук сосед не просит. Пока ресурсов на дворе хватает на разное, три весточки редко просят одно и то же.\n\nНенужный заказ можно уступить – он уйдёт в другую деревню без обиды и без награды. Свободная ячейка ждёт нового спроса полчаса, и каждая следующая уступка отсчитывает эти полчаса заново: сдать всю доску разом и тут же получить свежую не выйдет.',
    },
    {
        key: 'friendship',
        logic: 'friendship',
        name: 'Дружба с соседями',
        teaser: 'куда копится доброе имя',
        description: 'Доброе имя копится у каждого соседа отдельно, и заказы приходят вразнобой – чем больше выселок вокруг открыто, тем реже весточка от нужной. Чтобы не ждать милости случая, деревня выбирает, с кем нынче водить дружбу: заказы этого соседа заглядывают на доску втрое чаще. Дружить можно с одним, передумать – в любой день, и на уже висящие весточки это не влияет.\n\nВсю доску друг не занимает: хотя бы одна ячейка всегда остаётся другим выселкам, чтобы остальные не забывались вовсе.\n\nДружба ничего не стоит и сама по себе ничего не даёт – это прицел, а не подарок: заслуга по-прежнему зарабатывается заказами. У каждого соседа на доске видно, какая веха доброго имени ближе и что она откроет – обоз, чертёж или украшение. С теми, до кого дорога ещё не протоптана, дружбу не заведёшь: сперва обжитость.',
    },
    {
        key: 'profile',
        logic: 'uklad',
        name: 'Уклад деревни',
        teaser: 'сноровка, перенятая у соседа',
        description: 'У каждого выселка своё ремесло, и деревня может перенять уклад одного соседа: две постройки его толка набираются чужой сноровки, и смены в них короче на 15 %. Заречье делится выучкой кузницы и каменоломни, Боровое – лесопилки и мастерской, Каменка – каменоломни и каменотёса, Глинищи – глиняного карьера и гончарни, Дубрава – лесопилки и пекарни.\n\nПеренять уклад можно с обжитости 20, и сосед должен деревне доверять – нужна репутация 10 у него. Платы нет, штрафов тоже: уклад – чистый прибыток, и в обжитость он не входит. Одно условие – сменить уклад можно не чаще раза в 7 суток, так что выбирай под своё ремесло; заодно сподручно добрать и чертёж того же соседа.\n\nСкорость смены складывается из всего разом: черта трудяги, набитая рука (до −15 % ко времени) и уклад (−15 %) множатся друг на друга, но короче 60 % от исходной длительности смена не станет – это твёрдый предел. Потому на самом ловком дворе – Работящий (−20 %) да набитая рука – предел срабатывает, и уклад даёт −11,8 % вместо −15: чем больше ускорений собрано в одной постройке, тем меньше добавляет каждое новое. Выгоднее разводить источники скорости по разным дворам, чем громоздить их в один.',
    },
    {
        key: 'errands',
        logic: 'errands',
        name: 'Поручения соседей',
        teaser: 'квест-офферы на доске заказов',
        description: 'С обжитости 10 добор доски заказов может вместо обычного заказа принести поручение – квест с шансом 20 %: сосед просит помочь сыскать пропажу. Предложение висит 8 часов. Приняв, выбираешь зацепку (2 / 4 / 8 часов поисков, +3 / +5 / +8 репутации) и отправляешь 1–2 свободных трудяг; отказ от оффера и отзыв принятого – без штрафа. Награда – 10 монет за трудяго-час плюс репутация по зацепке, а с шансом 20 % (черта Везучий удваивает) поиски приносят ещё и бонусный ресурс соседа.',
    },
    {
        key: 'convoy',
        logic: 'convoy',
        name: 'Обозы соседей',
        teaser: 'докупить сырьё за монеты',
        description: 'Обоз – прилавок соседа на твоей доске заказов: то, чем выселки богаты, можно докупить за монеты мгновенно, без трудяги, станка и ожидания. Цена кусачая – впятеро против рыночной, – и это намеренно: обоз не заменяет своё производство, а выручает, когда до заказа не хватает пары кирпичей, а ждать целую смену некогда. Заодно это главный сток монет: копить их без дела больше незачем.\n\nОбоз пригоняет только тот сосед, кто тебе доверяет – с репутации 5; до этого прилавок закрыт, и открывают его заказами. Основной товар соседа в продаже сразу, а с репутации 20 рядом появляется второй. Взять можно 3 штуки в сутки у каждого соседа, а с репутации 40 – 5. Сутки скользящие: отсчёт идёт от первой покупки, а не от полуночи, и на прилавке видно, сколько осталось и через сколько обоз вернётся. Монеты и золото обоз не возит – их так не выменять.',
    },
    {
        key: 'incidents',
        logic: 'incident',
        name: 'Происшествия',
        teaser: 'истории с зацепками',
        description: 'Иногда трудяга возвращается из похода позже отряда: что-то заметил, нашёл или услышал – и задержался. На доске появляется карточка происшествия с зацепками. Выбери одну (2/4/8 часов – дольше поиски, больше находка) и отправь 1–2 свободных трудяг на подмогу. Финал всегда добрый: все возвращаются, задержавшийся отдыхает пару часов, а поиски приносят находку из мест похода и, бывает, меняют характер героя истории. Не хочешь искать – через двое суток трудяга вернётся сам, просто без находки. Происшествие случается не чаще раза в трое суток и только если в деревне хватает свободных рук.\n\nЗагадку может задать и собственная постройка: после перестройки в Руднике вдруг что-то стучит, а под новой крышей кто-то шуршит. На доске появляется карточка происшествия с зацепками. Выбери одну (2/4/8 часов – дольше разбираются, больше находка) и отправь 1–2 свободных трудяг разобраться. Финал всегда добрый: у каждой загадки простая тёплая разгадка, а деревне остаётся находка и новая байка. Не хочешь разбираться – через двое суток загадка разрешится сама, просто без находки. Такое случается не чаще раза в четверо суток и только после завершённой перестройки; в дождь и в сушь у карьера и лесопилки – свои особые истории.',
    },
    {
        key: 'gifts',
        logic: 'gifts',
        name: 'Гостинцы',
        teaser: 'соседи встречают из отлучки',
        description: 'Отлучился от деревни на 6 часов и дольше – кто-то из открытых соседей оставит у ворот гостинец с запиской: связку своих ресурсов, а при репутации 25 и выше – побогаче. Каждый седьмой такой визит – большой гостинец: декор от всей округи. Пропуски ничего не ломают: счётчик визитов не сгорает.',
    },
    {
        key: 'workers',
        logic: 'workers',
        name: 'Трудяги',
        teaser: 'у трудяг своя жизнь',
        description: 'Трудяги – именованные персонажи, а не счётчик. У каждого одна черта (Проворный, Запасливый, Соня, Везучий), опыт по профессии растёт от работы до +15 %, а после долгой смены нужен отдых в артельной избе. Кого куда ставить – решаешь ты, по умолчанию авторасстановка.\n\nУ каждого трудяги случаются вехи: первая смена, сотая зарубка на притолоке, месяц с котомкой на гвозде, набитая рука. Веха приходит сама – её не надо искать и снаряжать: загляни в деревню, и если у кого-то случилось, в журнале будет тёплая запись и маленький прибыток, а набивший руку в одном деле «Обычный» трудяга обретает характер. Вехи не сгорают: не зашёл вовремя – история дождётся. Чаще одной за двое суток не случается.',
    },
    {
        key: 'tavern',
        logic: 'tavern',
        name: 'Корчма',
        teaser: 'обед, котомки и тёплый угол',
        description: 'Корчма открывается на обжитости 16 и стоит 300 монет. У неё три ступени: «котёл» – уставшие трудяги обедают из запаса, и отдых вдвое короче; «котомки в дорогу» – еда в поход собирается сама; «тёплый угол» – хворые встают на четверть быстрее.\n\nОбед берёт любую еду, дешёвую первой: хлеб из пекарни или сыр из овчарни. Без корчмы обеда нет вовсе.\n\nКладовая: против каждой еды можно поставить заповедный запас – корчмарь берёт только то, что сверх него, а помеченное «не подавать» не тронет ни к обеду, ни в котомки.',
    },
    {
        key: 'elder_house',
        logic: 'elder_house',
        domikLogic: 'elder_house',
        name: 'Изба старосты',
        teaser: 'книга, мера, заповедь',
        description: 'Изба ничего не производит – она считает. Ставится с обжитости 32, одна на деревню, и открывается лестницей: сперва книга, потом мера, потом заповедь. Доска «Хозяйство» и без избы отвечает, чего сейчас нет; изба добавляет числа и сутки.\n\nСчётная книга (1 ур.). За сутки видно, что добыто, что потрачено и сколько двор простоял, а ещё – чего не хватит первым при нынешнем расходе. Книга ведётся с часа постройки, задним числом староста не считает, и держит записи за 8 суток. Монеты и золото в прогноз не берутся: они уходят покупками разом, и ровного расхода у них нет.\n\nМера наряда (2 ур.). Наряду можно назначить меру: повторять, пока кирпича меньше 50, – дальше он снимется сам. Мера переносится на каждый круг, а снятие по мере попадает в журнал своей записью, чтобы плановый конец не читался как заглохший наряд.\n\nЗаповедный припас (3 ур.). Метка на ларе: ниже отложенного наряды не тронут – запас под заказ цел. Заповедь связывает только наряды; смену, запущенную руками, она не остановит.',
    },
    {
        key: 'weather',
        logic: 'weather',
        name: 'Погода',
        teaser: 'глобальные ±% на выход',
        description: 'Погода одна на всю деревню и меняется раз в 6–12 часов, а прогноз виден на сутки вперёд. Дождь, сушь, мороз и ветер меняют выход разных построек на ±25–50 %, а в ясные дни всё идёт ровно, без прибавок и потерь. Это планирование, а не рулетка: подстраивай производства под выгодный период, но помни о цене бонуса – она описана в «Хворях». Постройки, которых погода касается, помечены во дворе её значком: зелёная метка – прибавка к выходу, красная – убавка; на непомеченных всё идёт как обычно.',
    },
    {
        key: 'illnesses',
        logic: 'ailments',
        name: 'Хвори',
        teaser: 'непогода берёт свою цену',
        description: 'Хворь приключается, только когда сам запускаешь производство с действующим погодным бонусом: чем щедрее прибавка, тем выше шанс. Дождь приносит простуду, сушь – перегрев, мороз – озноб, ветер – прострел. Слёгший отлёживается восемь часов, а «тёплый угол» корчмы сокращает лёжку на четверть; больше двух хворых разом в деревне не бывает, а кто переболел – сутки снова не сляжет. На смену с плюсом плащ сам берётся со склада и вполовину снижает риск, но в сушь не берётся: от перегрева он не спасает. Смена без погодного бонуса всегда проходит без хвори.',
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
        description: 'Заборы, фонтаны, сады – сток излишков ресурсов. Каждый такой предмет даёт очки уюта, а чем уютнее деревня, тем быстрее трудяги отдыхают в артельной избе.\n\nОтдельный ряд – украсы заезжих мастеров: резная калитка, колодец-журавль, беседка и пруд с карасями. Ставятся только за монеты, строго по череду и по одной на двор; уют прибавляет лишь калитка, остальные – краса двора для хозяина и гостей. Полный набор стоит без малого сто тысяч.',
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
}

const WikiBuildingsSection = ({ domikTypes, resourceTypes, receipts }: WikiBuildingsSectionProps) => {
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
    const receiptById = (id: number) => receipts.find(x => x.id === id);
    const buildings = [...domikTypes].sort((a, b) => a.unlockLevel - b.unlockLevel || a.id - b.id);

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
    );
};

interface WikiMechanicsSectionProps {
    villageLevel: VillageLevelDto;
    weather: WeatherStateDto;
    decor: DecorStateDto;
    domikTypes: DomikTypeDto[];
    convoys: ConvoyDto[];
    resourceTypes: ResourceTypeDto[];
    village: VillageDto;
    villageProfiles: VillageProfileDto[];
    reputation: NeighborReputationDto[];
}

const WikiMechanicsSection = ({ villageLevel, weather, decor, domikTypes, convoys, resourceTypes, village, villageProfiles, reputation }: WikiMechanicsSectionProps) => {
    const [openMechanics, setOpenMechanics] = useState<ReadonlySet<string>>(new Set());
    const unlocks = villageLevel.unlocks;
    const unlocked = unlocks.filter(unlock => unlock.unlocked);
    const upcoming = unlocks.filter(unlock => !unlock.unlocked);
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
                        <div key={m.key} className={'wiki-building pixel-panel' + (open ? ' receipt-open' : '')}>
                            <button type="button" className="wiki-building-head" aria-expanded={open} onClick={() => toggleMechanic(m.key)}>
                                {m.domikLogic != null
                                    ? <DomikSprite logicName={m.domikLogic} level={3} className="wiki-mech-ico" aria-hidden="true" />
                                    : <MechanicSprite logicName={m.logic} size={24} className="wiki-mech-ico" aria-hidden="true" />}
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
                                    {m.description.split('\n\n').map(paragraph => <p key={paragraph}>{paragraph}</p>)}
                                    {m.key === 'village' && (
                                        <div className="wiki-mechanic-live">
                                            <span className="wiki-mechanic-live-label wiki-village-level">
                                                <MechanicSprite logicName="obzhitost" size={32} className="weather-chip-ico" aria-hidden="true" />
                                                Текущая обжитость: {villageLevel.level}
                                            </span>
                                            <dl className="wiki-res-facts">
                                                <dt>Постройки</dt>
                                                <dd>{villageLevel.buildings} × 1 = {villageLevel.buildings}</dd>
                                                <dt>Жители</dt>
                                                <dd>{villageLevel.residents} × 2 = {villageLevel.residents * 2}</dd>
                                                <dt>Вехи репутации</dt>
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
                                                        {weather.forecast.map(period => {
                                                            const hint = strongestWeatherEffect(period.effects, domikTypes);
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
    );
};

export const Wiki = () => {
    const toast = useToast();
    const [catalog, setCatalog] = useState<Catalog | null>(null);

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
                    villageLevel: state.villageLevel,
                    convoys: state.convoys,
                    village: state.village,
                    villageProfiles: state.villageProfiles,
                    reputation: state.reputation,
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

    const { domikTypes, resourceTypes, receipts, weather, decor, villageLevel, convoys, village, villageProfiles, reputation } = catalog;

    return (
        <div className="wiki">
            <section className="wiki-intro pixel-panel">
                <h1 className="wiki-title">Справочник</h1>
                <p>Domiki – уютная idle-деревня. Заходи на пару минут: строй домики, запускай производства, бери заказы соседей. Ресурсы копятся сами, даже с закрытой вкладкой.</p>
                <p>Ниже – ресурсы, постройки, рецепты и обзор механик. Данные живые, прямо из игры, так что не устаревают.</p>
            </section>

            <WikiResourcesSection resourceTypes={resourceTypes} />

            <WikiBuildingsSection domikTypes={domikTypes} resourceTypes={resourceTypes} receipts={receipts} />

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

            <WikiMechanicsSection villageLevel={villageLevel} weather={weather} decor={decor} domikTypes={domikTypes} convoys={convoys} resourceTypes={resourceTypes}
                village={village} villageProfiles={villageProfiles} reputation={reputation} />
        </div>
    );
};

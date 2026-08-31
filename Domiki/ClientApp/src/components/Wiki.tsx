import { Fragment, useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { ApiError, getGameState } from '../services/api';
import { useToast } from '../services/toastContext';
import { formatDuration } from '../utils/time';
import { domikLore } from '../utils/domikLore';
import { unlockLore } from '../utils/unlockLore';
import { resourceLore } from '../utils/resourceLore';
import { flyoutLeft, flyoutWidth, useFlyoutTop } from '../utils/flyout';
import { weatherEffects } from '../utils/game';
import { profileGenitiveName, profileLore } from '../utils/profileLore';
import type { ConvoyDto, DecorStateDto, DomikTypeDto, NeighborReputationDto, ReceiptDto, ResourceDto, ResourceTypeDto, TolokaStateDto, VillageDto, VillageLevelDto, VillageProfileDto, WeatherStateDto } from '../types/api';
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
    toloka: TolokaStateDto | null;
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
    sectionTitles?: string[];
    /** Постройка, чей спрайт заменяет значок механики: у механик-надстроек своего значка нет. */
    domikLogic?: string;
}

const MECHANICS: Mechanic[] = [
    {
        key: 'village',
        logic: 'obzhitost',
        name: 'Обжитость',
        teaser: 'уровень деревни: что открыто, что впереди',
        sectionTitles: ['Из чего складывается', 'Пороги', 'Артельные избы', 'Переезд в новую долину'],
        description: 'Обжитость – общий уровень развития деревни. Она растёт от построек, коек, репутации и уюта и открывает новые постройки, соседей и механики.\n\nУровни построек дают по 1 очку; вместимость коек – по 2, но в расчёте учитывается не больше 35 трудяг; каждая полная десятка репутации у соседа – ещё 5; уют даёт по 1, но учитывается максимум 50 очков.\n\nКлючевые пороги: Умная артель открывается на 8, уклад деревни – на 20, Изба старосты – на 32, первый переезд – на 350. После этого следующий порог растёт на 50, пока не достигнет 500. Весь путь – что уже открыто и что впереди – расписан ниже.\n\nШестая артельная изба открывается на 60, седьмая – на 110, восьмая – на 175; выше 35 трудяг держать нельзя.\n\nПереезд доступен не чаще раза в 7 суток. Перед переездом не должно быть трудяг в экспедициях, поручениях или происшествиях и собственных активных лотов на ярмарке. Трудяги и чертежи едут с деревней, золота увезёшь не больше 25, а доброе имя у соседей сохранится наполовину.',
    },
    {
        key: 'orders',
        logic: 'orders',
        name: 'Заказы',
        teaser: 'спрос соседей, репутация',
        sectionTitles: ['Как считается объём', 'Спрос, срок и награда'],
        description: 'На доске одновременно 3 заказа. Каждый заказ просит один ресурс, а за полностью сданный объём сосед платит монетами, своей репутацией и иногда золотом – по выпавшему уровню спроса. Полученная репутация открывает его товары, чертежи и другие возможности.\n\nОбъём считают через рыночную стоимость ресурса и производственную мощность: заказ рассчитан примерно на половину выработки двора за свой срок. Мощность дополнительно делится между заказами одного ресурса, а минимум заказа – 2 единицы.\n\nСпрос выпадает наугад и разом задаёт срок, монетный множитель, золото и репутацию. Чем дольше срок, тем щедрее плата: за 4 часа – ×1,5 монет, золота нет, 1 репутация; за 8 часов – ×2, 1 золото, 2 репутации; за сутки – ×3, 2 золота, 4 репутации. От обжитости это не зависит. После выполнения, истечения срока или отказа ячейка остаётся пустой на 30 минут. Отказ добавляет 30 минут к уже запланированному времени пополнения, если оно есть.',
    },
    {
        key: 'friendship',
        logic: 'friendship',
        name: 'Дружба с соседями',
        teaser: 'куда копится доброе имя',
        sectionTitles: ['Место на доске', 'Чего дружба не даёт'],
        description: 'Репутация считается отдельно для каждого соседа. Можно выбрать одного открытого соседа для дружбы – в жеребьёвке новых заказов его имя идёт втрое тяжелее, и на доске он появляется заметно чаще. Смена друга бесплатна и не меняет уже выставленные заказы.\n\nДруг не занимает всю доску: из 3 слотов хотя бы один всегда оставляют заказу другого соседа. Так что и при верном друге остальные соседи с доски не пропадают.\n\nДружба сама по себе не выдаёт награду и не прибавляет репутацию. Она только направляет будущую жеребьёвку заказов; репутация по-прежнему зарабатывается выполнением заказов. Дружить можно только с уже открытым соседом.',
    },
    {
        key: 'profile',
        logic: 'uklad',
        name: 'Уклад деревни',
        teaser: 'сноровка, перенятая у соседа',
        sectionTitles: ['Когда доступен', 'Сочетание бонусов'],
        description: 'Уклад перенимает ремесло одного соседа и сокращает смены в двух его постройках на 15 %. Заречье ускоряет кузницу и каменоломню; Боровое – лесопилку и мастерскую; Каменка – каменоломню и каменотёс; Глинищи – глиняный карьер и гончарню; Дубрава – лесопилку и пекарню.\n\nУклад доступен с обжитости 20 и репутации 10 у выбранного соседа. Платы и штрафа нет, но менять уклад можно не чаще раза в 7 суток. Уклад не добавляет очков обжитости, не увеличивает выход и не создаёт риск хвори.\n\nСкидки ко времени перемножаются: «Долгая привычка» за узелки памяти, черта трудяги, навык профессии до −15 % и уклад −15 % работают разом. Короче 60 % от исходного срока смена не станет. Например, у Работящего с набитой рукой уклад срежет не полные −15 %, а всего −11,8 %.',
    },
    {
        key: 'errands',
        logic: 'errands',
        name: 'Поручения соседей',
        teaser: 'сосед просит не товар, а подмогу',
        sectionTitles: ['Поиск', 'Награда'],
        description: 'С обжитости 10, как доска заполнится, соседи с шансом 20 % присылают ещё и поручение. Оно не заменяет обычный заказ и не занимает один из 3 слотов. Предложение живёт 8 часов, одновременно может быть только одно незавершённое. Отказ или отзыв принятого поручения не штрафует репутацию.\n\nОтряди 1–2 свободных трудяг и выбери одну зацепку: поиск длится 2, 4 или 8 часов и приносит соответственно +3, +5 или +8 репутации. Пока поручение принято, отправленные трудяги заняты и не могут работать в других делах. Досрочное завершение награды не даёт.\n\nЗа доведённое дело платят 10 монет за каждый час работы каждого трудяги плюс репутацию за зацепку. Вдобавок поиски с шансом 20 % приносят основной ресурс соседа: удачливый отряд поднимает этот шанс, а «Везучий» его удваивает.',
    },
    {
        key: 'convoy',
        logic: 'convoy',
        name: 'Обозы соседей',
        teaser: 'докупить сырьё за монеты',
        sectionTitles: ['Кто и что привозит', 'Сколько уступят'],
        description: 'Обоз – мгновенная покупка ресурсов у открытого соседа: не нужны трудяга, постройка или ожидание производства. Цена каждой единицы – ×5 от её рыночной стоимости, поэтому обоз закрывает срочную нехватку, а не заменяет свою цепочку. Монеты и золото обоз не продаёт.\n\nОбоз тронется к тебе с репутации 5, но сперва самого соседа надо открыть обжитостью. Его основной ресурс появляется сразу, второй – на репутации 20, только если у этого соседа задан вторичный ресурс.\n\nСчитают штуками: у каждого соседа за период можно взять 3, а с репутации 40 – 5. Период длится 24 часа от первой покупки у конкретного соседа и считается скользящим, а не по календарным суткам. На прилавке ниже видно, сколько ещё уступят и когда обоз придёт снова.',
    },
    {
        key: 'incidents',
        logic: 'incident',
        name: 'Происшествия',
        teaser: 'истории с зацепками',
        sectionTitles: ['После экспедиции', 'Загадки построек', 'Зацепки и исход'],
        description: 'Происшествия бывают двух видов: задержавшийся после экспедиции трудяга или загадка в собственной постройке. В обоих случаях на доске заводится своя история, и кончается она добром: человек так или иначе вернётся, а если искать – вернётся с находкой.\n\nПосле экспедиции шанс происшествия – 12 %, между такими случаями проходит не меньше 72 часов. Нужны отряд минимум из 2 человек и хотя бы 3 свободных трудяги для поиска. Пропавший вернётся сам через 48 часов без находки; найденный трудяга после возвращения отдыхает 2 часа.\n\nЗагадка постройки доступна с обжитости 10 и появляется после завершённого улучшения, не чаще раза в 96 часов. Для неё нужно минимум 2 свободных трудяги. Особые истории привязаны к постройке: золотой рудник, артельная изба и торговый двор имеют свои сюжеты всегда, каменоломня и кузница – свои отдельные, а глиняный карьер отзывается только в дождь, лесопилка – только в сушь.\n\nНа любое происшествие выбираешь зацепку на 2, 4 или 8 часов и отряжаешь 1–2 свободных трудяг. Чем дольше поиск, тем выше находка: после успеха можно получить ресурс из походной добычи, а при истории с пропавшим – шанс изменить черту героя. Если не искать, происшествие разрешится через 48 часов без находки.',
    },
    {
        key: 'gifts',
        logic: 'gifts',
        name: 'Гостинцы',
        teaser: 'соседи встречают из отлучки',
        sectionTitles: ['Кто придёт и с чем', 'Большой гостинец'],
        description: 'Если деревня была без визита 6 часов или дольше, при возвращении один открытый сосед оставляет гостинец. За один период отсутствия выдаётся только один подарок – долгий простой не превращается в пачку накопленных сундуков.\n\nКто придёт – решает репутация: при 0–24 очках у соседа обычный шанс, при 25–49 он вдвое выше, при 50 и выше – втрое. Обычный подарок содержит основной ресурс соседа или второй с шансом 50 %. Собирают его на 40 по рыночной цене, а с репутации 25 – на 60; штуки округляют вверх, но кладут не меньше 1 и не больше 10.\n\nКаждый 7-й визит приносит большой гостинец – со случайным украшением из тех, что уже открыты; после этого счёт начинается заново. Обычные визиты его наращивают, пропущенные промежутки не сбрасывают.',
    },
    {
        key: 'workers',
        logic: 'workers',
        name: 'Трудяги',
        teaser: 'у трудяг своя жизнь',
        sectionTitles: ['Черты и усталость', 'Вехи трудяг'],
        description: 'Трудяга – не циферка в углу: у каждого своё имя, черта и опыт по профессиям. Вместимость деревни ограничена 35 трудягами и зависит от доступных коек; оставшийся без койки трудяга числится в отходе и работать не может. Свободных двор расставляет сам, но состав любого дела можно набрать и вручную.\n\nЧерты меняют работу: «Проворный» сокращает длительность на 10 %, «Работящий» – на 20 %, «Соня» растягивает её на 25 %, зато не устаёт и хворь его не берёт, «Везучий» вдвое повышает вес редкой добычи и шанс бонуса в поручении, а «Крепкий» защищён от хвори. Навык конкретной профессии даёт до −15 % времени. Усталость появляется после 8 часов работы и обычно требует 2 часов отдыха; торговый двор не накапливает рабочее время, а неустающий трудяга не отдыхает.\n\nВехи открываются с обжитости 8 и приходят не чаще одной за 48 часов на игрока: первая смена, 100 смен, 50 работ в одной постройке, по 25 работ в двух постройках, 10 экспедиций и 30 дней в артельной избе. Вехи не сгорают. «Обычный» трудяга за веху набитой руки получает случайную необычную черту, остальные вехи дают награду или находку.',
    },
    {
        key: 'tavern',
        logic: 'tavern',
        name: 'Корчма',
        teaser: 'обед, котомки и тёплый угол',
        sectionTitles: ['Обед', 'Котомка и тёплый угол'],
        description: 'Корчма открывается на обжитости 16 и стоит 300 монет. У неё три уровня: первый сокращает отдых едой, второй собирает провиант для экспедиций, третий ускоряет выздоровление в тёплом углу.\n\nНа 1 уровне корчмарь сам находит уставшему еду, начиная с той, что подешевле. Берёт только то, что лежит сверх заповеданного, и не тронет еду, которой сказано «не подавать». С едой отдых длится вдвое меньше, без еды – обычные 2 часа; без корчмы автоматического обеда нет.\n\nНа 2 уровне корчма сама собирает котомки в дорогу, если на складе набирается целая корзина; заповеданное и запрет «не подавать» она и тут соблюдает. Выйти можно и налегке – тогда после возвращения трудяги отдыхают обычные 2 часа, а те, кто с котомкой, не отдыхают вовсе. На 3 уровне «тёплый угол» сокращает оставшееся время хвори на 25 %.',
    },
    {
        key: 'elder_house',
        logic: 'elder_house',
        domikLogic: 'elder_house',
        name: 'Изба старосты',
        teaser: 'книга, мера, заповедь',
        sectionTitles: ['Счётная книга', 'Мера наряда', 'Заповедный припас'],
        description: 'Изба старосты ничего не производит – она помогает считать хозяйство. Она открывается на обжитости 32 и может быть только одна. У неё три уровня: книга, мера и заповедный припас.\n\nНа 1 уровне книга показывает за текущие сутки фактические добычу, расходы и отработанное время. Прогноз появляется только после первого часа данных и смотрит на следующие 24 часа. Счёт идёт с того дня, как избу поставили, – задним числом ничего не впишут. Зарубки староста держит 8 суток, а на виду – нынешний день. Монеты и золото в прогноз не берутся.\n\nНа 2 уровне для автоматического повтора можно задать ресурс и произвольную меру его запаса. Наряд продолжает повторяться, пока запас результата ниже меры, и останавливается, когда запас достигает её. Мера переносится на каждый следующий круг; ручной запуск мерой не ограничивается.\n\nНа 3 уровне задаётся заповедный запас отдельно для каждого входного ресурса. Автоповтор и наряд не начнутся, если после списания любого входа запас упадёт ниже резерва. Ручная смена, запущенная игроком, это ограничение игнорирует.',
    },
    {
        key: 'weather',
        logic: 'weather',
        name: 'Погода',
        teaser: 'одна на всю округу, меняет выход',
        sectionTitles: ['Кому прибавит, кому убавит', 'Счёт и значки'],
        description: 'Погода одна на всю деревню и меняется каждые 8 часов. Прогноз показывает сутки вперёд: текущую смену и три следующие. Выпадает погода наугад, но не поровну: ясно – 40 долей, дождь – 25, сушь – 25, мороз – 15, ветер – 15. Одна и та же погода может выпасть подряд.\n\nПогода меняет только выход производства, а не его длительность. Процент фиксируется в момент запуска смены и сохраняется до её завершения. Дождь даёт глиняному карьеру +50 %, полю +25 %, но лесопилке и мельнице −25 %; сушь даёт лесопилке +50 %, каменоломне +25 %, но глиняному карьеру и полю −25 %; мороз даёт кузнице и пекарне +25 %, но каменоломне −25 %; ветер даёт мельнице +50 %, лесопилке +25 %, но кузнице и пекарне −25 %. В ясную погоду не меняется ничего. У всякой постройки, до которой погоде есть дело, найдётся и своя погода, и своя непогода; прочие всегда работают на 100 %.\n\nПрибавка и убавка кладутся целыми единицами, дробь отбрасывается. Смена, что даёт 8 единиц, при четверти даёт 10, при половине – 12, а в непогоду – 6; суточная вместо 24 даёт 36 или 18; хлеб печётся по 4, и мороз доводит его до 5. А часовое дело, у которого выход и так одна единица, погода не берёт вовсе: четверти единицы не бывает, приписать её не к чему – оттого погода и есть дело долгих смен. Погодный процент перемножается с бонусом толоки, дробь отбрасывается уже после этого, а итоговый выход не опускается ниже одной единицы. В карточке дела пишут не процент, а прибавку – на сколько единиц выйдет больше или меньше; когда прибавки нет, о погоде там не поминают. Значок на постройке во дворе остаётся прежним: он метит постройку, а не дело.',
    },
    {
        key: 'illnesses',
        logic: 'ailments',
        name: 'Хвори',
        teaser: 'непогода берёт свою цену',
        sectionTitles: ['Виды и лечение', 'Плащи'],
        description: 'Слечь можно только на смене в постройке, которой погода прибавила выхода, и только с обжитости 15. Платят не за погоду, а за то, что она и вправду дала: на каждую лишнюю единицу выхода – 4 % риска, и они делятся на всех, кто вышел на смену; выше 15 % за смену риск не поднимается. Оттого восьмичасовая смена в одиночку при половинной прибавке даёт прежние 15 %, при четвертной – 8 %, а вчетвером за ту же прибавку – 4 %. Не дала погода ни одной лишней единицы – нет и хвори: часовые дела с выходом в одну единицу проходят без риска. Бонус толоки в счёт не идёт: риск меряют по одной погоде. Шанс и вид хвори фиксируются при старте смены, а проверка происходит при её завершении. В ясную погоду, при погодном убытке и там, где погода ничего не меняет, хворь не грозит.\n\nДождь приносит простуду, сушь – жар, мороз – озноб, ветер – прострел. Базовое время хвори – 8 часов: уют сокращает его максимум наполовину, а «тёплый угол» корчмы 3 уровня сокращает уже оставшееся время ещё на 25 %. Одновременно могут болеть не более двух трудяг, после выздоровления действует иммунитет 24 часа. «Соня» и «Крепкий» защищают от хвори.\n\nДождь, мороз и ветер – та непогода, от которой плащ спасает: когда смене прибавляет одна из них, каждому, кого может свалить хворь, со склада откладывают плащ. Он вдвое снижает риск, но итоговый шанс не опускается ниже 2 %. От жара в сушь плащ не защищает. Отложенные плащи со склада уже сняты; если на всех не хватило, укрывают первых по списку. Одного плаща хватает на 50 смен, потом он истлевает.',
    },
    {
        key: 'blueprints',
        logic: 'blueprints',
        name: 'Чертежи',
        teaser: 'открывают постройки и ремёсла',
        sectionTitles: ['Откуда берутся', 'Что открывают'],
        description: 'Чертёж – это доступ к новой постройке или к новому ремеслу, а не расходный материал.\n\nКупить чертёж нельзя: он приходит сам за репутацию у нужного соседа или выпадает редкой добычей в походе.\n\nЧертежи построек: мастерская – Боровое, репутация 30; каменотёс – Каменка, 20; гончарня – Глинищи, 15; пекарня – Дубрава, 25.\n\nЧертежи ремесла открывают одно дело в уже поставленной постройке: кайло – Каменка, репутация 40, куётся в кузнице с третьего уровня; клещи – Глинищи, 50, куются с четвёртого. Без чертежа дело видно в карточке смены, но под замком.\n\nПосле получения чертёж не тратится: постройку можно покупать и улучшать по её обычным требованиям, а ремесло – повторять сколько угодно.',
    },
    {
        key: 'expeditions',
        logic: 'expeditions',
        name: 'Экспедиции',
        teaser: 'поход за редкостями',
        sectionTitles: ['Виды походов', 'Припасы в дорогу', 'Добыча'],
        description: 'Экспедиции открывает Сторожка: на каком она уровне, столько походов и идёт разом. Отряд набираешь ровно по размеру, и все, кого берёшь, должны быть свободны.\n\nЕсть три вида: «Пешая вылазка» – 2 часа, 1 трудяга, без золота и снаряжения, один жребий на добычу; «Ближняя вылазка» – 4 часа, 2 трудяги, 1 золото, один жребий и 2 инструмента обязательно; «Дальний поход» – 24 часа, 5 трудяг, 2 золота, три жребия и 6 инструментов обязательно. Инструменты списываются при старте.\n\nКорчма 2 уровня сама добавляет в котомку самую дешёвую разрешённую еду целой корзиной, если её хватает с учётом запасов и запретов; вручную выбрать хлеб или сыр нельзя. Без котомки поход всё равно тронется. Кто вышел с котомкой, после похода не отдыхает; кто налегке – отдыхает обычные 2 часа, если только усталость его вообще берёт.\n\nДобыча выбирается случайно по весам. «Везучий» увеличивает вес редких наград вдвое, а после 8 завершённых экспедиций без редкой добычи следующая получает редкую награду гарантированно. Редкой добычей бывают ресурсы и инструменты, декор, чертежи и улучшение черты уже участвовавшего обычного трудяги; новый трудяга не создаётся.',
    },
    {
        key: 'market',
        logic: 'market',
        name: 'Ярмарка',
        teaser: 'обмен лотами между деревнями',
        sectionTitles: ['Лоты', 'Комиссия и места на прилавке', 'Залог и возврат'],
        description: 'Ярмарка открывается на обжитости 20 после покупки Торгового двора и торгует сама по себе: выставленный лот может принять другой игрок, пока тебя нет. Трудяги для торговли не нужны. На прилавок кладут только ресурсы: ни чертёж, ни трудягу, ни постройку в лот не положишь.\n\nЛот задаёт отдаваемый ресурс и желаемую сторону обмена. В заявке «Купить» отдаётся только золото, а получить золото или монеты такой заявкой нельзя. В обычной продаже можно отдавать любой существующий ресурс, включая золото, если второй тип ресурса отличается.\n\nКомиссия 1 уровня Торгового двора – 8 %, каждый следующий уровень уменьшает её на 1,25 процентного пункта, нижний предел – 3 %. Минимальная комиссия – 2 монеты. Мест на прилавке на одно больше, чем уровень Торгового двора; заявки и продажи делят их между собой, а каждый лот стоит на прилавке 24 часа.\n\nКак только лот выставлен, отдаваемый товар и комиссия ложатся в залог. Лот приняли – товар переходит из рук в руки. Снял сам или вышел срок – товар из залога вернётся, а комиссия нет. И чтобы выставить своё, и чтобы принять чужое, нужен собственный Торговый двор.',
    },
    {
        key: 'toloka',
        logic: 'toloka',
        name: 'Толока',
        teaser: 'общий проект деревни',
        sectionTitles: ['Корзина', 'Что даёт каждая толока', 'Праздник толоки', 'Голосование'],
        description: 'Толока открывается на обжитости 20, когда во дворе встанет Сборня. Одновременно существует один общий проект для всех игроков. Вкладываются по позициям корзины: сколько приняли, столько со склада и уйдёт, а лишнего сверх недобора не возьмут.\n\nДля моста через реку, общего амбара и гончарной печи базовая позиция требует 800 единиц ресурса. Торговый караван собирается из трёх позиций: 150 кирпичей, 150 досок и 50 инструментов. Следующая корзина растёт: каждую позицию умножают на то, сколько разных игроков вложилось в прошлую завершённую толоку, но меньше единицы множитель не бывает.\n\n«Мост через реку» требует камень и даёт участникам +40 % к монетной награде заказов. «Общий амбар» требует дерево и даёт +40 % к выходу глиняного карьера, лесопилки и поля. «Гончарная печь» требует глину и даёт +40 % к выходу кузницы, мастерской, каменотёса, гончарни, мельницы и пекарни. «Торговый караван» требует кирпичи, доски и инструменты и даёт +40 % к результату продажи на ярмарке.\n\nПраздник толоки достаётся только тому, кто вложился. Он длится 6 + 2 × уровень Сборни часов, то есть 8 часов на 1 уровне; повторно завершённый такой же проект заводит праздник заново. Следующая толока появляется сразу после завершения текущей.\n\nГолосовать можно и без вклада, а голос можно изменить до завершения проекта. Побеждает проект с большим числом голосов, при равенстве выбор случаен; если голосов нет, проект выбирается случайно по весам. Вклад и голосование относятся только к текущему проекту.',
    },
    {
        key: 'decor',
        logic: 'decor',
        name: 'Декор',
        teaser: 'уют ускоряет отдых',
        sectionTitles: ['Обычный декор', 'Цепочка мастеров', 'Особые находки'],
        description: 'Уют деревни – это все расставленные украшения, сложенные вместе: сколько предметов каждого вида, столько раз и считают его очки. Уют ускоряет отдых, сокращает длительность хвори и входит в формулу обжитости. Сам счётчик уюта может быть выше 50, но в этих формулах учитывается максимум 50. Обычных украшений можно ставить сколько угодно.\n\nОчки обычного декора: забор +2, клумба +3, скамья +4, сад +5, фонарь +5, фонтан +8, кирпичная арка +10. Кирпичная арка требует репутацию 30 у Заречья; остальные обычные предметы покупаются по своим требованиям и ценам, которые показаны в каталоге ниже.\n\nМастерскую цепочку ставят по порядку и по одному предмету каждого вида: резная калитка даёт +2 уюта и стоит 600 монет, затем колодец-журавль за 4 000, беседка за 25 000 и пруд с карасями за 70 000. Последние три предмета уюта не добавляют. Полная цепочка стоит 99 600 монет.\n\nПоходный идол даёт +3, штандарт странников – +6; это особый непокупаемый декор из редких наград экспедиции. Текущий список предметов, владение и цены показаны ниже.',
    },
];

interface WikiArticleProps {
    mechanic: Mechanic;
}

const WikiArticle = ({ mechanic }: WikiArticleProps) => {
    const paragraphs = mechanic.description.split('\n\n');
    const [lead, ...sections] = paragraphs;

    return (
        <div className="wiki-article">
            <p className="wiki-article-lead">{lead}</p>
            {sections.length > 0 && (
                <div className="wiki-article-grid">
                    {sections.map((paragraph, index) => (
                        <div key={`${mechanic.key}-${index}`} className="wiki-article-card">
                            <h3>{mechanic.sectionTitles?.[index] ?? 'Подробнее'}</h3>
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
    toloka: TolokaStateDto | null;
    resourceTypes: ResourceTypeDto[];
    village: VillageDto;
    villageProfiles: VillageProfileDto[];
    reputation: NeighborReputationDto[];
}

const WikiMechanicsSection = ({ villageLevel, weather, decor, domikTypes, convoys, toloka, resourceTypes, village, villageProfiles, reputation }: WikiMechanicsSectionProps) => {
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
                                    <WikiArticle mechanic={m} />
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
                                                            {type.neighborName != null && <span className="wiki-decor-meta">{type.neighborName}: репутация {reputationPoints}/{type.reputationThreshold}</span>}
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
                    toloka: state.toloka,
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

    const { domikTypes, resourceTypes, receipts, weather, decor, villageLevel, convoys, toloka, village, villageProfiles, reputation } = catalog;

    return (
        <div className="wiki">
            <section className="wiki-intro pixel-panel">
                <h1 className="wiki-title">Справочник</h1>
                <p>Domiki – уютная idle-деревня. Заходи на пару минут: строй домики, запускай производства, бери заказы соседей. Ресурсы копятся сами, даже с закрытой вкладкой.</p>
                <p>Ниже – ресурсы, постройки, рецепты и обзор механик. Данные загружаются из текущего состояния игры при открытии справочника.</p>
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

            <WikiMechanicsSection villageLevel={villageLevel} weather={weather} decor={decor} domikTypes={domikTypes} convoys={convoys} toloka={toloka} resourceTypes={resourceTypes}
                village={village} villageProfiles={villageProfiles} reputation={reputation} />
        </div>
    );
};

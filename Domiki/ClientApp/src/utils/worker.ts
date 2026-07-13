import type { DomikTypeDto, WorkerDto, WorkerSkillDto } from '../types/api';

export const SKILLED_BONUS_THRESHOLD = 10;
export const MASTER_BONUS_THRESHOLD = 25;

export type WorkerTier = 'none' | 'novice' | 'skilled' | 'master';

export function isSkilledWorker(worker: WorkerDto): boolean {
    return worker.skills.some(s => s.bonusPercent >= SKILLED_BONUS_THRESHOLD);
}

function tierOf(bonus: number): WorkerTier {
    if (bonus >= MASTER_BONUS_THRESHOLD) {
        return 'master';
    }
    if (bonus >= SKILLED_BONUS_THRESHOLD) {
        return 'skilled';
    }
    return 'novice';
}

export function rankedSkills(worker: WorkerDto): WorkerSkillDto[] {
    return worker.skills
        .filter(s => s.bonusPercent > 0)
        .sort((a, b) => b.bonusPercent - a.bonusPercent || a.domikTypeId - b.domikTypeId);
}

interface Craft {
    m: string;
    f: string;
    flavors: string[];
}

const CRAFTS: Record<string, Craft> = {
    bakery: { m: 'пекарь', f: 'пекарка', flavors: ['Тесто всходит по первому слову.', 'По запаху хлеба всю деревню будит.', 'Корку румянит золотистее заката.'] },
    barracks: { m: 'дружинник', f: 'дружинница', flavors: ['За порядком в деревне глядит в оба.', 'Спит вполглаза, а слышит за версту.', 'На тревогу первым к воротам.'] },
    clay_mine: { m: 'глинокоп', f: 'глинокопка', flavors: ['Глину чует носом, где пожирнее.', 'По локоть в глине с утра до ночи.', 'Пласт побогаче найдёт вслепую.'] },
    field: { m: 'пахарь', f: 'пахарка', flavors: ['Борозду ведёт ровнее натянутой нити.', 'С зарёй в поле, дотемна в поле.', 'Землю читает как раскрытую книгу.'] },
    fair: { m: 'зазывала', f: 'зазывала', flavors: ['На ярмарке слыхать от края до края.', 'Зазвонистей ярмарочного колокола.', 'Толпу сгоняет одним говорком.'] },
    forge: { m: 'кузнец', f: 'кузнечиха', flavors: ['Ради звона наковальни встаёт до петухов.', 'Искры летят, а работа поёт.', 'Молот держит крепче, чем ложку.'] },
    gathering: { m: 'собиратель', f: 'собирательница', flavors: ['Из леса без добычи не выходит.', 'Каждый гриб в округе знает в лицо.', 'Где пройдёт, там корзина полна.'] },
    gold_mine: { m: 'старатель', f: 'старательница', flavors: ['Золотую жилу нюхом находит.', 'Песок моет с рассвета, блеск в глазах.', 'Крупинку не упустит из лотка.'] },
    lumber_mill: { m: 'пильщик', f: 'пильщица', flavors: ['Бревно распустит, глазом не моргнув.', 'Опилки по колено, а брус к брусу.', 'Ствол валит точно куда метит.'] },
    market: { m: 'торговец', f: 'торговка', flavors: ['Языком снег зимой продаст.', 'Сдачу считает быстрее счётов.', 'Покупателя видит ещё за углом.'] },
    market_yard: { m: 'купец', f: 'купчиха', flavors: ['Барыш считает даже во сне.', 'Из гроша выторгует рубль.', 'Обоз снарядит, пока другие спят.'] },
    mill: { m: 'мельник', f: 'мельничиха', flavors: ['Жернова так и поют под руками.', 'Муку мелет тоньше первого снега.', 'По ветру ставит крылья с одного взгляда.'] },
    pottery: { m: 'гончар', f: 'гончариха', flavors: ['Из кома глины лепит загляденье.', 'Круг под руками словно живой.', 'Кувшин выведет, не глядя.'] },
    scout_hut: { m: 'следопыт', f: 'следопытка', flavors: ['Тропу видит там, где другие стену.', 'След читает по одной примятой травинке.', 'В тумане дорогу найдёт вслепую.'] },
    stone_mine: { m: 'каменолом', f: 'каменоломка', flavors: ['Скалу расколет ровно там, где надо.', 'Молот о камень с зари гремит.', 'Глыбу от глыбы отличит на слух.'] },
    stonecutter: { m: 'камнерез', f: 'камнерезка', flavors: ['Из глыбы вытешет что скажешь.', 'Резцом ведёт линию как по струне.', 'Камень слушается каждого удара.'] },
    workshop: { m: 'мастеровой', f: 'мастеровая', flavors: ['За что ни возьмётся, всё ладится.', 'Инструмент под руку сам просится.', 'Из ничего смастерит нужное.'] },
};

const FEMALE_NAMES = new Set([
    'Варвара', 'Дарья', 'Злата', 'Кира', 'Лада', 'Нина', 'Пелагея', 'Тая',
    'Ульяна', 'Ярина', 'Агата', 'Велена', 'Есения', 'Лукерья', 'Марта', 'Прасковья',
]);

const NOVICE_FLAVORS = [
    'Пока приглядывается, но искра есть.',
    'Учится на ходу и не ноет.',
    'Руки ещё дрожат, а глаза горят.',
    'Портит заготовки, но реже день ото дня.',
];

function tierWord(bonus: number, female: boolean): string {
    if (bonus >= 25) {
        return female ? 'знатная' : 'знатный';
    }
    if (bonus >= 10) {
        return female ? 'умелая' : 'умелый';
    }
    return female ? 'начинающая' : 'начинающий';
}

export interface WorkerCraft {
    title: string;
    primaryTitle: string;
    flavor: string;
    tier: WorkerTier;
}

export function describeWorkerParts(worker: WorkerDto, domikTypes: DomikTypeDto[]): WorkerCraft {
    const female = FEMALE_NAMES.has(worker.name);
    const skilled = rankedSkills(worker);
    const top = skilled[0];
    if (top == null) {
        return { title: '', primaryTitle: female ? 'ученица' : 'ученик', flavor: 'Пока без ремесла, зато рвётся учиться.', tier: 'none' };
    }

    const describePart = (skill: WorkerSkillDto): string => {
        const type = domikTypes.find(t => t.id === skill.domikTypeId);
        const craft = type != null ? CRAFTS[type.logicName] : undefined;
        const noun = craft != null ? (female ? craft.f : craft.m) : (female ? 'работница' : 'работник');
        return `${tierWord(skill.bonusPercent, female)} ${noun}`;
    };
    const capitalise = (text: string) => text.replace(/^./, c => c.toUpperCase());

    const topType = domikTypes.find(t => t.id === top.domikTypeId);
    const topCraft = topType != null ? CRAFTS[topType.logicName] : undefined;
    const flavors = top.bonusPercent >= SKILLED_BONUS_THRESHOLD && topCraft != null ? topCraft.flavors : NOVICE_FLAVORS;
    const flavor = flavors[worker.id % flavors.length] ?? flavors[0] ?? '';

    return {
        title: capitalise(skilled.map(describePart).join(', ')),
        primaryTitle: capitalise(describePart(top)),
        flavor,
        tier: tierOf(top.bonusPercent),
    };
}

export function describeWorker(worker: WorkerDto, domikTypes: DomikTypeDto[]): string {
    const { title, flavor } = describeWorkerParts(worker, domikTypes);
    return title === '' ? flavor : `${title}. ${flavor}`;
}

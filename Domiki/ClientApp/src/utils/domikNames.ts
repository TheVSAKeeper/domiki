const FALLBACK_DOMIK_NAME_POOL = [
    'Жёлудь', 'Пенёк', 'Опушка', 'Заря', 'Ручеёк', 'Шишка', 'Клевер', 'Ландыш',
    'Вереск', 'Полынь', 'Рябина', 'Калина', 'Орешек', 'Светлячок', 'Мотылёк', 'Зяблик',
    'Скворец', 'Барсук', 'Ёжик', 'Грибок', 'Лукошко', 'Туесок', 'Овражек', 'Пригорок',
    'Омут', 'Вьюнок', 'Первоцвет',
];

const DOMIK_NAME_POOLS: Record<string, string[]> = {
    bakery: ['Каравай', 'Опара', 'Печка', 'Мякиш', 'Закваска', 'Сдоба'],
    forge: ['Уголёк', 'Горнило', 'Наковальня', 'Искра', 'Подкова', 'Молот'],
    lumber_mill: ['Смолка', 'Тесина', 'Горбыль', 'Поленница', 'Щепка', 'Сучок'],
    clay_mine: ['Комок', 'Копанка', 'Глинка', 'Замес', 'Суглинок', 'Мазанка'],
    stone_mine: ['Валун', 'Осколок', 'Гранит', 'Кремень', 'Булыжник', 'Щебень'],
    gold_mine: ['Крупица', 'Жила', 'Самородок', 'Блёстка', 'Россыпь', 'Золотинка'],
    pottery: ['Черепок', 'Обжиг', 'Кувшин', 'Полива', 'Круг', 'Горшок'],
    mill: ['Жёрнов', 'Мучица', 'Помол', 'Крыло', 'Сусек', 'Зёрнышко'],
    field: ['Колосок', 'Борозда', 'Сноп', 'Роса', 'Всходы', 'Межа'],
    workshop: ['Стружка', 'Верстак', 'Долото', 'Киянка', 'Фуганок', 'Заноза'],
    stonecutter: ['Грань', 'Зубило', 'Скол', 'Глыба', 'Резец', 'Крошка'],
    market_yard: ['Весы', 'Прилавок', 'Мошна', 'Ряды', 'Задаток', 'Барыш'],
    market: ['Аршин', 'Короб', 'Ларь', 'Пятак', 'Меновка', 'Гостинец'],
    barracks: ['Лежанка', 'Полати', 'Печурка', 'Артель', 'Ночлег', 'Завалинка'],
    scout_hut: ['Тропка', 'Дозор', 'Зарубка', 'Привал', 'Веха', 'Котомка'],
    gathering: ['Сходка', 'Вече', 'Посиделки', 'Помочь', 'Ватага', 'Гурьба'],
    fair: ['Балаган', 'Пряник', 'Шатёр', 'Зазывала', 'Карусель', 'Гомон'],
};

export const domikThemedName = (baseName: string, logicName: string, ordinal: number): string => {
    const pool = DOMIK_NAME_POOLS[logicName] ?? FALLBACK_DOMIK_NAME_POOL;

    return ordinal > pool.length
        ? `${baseName} ${ordinal}`
        : `${baseName} «${pool[ordinal - 1]}»`;
};

export type DomikNamer = (typeId: number, id: number, typeName: string, logicName: string) => string;

export const buildDomikNamer = (domiks: { id: number; typeId: number }[]): DomikNamer => {
    const idsByType = new Map<number, number[]>();
    for (const domik of domiks) {
        const ids = idsByType.get(domik.typeId) ?? [];
        ids.push(domik.id);
        idsByType.set(domik.typeId, ids);
    }

    const ordinalById = new Map<number, number>();
    const countByType = new Map<number, number>();
    for (const [typeId, ids] of idsByType) {
        const sortedIds = [...ids].sort((a, b) => a - b);
        countByType.set(typeId, sortedIds.length);
        sortedIds.forEach((id, index) => ordinalById.set(id, index + 1));
    }

    return (typeId, id, typeName, logicName) =>
        (countByType.get(typeId) ?? 1) > 1
            ? domikThemedName(typeName, logicName, ordinalById.get(id) ?? 1)
            : typeName;
};

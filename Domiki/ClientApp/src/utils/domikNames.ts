const DOMIK_NAME_POOL = [
    'Жёлудь', 'Пенёк', 'Уголёк', 'Колосок', 'Роса', 'Опушка', 'Заря', 'Ручеёк',
    'Шишка', 'Смолка', 'Клевер', 'Ландыш', 'Вереск', 'Полынь', 'Рябина', 'Калина',
    'Орешек', 'Светлячок', 'Мотылёк', 'Зяблик', 'Скворец', 'Барсук', 'Ёжик', 'Грибок',
    'Лукошко', 'Туесок', 'Овражек', 'Пригорок', 'Валун', 'Омут', 'Вьюнок', 'Первоцвет',
];

export const domikThemedName = (baseName: string, typeId: number, ordinal: number): string =>
    ordinal > DOMIK_NAME_POOL.length
        ? `${baseName} ${ordinal}`
        : `${baseName} «${DOMIK_NAME_POOL[(typeId * 7 + ordinal - 1) % DOMIK_NAME_POOL.length]}»`;

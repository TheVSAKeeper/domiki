export const GUESTBOOK_PHRASES: Record<number, string> = {
    1: 'Славная у вас деревня!',
    2: 'Уютно, как дома',
    3: 'Растёте на глазах!',
    4: 'Поклон добрым соседям',
    5: 'Есть чему поучиться',
    6: 'Загляну ещё непременно',
    7: 'Мира вашему двору!',
    8: 'Хороши домики, хозяева!',
};

export const guestbookPhraseText = (phraseId: number): string => GUESTBOOK_PHRASES[phraseId] ?? '…';

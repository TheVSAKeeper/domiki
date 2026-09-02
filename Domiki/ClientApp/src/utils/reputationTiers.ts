export interface ReputationTier {
    from: number;
    name: string;
    gloss: string;
}

const lowestTier: ReputationTier = { from: 0, name: 'чужой', gloss: 'Сосед шлёт весточки, но своего не везёт: доброе имя зарабатывают заказами.' };

export const reputationTiers: ReputationTier[] = [
    { from: 50, name: 'в почёте', gloss: 'Дальше расти некуда: сосед идёт с гостинцем к тебе охотнее всех.' },
    { from: 40, name: 'родня', gloss: 'Обоз приходит щедрый: за сутки сосед отпускает больше прежнего.' },
    { from: 25, name: 'свой', gloss: 'Сосед заглядывает во двор чаще прежнего, да и гостинцы кладёт щедрее.' },
    { from: 15, name: 'в доверии', gloss: 'Сосед начинает делиться заветным, и в обозе появляется второй товар.' },
    { from: 5, name: 'знакомец', gloss: 'Обоз уже ходит к твоему двору: сосед продаёт свой товар за монеты.' },
    lowestTier,
];

export const reputationTier = (points: number): ReputationTier =>
    reputationTiers.find(tier => points >= tier.from) ?? lowestTier;

export const reputationTierAhead = (points: number, reward: number): ReputationTier | null => {
    const now = reputationTier(points);
    const next = reputationTier(points + reward);
    return next.from > now.from ? next : null;
};

export const termArticles: Record<string, string> = {
    obzhitost: 'village',
    elder_order: 'elder_house',
    naryad: 'elder_house',
    untouched_deposits: 'zeal',
    worker_away: 'workers',
    worker_missing: 'workers',
    worker_search: 'workers',
    worker_domik_search: 'incidents',
    toloka_feast: 'toloka',
    clue: 'incidents',
};

export const wikiArticleHref = (article: string) => `/wiki?article=${encodeURIComponent(article)}`;

export const wikiBuildingHref = (logicName: string) => `/wiki?building=${encodeURIComponent(logicName)}`;

export const wikiArticleAnchor = (article: string) => `wiki-article-${article}`;

export const wikiBuildingAnchor = (logicName: string) => `wiki-building-${logicName}`;

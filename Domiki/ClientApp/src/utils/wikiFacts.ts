const TOKEN = /\{([a-zA-Z][a-zA-Z0-9_]*)\}/g;

export const factTokens = (text: string): string[] => [...text.matchAll(TOKEN)].flatMap(match => match[1] ?? []);

export const fillFacts = (text: string, facts: Readonly<Record<string, string>>): string =>
    text.replace(TOKEN, (whole, key: string) => facts[key] ?? whole);

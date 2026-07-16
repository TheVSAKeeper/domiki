export const withStableKeys = <T>(items: T[], baseKey: (item: T) => string): { key: string; item: T }[] => {
    const seen = new Map<string, number>();
    return items.map(item => {
        const base = baseKey(item);
        const count = seen.get(base) ?? 0;
        seen.set(base, count + 1);
        return { key: count === 0 ? base : `${base}~${String(count)}`, item };
    });
};

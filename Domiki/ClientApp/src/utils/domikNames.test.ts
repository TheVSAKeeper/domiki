import { describe, expect, it } from 'vitest';
import { domikThemedName } from './domikNames';

describe('domikThemedName', () => {
    it('gives 32 distinct themed names for ordinals 1..32 of one typeId', () => {
        const names = Array.from({ length: 32 }, (_, index) => domikThemedName('Барак', 1, index + 1));
        expect(new Set(names).size).toBe(32);

        for (const name of names) {
            expect(name).toMatch(/^Барак «.+»$/);
        }
    });

    it.each([
        [1, 1],
        [1, 5],
        [3, 12],
        [7, 32],
    ])('is deterministic for typeId=%i, ordinal=%i', (typeId, ordinal) => {
        const first = domikThemedName('Барак', typeId, ordinal);
        const second = domikThemedName('Барак', typeId, ordinal);
        expect(first).toBe(second);
    });

    it('gives different names for different typeIds at the same ordinal', () => {
        const names = Array.from({ length: 20 }, (_, index) => domikThemedName('Барак', index + 1, 1));
        expect(new Set(names).size).toBeGreaterThan(1);
    });

    it('falls back to a numbered name past the pool size', () => {
        expect(domikThemedName('Барак', 1, 33)).toBe('Барак 33');
    });
});

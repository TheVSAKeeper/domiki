import { describe, expect, it } from 'vitest';
import { buildDomikNamer, domikThemedName } from './domikNames';

describe('domikThemedName', () => {
    it('selects a deterministic themed name', () => {
        expect(domikThemedName('Пекарня', 'bakery', 2)).toBe('Пекарня «Опара»');
        expect(domikThemedName('Пекарня', 'bakery', 2)).toBe('Пекарня «Опара»');
    });

    it('falls back to a numbered name past the themed pool size', () => {
        expect(domikThemedName('Пекарня', 'bakery', 7)).toBe('Пекарня 7');
    });

    it('uses the fallback pool for an unknown logic name', () => {
        expect(domikThemedName('Домик', 'unknown', 1)).toBe('Домик «Жёлудь»');
    });
});

describe('buildDomikNamer', () => {
    it('keeps the plain type name when the type has a single building', () => {
        const name = buildDomikNamer([{ id: 5, typeId: 3 }]);
        expect(name(3, 5, 'Кузница', 'forge')).toBe('Кузница');
    });

    it('themes duplicates by ascending id ordinal', () => {
        const name = buildDomikNamer([{ id: 40, typeId: 2 }, { id: 10, typeId: 2 }]);
        expect(name(2, 10, 'Золотой рудник', 'gold_mine')).toBe('Золотой рудник «Крупица»');
        expect(name(2, 40, 'Золотой рудник', 'gold_mine')).toBe('Золотой рудник «Жила»');
    });
});

import { describe, expect, it } from 'vitest';
import { domikThemedName } from './domikNames';

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

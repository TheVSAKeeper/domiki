import { describe, expect, it } from 'vitest';
import { CHANGELOG } from './changelog';

describe('CHANGELOG', () => {
    it('id-шники строго возрастают и не повторяются', () => {
        CHANGELOG.slice(1).forEach((entry, index) => {
            expect(entry.id).toBeGreaterThan(CHANGELOG[index]?.id ?? Infinity);
        });
    });

    it('дата каждого выпуска валидна', () => {
        for (const entry of CHANGELOG) {
            expect(Number.isNaN(new Date(entry.date).getTime())).toBe(false);
        }
    });

    it('у каждого выпуска есть хотя бы один пункт', () => {
        for (const entry of CHANGELOG) {
            expect(entry.items.length).toBeGreaterThan(0);
        }
    });
});

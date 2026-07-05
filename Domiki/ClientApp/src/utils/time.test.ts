import { describe, expect, it } from 'vitest';
import { formatDuration, remainingSeconds } from './time';

describe('formatDuration', () => {
    it.each<[number, string]>([
        [0, '0с'],
        [5, '5с'],
        [45, '45с'],
        [30.7, '31с'],
        [65, '1м 5с'],
        [600, '10м'],
        [3600, '1ч'],
        [18000, '5ч'],
        [3661, '1ч 1м 1с'],
        [28800, '8ч'],
        [90000, '1д 1ч'],
        [90061, '1д 1ч 1м 1с'],
    ])('formatDuration(%i) -> %s', (totalSeconds, expected) => {
        expect(formatDuration(totalSeconds)).toBe(expected);
    });
});

describe('remainingSeconds', () => {
    it('returns positive seconds when finishDate is in the future', () => {
        const now = Date.parse('2026-01-01T00:00:00.000Z');
        const finishDate = '2026-01-01T00:00:10.000Z';
        expect(remainingSeconds(finishDate, now)).toBe(10);
    });

    it('returns negative seconds when finishDate is in the past', () => {
        const now = Date.parse('2026-01-01T00:00:10.000Z');
        const finishDate = '2026-01-01T00:00:00.000Z';
        expect(remainingSeconds(finishDate, now)).toBe(-10);
    });
});

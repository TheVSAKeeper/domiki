import { describe, expect, it } from 'vitest';
import { formatDuration, remainingSeconds } from './time';

describe('formatDuration', () => {
    it.each<[number, string]>([
        [0, '00с '],
        [5, '05с '],
        [9, '09с '],
        [45, '45с '],
        [30.7, '31с '],
        [3.4, '03с '],
        [65, '01м 05с '],
        [600, '10м 00с '],
        [18000, '05ч 00м 00с '],
        [3661, '01ч 01м 01с '],
        [90061, '01д 01ч 01м '],
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

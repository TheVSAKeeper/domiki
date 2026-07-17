import { describe, expect, it } from 'vitest';
import { pickGiftText } from './giftTexts';

describe('pickGiftText', () => {
    it('returns the same note for the same seed', () => {
        expect(pickGiftText(2, false, '2026-07-10T00:06:00Z')).toBe(pickGiftText(2, false, '2026-07-10T00:06:00Z'));
    });

    it('uses the fallback pool for an unknown neighbor', () => {
        expect(pickGiftText(999, false, 'abc')).toBe('Кто-то из соседей заглядывал, пока вас не было, – оставил гостинец у ворот.');
    });
});

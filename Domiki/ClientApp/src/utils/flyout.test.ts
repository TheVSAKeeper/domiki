import { describe, expect, it } from 'vitest';
import { flyoutLeft, flyoutTop, flyoutWidth } from './flyout';

const anchor = (top: number, height: number) => ({ top, bottom: top + height });

describe('flyoutTop', () => {
    it('ставит панель под якорем, когда снизу есть место', () => {
        expect(flyoutTop(anchor(100, 40), 200)).toBe(146);
    });

    it('переносит панель над якорем, когда снизу не помещается', () => {
        const top = window.innerHeight - 100;
        expect(flyoutTop(anchor(top, 40), 200)).toBe(top - 206);
    });

    it('прижимает панель к верхнему краю, когда не помещается ни сверху, ни снизу', () => {
        expect(flyoutTop(anchor(10, 20), window.innerHeight + 100)).toBe(8);
    });
});

describe('flyoutLeft', () => {
    it.each([
        [-50, 8],
        [40, 40],
        [window.innerWidth - 10, window.innerWidth - 268],
    ])('якорь на %i даёт левый край %i', (anchorLeft, expected) => {
        expect(flyoutLeft(anchorLeft, 260)).toBe(expected);
    });
});

describe('flyoutWidth', () => {
    it('не даёт панели вылезти за оба края узкого экрана', () => {
        expect(flyoutWidth(window.innerWidth + 100)).toBe(window.innerWidth - 16);
    });
});

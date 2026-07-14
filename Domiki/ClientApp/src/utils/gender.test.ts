import { describe, expect, it } from 'vitest';
import { genderForm, traitLabel } from './gender';

describe('genderForm', () => {
    it.each([
        [2, 'b'],
        [1, 'a'],
        [0, 'a'],
        [undefined, 'a'],
    ])('gender %s resolves to %s', (gender, expected) => {
        expect(genderForm(gender, 'a', 'b')).toBe(expected);
    });
});

describe('traitLabel', () => {
    it.each([
        ['diligent', 'Работящий', 2, 'Работящая'],
        ['diligent', 'Работящий', 1, 'Работящий'],
        ['unknown', 'X', 2, 'X'],
        ['sonya', 'Соня', 2, 'Соня'],
    ])('logicName %s, fallback %s, gender %s resolves to %s', (logicName, fallback, gender, expected) => {
        expect(traitLabel(logicName, fallback, gender)).toBe(expected);
    });
});

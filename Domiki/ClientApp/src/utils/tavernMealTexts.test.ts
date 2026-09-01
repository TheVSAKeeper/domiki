import { describe, expect, it } from 'vitest';
import { getWorkerMealText } from './tavernMealTexts';

describe('getWorkerMealText', () => {
    it.each([
        [2, 'Аксинья пообедала и ушла отдыхать'],
        [1, 'Аким пообедал и ушёл отдыхать'],
    ])('variant 2 with gender %s picks the matching pronouns', (gender, expected) => {
        const name = gender === 2 ? 'Аксинья' : 'Аким';
        expect(getWorkerMealText({ count: 1, workerName: name, workerGender: gender, variant: 2, reason: null }, 'хлеб')).toBe(expected);
    });

    it('substitutes {еда} with the lowercased resource name', () => {
        expect(getWorkerMealText({ count: 1, workerName: 'Аким', workerGender: 1, variant: 1, reason: null }, 'сыр'))
            .toBe('Аким за столом: сыр и кружка кваса');
    });

    it('merges a real meal for count > 1, ignoring variant', () => {
        expect(getWorkerMealText({ count: 3, workerName: null, workerGender: null, variant: 5, reason: null }, 'хлеб'))
            .toBe('Обед в корчме ×3');
    });

    it.each([
        ['forbidden', 1, 'Аким ушёл отдыхать без обеда – на складе только заповедное'],
        ['empty', 1, 'Котёл пустой – Аким отдыхает полный срок'],
        ['forbidden', 4, 'Без обеда ×4 – заповедное не тронули'],
        ['empty', 4, 'Без обеда ×4 – на складе нет съестного'],
    ])('reason %s with count %s resolves to %s', (reason, count, expected) => {
        const workerName = count > 1 ? null : 'Аким';
        const workerGender = count > 1 ? null : 1;
        expect(getWorkerMealText({ count, workerName, workerGender, variant: 0, reason }, 'хлеб')).toBe(expected);
    });
});

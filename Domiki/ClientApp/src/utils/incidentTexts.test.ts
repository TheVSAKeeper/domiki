import { describe, expect, it } from 'vitest';
import { getIncidentTemplate, incidentText } from './incidentTexts';

describe('incident texts', () => {
    it('interpolates worker name and gender forms', () => {
        expect(incidentText('{имя} вернул{ся|ась} сам{|а}', 'Иван', 1)).toBe('Иван вернулся сам');
        expect(incidentText('{имя} вернул{ся|ась} сам{|а}', 'Анна', 2)).toBe('Анна вернулась сама');
    });

    it('selects templates by modulo', () => {
        expect(getIncidentTemplate(6)).toBe(getIncidentTemplate(0));
    });

    it('contains complete text for every template', () => {
        for (let index = 0; index < 6; index += 1) {
            const template = getIncidentTemplate(index);
            expect(template.clues).toHaveLength(3);
            expect(template.resolutions).toHaveLength(3);
            expect(template.title).not.toBe('');
            expect(template.hook).not.toBe('');
            expect(template.epilogue).not.toBe('');
            expect(template.clues.every(clue => clue.label !== '' && clue.detail !== '')).toBe(true);
            expect(template.resolutions.every(resolution => resolution !== '')).toBe(true);
        }
    });
});

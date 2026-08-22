import { describe, expect, it } from 'vitest';
import { errandTemplateCount, getErrandTemplate, getErrandThanks } from './errandTexts';

describe('errand texts', () => {
    it('selects templates by modulo', () => {
        expect(getErrandTemplate(errandTemplateCount)).toBe(getErrandTemplate(0));
    });

    it('contains complete text for every template', () => {
        for (let index = 0; index < errandTemplateCount; index += 1) {
            const template = getErrandTemplate(index);
            expect(template.clues).toHaveLength(3);
            expect(template.resolutions).toHaveLength(3);
            expect(template.title).not.toBe('');
            expect(template.offer).not.toBe('');
            expect(template.clues.every(clue => clue.label !== '' && clue.detail !== '')).toBe(true);
            expect(template.resolutions.every(resolution => resolution !== '')).toBe(true);
        }
    });

    it('uses the fallback thanks for an unknown neighbor', () => {
        expect(getErrandThanks(999)).toBe('Спасибо за подмогу, соседи. Добро ваше не забудется.');
    });
});

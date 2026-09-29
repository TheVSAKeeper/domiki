import { describe, expect, it } from 'vitest';
import { buildIdOf, needsReloadOnControllerChange } from './appVersion';

const indexHtml = (entry: string) => new DOMParser().parseFromString(
    `<!doctype html><html><head><link rel="modulepreload" href="/assets/sprites-abc.js"><script type="module" crossorigin src="${entry}"></script></head><body></body></html>`,
    'text/html',
);

describe('buildIdOf', () => {
    it.each([
        ['/assets/index-B1x_9-Qz.js', 'B1x_9-Qz'],
        ['/src/main.tsx', null],
    ])('входной скрипт %s даёт сборку %s', (entry, expected) => {
        expect(buildIdOf(indexHtml(entry))).toBe(expected);
    });
});

describe('needsReloadOnControllerChange', () => {
    it.each([
        ['первая установка воркера со свежим бандлом', false, 'aaa', 'aaa', false],
        ['захват страницы без контроллера воркером нового деплоя', false, 'aaa', 'bbb', true],
        ['сборку воркера узнать не удалось', false, 'aaa', null, true],
        ['сборку страницы узнать не удалось', false, null, 'aaa', true],
        ['смена уже бывшего контроллера', true, 'aaa', 'aaa', true],
    ])('%s', (_, hadController, loaded, workerBuild, expected) => {
        expect(needsReloadOnControllerChange(hadController, loaded, workerBuild)).toBe(expected);
    });
});

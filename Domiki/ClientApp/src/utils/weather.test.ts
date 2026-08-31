import { describe, expect, it } from 'vitest';
import type { SickTypeDto, WeatherPeriodDto } from '../types/api';
import { grantOutput, sickRiskPercent, sickTypeForWeather, weatherMark, weatherMarkSpeech, weatherOutputChange } from './weather';

const period = (weatherTypeId: number, weatherName: string, logicName: string, effects: { domikTypeId: number; outputPercent: number }[]): WeatherPeriodDto => ({
    weatherTypeId,
    weatherName,
    logicName,
    startDate: '2026-08-01T00:00:00Z',
    endDate: '2026-08-01T08:00:00Z',
    effects,
});

const rain = period(2, 'Дождь', 'rain', [{ domikTypeId: 3, outputPercent: 150 }, { domikTypeId: 4, outputPercent: 75 }, { domikTypeId: 5, outputPercent: 100 }]);

describe('weatherMark', () => {
    it.each([
        [3, '+50 %', true, 'Дождь помогает: +50 % выход'],
        [4, '−25 %', false, 'Дождь мешает: −25 % выход'],
    ])('размечает постройку %i', (domikTypeId, delta, buff, title) => {
        const mark = weatherMark(rain, domikTypeId);
        expect(mark).not.toBeNull();
        expect(mark?.buff).toBe(buff);
        expect(mark?.title).toBe(title);
        expect(mark?.title).toContain(delta);
    });

    it.each([
        ['нетронутую постройку', 5],
        ['постройку без записи', 99],
    ])('не метит %s', (_case, domikTypeId) => {
        expect(weatherMark(rain, domikTypeId)).toBeNull();
    });

    it('молчит без погоды', () => {
        expect(weatherMark(null, 3)).toBeNull();
    });

    it('озвучивает направление словом, а не знаком', () => {
        const mark = weatherMark(rain, 4);
        expect(mark == null ? null : weatherMarkSpeech(mark)).toBe(', дождь мешает, выход меньше на 25 %');
    });
});

describe('grantOutput', () => {
    it.each([
        [1, 75, 1], [1, 125, 1], [1, 150, 1],
        [2, 75, 2], [2, 125, 2], [2, 150, 3],
        [3, 75, 3], [3, 125, 3], [3, 150, 4],
        [4, 75, 3], [4, 125, 5], [4, 150, 6],
        [8, 75, 6], [8, 125, 10], [8, 150, 12],
        [24, 75, 18], [24, 125, 30], [24, 150, 36],
    ])('выход %i при %i%% даёт %i', (base, percent, expected) => {
        expect(grantOutput(base, percent)).toBe(expected);
    });
});

describe('weatherOutputChange', () => {
    it.each([
        ['часовой рецепт на единицу', [{ typeId: 1, value: 1 }], 150, 1, 1, 0],
        ['часовой рецепт под помехой', [{ typeId: 1, value: 1 }], 75, 1, 1, 0],
        ['крупную смену с прибавкой', [{ typeId: 1, value: 8 }], 125, 8, 10, 2],
        ['крупную смену с убытком', [{ typeId: 1, value: 8 }], 75, 8, 6, -2],
        ['смену на два ресурса', [{ typeId: 1, value: 4 }, { typeId: 2, value: 1 }], 150, 5, 7, 2],
        ['погоду без влияния', [{ typeId: 1, value: 8 }], 100, 8, 8, 0],
    ])('считает %s', (_case, outputResources, outputPercent, base, granted, delta) => {
        expect(weatherOutputChange(outputResources, outputPercent)).toEqual({ base, granted, delta });
    });
});

describe('sickRiskPercent', () => {
    it.each([
        [2, 1, 8],
        [2, 2, 4],
        [3, 4, 3],
        [12, 3, 15],
        [0, 2, 0],
        [-2, 2, 0],
        [4, 0, 0],
    ])('риск от %i лишних единиц на %i трудяг равен %i%%', (weatherExtra, plodderCount, expected) => {
        expect(sickRiskPercent(weatherExtra, plodderCount)).toBe(expected);
    });
});

describe('sickTypeForWeather', () => {
    const sickTypes: SickTypeDto[] = [
        { id: 1, name: 'Простуда', logicName: 'cold', weatherTypeId: 2, cloakProtects: true },
        { id: 3, name: 'Озноб', logicName: 'chill', weatherTypeId: 4, cloakProtects: true },
    ];

    it('берёт хворь текущей погоды', () => {
        expect(sickTypeForWeather(sickTypes, 4)?.name).toBe('Озноб');
    });

    it.each([
        ['погоды без хвори', 9],
        ['отсутствующей погоды', null],
    ])('молчит для %s', (_case, weatherTypeId) => {
        expect(sickTypeForWeather(sickTypes, weatherTypeId)).toBeNull();
    });
});

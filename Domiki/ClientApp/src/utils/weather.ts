import type { ResourceDto, SickTypeDto, WeatherPeriodDto } from '../types/api';

const SICK_CHANCE_PER_UNIT = 4;
const SICK_CHANCE_CAP = 15;

export interface WeatherOutputChange {
    base: number;
    granted: number;
    delta: number;
}

export interface WeatherMarkView {
    outputPercent: number;
    delta: number;
    buff: boolean;
    weatherName: string;
    weatherLogicName: string;
    title: string;
}

export function formatOutputDelta(delta: number) {
    return `${delta > 0 ? '+' : '−'}${Math.abs(delta)} %`;
}

export function weatherMark(weather: WeatherPeriodDto | null | undefined, domikTypeId: number): WeatherMarkView | null {
    const effect = weather?.effects.find(item => item.domikTypeId === domikTypeId);
    if (weather == null || effect == null || effect.outputPercent === 100) {
        return null;
    }
    const delta = effect.outputPercent - 100;
    const buff = delta > 0;
    return {
        outputPercent: effect.outputPercent,
        delta,
        buff,
        weatherName: weather.weatherName,
        weatherLogicName: weather.logicName,
        title: `${weather.weatherName} ${buff ? 'помогает' : 'мешает'}: ${formatOutputDelta(delta)} выход`,
    };
}

export function weatherMarkSpeech(mark: WeatherMarkView) {
    const direction = mark.buff ? 'помогает, выход больше на' : 'мешает, выход меньше на';
    return `, ${mark.weatherName.toLocaleLowerCase()} ${direction} ${Math.abs(mark.delta)} %`;
}

export function grantOutput(base: number, percent: number) {
    return Math.max(1, base + Math.trunc(base * (percent - 100) / 100));
}

export function weatherOutputChange(outputResources: ResourceDto[], outputPercent: number): WeatherOutputChange {
    return outputResources.reduce<WeatherOutputChange>((total, output) => {
        const granted = grantOutput(output.value, outputPercent);
        return { base: total.base + output.value, granted: total.granted + granted, delta: total.delta + granted - output.value };
    }, { base: 0, granted: 0, delta: 0 });
}

export function sickRiskPercent(weatherExtra: number, plodderCount: number) {
    return weatherExtra <= 0 || plodderCount <= 0 ? 0 : Math.min(SICK_CHANCE_CAP, Math.round(SICK_CHANCE_PER_UNIT * weatherExtra / plodderCount));
}

export function sickTypeForWeather(sickTypes: SickTypeDto[], weatherTypeId: number | null | undefined) {
    return weatherTypeId == null ? null : sickTypes.find(sickType => sickType.weatherTypeId === weatherTypeId) ?? null;
}

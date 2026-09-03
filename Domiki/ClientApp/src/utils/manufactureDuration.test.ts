import { describe, expect, it } from 'vitest';
import { computeManufactureDuration, type ManufactureDurationInput, type ManufactureDurationResult } from './manufactureDuration';
import vectors from './manufactureDurationVectors.json';

interface DurationVector {
    input: ManufactureDurationInput;
    expected: ManufactureDurationResult;
}

describe('computeManufactureDuration', () => {
    it('повторяет серверный расчёт на каждом случае из общего файла', () => {
        const cases: DurationVector[] = vectors;

        expect(cases.length).toBeGreaterThan(0);
        for (const vector of cases) {
            expect(computeManufactureDuration(vector.input), JSON.stringify(vector.input)).toEqual(vector.expected);
        }
    });
});

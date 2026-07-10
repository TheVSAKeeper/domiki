import { describe, expect, it } from 'vitest';
import type { DomikTypeDto, WorkerDto } from '../types/api';
import { describeWorker } from './worker';

const domikTypes: DomikTypeDto[] = [
    { id: 1, name: 'Рынок', logicName: 'market', maxCount: 1, availableCount: 0, maxLevel: 5, unlockLevel: 0, blueprintId: null, levels: [] },
    { id: 2, name: 'Кузня', logicName: 'forge', maxCount: 1, availableCount: 0, maxLevel: 5, unlockLevel: 0, blueprintId: null, levels: [] },
];

const baseWorker: WorkerDto = {
    id: 1,
    name: 'Аким',
    traitId: 1,
    traitName: 'Упрямый',
    traitDurationPercent: 0,
    noFatigue: false,
    manufactureId: null,
    expeditionId: null,
    restUntil: null,
    skills: [],
};

describe('describeWorker', () => {
    it.each<[string, WorkerDto['skills'], string]>([
        ['no skills', [], 'Пока без ремесла.'],
        ['only zero-bonus skills', [{ domikTypeId: 1, uses: 3, bonusPercent: 0 }], 'Пока без ремесла.'],
        ['beginner bonus 1-9', [{ domikTypeId: 1, uses: 1, bonusPercent: 5 }], 'Начинающий работник: Рынок.'],
        ['skilled bonus 10-24', [{ domikTypeId: 1, uses: 1, bonusPercent: 10 }], 'Умелый работник: Рынок.'],
        ['masterful bonus 25+', [{ domikTypeId: 1, uses: 1, bonusPercent: 25 }], 'Мастеровитый работник: Рынок.'],
        ['uses the highest bonus across multiple skills', [
            { domikTypeId: 1, uses: 1, bonusPercent: 5 },
            { domikTypeId: 2, uses: 1, bonusPercent: 30 },
        ], 'Мастеровитый работник: Рынок, Кузня.'],
    ])('%s -> %s', (_name, skills, expected) => {
        expect(describeWorker({ ...baseWorker, skills }, domikTypes)).toBe(expected);
    });
});

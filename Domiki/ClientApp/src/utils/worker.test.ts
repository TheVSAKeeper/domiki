import { describe, expect, it } from 'vitest';
import type { DomikTypeDto, WorkerDto } from '../types/api';
import { describeWorker } from './worker';

const domikTypes: DomikTypeDto[] = [
    { id: 1, name: 'Рынок', logicName: 'market', maxCount: 1, availableCount: 0, maxLevel: 5, unlockLevel: 0, blueprintId: null, nextCountGateLevel: null, levels: [] },
    { id: 2, name: 'Кузня', logicName: 'forge', maxCount: 1, availableCount: 0, maxLevel: 5, unlockLevel: 0, blueprintId: null, nextCountGateLevel: null, levels: [] },
];

const baseWorker: WorkerDto = {
    id: 1,
    name: 'Аким',
    gender: 1,
    traitId: 1,
    traitName: 'Упрямый',
    traitLogicName: 'ordinary',
    traitDurationPercent: 0,
    noFatigue: false,
    noSick: false,
    manufactureId: null,
    expeditionId: null,
    errandId: null,
    incidentId: null,
    workedSeconds: 0,
    restUntil: null,
    sickUntil: null,
    sickTypeId: null,
    skills: [],
};

describe('describeWorker', () => {
    it.each<[string, WorkerDto['skills'], string]>([
        ['no skills', [], 'Пока без ремесла, зато рвётся учиться.'],
        ['only zero-bonus skills', [{ domikTypeId: 1, uses: 3, bonusPercent: 0 }], 'Пока без ремесла, зато рвётся учиться.'],
        ['beginner bonus 1-9', [{ domikTypeId: 1, uses: 1, bonusPercent: 5 }], 'Начинающий торговец. Учится на ходу и не ноет.'],
        ['skilled bonus 10-24', [{ domikTypeId: 1, uses: 1, bonusPercent: 10 }], 'Умелый торговец. Сдачу считает быстрее счётов.'],
        ['masterful bonus 25+', [{ domikTypeId: 1, uses: 1, bonusPercent: 25 }], 'Знатный торговец. Сдачу считает быстрее счётов.'],
        ['each craft keeps its own tier word', [
            { domikTypeId: 1, uses: 1, bonusPercent: 5 },
            { domikTypeId: 2, uses: 1, bonusPercent: 30 },
        ], 'Знатный кузнец, начинающий торговец. Искры летят, а работа поёт.'],
    ])('%s -> %s', (_name, skills, expected) => {
        expect(describeWorker({ ...baseWorker, skills }, domikTypes)).toBe(expected);
    });

    it.each<[string, string, number, WorkerDto['skills'], string]>([
        ['female forge', 'Ульяна', 2, [{ domikTypeId: 2, uses: 1, bonusPercent: 10 }], 'Умелая кузнечиха. Искры летят, а работа поёт.'],
        ['female multi craft', 'Дарья', 2, [
            { domikTypeId: 1, uses: 1, bonusPercent: 5 },
            { domikTypeId: 2, uses: 1, bonusPercent: 30 },
        ], 'Знатная кузнечиха, начинающая торговка. Искры летят, а работа поёт.'],
        ['male name ending in -я stays male', 'Илья', 1, [{ domikTypeId: 2, uses: 1, bonusPercent: 10 }], 'Умелый кузнец. Искры летят, а работа поёт.'],
    ])('%s -> %s', (_name, name, gender, skills, expected) => {
        expect(describeWorker({ ...baseWorker, name, gender, skills }, domikTypes)).toBe(expected);
    });

    it('picks a different flavor for a different worker id at the same craft', () => {
        const skills = [{ domikTypeId: 2, uses: 1, bonusPercent: 15 }];
        const a = describeWorker({ ...baseWorker, id: 10, skills }, domikTypes);
        const b = describeWorker({ ...baseWorker, id: 11, skills }, domikTypes);
        expect(a).not.toBe(b);
    });
});

import { describe, expect, it } from 'vitest';
import type { DomikDto, DomikTypeDto, ManufactureDto, ReceiptDto, WorkerDto } from '../types/api';
import { buildAssignTarget, pickCrew, workerSkillPercent } from './assign';

const receipt = (id: number, plodderCount: number, inputs: { typeId: number; value: number }[] = []): ReceiptDto => ({
    id, name: `r${id}`, logicName: `r${id}`, inputResources: inputs, optionalInputResources: [],
    durationSeconds: 3600, outputBonusPercent: 0, plodderCount,
    outputResources: [{ typeId: 9, value: 1 }],
});

const domikType = (receiptIds: number[], maxManufactureCount = 2): DomikTypeDto => ({
    id: 7, name: 'Кузница', logicName: 'forge', maxCount: 1, availableCount: 0, maxLevel: 3, unlockLevel: 0,
    blueprintId: null, nextCountGateLevel: null,
    levels: [{ value: 1, resources: [], upgradeSeconds: 0, modificators: [], receiptIds, maxManufactureCount }],
});

const manufacture = (id: number, autoRepeat: boolean): ManufactureDto => ({
    id, receiptId: 1, finishDate: '2026-07-25T10:00:00Z', durationSeconds: 3600, plodderCount: 1, autoRepeat,
});

const domik = (over: Partial<DomikDto> = {}): DomikDto => ({
    id: 1, typeId: 7, level: 1, finishDate: null, upgradeSeconds: null, manufactures: [], ...over,
});

const worker = (id: number, traitDurationPercent: number, skills: { domikTypeId: number; bonusPercent: number }[] = []): WorkerDto => ({
    id, name: `w${id}`, gender: 1, traitId: 1, traitName: 'Проворный', traitLogicName: 'swift',
    traitDurationPercent, noFatigue: false, noSick: false,
    manufactureId: null, expeditionId: null, errandId: null, incidentId: null,
    workedSeconds: 0, restUntil: null, sickUntil: null, sickTypeId: null, isAway: false,
    skills: skills.map(skill => ({ ...skill, uses: 10 })),
});

describe('workerSkillPercent', () => {
    it('возвращает 0 для постройки, в которой трудяга ещё не работал', () => {
        expect(workerSkillPercent(worker(1, 0, [{ domikTypeId: 7, bonusPercent: 12 }]), 8)).toBe(0);
    });
});

describe('pickCrew', () => {
    it('ставит приставленного первым и добирает остальных по пригодности', () => {
        const held = worker(1, 0);
        const crew = pickCrew(held, [held, worker(2, 5), worker(3, -10, [{ domikTypeId: 7, bonusPercent: 3 }])], 7, 3);

        expect(crew.map(item => item.id)).toEqual([1, 3, 2]);
    });

    it('возвращает состав короче нужного, когда свободных не хватает', () => {
        const held = worker(1, 0);

        expect(pickCrew(held, [held], 7, 3)).toHaveLength(1);
    });
});

describe('buildAssignTarget', () => {
    const held = worker(1, 0);
    const free = [held, worker(2, 0), worker(3, 0)];
    const receipts = [receipt(1, 1), receipt(2, 2, [{ typeId: 4, value: 5 }])];
    const noVein = { domiks: [], receipts, goldMinedToday: 0, now: Date.UTC(2026, 6, 25, 12, 0, 0) };

    it('признаёт постройку годной целью, когда хотя бы один рецепт запускается', () => {
        const target = buildAssignTarget(domik(), domikType([1, 2]), receipts, [], free, held, noVein);

        expect(target.eligible).toBe(true);
        expect(target.reason).toBeNull();
        expect(target.options.map(option => option.canRun)).toEqual([true, false]);
    });

    it('называет нехватку припасов, когда рецепты упираются только в склад', () => {
        const target = buildAssignTarget(domik(), domikType([2]), receipts, [], free, held, noVein);

        expect(target.eligible).toBe(false);
        expect(target.reason).toBe('нет припасов');
        expect(target.options[0]?.shortfall).toEqual([{ typeId: 4, value: 5 }]);
    });

    it('называет нехватку трудяг, когда припасов хватает, а свободных нет', () => {
        const target = buildAssignTarget(domik(), domikType([2]), receipts, [{ typeId: 4, value: 5 }], [held], held, noVein);

        expect(target.reason).toBe('нет трудяг');
        expect(target.options[0]?.reason).toBe('нужно трудяг: 2');
    });

    it.each([
        ['наряд стоит', domik({ manufactures: [manufacture(1, true)] }), domikType([1])],
        ['мест нет', domik({ manufactures: [manufacture(1, false), manufacture(2, false)] }), domikType([1])],
        ['идёт стройка', domik({ finishDate: '2026-07-25T10:00:00Z' }), domikType([1])],
        ['нечего делать', domik(), domikType([])],
    ])('отказывает в приставлении с причиной «%s»', (reason, target, type) => {
        const result = buildAssignTarget(target, type, receipts, [], free, held, noVein);

        expect(result.eligible).toBe(false);
        expect(result.reason).toBe(reason);
    });

    it('отказывает в приставлении к руднику с выбранной жилой', () => {
        const goldReceipt = { ...receipt(9, 1), outputResources: [{ typeId: 5, value: 1 }] };
        const mine = domik({ id: 15, typeId: 4, level: 1 });
        const vein = { domiks: [mine], receipts: [goldReceipt], goldMinedToday: 1, now: Date.UTC(2026, 6, 25, 12, 0, 0) };
        const target = buildAssignTarget(mine, { ...domikType([9]), id: 4 }, [goldReceipt], [], free, held, vein);

        expect(target.eligible).toBe(false);
        expect(target.reason).toBe('жила выбрана');
        expect(target.options[0]?.canRun).toBe(false);
    });

    it('не предлагает рецепты, которым трудяги не нужны', () => {
        const target = buildAssignTarget(domik(), domikType([3]), [receipt(3, 0)], [], free, held, noVein);

        expect(target.options).toHaveLength(0);
        expect(target.reason).toBe('нечего делать');
    });
});

import type { DomikDto, DomikTypeDto, ReceiptDto, ResourceDto, WorkerDto } from '../types/api';
import { goldVeinView, resourceShortfall, workerFitness, type GoldVeinContext } from './game';

export interface AssignReceiptOption {
    receipt: ReceiptDto;
    crew: WorkerDto[];
    autoCrew: WorkerDto[];
    shortfall: ResourceDto[];
    canRun: boolean;
    reason: string | null;
}

export interface AssignTarget {
    eligible: boolean;
    reason: string | null;
    options: AssignReceiptOption[];
}

const BLOCKED_TARGET: AssignTarget = { eligible: false, reason: 'нечего делать', options: [] };

const VEIN_SPENT_REASON = 'жила выбрана';

export function workerSkillPercent(worker: WorkerDto, domikTypeId: number): number {
    return worker.skills.find(skill => skill.domikTypeId === domikTypeId)?.bonusPercent ?? 0;
}

export function bestSkill(worker: WorkerDto): { domikTypeId: number; bonusPercent: number } | null {
    return worker.skills.reduce<{ domikTypeId: number; bonusPercent: number } | null>(
        (best, skill) => skill.bonusPercent > 0 && (best == null || skill.bonusPercent > best.bonusPercent)
            ? { domikTypeId: skill.domikTypeId, bonusPercent: skill.bonusPercent }
            : best,
        null,
    );
}

export function pickCrew(held: WorkerDto, freeWorkers: WorkerDto[], domikTypeId: number, plodderCount: number): WorkerDto[] {
    const auto = freeWorkers
        .filter(worker => worker.id !== held.id)
        .sort((a, b) => workerFitness(b, domikTypeId) - workerFitness(a, domikTypeId) || a.id - b.id)
        .slice(0, Math.max(0, plodderCount - 1));

    return [held, ...auto];
}

export function buildAssignTarget(
    domik: DomikDto,
    domikType: DomikTypeDto,
    receipts: ReceiptDto[],
    resources: ResourceDto[],
    freeWorkers: WorkerDto[],
    held: WorkerDto,
    goldVein: GoldVeinContext,
): AssignTarget {
    if (domik.level === 0 || domik.finishDate != null) {
        return { eligible: false, reason: 'идёт стройка', options: [] };
    }

    const manufactures = domik.manufactures ?? [];
    if (manufactures.some(manufacture => manufacture.autoRepeat)) {
        return { eligible: false, reason: 'наряд стоит', options: [] };
    }

    const level = domikType.levels.find(item => item.value === domik.level);
    if (level == null) {
        return BLOCKED_TARGET;
    }

    if (manufactures.length >= level.maxManufactureCount) {
        return { eligible: false, reason: 'мест нет', options: [] };
    }

    const options = level.receiptIds
        .flatMap(receiptId => {
            const receipt = receipts.find(item => item.id === receiptId);
            return receipt == null || receipt.plodderCount < 1 ? [] : [receipt];
        })
        .map(receipt => {
            const crew = pickCrew(held, freeWorkers, domikType.id, receipt.plodderCount);
            const shortfall = resourceShortfall(receipt.inputResources, resources);
            const enoughCrew = crew.length === receipt.plodderCount;
            const veinBlocked = goldVeinView(receipt, domik, goldVein)?.blockReason != null;
            return {
                receipt,
                crew,
                autoCrew: crew.slice(1),
                shortfall,
                canRun: shortfall.length === 0 && enoughCrew && !veinBlocked,
                reason: shortfall.length > 0
                    ? 'нет припасов'
                    : enoughCrew ? veinBlocked ? VEIN_SPENT_REASON : null : `нужно трудяг: ${receipt.plodderCount}`,
            };
        });

    if (options.length === 0) {
        return BLOCKED_TARGET;
    }

    if (options.some(option => option.canRun)) {
        return { eligible: true, reason: null, options };
    }

    if (options.every(option => option.reason === VEIN_SPENT_REASON)) {
        return { eligible: false, reason: VEIN_SPENT_REASON, options };
    }

    return {
        eligible: false,
        reason: options.every(option => option.shortfall.length === 0) ? 'нет трудяг' : 'нет припасов',
        options,
    };
}

export function buildAssignTargets(
    domiks: DomikDto[],
    domikTypes: DomikTypeDto[],
    receipts: ReceiptDto[],
    resources: ResourceDto[],
    freeWorkers: WorkerDto[],
    held: WorkerDto,
    goldVein: GoldVeinContext,
): Map<number, AssignTarget> {
    return new Map(domiks.flatMap(domik => {
        const domikType = domikTypes.find(type => type.id === domik.typeId);
        return domikType == null
            ? []
            : [[domik.id, buildAssignTarget(domik, domikType, receipts, resources, freeWorkers, held, goldVein)] as const];
    }));
}

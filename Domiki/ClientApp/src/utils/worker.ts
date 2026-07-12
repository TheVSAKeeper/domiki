import type { DomikTypeDto, WorkerDto } from '../types/api';

export const SKILLED_BONUS_THRESHOLD = 10;

export function isSkilledWorker(worker: WorkerDto): boolean {
    return worker.skills.some(s => s.bonusPercent >= SKILLED_BONUS_THRESHOLD);
}

export function describeWorker(worker: WorkerDto, domikTypes: DomikTypeDto[]): string {
    const skilled = worker.skills.filter(s => s.bonusPercent > 0);
    if (skilled.length === 0) {
        return 'Пока без ремесла.';
    }

    const maxBonus = Math.max(...skilled.map(s => s.bonusPercent));
    const levelWord = maxBonus >= 25 ? 'Мастеровитый' : maxBonus >= 10 ? 'Умелый' : 'Начинающий';
    const names = skilled
        .map(s => domikTypes.find(t => t.id === s.domikTypeId)?.name)
        .filter((name): name is string => name != null)
        .join(', ');

    return names.length > 0 ? `${levelWord} работник: ${names}.` : `${levelWord} работник.`;
}

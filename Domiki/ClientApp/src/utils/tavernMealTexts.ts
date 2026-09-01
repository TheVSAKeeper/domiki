import { genderForm } from './gender';

const mealTemplate0 = '{имя} {сел|села} к котлу';

const mealTemplates: string[] = [
    mealTemplate0,
    '{имя} за столом: {еда} и кружка кваса',
    '{имя} {пообедал|пообедала} и {ушёл|ушла} отдыхать',
    'Ложка стучит – {имя} обедает',
    '{имя} {подкрепился|подкрепилась} перед отдыхом',
    'Котёл не остыл – {имя} {успел|успела} к обеду',
    'Корчмарь отрезал ломоть – {имя} обедает',
    '{имя} обедает: {еда} со своего склада',
];

export type WorkerMealNoMealReason = 'forbidden' | 'empty';

const noMealSingleTemplates: Record<WorkerMealNoMealReason, string> = {
    forbidden: '{имя} {ушёл|ушла} отдыхать без обеда – на складе только заповедное',
    empty: 'Котёл пустой – {имя} отдыхает полный срок',
};

const noMealMergedTemplates: Record<WorkerMealNoMealReason, string> = {
    forbidden: 'Без обеда ×{N} – заповедное не тронули',
    empty: 'Без обеда ×{N} – на складе нет съестного',
};

export function getWorkerMealTemplate(variant: number): string {
    return mealTemplates[variant] ?? mealTemplate0;
}

export function workerMealText(text: string, heroName: string, heroGender: number | undefined, foodName: string): string {
    return text
        .replaceAll('{еда}', foodName)
        .replaceAll('{имя}', heroName)
        .replace(/\{([^{}|]*)\|([^{}|]*)\}/g, (_, male: string, female: string) => genderForm(heroGender, male, female));
}

const isNoMealReason = (reason: string | null): reason is WorkerMealNoMealReason => reason === 'forbidden' || reason === 'empty';

export interface WorkerMealEventData {
    count: number;
    workerName: string | null;
    workerGender: number | null;
    variant: number;
    reason: string | null;
}

export function getWorkerMealText(data: WorkerMealEventData, foodName: string | null): string {
    const merged = data.count > 1;
    const heroName = data.workerName ?? 'Трудяга';
    const heroGender = data.workerGender ?? undefined;

    if (isNoMealReason(data.reason)) {
        return merged
            ? noMealMergedTemplates[data.reason].replaceAll('{N}', String(data.count))
            : workerMealText(noMealSingleTemplates[data.reason], heroName, heroGender, foodName ?? '');
    }

    if (merged) {
        return `Обед в корчме ×${data.count}`;
    }

    return workerMealText(getWorkerMealTemplate(data.variant), heroName, heroGender, foodName ?? '');
}

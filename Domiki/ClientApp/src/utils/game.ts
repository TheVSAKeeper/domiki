import type { DomikDto, DomikTypeDto, ManufactureDto, PlodderCount, ReceiptDto, ReceiptView, ResourceDto, SelectedDomikView, UpgradeView } from '../types/api';
import { formatDuration, remainingSeconds } from './time';

const plodderTypeId = 1;

function hasResourcesFor(cost: ResourceDto[], owned: ResourceDto[]): boolean {
    return cost.every(resource => {
        const have = owned.find(x => x.typeId === resource.typeId);
        return have != null && have.value >= resource.value;
    });
}

export function canAffordUpgrade(domik: DomikDto, domikType: DomikTypeDto, resources: ResourceDto[]): boolean {
    if (domik.level <= 0 || domik.level >= domikType.maxLevel || domik.finishDate != null) {
        return false;
    }

    const domikLevel = domikType.levels.find(x => x.value === domik.level);
    return domikLevel != null && hasResourcesFor(domikLevel.resources, resources);
}

export function computePlodderCount(domiks: DomikDto[], domikTypes: DomikTypeDto[]): PlodderCount {
    let maxPlodderCount = 0;
    let workingPlodderCount = 0;

    domiks.forEach(domik => {
        if (domik.level > 0) {
            const domikType = domikTypes.find(x => x.id === domik.typeId);
            const domikLevel = domikType?.levels.find(x => x.value === domik.level);
            const modificator = domikLevel?.modificators.find(x => x.typeId === plodderTypeId);
            if (modificator != null) {
                maxPlodderCount += modificator.value;
            }
        }

        if (domik.manufactures != null) {
            domik.manufactures.forEach(manufacture => {
                workingPlodderCount += manufacture.plodderCount;
            });
        }
    });

    return { max: maxPlodderCount, free: maxPlodderCount - workingPlodderCount };
}

export function computeSelectedDomikView(
    selectedId: number | null,
    domiks: DomikDto[],
    domikTypes: DomikTypeDto[],
    receipts: ReceiptDto[],
    resources: ResourceDto[],
    now: number,
): SelectedDomikView | null {
    if (selectedId == null) {
        return null;
    }

    const domik = domiks.find(x => x.id === selectedId);
    if (domik == null || domik.level === 0) {
        return null;
    }

    const domikType = domikTypes.find(x => x.id === domik.typeId);
    if (domikType == null) {
        return null;
    }

    let domikReceipts: ReceiptDto[] = [];
    let upgrade: UpgradeView | null = null;

    if (domik.level > 0) {
        const domikLevel = domikType.levels.find(x => x.value === domik.level);
        if (domikLevel != null) {
            domikReceipts = domikLevel.receiptIds
                .map(receiptId => receipts.find(x => x.id === receiptId))
                .filter((receipt): receipt is ReceiptDto => receipt != null);

            if (domik.level < domikType.maxLevel && domik.finishDate == null) {
                const hasResources = hasResourcesFor(domikLevel.resources, resources);

                upgrade = {
                    nextLevel: domik.level + 1,
                    resources: domikLevel.resources,
                    hasResources,
                };
            }
        }
    }

    const remainingText = domik.finishDate != null ? formatDuration(remainingSeconds(domik.finishDate, now)) : null;

    return { domik, domikType, receipts: domikReceipts, upgrade, remainingText };
}

function mergeResources(resources: ResourceDto[]): ResourceDto[] {
    const byType = new Map<number, number>();
    resources.forEach(res => byType.set(res.typeId, (byType.get(res.typeId) ?? 0) + res.value));
    return [...byType].map(([typeId, value]) => ({ typeId, value }));
}

export function computeReceiptView(
    receipt: ReceiptDto,
    resources: ResourceDto[],
    freePlodders: number,
    useOptional: boolean,
): ReceiptView {
    const withOptional = useOptional && receipt.optionalInputResources.length > 0;
    const inputs = mergeResources(
        withOptional ? [...receipt.inputResources, ...receipt.optionalInputResources] : receipt.inputResources,
    );
    const durationSeconds = withOptional
        ? Math.floor((receipt.durationSeconds * (100 - receipt.speedupPercent)) / 100)
        : receipt.durationSeconds;
    const hasResources = hasResourcesFor(inputs, resources);
    const hasPlodders = freePlodders >= receipt.plodderCount;

    return { receipt, inputs, durationSeconds, hasResources, hasPlodders, canRun: hasResources && hasPlodders };
}

export function manufactureProgressPercent(manufacture: ManufactureDto, receipt: ReceiptDto, now: number): number {
    const total = receipt.durationSeconds;
    const current = remainingSeconds(manufacture.finishDate, now);
    const percent = 100 - Math.floor((current * 100) / total);
    return Math.min(100, Math.max(0, percent));
}

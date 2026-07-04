export enum ResponseType {
    Success = 1,
    ErrorMessage = 2,
}

export type ApiResult<T> =
    | { type: ResponseType.Success; content: T }
    | { type: ResponseType.ErrorMessage; content: string };

export interface DomikDto {
    id: number;
    typeId: number;
    level: number;
    finishDate: string | null;
    manufactures: ManufactureDto[] | null;
}

export interface ManufactureDto {
    id: number;
    finishDate: string;
    plodderCount: number;
    receiptId: number;
}

export interface DomikTypeDto {
    id: number;
    name: string;
    logicName: string;
    maxCount: number;
    availableCount: number;
    maxLevel: number;
    levels: UpgradeLevelDto[];
}

export interface UpgradeLevelDto {
    value: number;
    resources: ResourceDto[];
    modificators: ModificatorDto[];
    receiptIds: number[];
}

export interface ResourceDto {
    typeId: number;
    value: number;
}

export interface ResourceTypeDto {
    id: number;
    name: string;
    logicName: string;
}

export interface ModificatorDto {
    typeId: number;
    value: number;
}

export interface ReceiptDto {
    id: number;
    name: string;
    logicName: string;
    inputResources: ResourceDto[];
    durationSeconds: number;
    outputResources: ResourceDto[];
    plodderCount: number;
}

export interface PlodderCount {
    max: number;
    free: number;
}

export interface UpgradeView {
    nextLevel: number;
    resources: ResourceDto[];
    hasResources: boolean;
}

export interface SelectedDomikView {
    domik: DomikDto;
    domikType: DomikTypeDto;
    receipts: ReceiptDto[];
    upgrade: UpgradeView | null;
    remainingText: string | null;
}

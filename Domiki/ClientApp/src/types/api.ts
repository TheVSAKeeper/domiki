import { z } from 'zod';

export enum ResponseType {
    Success = 1,
    ErrorMessage = 2,
}

export const resourceSchema = z.object({
    typeId: z.number(),
    value: z.number(),
});
export type ResourceDto = z.infer<typeof resourceSchema>;

export const modificatorSchema = z.object({
    typeId: z.number(),
    value: z.number(),
});
export type ModificatorDto = z.infer<typeof modificatorSchema>;

export const manufactureSchema = z.object({
    id: z.number(),
    finishDate: z.string(),
    plodderCount: z.number(),
    receiptId: z.number(),
});
export type ManufactureDto = z.infer<typeof manufactureSchema>;

export const domikSchema = z.object({
    id: z.number(),
    typeId: z.number(),
    level: z.number(),
    finishDate: z.string().nullable(),
    manufactures: z.array(manufactureSchema).nullable(),
});
export type DomikDto = z.infer<typeof domikSchema>;

export const upgradeLevelSchema = z.object({
    value: z.number(),
    resources: z.array(resourceSchema),
    modificators: z.array(modificatorSchema),
    receiptIds: z.array(z.number()),
});
export type UpgradeLevelDto = z.infer<typeof upgradeLevelSchema>;

export const domikTypeSchema = z.object({
    id: z.number(),
    name: z.string(),
    logicName: z.string(),
    maxCount: z.number(),
    availableCount: z.number(),
    maxLevel: z.number(),
    levels: z.array(upgradeLevelSchema),
});
export type DomikTypeDto = z.infer<typeof domikTypeSchema>;

export const resourceTypeSchema = z.object({
    id: z.number(),
    name: z.string(),
    logicName: z.string(),
});
export type ResourceTypeDto = z.infer<typeof resourceTypeSchema>;

export const receiptSchema = z.object({
    id: z.number(),
    name: z.string(),
    logicName: z.string(),
    inputResources: z.array(resourceSchema),
    optionalInputResources: z.array(resourceSchema),
    durationSeconds: z.number(),
    speedupPercent: z.number(),
    outputResources: z.array(resourceSchema),
    plodderCount: z.number(),
});
export type ReceiptDto = z.infer<typeof receiptSchema>;

export const orderResourceSchema = z.object({
    resourceTypeId: z.number(),
    value: z.number(),
});
export type OrderResourceDto = z.infer<typeof orderResourceSchema>;

export const orderSchema = z.object({
    id: z.number(),
    neighborId: z.number(),
    neighborName: z.string(),
    expireDate: z.string(),
    required: z.array(orderResourceSchema),
    rewardCoins: z.number(),
    rewardGold: z.number(),
    rewardReputation: z.number(),
});
export type OrderDto = z.infer<typeof orderSchema>;

export const neighborReputationSchema = z.object({
    neighborId: z.number(),
    neighborName: z.string(),
    points: z.number(),
});
export type NeighborReputationDto = z.infer<typeof neighborReputationSchema>;

export const villageSchema = z.object({
    villageName: z.string().nullable(),
    crestIcon: z.number(),
    crestColor: z.number(),
});
export type VillageDto = z.infer<typeof villageSchema>;

export const workerSkillSchema = z.object({
    domikTypeId: z.number(),
    uses: z.number(),
    bonusPercent: z.number(),
});
export type WorkerSkillDto = z.infer<typeof workerSkillSchema>;

export const workerSchema = z.object({
    id: z.number(),
    name: z.string(),
    traitId: z.number(),
    traitName: z.string(),
    traitDurationPercent: z.number(),
    noFatigue: z.boolean(),
    manufactureId: z.number().nullable(),
    restUntil: z.string().nullable(),
    skills: z.array(workerSkillSchema),
});
export type WorkerDto = z.infer<typeof workerSchema>;

export interface PlodderCount {
    max: number;
    free: number;
}

export interface UpgradeView {
    nextLevel: number;
    resources: ResourceDto[];
    hasResources: boolean;
}

export interface ReceiptView {
    receipt: ReceiptDto;
    inputs: ResourceDto[];
    durationSeconds: number;
    hasResources: boolean;
    hasPlodders: boolean;
    canRun: boolean;
}

export interface SelectedDomikView {
    domik: DomikDto;
    domikType: DomikTypeDto;
    receipts: ReceiptDto[];
    upgrade: UpgradeView | null;
    remainingText: string | null;
}

import { z } from 'zod';

export enum ResponseType {
    Success = 1,
    ErrorMessage = 2,
}

export const responseEnvelopeSchema = z.object({
    type: z.number(),
    content: z.unknown().optional(),
});
export type ResponseEnvelope = z.infer<typeof responseEnvelopeSchema>;

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
    autoRepeat: z.boolean(),
});
export type ManufactureDto = z.infer<typeof manufactureSchema>;

export const domikSchema = z.object({
    id: z.number(),
    typeId: z.number(),
    level: z.number(),
    finishDate: z.string().nullable(),
    upgradeSeconds: z.number().nullable(),
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
    unlockLevel: z.number(),
    blueprintId: z.number().nullable(),
    levels: z.array(upgradeLevelSchema),
});
export type DomikTypeDto = z.infer<typeof domikTypeSchema>;

export const resourceTypeSchema = z.object({
    id: z.number(),
    name: z.string(),
    logicName: z.string(),
    marketValue: z.number(),
});
export type ResourceTypeDto = z.infer<typeof resourceTypeSchema>;

export const receiptSchema = z.object({
    id: z.number(),
    name: z.string(),
    logicName: z.string(),
    inputResources: z.array(resourceSchema),
    optionalInputResources: z.array(resourceSchema),
    durationSeconds: z.number(),
    outputBonusPercent: z.number(),
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
    neighborLogicName: z.string(),
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
    neighborLogicName: z.string(),
    points: z.number(),
});
export type NeighborReputationDto = z.infer<typeof neighborReputationSchema>;

export const blueprintSchema = z.object({
    id: z.number(),
    name: z.string(),
    domikTypeId: z.number(),
    neighborId: z.number(),
    neighborName: z.string(),
    reputationThreshold: z.number(),
    currentReputation: z.number(),
    owned: z.boolean(),
});
export type BlueprintDto = z.infer<typeof blueprintSchema>;

export const villageSchema = z.object({
    villageName: z.string().nullable(),
    crestIcon: z.number(),
    crestColor: z.number(),
});
export type VillageDto = z.infer<typeof villageSchema>;

export const villageLevelUnlockSchema = z.object({
    level: z.number(),
    label: z.string(),
});
export type VillageLevelUnlockDto = z.infer<typeof villageLevelUnlockSchema>;

export const villageLevelSchema = z.object({
    level: z.number(),
    buildings: z.number(),
    residents: z.number(),
    reputation: z.number(),
    comfort: z.number(),
    upcomingUnlocks: z.array(villageLevelUnlockSchema),
});
export type VillageLevelDto = z.infer<typeof villageLevelSchema>;

export const worldVillageSchema = z.object({
    playerId: z.number().nullable(),
    villageName: z.string(),
    crestIcon: z.number(),
    crestColor: z.number(),
    level: z.number(),
    isNpc: z.boolean(),
    isMe: z.boolean(),
    npcResourceTypeId: z.number().nullable(),
    seasonOrders: z.number(),
    seasonToloka: z.number(),
    seasonExpeditions: z.number(),
    comfort: z.number(),
});
export type WorldVillageDto = z.infer<typeof worldVillageSchema>;

export const seasonSchema = z.object({
    number: z.number(),
    startDate: z.string(),
    endDate: z.string(),
});
export type SeasonDto = z.infer<typeof seasonSchema>;

export const worldSchema = z.object({
    villages: z.array(worldVillageSchema),
    season: seasonSchema,
});
export type WorldDto = z.infer<typeof worldSchema>;

export const visitBuildingSchema = z.object({
    typeName: z.string(),
    level: z.number(),
});
export type VisitBuildingDto = z.infer<typeof visitBuildingSchema>;

export const villageVisitSchema = z.object({
    villageName: z.string(),
    crestIcon: z.number(),
    crestColor: z.number(),
    level: villageLevelSchema,
    buildings: z.array(visitBuildingSchema),
});
export type VillageVisitDto = z.infer<typeof villageVisitSchema>;

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
    traitLogicName: z.string(),
    traitDurationPercent: z.number(),
    noFatigue: z.boolean(),
    manufactureId: z.number().nullable(),
    expeditionId: z.number().nullable(),
    restUntil: z.string().nullable(),
    skills: z.array(workerSkillSchema),
});
export type WorkerDto = z.infer<typeof workerSchema>;

export const weatherEffectSchema = z.object({
    domikTypeId: z.number(),
    outputPercent: z.number(),
});
export type WeatherEffectDto = z.infer<typeof weatherEffectSchema>;

export const weatherPeriodSchema = z.object({
    weatherTypeId: z.number(),
    weatherName: z.string(),
    logicName: z.string(),
    startDate: z.string(),
    endDate: z.string(),
    effects: z.array(weatherEffectSchema),
});
export type WeatherPeriodDto = z.infer<typeof weatherPeriodSchema>;

export const weatherStateSchema = z.object({
    current: weatherPeriodSchema.nullable(),
    forecast: z.array(weatherPeriodSchema),
});
export type WeatherStateDto = z.infer<typeof weatherStateSchema>;

export const expeditionLootSchema = z.object({
    kind: z.number(),
    resourceTypeId: z.number().nullable(),
    decorTypeId: z.number().nullable(),
    minValue: z.number(),
    maxValue: z.number(),
    isRare: z.boolean(),
});
export type ExpeditionLootDto = z.infer<typeof expeditionLootSchema>;

export const expeditionEquipmentSchema = z.object({
    resourceTypeId: z.number(),
    value: z.number(),
});
export type ExpeditionEquipmentDto = z.infer<typeof expeditionEquipmentSchema>;

export const expeditionTypeSchema = z.object({
    id: z.number(),
    name: z.string(),
    logicName: z.string(),
    durationSeconds: z.number(),
    workerCount: z.number(),
    goldCost: z.number(),
    rollCount: z.number(),
    loot: z.array(expeditionLootSchema),
    equipment: z.array(expeditionEquipmentSchema),
});
export type ExpeditionTypeDto = z.infer<typeof expeditionTypeSchema>;

export const expeditionSchema = z.object({
    id: z.number(),
    expeditionTypeId: z.number(),
    expeditionName: z.string(),
    startDate: z.string(),
    finishDate: z.string(),
});
export type ExpeditionDto = z.infer<typeof expeditionSchema>;

export const expeditionStateSchema = z.object({
    active: z.array(expeditionSchema),
    types: z.array(expeditionTypeSchema),
    expeditionsSincePity: z.number(),
    pityThreshold: z.number(),
    maxActive: z.number(),
});
export type ExpeditionStateDto = z.infer<typeof expeditionStateSchema>;

export const decorTypeSchema = z.object({
    id: z.number(),
    name: z.string(),
    logicName: z.string(),
    comfortPoints: z.number(),
    isPurchasable: z.boolean(),
    cost: z.array(resourceSchema),
});
export type DecorTypeDto = z.infer<typeof decorTypeSchema>;

export const playerDecorSchema = z.object({
    decorTypeId: z.number(),
    count: z.number(),
});
export type PlayerDecorDto = z.infer<typeof playerDecorSchema>;

export const decorStateSchema = z.object({
    types: z.array(decorTypeSchema),
    owned: z.array(playerDecorSchema),
    comfort: z.number(),
});
export type DecorStateDto = z.infer<typeof decorStateSchema>;

export const tolokaSchema = z.object({
    id: z.number(),
    tolokaTypeId: z.number(),
    name: z.string(),
    logicName: z.string(),
    resourceTypeId: z.number(),
    goal: z.number(),
    collected: z.number(),
    startDate: z.string(),
});
export type TolokaDto = z.infer<typeof tolokaSchema>;

export const tolokaStateSchema = z.object({
    active: tolokaSchema,
    myContribution: z.number(),
    buffActive: z.boolean(),
    buffUntil: z.string().nullable(),
    buffPercent: z.number(),
    buffHours: z.number(),
    nextBuffHours: z.number().nullable(),
});
export type TolokaStateDto = z.infer<typeof tolokaStateSchema>;

export const tradeLotSchema = z.object({
    id: z.number(),
    sellerId: z.number(),
    sellerVillageName: z.string().nullable(),
    sellerCrestIcon: z.number(),
    sellerCrestColor: z.number(),
    giveResourceTypeId: z.number(),
    giveValue: z.number(),
    wantResourceTypeId: z.number(),
    wantValue: z.number(),
    commissionCoins: z.number(),
    expireDate: z.string(),
});
export type TradeLotDto = z.infer<typeof tradeLotSchema>;

export const marketStateSchema = z.object({
    lots: z.array(tradeLotSchema),
    myLots: z.array(tradeLotSchema),
    buildingLevel: z.number(),
    commissionRate: z.number(),
    commissionMin: z.number(),
    nextCommissionRate: z.number().nullable(),
    maxLots: z.number(),
});
export type MarketStateDto = z.infer<typeof marketStateSchema>;

export const recapEventSchema = z.object({
    type: z.string(),
    date: z.string(),
    data: z.unknown(),
});
export type RecapEventDto = z.infer<typeof recapEventSchema>;

export const recapSchema = z.object({
    awaySeconds: z.number(),
    events: z.array(z.unknown()).transform(events => events.flatMap(event => {
        const parsed = recapEventSchema.safeParse(event);
        return parsed.success ? [parsed.data] : [];
    })),
});
export type RecapDto = z.infer<typeof recapSchema>;

export const gameStateSchema = z.object({
    domikTypes: domikTypeSchema.array(),
    resourceTypes: resourceTypeSchema.array(),
    receipts: receiptSchema.array(),
    domiks: domikSchema.array(),
    resources: resourceSchema.array(),
    orders: orderSchema.array(),
    reputation: neighborReputationSchema.array(),
    blueprints: blueprintSchema.array(),
    village: villageSchema,
    villageLevel: villageLevelSchema,
    workers: workerSchema.array(),
    purchaseAvailableDomiks: domikTypeSchema.array(),
    weather: weatherStateSchema,
    expeditions: expeditionStateSchema.nullable(),
    decor: decorStateSchema,
    toloka: tolokaStateSchema.nullable(),
    market: marketStateSchema.nullable(),
    recap: recapSchema.nullish(),
    events: z.array(z.unknown()).transform(items => items.flatMap(item => {
        const parsed = recapEventSchema.safeParse(item);
        return parsed.success ? [parsed.data] : [];
    })).optional().default([]),
});
export type GameStateDto = z.infer<typeof gameStateSchema>;

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

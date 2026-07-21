import { z } from 'zod';

export const problemDetailsSchema = z.object({
    type: z.string().optional(),
    title: z.string().optional(),
    status: z.number().optional(),
    detail: z.string().optional(),
});
export type ProblemDetails = z.infer<typeof problemDetailsSchema>;

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
    durationSeconds: z.number(),
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
    maxManufactureCount: z.number(),
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
    nextCountGateLevel: z.number().nullable(),
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

export const errandSchema = z.object({
    id: z.number(),
    neighborId: z.number(),
    neighborName: z.string(),
    neighborLogicName: z.string(),
    templateId: z.number(),
    expireDate: z.string(),
    acceptDate: z.string().nullable(),
    clueId: z.number().nullable(),
    finishDate: z.string().nullable(),
    workerIds: z.array(z.number()),
});
export type ErrandDto = z.infer<typeof errandSchema>;

export const incidentSchema = z.object({
    id: z.number(),
    missingWorkerId: z.number(),
    expeditionTypeId: z.number(),
    templateId: z.number(),
    createDate: z.string(),
    clueId: z.number().nullable(),
    searchEndDate: z.string().nullable(),
    autoReturnDate: z.string(),
    searchWorkerIds: z.array(z.number()),
});
export type IncidentDto = z.infer<typeof incidentSchema>;

export const domikIncidentSchema = z.object({
    id: z.number(),
    domikTypeId: z.number(),
    templateId: z.number(),
    createDate: z.string(),
    clueId: z.number().nullable(),
    searchEndDate: z.string().nullable(),
    autoResolveDate: z.string(),
    searchWorkerIds: z.array(z.number()),
});
export type DomikIncidentDto = z.infer<typeof domikIncidentSchema>;

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
    feedWorkers: z.boolean(),
});
export type VillageDto = z.infer<typeof villageSchema>;

export const villageLevelUnlockSchema = z.object({
    level: z.number().nullable(),
    label: z.string(),
    requirement: z.string().nullable(),
    unlocked: z.boolean(),
    kind: z.string(),
    logicName: z.string().nullable(),
});
export type VillageLevelUnlockDto = z.infer<typeof villageLevelUnlockSchema>;

export const villageLevelSchema = z.object({
    level: z.number(),
    buildings: z.number(),
    residents: z.number(),
    reputation: z.number(),
    comfort: z.number(),
    visitsSinceBigGift: z.number(),
    unlocks: z.array(villageLevelUnlockSchema),
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
    npcLogicName: z.string().nullable(),
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

export const tolokaArtifactSchema = z.object({
    name: z.string(),
    resourcesText: z.string(),
    seasonNumber: z.number(),
    participants: z.number(),
    completedDate: z.string(),
});
export type TolokaArtifactDto = z.infer<typeof tolokaArtifactSchema>;

export const worldSchema = z.object({
    villages: z.array(worldVillageSchema),
    season: seasonSchema,
    tolokaArtifacts: z.array(tolokaArtifactSchema),
});
export type WorldDto = z.infer<typeof worldSchema>;

export const visitBuildingSchema = z.object({
    typeName: z.string(),
    level: z.number(),
});
export type VisitBuildingDto = z.infer<typeof visitBuildingSchema>;

export const guestbookEntrySchema = z.object({
    guestPlayerId: z.number(),
    guestVillageName: z.string(),
    guestCrestIcon: z.number(),
    guestCrestColor: z.number(),
    phraseId: z.number(),
    date: z.string(),
});
export type GuestbookEntryDto = z.infer<typeof guestbookEntrySchema>;

export const guestbookSchema = z.object({
    visitsThisSeason: z.number(),
    entries: z.array(guestbookEntrySchema),
});
export type GuestbookDto = z.infer<typeof guestbookSchema>;

export const villageVisitSchema = z.object({
    villageName: z.string(),
    crestIcon: z.number(),
    crestColor: z.number(),
    level: villageLevelSchema,
    buildings: z.array(visitBuildingSchema),
    guestbook: z.array(guestbookEntrySchema),
    canLeaveEntry: z.boolean(),
    alreadyLeftToday: z.boolean(),
    guestbookUnlockLevel: z.number(),
    canHelp: z.boolean(),
    alreadyHelpedToday: z.boolean(),
    hostCapReached: z.boolean(),
    hasActiveWork: z.boolean(),
    helpUnlockLevel: z.number(),
});
export type VillageVisitDto = z.infer<typeof villageVisitSchema>;

export const helpResultSchema = z.object({
    domikTypeName: z.string(),
    reducedSeconds: z.number(),
    rewardCoins: z.number(),
});
export type HelpResultDto = z.infer<typeof helpResultSchema>;

export const workerSkillSchema = z.object({
    domikTypeId: z.number(),
    uses: z.number(),
    bonusPercent: z.number(),
});
export type WorkerSkillDto = z.infer<typeof workerSkillSchema>;

export const workerSchema = z.object({
    id: z.number(),
    name: z.string(),
    gender: z.number(),
    traitId: z.number(),
    traitName: z.string(),
    traitLogicName: z.string(),
    traitDurationPercent: z.number(),
    noFatigue: z.boolean(),
    noSick: z.boolean(),
    manufactureId: z.number().nullable(),
    expeditionId: z.number().nullable(),
    errandId: z.number().nullable(),
    incidentId: z.number().nullable(),
    workedSeconds: z.number(),
    restUntil: z.string().nullable(),
    sickUntil: z.string().nullable(),
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
    blueprintId: z.number().nullable(),
    minValue: z.number(),
    maxValue: z.number(),
    isRare: z.boolean(),
});
export type ExpeditionLootDto = z.infer<typeof expeditionLootSchema>;

export const expeditionEquipmentSchema = z.object({
    resourceTypeId: z.number(),
    value: z.number(),
    isOptional: z.boolean(),
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
    neighborId: z.number().nullable(),
    neighborName: z.string().nullable(),
    reputationThreshold: z.number(),
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

export const tolokaPositionSchema = z.object({
    resourceTypeId: z.number(),
    goal: z.number(),
    collected: z.number(),
    myContribution: z.number(),
});
export type TolokaPositionDto = z.infer<typeof tolokaPositionSchema>;

export const tolokaSchema = z.object({
    id: z.number(),
    tolokaTypeId: z.number(),
    name: z.string(),
    logicName: z.string(),
    positions: z.array(tolokaPositionSchema),
    startDate: z.string(),
});
export type TolokaDto = z.infer<typeof tolokaSchema>;

export const tolokaActiveBuffSchema = z.object({
    logicName: z.string(),
    label: z.string(),
    percent: z.number(),
    buffUntil: z.string(),
});
export type TolokaActiveBuffDto = z.infer<typeof tolokaActiveBuffSchema>;

export const tolokaVoteCandidateSchema = z.object({
    tolokaTypeId: z.number(),
    name: z.string(),
    logicName: z.string(),
    votes: z.number(),
});
export type TolokaVoteCandidateDto = z.infer<typeof tolokaVoteCandidateSchema>;

export const tolokaStateSchema = z.object({
    active: tolokaSchema,
    activeBuffs: z.array(tolokaActiveBuffSchema),
    buffHours: z.number(),
    nextBuffHours: z.number().nullable(),
    candidates: z.array(tolokaVoteCandidateSchema),
    myVoteTolokaTypeId: z.number().nullable(),
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
    kind: z.number(),
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

export const activeGoalSchema = z.object({
    id: z.number(),
    ordinal: z.number(),
    name: z.string(),
    rewardCoins: z.number(),
});
export type ActiveGoalDto = z.infer<typeof activeGoalSchema>;

export const goalsStateSchema = z.object({
    active: activeGoalSchema.nullable(),
    completedCount: z.number(),
    totalCount: z.number(),
    zealCharges: z.number(),
});
export type GoalsStateDto = z.infer<typeof goalsStateSchema>;

export const gameStateSchema = z.object({
    domikTypes: domikTypeSchema.array(),
    resourceTypes: resourceTypeSchema.array(),
    receipts: receiptSchema.array(),
    domiks: domikSchema.array(),
    resources: resourceSchema.array(),
    orders: orderSchema.array(),
    errand: errandSchema.nullable(),
    incident: incidentSchema.nullable(),
    domikIncident: domikIncidentSchema.nullable(),
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
    goals: goalsStateSchema,
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
    effectiveDurationSeconds: number;
    zealMultiplier: number;
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

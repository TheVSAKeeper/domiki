import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, it } from 'vitest';
import * as api from './api';

type DtoClass = {
    name: string;
    propertyNames: string[];
};

type ContractSchema = {
    schemaName: string;
    shape: Record<string, unknown>;
};

const backendDirectory = resolve(fileURLToPath(import.meta.url), '../../../..');

const mappings: Record<string, ContractSchema> = {
    ActiveGoalDto: { schemaName: 'activeGoalSchema', shape: api.activeGoalSchema.shape },
    BlueprintDto: { schemaName: 'blueprintSchema', shape: api.blueprintSchema.shape },
    CloakStateDto: { schemaName: 'cloakStateSchema', shape: api.cloakStateSchema.shape },
    ConvoyDto: { schemaName: 'convoySchema', shape: api.convoySchema.shape },
    ConvoyItemDto: { schemaName: 'convoyItemSchema', shape: api.convoyItemSchema.shape },
    DecorStateDto: { schemaName: 'decorStateSchema', shape: api.decorStateSchema.shape },
    DecorTypeDto: { schemaName: 'decorTypeSchema', shape: api.decorTypeSchema.shape },
    DomikDto: { schemaName: 'domikSchema', shape: api.domikSchema.shape },
    DomikTypeDto: { schemaName: 'domikTypeSchema', shape: api.domikTypeSchema.shape },
    ErrandDto: { schemaName: 'errandSchema', shape: api.errandSchema.shape },
    IncidentDto: { schemaName: 'incidentSchema', shape: api.incidentSchema.shape },
    DomikIncidentDto: { schemaName: 'domikIncidentSchema', shape: api.domikIncidentSchema.shape },
    ExpeditionDto: { schemaName: 'expeditionSchema', shape: api.expeditionSchema.shape },
    ExpeditionEquipmentDto: { schemaName: 'expeditionEquipmentSchema', shape: api.expeditionEquipmentSchema.shape },
    ExpeditionLootDto: { schemaName: 'expeditionLootSchema', shape: api.expeditionLootSchema.shape },
    ExpeditionStateDto: { schemaName: 'expeditionStateSchema', shape: api.expeditionStateSchema.shape },
    ExpeditionTypeDto: { schemaName: 'expeditionTypeSchema', shape: api.expeditionTypeSchema.shape },
    GameStateDto: { schemaName: 'gameStateSchema', shape: api.gameStateSchema.shape },
    GoalsStateDto: { schemaName: 'goalsStateSchema', shape: api.goalsStateSchema.shape },
    GuestbookDto: { schemaName: 'guestbookSchema', shape: api.guestbookSchema.shape },
    GuestbookEntryDto: { schemaName: 'guestbookEntrySchema', shape: api.guestbookEntrySchema.shape },
    HelpResultDto: { schemaName: 'helpResultSchema', shape: api.helpResultSchema.shape },
    ManufactureDto: { schemaName: 'manufactureSchema', shape: api.manufactureSchema.shape },
    MarketStateDto: { schemaName: 'marketStateSchema', shape: api.marketStateSchema.shape },
    ModificatorDto: { schemaName: 'modificatorSchema', shape: api.modificatorSchema.shape },
    NeighborReputationDto: { schemaName: 'neighborReputationSchema', shape: api.neighborReputationSchema.shape },
    OrderDto: { schemaName: 'orderSchema', shape: api.orderSchema.shape },
    OrderResourceDto: { schemaName: 'orderResourceSchema', shape: api.orderResourceSchema.shape },
    PlayerDecorDto: { schemaName: 'playerDecorSchema', shape: api.playerDecorSchema.shape },
    RecapDto: { schemaName: 'recapSchema', shape: api.recapSchema.shape },
    RecapEventDto: { schemaName: 'recapEventSchema', shape: api.recapEventSchema.shape },
    ReceiptDto: { schemaName: 'receiptSchema', shape: api.receiptSchema.shape },
    ResourceDto: { schemaName: 'resourceSchema', shape: api.resourceSchema.shape },
    ResourceTypeDto: { schemaName: 'resourceTypeSchema', shape: api.resourceTypeSchema.shape },
    SeasonDto: { schemaName: 'seasonSchema', shape: api.seasonSchema.shape },
    SickTypeDto: { schemaName: 'sickTypeSchema', shape: api.sickTypeSchema.shape },
    TolokaActiveBuffDto: { schemaName: 'tolokaActiveBuffSchema', shape: api.tolokaActiveBuffSchema.shape },
    TolokaArtifactDto: { schemaName: 'tolokaArtifactSchema', shape: api.tolokaArtifactSchema.shape },
    TolokaDto: { schemaName: 'tolokaSchema', shape: api.tolokaSchema.shape },
    TolokaPositionDto: { schemaName: 'tolokaPositionSchema', shape: api.tolokaPositionSchema.shape },
    TolokaStateDto: { schemaName: 'tolokaStateSchema', shape: api.tolokaStateSchema.shape },
    TolokaVoteCandidateDto: { schemaName: 'tolokaVoteCandidateSchema', shape: api.tolokaVoteCandidateSchema.shape },
    TradeLotDto: { schemaName: 'tradeLotSchema', shape: api.tradeLotSchema.shape },
    UpgradeLevelDto: { schemaName: 'upgradeLevelSchema', shape: api.upgradeLevelSchema.shape },
    VillageDto: { schemaName: 'villageSchema', shape: api.villageSchema.shape },
    VillageLevelDto: { schemaName: 'villageLevelSchema', shape: api.villageLevelSchema.shape },
    VillageLevelUnlockDto: { schemaName: 'villageLevelUnlockSchema', shape: api.villageLevelUnlockSchema.shape },
    VillageVisitDto: { schemaName: 'villageVisitSchema', shape: api.villageVisitSchema.shape },
    VisitBuildingDto: { schemaName: 'visitBuildingSchema', shape: api.visitBuildingSchema.shape },
    WeatherEffectDto: { schemaName: 'weatherEffectSchema', shape: api.weatherEffectSchema.shape },
    WeatherPeriodDto: { schemaName: 'weatherPeriodSchema', shape: api.weatherPeriodSchema.shape },
    WeatherStateDto: { schemaName: 'weatherStateSchema', shape: api.weatherStateSchema.shape },
    WorkerDto: { schemaName: 'workerSchema', shape: api.workerSchema.shape },
    WorkerSkillDto: { schemaName: 'workerSkillSchema', shape: api.workerSkillSchema.shape },
    WorldDto: { schemaName: 'worldSchema', shape: api.worldSchema.shape },
    WorldVillageDto: { schemaName: 'worldVillageSchema', shape: api.worldVillageSchema.shape },
};

const skippedDtos: Record<string, string> = {
    BuyFromConvoyDto: 'request payload BuyFromConvoy отправляется без zod-схемы',
    SetVillageDto: 'request payload SetVillage отправляется без zod-схемы',
    SetFriendNeighborDto: 'request payload SetFriendNeighbor отправляется без zod-схемы',
    StartIncidentSearchDto: 'request payload StartIncidentSearch отправляется без zod-схемы',
    PushSubscribeDto: 'request payload Push/Subscribe отправляется без zod-схемы',
    PushUnsubscribeDto: 'request payload Push/Unsubscribe отправляется без zod-схемы',
};

const getClassBody = (source: string, openingBraceIndex: number): string => {
    let depth = 0;

    for (let index = openingBraceIndex; index < source.length; index += 1) {
        const character = source[index];

        if (character === '{') {
            depth += 1;
        }

        if (character === '}') {
            depth -= 1;

            if (depth === 0) {
                return source.slice(openingBraceIndex + 1, index);
            }
        }
    }

    throw new Error('Не найдена закрывающая скобка класса DTO');
};

const getSerializedPropertyName = (attributes: string, propertyName: string): string => {
    const jsonPropertyName = /\[\s*(?:[\w.]+\.)?JsonPropertyName\s*\(\s*"(?<name>[^"]+)"\s*\)\s*\]/.exec(attributes)?.groups?.name;
    return jsonPropertyName ?? `${propertyName[0]?.toLowerCase() ?? ''}${propertyName.slice(1)}`;
};

const getBraceDepth = (source: string, endIndex: number): number => {
    let depth = 0;

    for (let index = 0; index < endIndex; index += 1) {
        if (source[index] === '{') {
            depth += 1;
        }

        if (source[index] === '}') {
            depth -= 1;
        }
    }

    return depth;
};

const parseDtoClasses = (source: string): DtoClass[] => {
    const classExpression = /\bpublic\s+(?:sealed\s+)?(?:record|class)\s+(?<name>\w+Dto)\b/g;
    const propertyExpression = /(?<attributes>(?:\s*\[[^]]+\]\s*)*)public\s+(?:required\s+)?[\w.]+(?:\s*<[^>{}]+>)?(?:\?|\[\])*\s+(?<name>\w+)\s*\{\s*get;\s*(?:(?:(?:public|protected|internal|private)\s+)?(?:set|init);\s*)?\}/g;
    const dtoClasses: DtoClass[] = [];

    for (const classMatch of source.matchAll(classExpression)) {
        const className = classMatch.groups?.name;
        const classIndex = classMatch.index;

        if (className === undefined) {
            continue;
        }

        const openingBraceIndex = source.indexOf('{', classIndex + classMatch[0].length);

        if (openingBraceIndex === -1) {
            throw new Error(`Не найдена открывающая скобка класса ${className}`);
        }

        const classBody = getClassBody(source, openingBraceIndex);
        const propertyNames = [...classBody.matchAll(propertyExpression)].flatMap(propertyMatch => {
            const propertyName = propertyMatch.groups?.name;

            return propertyName === undefined || getBraceDepth(classBody, propertyMatch.index) !== 0
                ? []
                : [getSerializedPropertyName(propertyMatch.groups?.attributes ?? '', propertyName)];
        });

        dtoClasses.push({ name: className, propertyNames });
    }

    return dtoClasses;
};

const readDtoClasses = (): DtoClass[] => readdirSync(backendDirectory, { withFileTypes: true })
    .filter(entry => entry.isDirectory())
    .map(entry => resolve(backendDirectory, entry.name, 'Dto'))
    .filter(dtoDirectory => existsSync(dtoDirectory))
    .flatMap(dtoDirectory => readdirSync(dtoDirectory)
        .filter(fileName => fileName.endsWith('.cs'))
        .flatMap(fileName => parseDtoClasses(readFileSync(resolve(dtoDirectory, fileName), 'utf8'))));

describe('C# DTO and zod schema contracts', () => {
    it('models every serialized DTO property and every schema key', () => {
        const dtoClasses = readDtoClasses();
        const dtoNames = new Set(dtoClasses.map(dtoClass => dtoClass.name));
        const unclassifiedDtos = [...dtoNames]
            .filter(dtoName => mappings[dtoName] === undefined && skippedDtos[dtoName] === undefined);

        if (unclassifiedDtos.length > 0) {
            throw new Error(`DTO без zod-схемы или skip-причины: ${unclassifiedDtos.join(', ')}`);
        }

        const staleMappings = Object.keys(mappings).filter(mappedName => !dtoNames.has(mappedName));

        if (staleMappings.length > 0) {
            throw new Error(`Маппинг ссылается на несуществующие DTO: ${staleMappings.join(', ')}`);
        }

        for (const dtoClass of dtoClasses) {
            const mapping = mappings[dtoClass.name];

            if (mapping === undefined) {
                continue;
            }

            const schemaKeys = Object.keys(mapping.shape);
            const missingSchemaKeys = dtoClass.propertyNames.filter(propertyName => !schemaKeys.includes(propertyName));
            const missingDtoProperties = schemaKeys.filter(schemaKey => !dtoClass.propertyNames.includes(schemaKey));

            if (missingSchemaKeys.length > 0) {
                throw new Error(`${dtoClass.name} -> ${mapping.schemaName}: в zod-схеме нет полей ${missingSchemaKeys.join(', ')}`);
            }

            if (missingDtoProperties.length > 0) {
                throw new Error(`${dtoClass.name} -> ${mapping.schemaName}: в C# DTO нет полей ${missingDtoProperties.join(', ')}`);
            }
        }
    });
});

import { useCallback, useEffect, useRef, useState } from 'react';
import { acceptLot as acceptLotApi, apiGet, ApiError, buyDecor as buyDecorApi, buyFromConvoy as buyFromConvoyApi, cancelLot as cancelLotApi, contributeToloka as contributeTolokaApi, getDecor, getGameState, getMarket, getToloka, getVillage, hurryDomik as hurryDomikApi, hurryManufacture as hurryManufactureApi, postLot as postLotApi, setManufactureAutoRepeat as setManufactureAutoRepeatApi, setVillage as setVillageApi, startExpedition as startExpeditionApi, voteToloka as voteTolokaApi } from '../services/api';
import { useToast } from '../services/toastContext';
import {
    domikTypeSchema,
    resourceSchema,
    villageLevelSchema,
    type BlueprintDto,
    type CloakStateDto,
    type DecorStateDto,
    type DomikDto,
    type DomikIncidentDto,
    type DomikTypeDto,
    type ConvoyDto,
    type ErrandDto,
    type IncidentDto,
    type ExpeditionStateDto,
    type GoalsStateDto,
    type MarketStateDto,
    type NeighborReputationDto,
    type OrderDto,
    type ReceiptDto,
    type ResourceDto,
    type ResourceTypeDto,
    type RecapDto,
    type RecapEventDto,
    type SickTypeDto,
    type TolokaStateDto,
    type VillageDto,
    type VillageLevelDto,
    type WeatherStateDto,
    type WorkerDto,
} from '../types/api';
import { isNumber, isRecord } from '../utils/recap';
import { remainingSeconds } from '../utils/time';
import { getWorkerMilestoneTemplate, workerMilestoneText } from '../utils/workerMilestoneTexts';

export interface GameData {
    domiks: DomikDto[];
    domikTypes: DomikTypeDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
    resources: ResourceDto[];
    orders: OrderDto[];
    errand: ErrandDto | null;
    incident: IncidentDto | null;
    domikIncident: DomikIncidentDto | null;
    reputation: NeighborReputationDto[];
    blueprints: BlueprintDto[];
    loading: boolean;
    village: VillageDto | null;
    villageLevel: VillageLevelDto | null;
    weather: WeatherStateDto | null;
    expeditions: ExpeditionStateDto | null;
    decor: DecorStateDto | null;
    toloka: TolokaStateDto | null;
    market: MarketStateDto | null;
    convoys: ConvoyDto[];
    goals: GoalsStateDto | null;
    workers: WorkerDto[];
    cloaks: CloakStateDto;
    sickTypes: SickTypeDto[];
    purchaseDomikTypes: DomikTypeDto[] | null;
    now: number;
    reload: () => Promise<void>;
    scheduleReload: () => void;
    refreshPurchaseTypes: () => Promise<void>;
    setVillage: (name: string, crestIcon: number, crestColor: number) => Promise<void>;
    hurryManufacture: (manufactureId: number) => Promise<void>;
    setManufactureAutoRepeat: (manufactureId: number, autoRepeat: boolean) => Promise<void>;
    hurryDomik: (domikId: number) => Promise<void>;
    startExpedition: (expeditionTypeId: number, workerIds?: number[], provisions?: boolean) => Promise<void>;
    buyDecor: (decorTypeId: number) => Promise<void>;
    contributeToloka: (resourceTypeId: number, amount: number) => Promise<void>;
    voteToloka: (tolokaTypeId: number) => Promise<void>;
    postLot: (kind: number, giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number) => Promise<void>;
    acceptLot: (lotId: number) => Promise<void>;
    cancelLot: (lotId: number) => Promise<void>;
    buyFromConvoy: (neighborId: number, resourceTypeId: number, count: number) => Promise<void>;
    recap: RecapDto | null;
    clearRecap: () => void;
    events: RecapEventDto[];
}

const workerMilestoneEvent = (event: RecapEventDto) => {
    if (event.type !== 'WorkerMilestone' || !isRecord(event.data) || !isNumber(event.data.workerId) || !isNumber(event.data.milestoneType) || typeof event.data.workerName !== 'string' || !isNumber(event.data.workerGender)) {
        return null;
    }
    return {
        event,
        key: `${event.type}|${event.date}|${event.data.workerId}|${event.data.milestoneType}`,
        milestoneType: event.data.milestoneType,
        workerName: event.data.workerName,
        workerGender: event.data.workerGender,
        workerName2: typeof event.data.workerName2 === 'string' ? event.data.workerName2 : undefined,
    };
};

export function useGameData(): GameData {
    const toast = useToast();

    const [domiks, setDomiks] = useState<DomikDto[]>([]);
    const [domikTypes, setDomikTypes] = useState<DomikTypeDto[]>([]);
    const [resourceTypes, setResourceTypes] = useState<ResourceTypeDto[]>([]);
    const [receipts, setReceipts] = useState<ReceiptDto[]>([]);
    const [resources, setResources] = useState<ResourceDto[]>([]);
    const [orders, setOrders] = useState<OrderDto[]>([]);
    const [errand, setErrand] = useState<ErrandDto | null>(null);
    const [incident, setIncident] = useState<IncidentDto | null>(null);
    const [domikIncident, setDomikIncident] = useState<DomikIncidentDto | null>(null);
    const [reputation, setReputation] = useState<NeighborReputationDto[]>([]);
    const [blueprints, setBlueprints] = useState<BlueprintDto[]>([]);
    const [village, setVillageState] = useState<VillageDto | null>(null);
    const [villageLevel, setVillageLevel] = useState<VillageLevelDto | null>(null);
    const [weather, setWeather] = useState<WeatherStateDto | null>(null);
    const [expeditions, setExpeditions] = useState<ExpeditionStateDto | null>(null);
    const [decor, setDecor] = useState<DecorStateDto | null>(null);
    const [toloka, setToloka] = useState<TolokaStateDto | null>(null);
    const [market, setMarket] = useState<MarketStateDto | null>(null);
    const [convoys, setConvoys] = useState<ConvoyDto[]>([]);
    const [goals, setGoals] = useState<GoalsStateDto | null>(null);
    const [workers, setWorkers] = useState<WorkerDto[]>([]);
    const [cloaks, setCloaks] = useState<CloakStateDto>({ stock: 0, outOnShifts: 0, wearPoints: 0, lifetimeShifts: 0 });
    const [sickTypes, setSickTypes] = useState<SickTypeDto[]>([]);
    const [purchaseDomikTypes, setPurchaseDomikTypes] = useState<DomikTypeDto[] | null>(null);
    const [recap, setRecap] = useState<RecapDto | null>(null);
    const [events, setEvents] = useState<RecapEventDto[]>([]);
    const [now, setNow] = useState(() => Date.now());
    const [loading, setLoading] = useState(true);

    const refetching = useRef(false);
    const pendingReload = useRef(false);
    const workersRef = useRef(workers);
    const domiksRef = useRef(domiks);
    const expeditionsRef = useRef(expeditions);
    const goalsRef = useRef(goals);
    const eventsRef = useRef<RecapEventDto[] | undefined>(undefined);
    const tolokaRef = useRef(toloka);
    const errandRef = useRef(errand);
    const incidentRef = useRef(incident);
    const domikIncidentRef = useRef(domikIncident);
    const convoysRef = useRef(convoys);
    const reloadedRestDeadlinesRef = useRef<Set<string>>(new Set());
    const reloadedTolokaBuffDeadlinesRef = useRef<Set<string>>(new Set());
    const reloadedFinishDeadlinesRef = useRef<Set<string>>(new Set());

    useEffect(() => {
        workersRef.current = workers;
    }, [workers]);

    useEffect(() => {
        domiksRef.current = domiks;
    }, [domiks]);

    useEffect(() => {
        expeditionsRef.current = expeditions;
    }, [expeditions]);

    useEffect(() => {
        goalsRef.current = goals;
    }, [goals]);

    useEffect(() => {
        eventsRef.current = events;
    }, [events]);

    useEffect(() => {
        tolokaRef.current = toloka;
    }, [toloka]);

    useEffect(() => {
        convoysRef.current = convoys;
    }, [convoys]);

    useEffect(() => {
        errandRef.current = errand;
    }, [errand]);

    useEffect(() => {
        incidentRef.current = incident;
    }, [incident]);

    useEffect(() => {
        domikIncidentRef.current = domikIncident;
    }, [domikIncident]);

    const reload = useCallback(async () => {
        const state = await getGameState();
        const prevActive = expeditionsRef.current?.active ?? [];
        const nextActive = state.expeditions?.active ?? [];
        for (const finished of prevActive) {
            if (!nextActive.some(active => active.id === finished.id)) {
                toast.success(`Экспедиция «${finished.expeditionName}» вернулась`);
            }
        }
        const prevGoal = goalsRef.current?.active;
        if (prevGoal != null && (state.goals.active == null || state.goals.active.ordinal > prevGoal.ordinal)) {
            toast.success(`Наказ выполнен: «${prevGoal.name}» (+${prevGoal.rewardCoins} монет)`);
        }
        const previousEvents = eventsRef.current;
        if (previousEvents != null) {
            const previousMilestoneKeys = new Set(previousEvents.flatMap(event => {
                const milestone = workerMilestoneEvent(event);
                return milestone == null ? [] : [milestone.key];
            }));
            const newMilestones = state.events.flatMap(event => {
                const milestone = workerMilestoneEvent(event);
                return milestone == null || previousMilestoneKeys.has(milestone.key) ? [] : [milestone];
            });
            const [firstMilestone, ...remainingMilestones] = newMilestones;
            if (firstMilestone != null) {
                const newestMilestone = remainingMilestones.reduce((newest, milestone) => Date.parse(milestone.event.date) > Date.parse(newest.event.date) ? milestone : newest, firstMilestone);
                const template = getWorkerMilestoneTemplate(newestMilestone.milestoneType);
                toast.success(workerMilestoneText(template.toast, newestMilestone.workerName, newestMilestone.workerGender, newestMilestone.workerName2));
            }
        }
        eventsRef.current = state.events;
        setDomiks(state.domiks);
        setPurchaseDomikTypes(state.purchaseAvailableDomiks);
        setResources(state.resources);
        setOrders(state.orders);
        setErrand(state.errand);
        setIncident(state.incident);
        setDomikIncident(state.domikIncident);
        setReputation(state.reputation);
        setBlueprints(state.blueprints);
        setVillageState(state.village);
        setVillageLevel(state.villageLevel);
        setWorkers(state.workers);
        setCloaks(state.cloaks);
        setSickTypes(state.sickTypes);
        setWeather(state.weather);
        setExpeditions(state.expeditions);
        setDecor(state.decor);
        setToloka(state.toloka);
        setMarket(state.market);
        setConvoys(state.convoys);
        setGoals(state.goals);
        setEvents(state.events);
        if (state.recap != null && state.recap.events.length > 0) {
            setRecap(state.recap);
        }
    }, [toast]);

    const scheduleReload = useCallback(() => {
        if (refetching.current) {
            pendingReload.current = true;
            return;
        }

        refetching.current = true;

        const run = async (): Promise<void> => {
            try {
                await reload();
            } catch (err) {
                if (err instanceof ApiError) {
                    toast.error(err.message);
                } else {
                    throw err;
                }
            } finally {
                if (pendingReload.current) {
                    pendingReload.current = false;
                    void run();
                } else {
                    refetching.current = false;
                }
            }
        };

        void run();
    }, [reload, toast]);

    const refreshPurchaseTypes = useCallback(async () => {
        setPurchaseDomikTypes(await apiGet('Domiki/GetPurchaseAvaialableDomiks', domikTypeSchema.array()));
    }, []);

    const setVillage = useCallback(async (name: string, crestIcon: number, crestColor: number) => {
        await setVillageApi(name, crestIcon, crestColor);
        setVillageState(await getVillage());
    }, []);

    const hurryManufacture = useCallback(async (manufactureId: number) => {
        await hurryManufactureApi(manufactureId);
        scheduleReload();
    }, [scheduleReload]);

    const setManufactureAutoRepeat = useCallback(async (manufactureId: number, autoRepeat: boolean) => {
        await setManufactureAutoRepeatApi(manufactureId, autoRepeat);
        scheduleReload();
    }, [scheduleReload]);

    const hurryDomik = useCallback(async (domikId: number) => {
        await hurryDomikApi(domikId);
        scheduleReload();
    }, [scheduleReload]);

    const startExpedition = useCallback(async (expeditionTypeId: number, workerIds?: number[], provisions?: boolean) => {
        await startExpeditionApi(expeditionTypeId, workerIds, provisions);
        scheduleReload();
    }, [scheduleReload]);

    const contributeToloka = useCallback(async (resourceTypeId: number, amount: number) => {
        await contributeTolokaApi(resourceTypeId, amount);
        const [nextToloka, nextResources] = await Promise.all([
            getToloka(),
            apiGet('Domiki/GetResources', resourceSchema.array()),
        ]);
        setToloka(nextToloka);
        setResources(nextResources);
    }, []);

    const voteToloka = useCallback(async (tolokaTypeId: number) => {
        await voteTolokaApi(tolokaTypeId);
        setToloka(await getToloka());
    }, []);

    const refreshMarketAndResources = useCallback(async () => {
        const [nextMarket, nextResources] = await Promise.all([
            getMarket(),
            apiGet('Domiki/GetResources', resourceSchema.array()),
        ]);
        setMarket(nextMarket);
        setResources(nextResources);
    }, []);

    const postLot = useCallback(async (kind: number, giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number) => {
        await postLotApi(kind, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue);
        await refreshMarketAndResources();
    }, [refreshMarketAndResources]);

    const acceptLot = useCallback(async (lotId: number) => {
        await acceptLotApi(lotId);
        await refreshMarketAndResources();
    }, [refreshMarketAndResources]);

    const cancelLot = useCallback(async (lotId: number) => {
        await cancelLotApi(lotId);
        await refreshMarketAndResources();
    }, [refreshMarketAndResources]);

    const buyFromConvoy = useCallback(async (neighborId: number, resourceTypeId: number, count: number) => {
        await buyFromConvoyApi(neighborId, resourceTypeId, count);
        scheduleReload();
    }, [scheduleReload]);

    const clearRecap = useCallback(() => setRecap(null), []);

    const buyDecor = useCallback(async (decorTypeId: number) => {
        await buyDecorApi(decorTypeId);
        const [nextDecor, nextResources, nextVillageLevel] = await Promise.all([
            getDecor(),
            apiGet('Domiki/GetResources', resourceSchema.array()),
            apiGet('Domiki/GetVillageLevel', villageLevelSchema),
        ]);
        setDecor(nextDecor);
        setResources(nextResources);
        setVillageLevel(nextVillageLevel);
    }, []);

    useEffect(() => {
        const id = setInterval(() => setNow(Date.now()), 1000);
        return () => clearInterval(id);
    }, []);

    useEffect(() => {
        const controller = new AbortController();
        const { signal } = controller;

        void (async () => {
            try {
                const state = await getGameState(signal);
                setDomikTypes(state.domikTypes);
                setResourceTypes(state.resourceTypes);
                setReceipts(state.receipts);
                setDomiks(state.domiks);
                setResources(state.resources);
                setOrders(state.orders);
                setErrand(state.errand);
                setIncident(state.incident);
                setDomikIncident(state.domikIncident);
                setReputation(state.reputation);
                setBlueprints(state.blueprints);
                setVillageState(state.village);
                setVillageLevel(state.villageLevel);
                setWorkers(state.workers);
                setPurchaseDomikTypes(state.purchaseAvailableDomiks);
                setWeather(state.weather);
                setExpeditions(state.expeditions);
                setDecor(state.decor);
                setToloka(state.toloka);
                setMarket(state.market);
                setConvoys(state.convoys);
                setGoals(state.goals);
                setEvents(state.events);
                if (state.recap != null && state.recap.events.length > 0) {
                    setRecap(state.recap);
                }
            } catch (err) {
                if (err instanceof DOMException && err.name === 'AbortError') {
                    return;
                }
                if (err instanceof ApiError) {
                    toast.error(err.message);
                }
            } finally {
                if (!signal.aborted) {
                    setLoading(false);
                }
            }
        })();

        return () => controller.abort();
    }, [toast]);

    useEffect(() => {
        const source = new EventSource('Domiki/Stream');
        let opened = false;

        source.onmessage = event => {
            if (event.data === 'state') {
                scheduleReload();
                return;
            }

            if (event.data === 'market') {
                void getMarket()
                    .then(setMarket)
                    .catch((err: unknown) => {
                        if (err instanceof ApiError) {
                            toast.error(err.message);
                            return;
                        }
                        throw err;
                    });
                return;
            }

            if (event.data === 'toloka') {
                void getToloka()
                    .then(setToloka)
                    .catch((err: unknown) => {
                        if (err instanceof ApiError) {
                            toast.error(err.message);
                            return;
                        }
                        throw err;
                    });
            }
        };

        source.onopen = () => {
            if (opened) {
                scheduleReload();
            }
            opened = true;
        };

        return () => source.close();
    }, [scheduleReload, toast]);

    useEffect(() => {
        const handleVisibilityChange = () => {
            if (document.visibilityState === 'visible') {
                scheduleReload();
            }
        };

        document.addEventListener('visibilitychange', handleVisibilityChange);
        return () => document.removeEventListener('visibilitychange', handleVisibilityChange);
    }, [scheduleReload]);

    useEffect(() => {
        let expiredWorkerRest = false;
        for (const worker of workersRef.current) {
            if (worker.restUntil == null) {
                continue;
            }

            const key = `${worker.id}:${worker.restUntil}`;
            if (reloadedRestDeadlinesRef.current.has(key) || remainingSeconds(worker.restUntil, now) > 0) {
                continue;
            }

            reloadedRestDeadlinesRef.current.add(key);
            expiredWorkerRest = true;
        }

        let expiredFinish = false;
        for (const domik of domiksRef.current) {
            if (domik.finishDate != null) {
                const key = `domik:${domik.id}:${domik.finishDate}`;
                if (!reloadedFinishDeadlinesRef.current.has(key) && remainingSeconds(domik.finishDate, now) <= 0) {
                    reloadedFinishDeadlinesRef.current.add(key);
                    expiredFinish = true;
                }
            }

            for (const manufacture of domik.manufactures ?? []) {
                const key = `manufacture:${manufacture.id}:${manufacture.finishDate}`;
                if (!reloadedFinishDeadlinesRef.current.has(key) && remainingSeconds(manufacture.finishDate, now) <= 0) {
                    reloadedFinishDeadlinesRef.current.add(key);
                    expiredFinish = true;
                }
            }
        }

        const errand = errandRef.current;
        const errandDeadline = errand == null ? null : errand.acceptDate == null ? errand.expireDate : errand.finishDate;
        if (errand != null && errandDeadline != null) {
            const key = `errand:${errand.id}:${errandDeadline}`;
            if (!reloadedFinishDeadlinesRef.current.has(key) && remainingSeconds(errandDeadline, now) <= 0) {
                reloadedFinishDeadlinesRef.current.add(key);
                expiredFinish = true;
            }
        }

        const incident = incidentRef.current;
        const incidentDeadline = incident == null ? null : incident.searchEndDate ?? incident.autoReturnDate;
        if (incident != null && incidentDeadline != null) {
            const key = `incident:${incident.id}:${incidentDeadline}`;
            if (!reloadedFinishDeadlinesRef.current.has(key) && remainingSeconds(incidentDeadline, now) <= 0) {
                reloadedFinishDeadlinesRef.current.add(key);
                expiredFinish = true;
            }
        }

        const domikIncident = domikIncidentRef.current;
        const domikIncidentDeadline = domikIncident == null ? null : domikIncident.searchEndDate ?? domikIncident.autoResolveDate;
        if (domikIncident != null && domikIncidentDeadline != null) {
            const key = `domikIncident:${domikIncident.id}:${domikIncidentDeadline}`;
            if (!reloadedFinishDeadlinesRef.current.has(key) && remainingSeconds(domikIncidentDeadline, now) <= 0) {
                reloadedFinishDeadlinesRef.current.add(key);
                expiredFinish = true;
            }
        }

        for (const convoy of convoysRef.current) {
            if (convoy.windowResetDate == null) {
                continue;
            }

            const key = `convoy:${convoy.neighborId}:${convoy.windowResetDate}`;
            if (!reloadedFinishDeadlinesRef.current.has(key) && remainingSeconds(convoy.windowResetDate, now) <= 0) {
                reloadedFinishDeadlinesRef.current.add(key);
                expiredFinish = true;
            }
        }

        const expiredTolokaBuffs = tolokaRef.current?.activeBuffs.filter(buff => {
            const key = `toloka:${buff.logicName}:${buff.buffUntil}`;
            return remainingSeconds(buff.buffUntil, now) <= 0 && !reloadedTolokaBuffDeadlinesRef.current.has(key);
        }) ?? [];

        if (!expiredWorkerRest && !expiredFinish && expiredTolokaBuffs.length === 0) {
            return;
        }

        for (const buff of expiredTolokaBuffs) {
            reloadedTolokaBuffDeadlinesRef.current.add(`toloka:${buff.logicName}:${buff.buffUntil}`);
        }

        scheduleReload();
    }, [now, scheduleReload]);

    return {
        domiks,
        domikTypes,
        resourceTypes,
        receipts,
        resources,
        orders,
        errand,
        incident,
        domikIncident,
        reputation,
        blueprints,
        village,
        villageLevel,
        weather,
        expeditions,
        decor,
        toloka,
        market,
        convoys,
        goals,
        workers,
        cloaks,
        sickTypes,
        purchaseDomikTypes,
        now,
        loading,
        reload,
        scheduleReload,
        refreshPurchaseTypes,
        setVillage,
        hurryManufacture,
        setManufactureAutoRepeat,
        hurryDomik,
        startExpedition,
        buyDecor,
        contributeToloka,
        voteToloka,
        postLot,
        acceptLot,
        cancelLot,
        buyFromConvoy,
        recap,
        clearRecap,
        events,
    };
}

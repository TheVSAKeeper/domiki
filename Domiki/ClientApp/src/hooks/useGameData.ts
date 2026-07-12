import { useCallback, useEffect, useRef, useState } from 'react';
import { acceptLot as acceptLotApi, apiGet, ApiError, buyDecor as buyDecorApi, cancelLot as cancelLotApi, contributeToloka as contributeTolokaApi, getDecor, getGameState, getMarket, getToloka, getVillage, hurryDomik as hurryDomikApi, hurryManufacture as hurryManufactureApi, postLot as postLotApi, setFeedWorkers as setFeedWorkersApi, setManufactureAutoRepeat as setManufactureAutoRepeatApi, setVillage as setVillageApi, startExpedition as startExpeditionApi } from '../services/api';
import { useToast } from '../services/toast';
import {
    domikTypeSchema,
    resourceSchema,
    villageLevelSchema,
    type BlueprintDto,
    type DecorStateDto,
    type DomikDto,
    type DomikTypeDto,
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
    type TolokaStateDto,
    type VillageDto,
    type VillageLevelDto,
    type WeatherStateDto,
    type WorkerDto,
} from '../types/api';
import { remainingSeconds } from '../utils/time';

export interface GameData {
    domiks: DomikDto[];
    domikTypes: DomikTypeDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
    resources: ResourceDto[];
    orders: OrderDto[];
    reputation: NeighborReputationDto[];
    blueprints: BlueprintDto[];
    village: VillageDto | null;
    villageLevel: VillageLevelDto | null;
    weather: WeatherStateDto | null;
    expeditions: ExpeditionStateDto | null;
    decor: DecorStateDto | null;
    toloka: TolokaStateDto | null;
    market: MarketStateDto | null;
    goals: GoalsStateDto | null;
    workers: WorkerDto[];
    purchaseDomikTypes: DomikTypeDto[] | null;
    now: number;
    reload: () => Promise<void>;
    refreshPurchaseTypes: () => Promise<void>;
    setVillage: (name: string, crestIcon: number, crestColor: number) => Promise<void>;
    setFeedWorkers: (enabled: boolean) => Promise<void>;
    hurryManufacture: (manufactureId: number) => Promise<void>;
    setManufactureAutoRepeat: (manufactureId: number, autoRepeat: boolean) => Promise<void>;
    hurryDomik: (domikId: number) => Promise<void>;
    startExpedition: (expeditionTypeId: number, workerIds?: number[], provisions?: boolean) => Promise<void>;
    buyDecor: (decorTypeId: number) => Promise<void>;
    contributeToloka: (amount: number) => Promise<void>;
    postLot: (giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number) => Promise<void>;
    acceptLot: (lotId: number) => Promise<void>;
    cancelLot: (lotId: number) => Promise<void>;
    recap: RecapDto | null;
    clearRecap: () => void;
    events: RecapEventDto[];
}

export function useGameData(): GameData {
    const toast = useToast();

    const [domiks, setDomiks] = useState<DomikDto[]>([]);
    const [domikTypes, setDomikTypes] = useState<DomikTypeDto[]>([]);
    const [resourceTypes, setResourceTypes] = useState<ResourceTypeDto[]>([]);
    const [receipts, setReceipts] = useState<ReceiptDto[]>([]);
    const [resources, setResources] = useState<ResourceDto[]>([]);
    const [orders, setOrders] = useState<OrderDto[]>([]);
    const [reputation, setReputation] = useState<NeighborReputationDto[]>([]);
    const [blueprints, setBlueprints] = useState<BlueprintDto[]>([]);
    const [village, setVillageState] = useState<VillageDto | null>(null);
    const [villageLevel, setVillageLevel] = useState<VillageLevelDto | null>(null);
    const [weather, setWeather] = useState<WeatherStateDto | null>(null);
    const [expeditions, setExpeditions] = useState<ExpeditionStateDto | null>(null);
    const [decor, setDecor] = useState<DecorStateDto | null>(null);
    const [toloka, setToloka] = useState<TolokaStateDto | null>(null);
    const [market, setMarket] = useState<MarketStateDto | null>(null);
    const [goals, setGoals] = useState<GoalsStateDto | null>(null);
    const [workers, setWorkers] = useState<WorkerDto[]>([]);
    const [purchaseDomikTypes, setPurchaseDomikTypes] = useState<DomikTypeDto[] | null>(null);
    const [recap, setRecap] = useState<RecapDto | null>(null);
    const [events, setEvents] = useState<RecapEventDto[]>([]);
    const [now, setNow] = useState(() => Date.now());

    const refetching = useRef(false);
    const pendingReload = useRef(false);
    const workersRef = useRef(workers);
    const expeditionsRef = useRef(expeditions);
    const goalsRef = useRef(goals);
    const tolokaRef = useRef(toloka);
    const reloadedRestDeadlinesRef = useRef<Set<string>>(new Set());
    const reloadedTolokaBuffDeadlinesRef = useRef<Set<string>>(new Set());

    useEffect(() => {
        workersRef.current = workers;
    }, [workers]);

    useEffect(() => {
        expeditionsRef.current = expeditions;
    }, [expeditions]);

    useEffect(() => {
        goalsRef.current = goals;
    }, [goals]);

    useEffect(() => {
        tolokaRef.current = toloka;
    }, [toloka]);

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
        setDomiks(state.domiks);
        setResources(state.resources);
        setOrders(state.orders);
        setReputation(state.reputation);
        setBlueprints(state.blueprints);
        setVillageState(state.village);
        setVillageLevel(state.villageLevel);
        setWorkers(state.workers);
        setWeather(state.weather);
        setExpeditions(state.expeditions);
        setDecor(state.decor);
        setToloka(state.toloka);
        setMarket(state.market);
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

    const setFeedWorkers = useCallback(async (enabled: boolean) => {
        await setFeedWorkersApi(enabled);
        setVillageState(await getVillage());
    }, []);

    const hurryManufacture = useCallback(async (manufactureId: number) => {
        await hurryManufactureApi(manufactureId);
        await reload();
    }, [reload]);

    const setManufactureAutoRepeat = useCallback(async (manufactureId: number, autoRepeat: boolean) => {
        await setManufactureAutoRepeatApi(manufactureId, autoRepeat);
        await reload();
    }, [reload]);

    const hurryDomik = useCallback(async (domikId: number) => {
        await hurryDomikApi(domikId);
        await reload();
    }, [reload]);

    const startExpedition = useCallback(async (expeditionTypeId: number, workerIds?: number[], provisions?: boolean) => {
        await startExpeditionApi(expeditionTypeId, workerIds, provisions);
        await reload();
    }, [reload]);

    const contributeToloka = useCallback(async (amount: number) => {
        await contributeTolokaApi(amount);
        const [nextToloka, nextResources] = await Promise.all([
            getToloka(),
            apiGet('Domiki/GetResources', resourceSchema.array()),
        ]);
        setToloka(nextToloka);
        setResources(nextResources);
    }, []);

    const refreshMarketAndResources = useCallback(async () => {
        const [nextMarket, nextResources] = await Promise.all([
            getMarket(),
            apiGet('Domiki/GetResources', resourceSchema.array()),
        ]);
        setMarket(nextMarket);
        setResources(nextResources);
    }, []);

    const postLot = useCallback(async (giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number) => {
        await postLotApi(giveResourceTypeId, giveValue, wantResourceTypeId, wantValue);
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

        const expiredTolokaBuffs = tolokaRef.current?.activeBuffs.filter(buff => {
            const key = `toloka:${buff.logicName}:${buff.buffUntil}`;
            return remainingSeconds(buff.buffUntil, now) <= 0 && !reloadedTolokaBuffDeadlinesRef.current.has(key);
        }) ?? [];

        if (!expiredWorkerRest && expiredTolokaBuffs.length === 0) {
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
        reputation,
        blueprints,
        village,
        villageLevel,
        weather,
        expeditions,
        decor,
        toloka,
        market,
        goals,
        workers,
        purchaseDomikTypes,
        now,
        reload,
        refreshPurchaseTypes,
        setVillage,
        setFeedWorkers,
        hurryManufacture,
        setManufactureAutoRepeat,
        hurryDomik,
        startExpedition,
        buyDecor,
        contributeToloka,
        postLot,
        acceptLot,
        cancelLot,
        recap,
        clearRecap,
        events,
    };
}

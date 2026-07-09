import { useCallback, useEffect, useRef, useState } from 'react';
import { acceptLot as acceptLotApi, apiGet, ApiError, buyDecor as buyDecorApi, cancelLot as cancelLotApi, contributeToloka as contributeTolokaApi, getDecor, getGameState, getMarket, getToloka, getVillage, hurryDomik as hurryDomikApi, hurryManufacture as hurryManufactureApi, postLot as postLotApi, setVillage as setVillageApi, startExpedition as startExpeditionApi } from '../services/api';
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
    type MarketStateDto,
    type NeighborReputationDto,
    type OrderDto,
    type ReceiptDto,
    type ResourceDto,
    type ResourceTypeDto,
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
    workers: WorkerDto[];
    purchaseDomikTypes: DomikTypeDto[] | null;
    now: number;
    reload: () => Promise<void>;
    refreshPurchaseTypes: () => Promise<void>;
    setVillage: (name: string, crestIcon: number, crestColor: number) => Promise<void>;
    hurryManufacture: (manufactureId: number) => Promise<void>;
    hurryDomik: (domikId: number) => Promise<void>;
    startExpedition: (expeditionTypeId: number, workerIds?: number[]) => Promise<void>;
    buyDecor: (decorTypeId: number) => Promise<void>;
    contributeToloka: (amount: number) => Promise<void>;
    postLot: (giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number) => Promise<void>;
    acceptLot: (lotId: number) => Promise<void>;
    cancelLot: (lotId: number) => Promise<void>;
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
    const [workers, setWorkers] = useState<WorkerDto[]>([]);
    const [purchaseDomikTypes, setPurchaseDomikTypes] = useState<DomikTypeDto[] | null>(null);
    const [now, setNow] = useState(() => Date.now());

    const refetching = useRef(false);
    const domiksRef = useRef(domiks);
    const ordersRef = useRef(orders);
    const workersRef = useRef(workers);
    const weatherRef = useRef(weather);
    const expeditionsRef = useRef(expeditions);
    const tolokaRef = useRef(toloka);
    const marketRef = useRef(market);
    const reloadedRestDeadlinesRef = useRef<Set<string>>(new Set());

    useEffect(() => {
        domiksRef.current = domiks;
    }, [domiks]);

    useEffect(() => {
        ordersRef.current = orders;
    }, [orders]);

    useEffect(() => {
        workersRef.current = workers;
    }, [workers]);

    useEffect(() => {
        weatherRef.current = weather;
    }, [weather]);

    useEffect(() => {
        expeditionsRef.current = expeditions;
    }, [expeditions]);

    useEffect(() => {
        tolokaRef.current = toloka;
    }, [toloka]);

    useEffect(() => {
        marketRef.current = market;
    }, [market]);

    const reload = useCallback(async () => {
        const state = await getGameState();
        const prevActive = expeditionsRef.current?.active ?? [];
        const nextActive = state.expeditions?.active ?? [];
        for (const finished of prevActive) {
            if (!nextActive.some(active => active.id === finished.id)) {
                toast.success(`Экспедиция «${finished.expeditionName}» вернулась`);
            }
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
    }, [toast]);

    const refreshPurchaseTypes = useCallback(async () => {
        setPurchaseDomikTypes(await apiGet('Domiki/GetPurchaseAvaialableDomiks', domikTypeSchema.array()));
    }, []);

    const setVillage = useCallback(async (name: string, crestIcon: number, crestColor: number) => {
        await setVillageApi(name, crestIcon, crestColor);
        setVillageState(await getVillage());
    }, []);

    const hurryManufacture = useCallback(async (manufactureId: number) => {
        await hurryManufactureApi(manufactureId);
        await reload();
    }, [reload]);

    const hurryDomik = useCallback(async (domikId: number) => {
        await hurryDomikApi(domikId);
        await reload();
    }, [reload]);

    const startExpedition = useCallback(async (expeditionTypeId: number, workerIds?: number[]) => {
        await startExpeditionApi(expeditionTypeId, workerIds);
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
        const id = setInterval(() => {
            void Promise.all([getToloka(), getMarket()])
                .then(([nextToloka, nextMarket]) => {
                    setToloka(nextToloka);
                    setMarket(nextMarket);
                })
                .catch((err: unknown) => {
                    if (err instanceof ApiError) {
                        toast.error(err.message);
                        return;
                    }
                    throw err;
                });
        }, 15000);
        return () => clearInterval(id);
    }, [toast]);

    useEffect(() => {
        if (refetching.current) {
            return;
        }

        const expired = domiksRef.current.some(domik => {
            if (domik.finishDate != null && remainingSeconds(domik.finishDate, now) <= 0) {
                return true;
            }
            return domik.manufactures?.some(manufacture => remainingSeconds(manufacture.finishDate, now) <= 0) ?? false;
        }) || ordersRef.current.some(order => remainingSeconds(order.expireDate, now) <= 0)
            || (weatherRef.current?.current != null && remainingSeconds(weatherRef.current.current.endDate, now) <= 0)
            || (expeditionsRef.current?.active.some(expedition => remainingSeconds(expedition.finishDate, now) <= 0) ?? false)
            || (tolokaRef.current?.buffUntil != null && remainingSeconds(tolokaRef.current.buffUntil, now) <= 0)
            || (marketRef.current?.lots.some(lot => remainingSeconds(lot.expireDate, now) <= 0) ?? false)
            || (marketRef.current?.myLots.some(lot => remainingSeconds(lot.expireDate, now) <= 0) ?? false)
            || workersRef.current.some(worker => {
                if (worker.restUntil == null) {
                    return false;
                }

                const key = `${worker.id}:${worker.restUntil}`;
                if (reloadedRestDeadlinesRef.current.has(key)) {
                    return false;
                }

                if (remainingSeconds(worker.restUntil, now) > 0) {
                    return false;
                }

                reloadedRestDeadlinesRef.current.add(key);
                return true;
            });

        if (!expired) {
            return;
        }

        refetching.current = true;

        void reload()
            .catch((err: unknown) => {
                if (err instanceof ApiError) {
                    toast.error(err.message);
                    return;
                }
                throw err;
            })
            .finally(() => {
                refetching.current = false;
            });
    }, [now, toast, reload]);

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
        workers,
        purchaseDomikTypes,
        now,
        reload,
        refreshPurchaseTypes,
        setVillage,
        hurryManufacture,
        hurryDomik,
        startExpedition,
        buyDecor,
        contributeToloka,
        postLot,
        acceptLot,
        cancelLot,
    };
}

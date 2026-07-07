import { useCallback, useEffect, useRef, useState } from 'react';
import { apiGet, ApiError, getGameState, getVillage, hurryDomik as hurryDomikApi, hurryManufacture as hurryManufactureApi, setVillage as setVillageApi, startExpedition as startExpeditionApi } from '../services/api';
import { useToast } from '../services/toast';
import {
    domikTypeSchema,
    type BlueprintDto,
    type DomikDto,
    type DomikTypeDto,
    type ExpeditionStateDto,
    type NeighborReputationDto,
    type OrderDto,
    type ReceiptDto,
    type ResourceDto,
    type ResourceTypeDto,
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
    workers: WorkerDto[];
    purchaseDomikTypes: DomikTypeDto[] | null;
    now: number;
    reload: () => Promise<void>;
    refreshPurchaseTypes: () => Promise<void>;
    setVillage: (name: string, crestIcon: number, crestColor: number) => Promise<void>;
    hurryManufacture: (manufactureId: number) => Promise<void>;
    hurryDomik: (domikId: number) => Promise<void>;
    startExpedition: (expeditionTypeId: number) => Promise<void>;
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
    const [workers, setWorkers] = useState<WorkerDto[]>([]);
    const [purchaseDomikTypes, setPurchaseDomikTypes] = useState<DomikTypeDto[] | null>(null);
    const [now, setNow] = useState(() => Date.now());

    const refetching = useRef(false);
    const domiksRef = useRef(domiks);
    const ordersRef = useRef(orders);
    const workersRef = useRef(workers);
    const weatherRef = useRef(weather);
    const expeditionsRef = useRef(expeditions);
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

    const reload = useCallback(async () => {
        const state = await getGameState();
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
    }, []);

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

    const startExpedition = useCallback(async (expeditionTypeId: number) => {
        await startExpeditionApi(expeditionTypeId);
        await reload();
    }, [reload]);

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
        workers,
        purchaseDomikTypes,
        now,
        reload,
        refreshPurchaseTypes,
        setVillage,
        hurryManufacture,
        hurryDomik,
        startExpedition,
    };
}

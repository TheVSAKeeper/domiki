import { useCallback, useEffect, useRef, useState } from 'react';
import type { z } from 'zod';
import { apiGet, ApiError, getOrders, getReputation, getVillage, getWeather, getWorkers, setVillage as setVillageApi } from '../services/api';
import { useToast } from '../services/toast';
import {
    domikSchema,
    domikTypeSchema,
    neighborReputationSchema,
    orderSchema,
    receiptSchema,
    resourceSchema,
    resourceTypeSchema,
    villageSchema,
    weatherStateSchema,
    workerSchema,
    type DomikDto,
    type DomikTypeDto,
    type NeighborReputationDto,
    type OrderDto,
    type ReceiptDto,
    type ResourceDto,
    type ResourceTypeDto,
    type VillageDto,
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
    village: VillageDto | null;
    weather: WeatherStateDto | null;
    workers: WorkerDto[];
    purchaseDomikTypes: DomikTypeDto[] | null;
    now: number;
    reload: () => Promise<void>;
    refreshPurchaseTypes: () => Promise<void>;
    setVillage: (name: string, crestIcon: number, crestColor: number) => Promise<void>;
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
    const [village, setVillageState] = useState<VillageDto | null>(null);
    const [weather, setWeather] = useState<WeatherStateDto | null>(null);
    const [workers, setWorkers] = useState<WorkerDto[]>([]);
    const [purchaseDomikTypes, setPurchaseDomikTypes] = useState<DomikTypeDto[] | null>(null);
    const [now, setNow] = useState(() => Date.now());

    const refetching = useRef(false);
    const domiksRef = useRef(domiks);
    const ordersRef = useRef(orders);
    const workersRef = useRef(workers);
    const weatherRef = useRef(weather);
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

    const reload = useCallback(async () => {
        const [domiksData, resourcesData, ordersData, reputationData, villageData, workersData, weatherData] = await Promise.all([
            apiGet('Domiki/GetDomiks', domikSchema.array()),
            apiGet('Domiki/GetResources', resourceSchema.array()),
            getOrders(),
            getReputation(),
            getVillage(),
            getWorkers(),
            getWeather(),
        ]);
        setDomiks(domiksData);
        setResources(resourcesData);
        setOrders(ordersData);
        setReputation(reputationData);
        setVillageState(villageData);
        setWorkers(workersData);
        setWeather(weatherData);
    }, []);

    const refreshPurchaseTypes = useCallback(async () => {
        setPurchaseDomikTypes(await apiGet('Domiki/GetPurchaseAvaialableDomiks', domikTypeSchema.array()));
    }, []);

    const setVillage = useCallback(async (name: string, crestIcon: number, crestColor: number) => {
        await setVillageApi(name, crestIcon, crestColor);
        setVillageState(await getVillage());
    }, []);

    useEffect(() => {
        const id = setInterval(() => setNow(Date.now()), 1000);
        return () => clearInterval(id);
    }, []);

    useEffect(() => {
        const controller = new AbortController();
        const { signal } = controller;

        const safeLoad = async <T,>(url: string, schema: z.ZodType<T>, setter: (data: T) => void) => {
            try {
                setter(await apiGet(url, schema, signal));
            } catch (err) {
                if (err instanceof DOMException && err.name === 'AbortError') {
                    return;
                }
                if (err instanceof ApiError) {
                    toast.error(err.message);
                    return;
                }
            }
        };

        void Promise.all([
            safeLoad('Domiki/GetDomikTypes', domikTypeSchema.array(), setDomikTypes),
            safeLoad('Domiki/GetResourceTypes', resourceTypeSchema.array(), setResourceTypes),
            safeLoad('Domiki/GetReceipts', receiptSchema.array(), setReceipts),
            safeLoad('Domiki/GetDomiks', domikSchema.array(), setDomiks),
            safeLoad('Domiki/GetResources', resourceSchema.array(), setResources),
            safeLoad('Domiki/GetOrders', orderSchema.array(), setOrders),
            safeLoad('Domiki/GetReputation', neighborReputationSchema.array(), setReputation),
            safeLoad('Domiki/GetVillage', villageSchema, setVillageState),
            safeLoad('Domiki/GetWorkers', workerSchema.array(), setWorkers),
            safeLoad('Domiki/GetPurchaseAvaialableDomiks', domikTypeSchema.array(), setPurchaseDomikTypes),
            safeLoad('Domiki/GetWeather', weatherStateSchema, setWeather),
        ]);

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
        village,
        weather,
        workers,
        purchaseDomikTypes,
        now,
        reload,
        refreshPurchaseTypes,
        setVillage,
    };
}

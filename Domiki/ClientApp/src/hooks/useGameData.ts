import { useCallback, useEffect, useRef, useState } from 'react';
import type { z } from 'zod';
import { apiGet, ApiError } from '../services/api';
import { useToast } from '../services/toast';
import {
    domikSchema,
    domikTypeSchema,
    receiptSchema,
    resourceSchema,
    resourceTypeSchema,
    type DomikDto,
    type DomikTypeDto,
    type ReceiptDto,
    type ResourceDto,
    type ResourceTypeDto,
} from '../types/api';
import { remainingSeconds } from '../utils/time';

export interface GameData {
    domiks: DomikDto[];
    domikTypes: DomikTypeDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
    resources: ResourceDto[];
    purchaseDomikTypes: DomikTypeDto[] | null;
    now: number;
    reload: () => Promise<void>;
    refreshPurchaseTypes: () => Promise<void>;
}

export function useGameData(): GameData {
    const toast = useToast();

    const [domiks, setDomiks] = useState<DomikDto[]>([]);
    const [domikTypes, setDomikTypes] = useState<DomikTypeDto[]>([]);
    const [resourceTypes, setResourceTypes] = useState<ResourceTypeDto[]>([]);
    const [receipts, setReceipts] = useState<ReceiptDto[]>([]);
    const [resources, setResources] = useState<ResourceDto[]>([]);
    const [purchaseDomikTypes, setPurchaseDomikTypes] = useState<DomikTypeDto[] | null>(null);
    const [now, setNow] = useState(() => Date.now());

    const refetching = useRef(false);
    const domiksRef = useRef(domiks);

    useEffect(() => {
        domiksRef.current = domiks;
    }, [domiks]);

    const reload = useCallback(async () => {
        const [domiksData, resourcesData] = await Promise.all([
            apiGet('Domiki/GetDomiks', domikSchema.array()),
            apiGet('Domiki/GetResources', resourceSchema.array()),
        ]);
        setDomiks(domiksData);
        setResources(resourcesData);
    }, []);

    const refreshPurchaseTypes = useCallback(async () => {
        setPurchaseDomikTypes(await apiGet('Domiki/GetPurchaseAvaialableDomiks', domikTypeSchema.array()));
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
            safeLoad('Domiki/GetPurchaseAvaialableDomiks', domikTypeSchema.array(), setPurchaseDomikTypes),
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
        purchaseDomikTypes,
        now,
        reload,
        refreshPurchaseTypes,
    };
}

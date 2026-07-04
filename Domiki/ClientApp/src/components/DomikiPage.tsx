import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import ArrowUpIcon from 'pixelarticons/svg/arrow-up.svg?react';
import PlayIcon from 'pixelarticons/svg/play.svg?react';
import { apiGet, apiPost, ApiError } from '../services/api';
import { useToast } from '../services/toast';
import { DomikDto, DomikTypeDto, ReceiptDto, ResourceDto, ResourceTypeDto } from '../types/api';
import { computePlodderCount, computeSelectedDomikView } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { ManufactureBox } from './ManufactureBox';
import { ResourcesBox } from './ResourcesBox';
import { UpgradeBox } from './UpgradeBox';

export const DomikiPage = () => {
    const toast = useToast();

    const [domiks, setDomiks] = useState<DomikDto[]>([]);
    const [domikTypes, setDomikTypes] = useState<DomikTypeDto[]>([]);
    const [resourceTypes, setResourceTypes] = useState<ResourceTypeDto[]>([]);
    const [receipts, setReceipts] = useState<ReceiptDto[]>([]);
    const [resources, setResources] = useState<ResourceDto[]>([]);
    const [purchaseDomikTypes, setPurchaseDomikTypes] = useState<DomikTypeDto[] | null>(null);
    const [shopVisible, setShopVisible] = useState(false);
    const [selectedDomikId, setSelectedDomikId] = useState<number | null>(null);
    const [now, setNow] = useState(() => Date.now());

    const refetching = useRef(false);
    const domiksRef = useRef(domiks);

    useEffect(() => {
        domiksRef.current = domiks;
    }, [domiks]);

    const reloadDomiksAndResources = useCallback(async () => {
        const [domiksData, resourcesData] = await Promise.all([
            apiGet<DomikDto[]>('Domiki/GetDomiks'),
            apiGet<ResourceDto[]>('Domiki/GetResources'),
        ]);
        setDomiks(domiksData);
        setResources(resourcesData);
    }, []);

    useEffect(() => {
        const id = setInterval(() => setNow(Date.now()), 1000);
        return () => clearInterval(id);
    }, []);

    useEffect(() => {
        const controller = new AbortController();
        const { signal } = controller;

        const safeLoad = async <T,>(url: string, setter: (data: T) => void) => {
            try {
                const data = await apiGet<T>(url, signal);
                setter(data);
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
            safeLoad<DomikTypeDto[]>('Domiki/GetDomikTypes', setDomikTypes),
            safeLoad<ResourceTypeDto[]>('Domiki/GetResourceTypes', setResourceTypes),
            safeLoad<ReceiptDto[]>('Domiki/GetReceipts', setReceipts),
            safeLoad<DomikDto[]>('Domiki/GetDomiks', setDomiks),
            safeLoad<ResourceDto[]>('Domiki/GetResources', setResources),
            safeLoad<DomikTypeDto[]>('Domiki/GetPurchaseAvaialableDomiks', setPurchaseDomikTypes),
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

        reloadDomiksAndResources()
            .catch(err => {
                if (err instanceof ApiError) {
                    toast.error(err.message);
                    return;
                }
                throw err;
            })
            .finally(() => {
                refetching.current = false;
            });
    }, [now, toast, reloadDomiksAndResources]);

    const plodder = useMemo(() => computePlodderCount(domiks, domikTypes), [domiks, domikTypes]);
    const selected = useMemo(
        () => computeSelectedDomikView(selectedDomikId, domiks, domikTypes, receipts, resources, now),
        [selectedDomikId, domiks, domikTypes, receipts, resources, now],
    );

    const runAction = async (action: () => Promise<void>) => {
        try {
            await action();
        } catch (err) {
            if (err instanceof ApiError) {
                toast.error(err.message);
                return;
            }
            throw err;
        }
    };

    const buy = (typeId: number) => runAction(async () => {
        await apiPost(`Domiki/BuyDomik/${typeId}`);
        await reloadDomiksAndResources();
        setPurchaseDomikTypes(await apiGet<DomikTypeDto[]>('Domiki/GetPurchaseAvaialableDomiks'));
    });

    const upgrade = (id: number) => runAction(async () => {
        await apiPost(`Domiki/UpgradeDomik/${id}`);
        await reloadDomiksAndResources();
    });

    const startManufacture = (domikId: number, receiptId: number) => runAction(async () => {
        await apiPost(`Domiki/StartManufacture/${domikId}/${receiptId}`);
        await reloadDomiksAndResources();
    });

    const toggleShop = () => runAction(async () => {
        const willShow = !shopVisible;
        setShopVisible(willShow);
        if (willShow && purchaseDomikTypes == null) {
            setPurchaseDomikTypes(await apiGet<DomikTypeDto[]>('Domiki/GetPurchaseAvaialableDomiks'));
        }
    });

    const selectDomik = (id: number) => setSelectedDomikId(id);

    return (
        <div className="game">
            <header className="hud pixel-panel">
                <div className="resources">
                    {resourceTypes.length > 0 &&
                        resources.map(resource => {
                            const resourceType = resourceTypes.find(x => x.id === resource.typeId);
                            if (resourceType == null) {
                                return null;
                            }

                            const image = '/images/resourceTypes/' + resourceType.logicName + '.png';
                            return (
                                <div key={resource.typeId} className="resource-box" title={resourceType.name}>
                                    <img src={image} alt={resourceType.name} />
                                    <span className="resource-value">{resource.value}</span>
                                </div>
                            );
                        })
                    }
                </div>
                {domikTypes.length > 0 &&
                    <div className="resource-box hud-plodder" title="Трудяги">
                        <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                        <span className="resource-value">{plodder.free}/{plodder.max}</span>
                    </div>
                }
            </header>
            <div className="workspace">
                <section className="village">
                    <div className="village-header">
                        <h2 className="section-title">Деревня</h2>
                        {purchaseDomikTypes != null &&
                            <button className="btn-game" onClick={() => toggleShop()}>
                                <StoreIcon className="btn-ico" aria-hidden="true" />
                                {shopVisible ? 'Закрыть магазин' : 'Магазин'}
                            </button>
                        }
                    </div>
                    {shopVisible && purchaseDomikTypes != null &&
                        <div className="purchase-box">
                            {purchaseDomikTypes.length === 0 &&
                                <span className="hint">Магазин пуст</span>
                            }
                            {purchaseDomikTypes.map(purchaseDomikType => {
                                const image = '/images/domikTypes/' + purchaseDomikType.logicName + '.png';
                                return (
                                    <div key={purchaseDomikType.id} className="plot plot-shop">
                                        <img className="plot-sprite" src={image} alt={purchaseDomikType.name} />
                                        <span className="plot-name">{purchaseDomikType.name}</span>
                                        <span className="plot-status">Доступно: {purchaseDomikType.availableCount}/{purchaseDomikType.maxCount}</span>
                                        <ResourcesBox resources={purchaseDomikType.levels[0].resources} resourceTypes={resourceTypes} />
                                        <button className="btn-game" onClick={() => buy(purchaseDomikType.id)}>
                                            <BuildingIcon className="btn-ico" aria-hidden="true" />
                                            Купить
                                        </button>
                                    </div>
                                );
                            })
                            }
                        </div>
                    }
                    <div className="domiks">
                        {domikTypes.length > 0 &&
                            domiks.map(domik => {
                                const domikType = domikTypes.find(x => x.id === domik.typeId);
                                if (domikType == null) {
                                    return null;
                                }

                                const image = '/images/domikTypes/' + domikType.logicName + '.png';
                                const hasManufacture = domik.manufactures != null && domik.manufactures.length > 0;
                                const durationSecondsText = domik.finishDate != null
                                    ? formatDuration(remainingSeconds(domik.finishDate, now))
                                    : null;
                                return (
                                    <button key={domik.id}
                                        className={'plot' + (selectedDomikId === domik.id ? ' plot-selected' : '')}
                                        onClick={() => selectDomik(domik.id)}>
                                        <img className="plot-sprite" src={image} alt={domikType.name} />
                                        <span className="plot-name">{domikType.name}</span>
                                        <UpgradeBox durationSeconds={durationSecondsText} level={domik.level} />
                                        <span className="plot-status">
                                            {domik.level < domikType.maxLevel && domik.finishDate == null &&
                                                <img className="status-icon" src="/images/upgrade_available.png" alt="Доступно улучшение" title="Доступно улучшение" />
                                            }
                                            {domik.finishDate != null &&
                                                <img className="status-icon icon-busy" src="/images/upgrade_in_process.png" alt="Идёт улучшение" title="Идёт улучшение" />
                                            }
                                            {hasManufacture &&
                                                <img className="status-icon" src="/images/manufacture.png" alt="Идёт производство" title="Идёт производство" />
                                            }
                                        </span>
                                    </button>
                                );
                            })
                        }
                    </div>
                </section>
                <aside className="actions pixel-panel">
                    {selected == null &&
                        <p className="hint">Выберите домик в деревне – здесь появятся улучшение и производство.</p>
                    }
                    {selected != null &&
                        <div>
                            <h3 className="panel-title">{selected.domikType.name}</h3>
                            <span className="domik-level">ур. {selected.domik.level}</span>
                            {selected.upgrade != null &&
                                <div className="panel-block">
                                    <span className="panel-label">Улучшение до ур. {selected.upgrade.nextLevel}</span>
                                    <ResourcesBox resources={selected.upgrade.resources} resourceTypes={resourceTypes} />
                                    <button className="btn-game"
                                        disabled={!selected.upgrade.hasResources}
                                        onClick={() => upgrade(selected.domik.id)}>
                                        <ArrowUpIcon className="btn-ico" aria-hidden="true" />
                                        Улучшить
                                    </button>
                                    {!selected.upgrade.hasResources &&
                                        <p className="note-warn">
                                            <img src="/images/upgrade_no_resources.png" alt="" />
                                            Не хватает ресурсов
                                        </p>
                                    }
                                </div>
                            }
                            {selected.domik.finishDate != null &&
                                <div className="panel-block">
                                    <span className="panel-label">Строится</span>
                                    <span className="timer">{selected.remainingText}</span>
                                </div>
                            }
                            {selected.receipts.length > 0 &&
                                <div className="panel-block">
                                    <span className="panel-label">Запустить производство</span>
                                    <div className="receipt-list">
                                        {selected.receipts.map(receipt => (
                                            <button key={receipt.id} className="btn-game"
                                                onClick={() => startManufacture(selected.domik.id, receipt.id)}>
                                                <PlayIcon className="btn-ico" aria-hidden="true" />
                                                {receipt.name}
                                            </button>
                                        ))}
                                    </div>
                                </div>
                            }
                            {selected.domik.manufactures != null && selected.domik.manufactures.length > 0 &&
                                <div className="panel-block">
                                    <span className="panel-label">Сейчас производится</span>
                                    {selected.domik.manufactures.map(manufacture => {
                                        const receipt = receipts.find(x => x.id === manufacture.receiptId);
                                        if (receipt == null) {
                                            return null;
                                        }

                                        return (
                                            <ManufactureBox key={manufacture.id} manufacture={manufacture} receipt={receipt}
                                                now={now} remainingText={formatDuration(remainingSeconds(manufacture.finishDate, now))} />
                                        );
                                    })}
                                </div>
                            }
                        </div>
                    }
                </aside>
            </div>
        </div>
    );
};

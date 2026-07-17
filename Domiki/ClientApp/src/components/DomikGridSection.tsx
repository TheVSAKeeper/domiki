import { useEffect, useMemo, useRef, useState } from 'react';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import ChevronLeftIcon from 'pixelarticons/svg/chevron-left.svg?react';
import ChevronRightIcon from 'pixelarticons/svg/chevron-right.svg?react';
import RepeatIcon from 'pixelarticons/svg/repeat.svg?react';
import BellIcon from 'pixelarticons/svg/bell.svg?react';
import GridIcon from 'pixelarticons/svg/grid-3x3.svg?react';
import type { DomikDto, DomikTypeDto, ReceiptDto, ResourceDto, WeatherPeriodDto, WorkerDto } from '../types/api';
import type { DomikNamer } from '../utils/domikNames';
import { canAffordUpgrade, sortDomiks } from '../utils/game';
import type { DomikSortMode } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { AbstractSprite, WorkerSprite } from './sprites';
import { AnimatedDomikSprite } from './AnimatedDomikSprite';
import { UpgradeBox } from './UpgradeBox';

type SortModeEntry = { mode: DomikSortMode; label: string; Icon: typeof StoreIcon };

const SORT_MODES: readonly [SortModeEntry, ...SortModeEntry[]] = [
    { mode: 'attention', label: 'По важности', Icon: BellIcon },
    { mode: 'type', label: 'По типу', Icon: GridIcon },
    { mode: 'level', label: 'По уровню', Icon: ChevronUpIcon },
];

type RowsPerPage = 2 | 3 | 5 | 'all';

const ROWS_PER_PAGE_OPTIONS: { value: RowsPerPage; label: string }[] = [
    { value: 2, label: '2 ряда' },
    { value: 3, label: '3 ряда' },
    { value: 5, label: '5 рядов' },
    { value: 'all', label: 'Все' },
];

interface DomikSortMenuProps {
    value: DomikSortMode;
    onChange: (mode: DomikSortMode) => void;
}

export const DomikSortMenu = ({ value, onChange }: DomikSortMenuProps) => {
    const [sortOpen, setSortOpen] = useState(false);
    const sortRef = useRef<HTMLDivElement>(null);
    const activeSort = SORT_MODES.find(item => item.mode === value) ?? SORT_MODES[0];

    useEffect(() => {
        if (!sortOpen) {
            return;
        }

        const onDown = (event: MouseEvent) => {
            if (sortRef.current != null && !sortRef.current.contains(event.target as Node)) {
                setSortOpen(false);
            }
        };

        document.addEventListener('mousedown', onDown);
        return () => { document.removeEventListener('mousedown', onDown); };
    }, [sortOpen]);

    return (
        <div className="domik-sort-menu" ref={sortRef}>
            <button type="button" className="btn-game btn-ghost" aria-expanded={sortOpen}
                onClick={() => setSortOpen(prev => !prev)}>
                <activeSort.Icon className="btn-ico" aria-hidden="true" />
                {activeSort.label}
                <ChevronDownIcon className="btn-ico" aria-hidden="true" />
            </button>
            {sortOpen &&
                <div className="domik-sort-pop">
                    {SORT_MODES.map(item =>
                        <button key={item.mode} type="button"
                            className={'domik-sort-option' + (item.mode === value ? ' domik-sort-option-active' : '')}
                            onClick={() => { onChange(item.mode); setSortOpen(false); }}>
                            <item.Icon className="game-tab-ico" aria-hidden="true" />
                            {item.label}
                        </button>,
                    )}
                </div>
            }
        </div>
    );
};

interface DomikGridSectionProps {
    domiks: DomikDto[];
    domikTypes: DomikTypeDto[];
    receipts: ReceiptDto[];
    resources: ResourceDto[];
    currentWeather: WeatherPeriodDto | null;
    now: number;
    sortMode: DomikSortMode;
    selectedDomikId: number | null;
    displayName: DomikNamer;
    onSelect: (id: number, logicName: string) => void;
    workers: WorkerDto[];
}

export const DomikGridSection = ({ domiks, domikTypes, receipts, resources, currentWeather, now, sortMode, selectedDomikId, displayName: namer, onSelect, workers }: DomikGridSectionProps) => {
    const [rowsPerPage, setRowsPerPage] = useState<RowsPerPage>(() => {
        const saved = localStorage.getItem('domik-page-size');
        if (saved === '2') return 2;
        if (saved === '5') return 5;
        if (saved === 'all') return 'all';
        return 3;
    });
    const [page, setPage] = useState(1);
    const [pageSizeOpen, setPageSizeOpen] = useState(false);
    const pageSizeRef = useRef<HTMLDivElement>(null);
    const domiksRef = useRef<HTMLDivElement>(null);
    const [columns, setColumns] = useState(1);
    const [lastSort, setLastSort] = useState(sortMode);
    if (lastSort !== sortMode) {
        setLastSort(sortMode);
        setPage(1);
    }

    const changeRowsPerPage = (rows: RowsPerPage) => {
        setRowsPerPage(rows);
        setPage(1);
        localStorage.setItem('domik-page-size', String(rows));
    };

    useEffect(() => {
        if (!pageSizeOpen) {
            return;
        }

        const onDown = (event: MouseEvent) => {
            if (pageSizeRef.current != null && !pageSizeRef.current.contains(event.target as Node)) {
                setPageSizeOpen(false);
            }
        };

        document.addEventListener('mousedown', onDown);
        return () => { document.removeEventListener('mousedown', onDown); };
    }, [pageSizeOpen]);

    useEffect(() => {
        const grid = domiksRef.current;
        if (grid == null) {
            return;
        }

        const measure = () => {
            const count = getComputedStyle(grid).gridTemplateColumns.split(' ').filter(Boolean).length;
            setColumns(Math.max(1, count));
        };
        measure();
        const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(measure);
        observer?.observe(grid);
        return () => { observer?.disconnect(); };
    }, []);

    const sortedDomiks = useMemo(
        () => sortDomiks(domiks, domikTypes, resources, sortMode),
        [domiks, domikTypes, resources, sortMode],
    );
    const perPage = rowsPerPage === 'all' ? Math.max(1, sortedDomiks.length) : Math.max(1, rowsPerPage * columns);
    const totalPages = Math.max(1, Math.ceil(sortedDomiks.length / perPage));
    const safePage = Math.min(page, totalPages);
    const pagedDomiks = sortedDomiks.slice((safePage - 1) * perPage, safePage * perPage);

    return (
        <>
            <div className="domiks" ref={domiksRef} data-weather={currentWeather?.logicName}>
                {domikTypes.length > 0 &&
                    pagedDomiks.map(domik => {
                        const domikType = domikTypes.find(x => x.id === domik.typeId);
                        if (domikType == null) {
                            return null;
                        }

                        const hasManufacture = domik.manufactures != null && domik.manufactures.length > 0;
                        const activeCount = domik.manufactures?.length ?? 0;
                        const maxSlots = domikType.levels.find(level => level.value === domik.level)?.maxManufactureCount;
                        const intensity = maxSlots == null
                            ? 'normal'
                            : activeCount >= maxSlots
                                ? 'fast'
                                : maxSlots > 1 && activeCount === 1
                                    ? 'slow'
                                    : 'normal';
                        const repeatedRecipeNames = (domik.manufactures ?? []).flatMap(manufacture => {
                            if (!manufacture.autoRepeat) {
                                return [];
                            }
                            const name = receipts.find(receipt => receipt.id === manufacture.receiptId)?.name;
                            return name == null ? [] : [name];
                        });
                        const repeatTitle = repeatedRecipeNames.length > 0
                            ? `Автоповтор: ${repeatedRecipeNames.join(', ')}`
                            : null;
                        const durationSecondsText = domik.finishDate != null
                            ? formatDuration(remainingSeconds(domik.finishDate, now))
                            : null;
                        const cardWeather = currentWeather?.effects.find(
                            effect => effect.domikTypeId === domik.typeId && effect.outputPercent !== 100) ?? null;
                        const crew = workers
                            .filter(worker => worker.manufactureId != null && (domik.manufactures ?? []).some(manufacture => manufacture.id === worker.manufactureId))
                            .slice(0, 4);
                        const displayName = namer(domik.typeId, domik.id, domikType.name, domikType.logicName);
                        const upgradeAvailable = canAffordUpgrade(domik, domikType, resources);
                        const cardStatus = domik.finishDate != null
                                ? 'идёт улучшение'
                            : hasManufacture
                                ? `идёт производство${repeatTitle == null ? '' : `, ${repeatTitle.toLocaleLowerCase()}`}`
                                : upgradeAvailable
                                    ? 'доступно улучшение'
                                    : 'готов к работе';
                        return (
                            <button key={domik.id} type="button"
                                className={'plot' + (selectedDomikId === domik.id ? ' plot-selected' : '')}
                                aria-label={`${displayName}, уровень ${domik.level}, ${cardStatus}`}
                                aria-pressed={selectedDomikId === domik.id}
                                onClick={() => onSelect(domik.id, domikType.logicName)}>
                                {cardWeather != null &&
                                    <span className={'plot-weather' + (cardWeather.outputPercent > 100 ? ' plot-weather-buff' : ' plot-weather-nerf')}
                                        title={`Погода: ${cardWeather.outputPercent > 100 ? "+" : ""}${cardWeather.outputPercent - 100}% выход`}>
                                        {cardWeather.outputPercent > 100 ? '+' : ''}{cardWeather.outputPercent - 100}%
                                    </span>
                                }
                                <AnimatedDomikSprite mode="levelup" className="plot-sprite" logicName={domikType.logicName} level={domik.level} working={hasManufacture} intensity={intensity} />
                                {crew.length > 0 &&
                                    <span className="plot-crew">
                                        {crew.map(worker =>
                                            <span key={worker.id} className="plot-crew-face" title={worker.name}>
                                                <WorkerSprite name={worker.name} state="working" data-size="32" aria-hidden="true" />
                                            </span>,
                                        )}
                                    </span>
                                }
                                <span className="plot-name">{displayName}</span>
                                <UpgradeBox durationSeconds={durationSecondsText} level={domik.level} />
                                <span className="plot-status">
                                    {upgradeAvailable &&
                                        <img className="status-icon" src="/images/upgrade_available.png" alt="" aria-hidden="true" title="Доступно улучшение" />
                                    }
                                    {domik.finishDate != null &&
                                        <img className="status-icon icon-busy" src="/images/upgrade_in_process.png" alt="" aria-hidden="true" title="Идёт улучшение" />
                                    }
                                    {hasManufacture &&
                                        <AbstractSprite logicName="production_recipe" size={24} className="status-icon" aria-hidden="true" />
                                    }
                                    {repeatTitle != null &&
                                        <RepeatIcon className="status-icon status-repeat" aria-hidden="true" title={repeatTitle} />
                                    }
                                </span>
                            </button>
                        );
                    })
                }
            </div>
            {(totalPages > 1 || domiks.length > 2 * columns) &&
                <div className="domik-pager">
                    {totalPages > 1 &&
                        <div className="domik-pager-nav">
                            <button type="button" className="btn-game btn-ghost btn-icon" disabled={safePage <= 1}
                                onClick={() => setPage(safePage - 1)} aria-label="Предыдущая страница">
                                <ChevronLeftIcon className="btn-ico" aria-hidden="true" />
                            </button>
                            <span className="domik-pager-status">Стр. {safePage} из {totalPages}</span>
                            <button type="button" className="btn-game btn-ghost btn-icon" disabled={safePage >= totalPages}
                                onClick={() => setPage(safePage + 1)} aria-label="Следующая страница">
                                <ChevronRightIcon className="btn-ico" aria-hidden="true" />
                            </button>
                        </div>
                    }
                    {domiks.length > 2 * columns &&
                        <div className="domik-sort-menu domik-page-size" ref={pageSizeRef}>
                            <button type="button" className="btn-game btn-ghost" aria-expanded={pageSizeOpen}
                                title="Рядов домиков на странице"
                                onClick={() => setPageSizeOpen(prev => !prev)}>
                                <BuildingIcon className="btn-ico" aria-hidden="true" />
                                {ROWS_PER_PAGE_OPTIONS.find(opt => opt.value === rowsPerPage)?.label ?? '3 ряда'}
                                <ChevronDownIcon className="btn-ico" aria-hidden="true" />
                            </button>
                            {pageSizeOpen &&
                                <div className="domik-sort-pop domik-page-size-pop">
                                    {ROWS_PER_PAGE_OPTIONS.map(opt =>
                                        <button key={String(opt.value)} type="button"
                                            className={'domik-sort-option' + (rowsPerPage === opt.value ? ' domik-sort-option-active' : '')}
                                            onClick={() => { changeRowsPerPage(opt.value); setPageSizeOpen(false); }}>
                                            {opt.label}
                                        </button>,
                                    )}
                                </div>
                            }
                        </div>
                    }
                </div>
            }
        </>
    );
};

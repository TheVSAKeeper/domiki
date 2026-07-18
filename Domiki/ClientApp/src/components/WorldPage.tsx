import { useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import ArrowLeftIcon from 'pixelarticons/svg/arrow-left.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import CoinsIcon from 'pixelarticons/svg/coins.svg?react';
import UsersIcon from 'pixelarticons/svg/users.svg?react';
import MapPinIcon from 'pixelarticons/svg/map-pin.svg?react';
import HeartIcon from 'pixelarticons/svg/heart.svg?react';
import CalendarIcon from 'pixelarticons/svg/calendar.svg?react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import BookOpenIcon from 'pixelarticons/svg/book-open.svg?react';
import { ApiError, getWorld, leaveGuestbookEntry, visitVillage } from '../services/api';
import { useToast } from '../services/toastContext';
import { GUESTBOOK_PHRASES } from '../constants/guestbookPhrases';
import { formatDuration, remainingSeconds } from '../utils/time';
import { StatChip } from './StatChip';
import { PixelLoader } from './PixelLoader';
import { WorldMap } from './WorldMap';
import { Crest } from './Crest';
import { GuestbookEntryRow } from './GuestbookEntryRow';
import { villageKey } from '../utils/worldMap';
import type { VillageVisitDto, WorldDto, WorldVillageDto } from '../types/api';

type SortKey = 'level' | 'seasonOrders' | 'seasonToloka' | 'seasonExpeditions' | 'comfort';

const SORT_META: Record<SortKey, { label: string; Icon: typeof HomeIcon }> = {
    level: { label: 'Обжитость', Icon: HomeIcon },
    seasonOrders: { label: 'Поставщик', Icon: CoinsIcon },
    seasonToloka: { label: 'Толока', Icon: UsersIcon },
    seasonExpeditions: { label: 'Странник', Icon: MapPinIcon },
    comfort: { label: 'Уют', Icon: HeartIcon },
};

const SORT_TABS = (Object.keys(SORT_META) as SortKey[]).map(key => ({ key, ...SORT_META[key] }));

const LevelBreakdown = ({ visit }: { visit: VillageVisitDto }) => (
    <div className="world-level-grid">
        <span>Постройки: {visit.level.buildings}</span>
        <span>Жители: {visit.level.residents}</span>
        <span>Репутация: {visit.level.reputation}</span>
        <span>Уют: {visit.level.comfort}</span>
    </div>
);

export const WorldPage = () => {
    const toast = useToast();
    const [world, setWorld] = useState<WorldDto | null>(null);
    const [selectedVillage, setSelectedVillage] = useState<WorldVillageDto | null>(null);
    const [visit, setVisit] = useState<VillageVisitDto | null>(null);
    const [visitLoading, setVisitLoading] = useState(false);
    const [sortKey, setSortKey] = useState<SortKey>('level');
    const [focus, setFocus] = useState<{ key: string; seq: number } | null>(null);
    const [now, setNow] = useState(() => Date.now());
    const [guestbookPhraseId, setGuestbookPhraseId] = useState<number | null>(null);
    const [guestbookBusy, setGuestbookBusy] = useState(false);
    const visitControllerRef = useRef<AbortController | null>(null);

    useEffect(() => () => { visitControllerRef.current?.abort(); }, []);

    useEffect(() => {
        const id = setInterval(() => { setNow(Date.now()); }, 1000);
        return () => clearInterval(id);
    }, []);

    const sortedVillages = useMemo(() => {
        if (world == null) {
            return [];
        }
        return [...world.villages].sort((a, b) => b[sortKey] - a[sortKey] || a.villageName.localeCompare(b.villageName, 'ru'));
    }, [world, sortKey]);

    const groupedBuildings = useMemo(() => {
        if (visit == null) return [];
        const m = new Map<string, { typeName: string; level: number; count: number }>();
        for (const b of visit.buildings) {
            const key = `${b.typeName}#${b.level}`;
            const g = m.get(key);
            if (g) g.count += 1;
            else m.set(key, { typeName: b.typeName, level: b.level, count: 1 });
        }
        return [...m.values()].sort((a, b) => b.level - a.level || a.typeName.localeCompare(b.typeName));
    }, [visit]);

    useEffect(() => {
        const controller = new AbortController();

        void (async () => {
            try {
                setWorld(await getWorld(controller.signal));
            } catch (err) {
                if (err instanceof DOMException && err.name === 'AbortError') {
                    return;
                }
                if (err instanceof ApiError) {
                    toast.error(err.message);
                }
            }
        })();

        return () => { controller.abort(); };
    }, [toast]);

    const openVillage = async (village: WorldVillageDto) => {
        visitControllerRef.current?.abort();
        setGuestbookPhraseId(null);

        if (village.playerId == null) {
            visitControllerRef.current = null;
            setSelectedVillage(village);
            setVisit(null);
            setVisitLoading(false);
            return;
        }

        const controller = new AbortController();
        visitControllerRef.current = controller;
        setSelectedVillage(village);
        setVisit(null);
        setVisitLoading(true);
        try {
            setVisit(await visitVillage(village.playerId, controller.signal));
        } catch (err) {
            if (err instanceof DOMException && err.name === 'AbortError') {
                return;
            }
            if (err instanceof ApiError) {
                toast.error(err.message);
                return;
            }
            throw err;
        } finally {
            if (visitControllerRef.current === controller) {
                setVisitLoading(false);
            }
        }
    };

    const submitGuestbookEntry = async () => {
        const target = selectedVillage;
        if (target?.playerId == null || guestbookPhraseId == null) {
            return;
        }
        const controllerBefore = visitControllerRef.current;
        setGuestbookBusy(true);
        try {
            await leaveGuestbookEntry(target.playerId, guestbookPhraseId);
            if (visitControllerRef.current === controllerBefore) {
                await openVillage(target);
            }
        } catch (err) {
            if (err instanceof ApiError) {
                toast.error(err.message);
                return;
            }
            throw err;
        } finally {
            setGuestbookBusy(false);
        }
    };

    if (world == null) {
        return <div className="wiki"><PixelLoader label="Загрузка мира…" /></div>;
    }

    const seasonLeft = remainingSeconds(world.season.endDate, now);
    const activeMetric = SORT_META[sortKey];

    return (
        <div className="wiki world-page">
            <section className="wiki-intro pixel-panel world-head">
                <div>
                    <h1 className="wiki-title">Мир</h1>
                    <div className="world-head-stats">
                        <StatChip icon={<HomeIcon className="stat-chip-ico" aria-hidden="true" />} title="Деревень в мире">
                            {world.villages.length} деревень
                        </StatChip>
                        <StatChip icon={<CalendarIcon className="stat-chip-ico" aria-hidden="true" />} title="Текущий сезон">
                            Сезон {world.season.number}
                        </StatChip>
                        <StatChip icon={<ClockIcon className="stat-chip-ico" aria-hidden="true" />} title="До конца сезона">
                            {formatDuration(seasonLeft)}
                        </StatChip>
                    </div>
                </div>
                <Link className="btn-game" to="/domiki-page">
                    <ArrowLeftIcon className="btn-ico" aria-hidden="true" />
                    В игру
                </Link>
            </section>

            <section className="wiki-section world-tabs pixel-panel">
                {SORT_TABS.map(tab => (
                    <button
                        type="button"
                        key={tab.key}
                        className={'world-tab' + (tab.key === sortKey ? ' world-tab-active' : '')}
                        onClick={() => { setSortKey(tab.key); }}
                    >
                        <tab.Icon className="world-tab-ico" aria-hidden="true" />
                        {tab.label}
                    </button>
                ))}
            </section>

            <section className="wiki-section world-layout">
                <div className="world-main">
                    <WorldMap
                        villages={world.villages}
                        metricKey={sortKey}
                        metricLabel={activeMetric.label}
                        selectedKey={selectedVillage == null ? null : villageKey(selectedVillage)}
                        onSelect={village => { void openVillage(village); }}
                        focus={focus}
                    />
                    <div className="world-ledger pixel-panel">
                        <h2 className="panel-title world-ledger-title">
                            <activeMetric.Icon className="world-tab-ico" aria-hidden="true" />
                            Летопись – {activeMetric.label}
                        </h2>
                        {sortedVillages.slice(0, 10).map((village, index) => (
                            <button
                                type="button"
                                key={villageKey(village)}
                                className={'world-row' + (village.isMe ? ' world-row-me' : '') + (selectedVillage === village ? ' world-row-selected' : '')}
                                onClick={() => {
                                    void openVillage(village);
                                    setFocus({ key: villageKey(village), seq: Date.now() });
                                }}
                                title={village.isNpc ? 'Сосед' : 'Визит'}
                            >
                                <span className="world-rank">{index + 1}</span>
                                <Crest icon={village.crestIcon} color={village.crestColor} />
                                <span className="world-name">
                                    {village.villageName}
                                    {village.isMe && <span className="world-tag">моя</span>}
                                    {village.isNpc && <span className="world-tag">NPC</span>}
                                </span>
                                <span className="world-metric" title={activeMetric.label}>
                                    {sortKey !== 'level' &&
                                        <>
                                            <activeMetric.Icon className="world-metric-ico" aria-hidden="true" />
                                            {village[sortKey]}
                                        </>
                                    }
                                </span>
                                <span className="world-level">{village.level}</span>
                            </button>
                        ))}
                    </div>
                </div>

                <aside className="world-visit pixel-panel">
                    {selectedVillage == null &&
                        <div className="world-legend">
                            <h2 className="panel-title">Весь мир — на ладони</h2>
                            <p className="hint">Исследуй долину, находи сильнейшие артели и заглядывай в гости. Карта живая: тяни её и меняй масштаб.</p>
                            <ul className="world-legend-list">
                                <li>Поселения растут вместе с обжитостью</li>
                                <li>Золотой вымпел отмечает твою деревню</li>
                                <li>Номер над поляной — место в сезонном топ-3</li>
                                <li>Большая эмблема — торговое село соседей</li>
                            </ul>
                        </div>
                    }
                    {selectedVillage != null && selectedVillage.isNpc &&
                        <div className="world-visit-head">
                            <Crest icon={selectedVillage.crestIcon} color={selectedVillage.crestColor} />
                            <div>
                                <h2 className="panel-title">{selectedVillage.villageName}</h2>
                                <p className="hint">Торгует ресурсом #{selectedVillage.npcResourceTypeId}</p>
                            </div>
                        </div>
                    }
                    {visitLoading && <PixelLoader label="Загрузка визита…" />}
                    {visit != null &&
                        <>
                            <div className="world-visit-head">
                                <Crest icon={visit.crestIcon} color={visit.crestColor} />
                                <div>
                                    <h2 className="panel-title">{visit.villageName}</h2>
                                    <div className="world-visit-level">Обжитость {visit.level.level}</div>
                                </div>
                            </div>
                            <LevelBreakdown visit={visit} />
                            <div className="world-buildings">
                                {groupedBuildings.length === 0 && <p className="hint">Построек нет</p>}
                                {groupedBuildings.map(g => (
                                    <div key={`${g.typeName}#${g.level}`} className="world-building-row">
                                        <span>{g.typeName}</span>
                                        <span>ур. {g.level}</span>
                                        {g.count > 1 && <span className="world-building-count">×{g.count}</span>}
                                    </div>
                                ))}
                            </div>

                            <div className="world-guestbook">
                                <h3 className="panel-title world-guestbook-title">
                                    <BookOpenIcon className="world-guestbook-ico" aria-hidden="true" />
                                    Книга гостей
                                </h3>
                                {visit.guestbook.length === 0
                                    ? <p className="hint">Пока никто не расписался</p>
                                    : (
                                        <div className="world-guestbook-list">
                                            {visit.guestbook.map(entry => (
                                                <GuestbookEntryRow key={`${entry.guestPlayerId}-${entry.date}`} entry={entry} now={now} />
                                            ))}
                                        </div>
                                    )
                                }
                                {selectedVillage?.isMe !== true &&
                                    <div className="world-guestbook-action">
                                        {visit.canLeaveEntry &&
                                            <>
                                                <div className="world-guestbook-phrases">
                                                    {Object.entries(GUESTBOOK_PHRASES).map(([id, text]) => (
                                                        <button type="button" key={id}
                                                            className={'world-guestbook-phrase' + (guestbookPhraseId === Number(id) ? ' world-guestbook-phrase-active' : '')}
                                                            onClick={() => { setGuestbookPhraseId(Number(id)); }}>
                                                            {text}
                                                        </button>
                                                    ))}
                                                </div>
                                                <button type="button" className="btn-game" disabled={guestbookPhraseId == null || guestbookBusy}
                                                    onClick={() => { void submitGuestbookEntry(); }}>
                                                    <BookOpenIcon className="btn-ico" aria-hidden="true" />
                                                    Расписаться
                                                </button>
                                            </>
                                        }
                                        {!visit.canLeaveEntry && visit.alreadyLeftToday &&
                                            <p className="hint world-guestbook-status">Вы уже расписались сегодня</p>
                                        }
                                        {!visit.canLeaveEntry && !visit.alreadyLeftToday &&
                                            <p className="hint world-guestbook-status">Чтобы расписаться, нужна своя деревня с обжитостью {visit.guestbookUnlockLevel}</p>
                                        }
                                    </div>
                                }
                            </div>
                        </>
                    }
                </aside>
            </section>
        </div>
    );
};

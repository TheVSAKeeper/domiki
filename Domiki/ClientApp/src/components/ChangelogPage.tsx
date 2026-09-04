import { useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import ArrowLeftIcon from 'pixelarticons/svg/arrow-left.svg?react';
import ChevronRightIcon from 'pixelarticons/svg/chevron-right.svg?react';
import type { ChangelogEntry } from '../constants/changelog';
import { CHANGELOG } from '../constants/changelog';
import { getLastSeenChangelogId, markChangelogSeen } from '../utils/changelogSeen';
import { applyUpdate, checkForUpdate, loadedBuildId, useUpdateAvailable } from '../services/appVersion';
import '../styles/changelog.css';

type CheckState = 'idle' | 'checking' | 'fresh' | 'unknown' | 'failed';

const checkNotes: Record<CheckState, string> = {
    idle: '',
    checking: 'Смотрю, что пишут из города…',
    fresh: 'Свежее нет – у вас последний выпуск',
    unknown: 'Городская почта молчит – новостей не привезли',
    failed: 'Гонец не доехал – попробуйте позже',
};

const dateFormatter = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' });

const dayFormatter = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'long' });

const monthFormatter = new Intl.DateTimeFormat('ru-RU', { month: 'long', year: 'numeric' });

const monthLabel = (date: string): string => {
    const text = monthFormatter.format(new Date(date)).replace(' г.', '');
    return text.charAt(0).toUpperCase() + text.slice(1);
};

interface ChangelogMonth {
    key: string;
    label: string;
    entries: ChangelogEntry[];
}

const buildMonths = (): ChangelogMonth[] => {
    const byMonth = new Map<string, ChangelogEntry[]>();
    for (const entry of [...CHANGELOG].reverse()) {
        const key = entry.date.slice(0, 7);
        const bucket = byMonth.get(key);
        if (bucket == null) {
            byMonth.set(key, [entry]);
        } else {
            bucket.push(entry);
        }
    }

    return [...byMonth.entries()].flatMap(([key, entries]) => {
        const first = entries[0];
        return first == null ? [] : [{ key, label: monthLabel(first.date), entries }];
    });
};

export const ChangelogPage = () => {
    const [lastSeenId] = useState(getLastSeenChangelogId);
    const [checkState, setCheckState] = useState<CheckState>('idle');
    const updateAvailable = useUpdateAvailable();

    const titleRef = useRef<HTMLHeadingElement>(null);

    useEffect(() => {
        markChangelogSeen();
        titleRef.current?.focus({ preventScroll: true });
    }, []);

    const months = useMemo(() => buildMonths(), []);
    const markFresh = lastSeenId > 0;
    const latest = months[0]?.entries[0];
    const buildId = loadedBuildId();

    const [openIds, setOpenIds] = useState<ReadonlySet<number>>(() => {
        const unread = CHANGELOG.filter(entry => lastSeenId > 0 && entry.id > lastSeenId).map(entry => entry.id);
        const newest = CHANGELOG.reduce((max, entry) => Math.max(max, entry.id), 0);
        return new Set(unread.length > 0 ? unread : [newest]);
    });

    const toggle = (id: number) => {
        setOpenIds(previous => {
            const next = new Set(previous);
            if (!next.delete(id)) {
                next.add(id);
            }
            return next;
        });
    };

    const runCheck = async () => {
        setCheckState('checking');
        try {
            const result = await checkForUpdate();
            setCheckState(result === 'update' ? 'idle' : result);
        } catch {
            setCheckState('failed');
        }
    };

    return (
        <div className="changelog-page">
            <section className="changelog-intro pixel-panel">
                <div className="changelog-head">
                    <h1 ref={titleRef} tabIndex={-1} className="changelog-page-title">Сельский вестник</h1>
                    <span className="changelog-subtitle">Летопись деревни – выпуск за выпуском</span>
                </div>
                <div className="changelog-status">
                    <div className="changelog-version">
                        <span className="changelog-version-label">У вас на руках</span>
                        <span className="changelog-version-value">
                            {latest == null
                                ? 'выпуск неизвестен'
                                : `Выпуск №${latest.id} от ${dateFormatter.format(new Date(latest.date))}`}
                        </span>
                        {buildId != null && <span className="changelog-build">сборка {buildId}</span>}
                    </div>
                    <div className="changelog-check">
                        {updateAvailable
                            ? (
                                <button type="button" className="changelog-check-button" onClick={applyUpdate}>
                                    Взять новый выпуск
                                </button>
                            )
                            : (
                                <button type="button" className="changelog-check-button" disabled={checkState === 'checking'}
                                    onClick={() => void runCheck()}>
                                    Проверить обновления
                                </button>
                            )}
                        <span className="changelog-check-note" role="status" aria-live="polite">
                            {updateAvailable ? 'Вышел новый выпуск' : checkNotes[checkState]}
                        </span>
                    </div>
                </div>
                <Link className="btn-game" to="/domiki-page">
                    <ArrowLeftIcon className="btn-ico" aria-hidden="true" />
                    В игру
                </Link>
            </section>
            <div className="changelog-list">
                {months.map(month => (
                    <section key={month.key} className="changelog-month pixel-panel">
                        <h2 className="changelog-month-title">{month.label}</h2>
                        {month.entries.map(entry => {
                            const expanded = openIds.has(entry.id);
                            const fresh = markFresh && entry.id > lastSeenId;
                            return (
                                <article key={entry.id} className="changelog-issue" data-fresh={fresh} data-open={expanded}>
                                    <h3 className="changelog-issue-heading">
                                        <button type="button" className="changelog-issue-toggle" aria-expanded={expanded} onClick={() => { toggle(entry.id); }}>
                                            <ChevronRightIcon className="changelog-chevron" aria-hidden="true" />
                                            <span className="changelog-title">{entry.title}</span>
                                            {fresh && <span className="changelog-fresh">новое</span>}
                                            <span className="changelog-date">{dayFormatter.format(new Date(entry.date))}</span>
                                        </button>
                                    </h3>
                                    {expanded &&
                                        <div className="changelog-issue-body">
                                            <p className="changelog-lore">{entry.lore}</p>
                                            <ul className="changelog-items">
                                                {entry.items.map(item => <li key={item}>{item}</li>)}
                                            </ul>
                                        </div>
                                    }
                                </article>
                            );
                        })}
                    </section>
                ))}
            </div>
        </div>
    );
};

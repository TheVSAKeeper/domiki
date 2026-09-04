import { useLayoutEffect, useMemo, useRef, useState } from 'react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import ChevronRightIcon from 'pixelarticons/svg/chevron-right.svg?react';
import type { ChangelogEntry } from '../constants/changelog';
import { CHANGELOG } from '../constants/changelog';
import { applyUpdate, checkForUpdate, loadedBuildId, useUpdateAvailable } from '../services/appVersion';

interface ChangelogModalProps {
    lastSeenId: number;
    onClose: () => void;
}

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

export const ChangelogModal = ({ lastSeenId, onClose }: ChangelogModalProps) => {
    const dialogRef = useRef<HTMLDialogElement>(null);
    const [checkState, setCheckState] = useState<CheckState>('idle');
    const updateAvailable = useUpdateAvailable();

    useLayoutEffect(() => {
        const dialog = dialogRef.current;
        if (dialog != null && !dialog.open) {
            dialog.showModal();
        }
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
        <dialog ref={dialogRef} className="changelog-modal pixel-panel" aria-label="Сельский вестник" onClose={onClose}>
            <div className="changelog-head">
                <div>
                    <h2 className="changelog-modal-title">Сельский вестник</h2>
                    <span className="changelog-subtitle">Летопись деревни – выпуск за выпуском</span>
                </div>
                <button type="button" className="changelog-close" title="Закрыть" onClick={onClose}>
                    <CloseIcon aria-hidden="true" />
                </button>
            </div>
            <div className="changelog-scroll">
                {months.map(month => (
                    <section key={month.key} className="changelog-month">
                        <h3 className="changelog-month-title">{month.label}</h3>
                        {month.entries.map(entry => {
                            const expanded = openIds.has(entry.id);
                            const fresh = markFresh && entry.id > lastSeenId;
                            return (
                                <article key={entry.id} className="changelog-issue" data-fresh={fresh} data-open={expanded}>
                                    <h4 className="changelog-issue-heading">
                                        <button type="button" className="changelog-issue-toggle" aria-expanded={expanded} onClick={() => { toggle(entry.id); }}>
                                            <ChevronRightIcon className="changelog-chevron" aria-hidden="true" />
                                            <span className="changelog-title">{entry.title}</span>
                                            {fresh && <span className="changelog-fresh">новое</span>}
                                            <span className="changelog-date">{dayFormatter.format(new Date(entry.date))}</span>
                                        </button>
                                    </h4>
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
            <div className="changelog-foot">
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
        </dialog>
    );
};

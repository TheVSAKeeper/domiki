import { useLayoutEffect, useRef, useState } from 'react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
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

    const entries = [...CHANGELOG].reverse();
    const markFresh = lastSeenId > 0;
    const latest = entries[0];
    const buildId = loadedBuildId();

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
                {entries.map(entry => (
                    <article key={entry.id} className="changelog-issue" data-fresh={markFresh && entry.id > lastSeenId}>
                        <div className="changelog-issue-head">
                            <span className="changelog-date">Выпуск от {dateFormatter.format(new Date(entry.date))}</span>
                            {markFresh && entry.id > lastSeenId && <span className="changelog-fresh">новое</span>}
                        </div>
                        <h3 className="changelog-title">{entry.title}</h3>
                        <p className="changelog-lore">{entry.lore}</p>
                        <ul className="changelog-items">
                            {entry.items.map(item => <li key={item}>{item}</li>)}
                        </ul>
                    </article>
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

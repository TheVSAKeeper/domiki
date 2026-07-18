import { useLayoutEffect, useRef } from 'react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import { CHANGELOG } from '../constants/changelog';

interface ChangelogModalProps {
    lastSeenId: number;
    onClose: () => void;
}

const dateFormatter = new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' });

export const ChangelogModal = ({ lastSeenId, onClose }: ChangelogModalProps) => {
    const dialogRef = useRef<HTMLDialogElement>(null);

    useLayoutEffect(() => {
        const dialog = dialogRef.current;
        if (dialog != null && !dialog.open) {
            dialog.showModal();
        }
    }, []);

    const entries = [...CHANGELOG].reverse();
    const markFresh = lastSeenId > 0;

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
        </dialog>
    );
};

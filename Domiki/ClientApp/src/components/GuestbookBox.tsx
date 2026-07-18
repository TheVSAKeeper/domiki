import { useEffect, useState } from 'react';
import BookOpenIcon from 'pixelarticons/svg/book-open.svg?react';
import { ApiError, getGuestbook } from '../services/api';
import { useToast } from '../services/toastContext';
import type { GuestbookDto } from '../types/api';
import { GuestbookEntryRow } from './GuestbookEntryRow';
import { PixelLoader } from './PixelLoader';

interface GuestbookBoxProps {
    now: number;
}

export const GuestbookBox = ({ now }: GuestbookBoxProps) => {
    const toast = useToast();
    const [guestbook, setGuestbook] = useState<GuestbookDto | null>(null);

    useEffect(() => {
        const controller = new AbortController();

        void (async () => {
            try {
                setGuestbook(await getGuestbook(controller.signal));
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

    return (
        <section className="guestbook-panel pixel-panel">
            <header className="guestbook-hero">
                <span className="guestbook-hero-emblem" aria-hidden="true"><BookOpenIcon /></span>
                <div className="guestbook-hero-text">
                    <h3 className="guestbook-hero-title panel-title">Книга гостей</h3>
                    <p className="guestbook-hero-sub">Гостей за сезон: {guestbook?.visitsThisSeason ?? 0}</p>
                </div>
            </header>

            {guestbook == null && <PixelLoader label="Загрузка книги гостей…" />}

            {guestbook != null && guestbook.entries.length === 0 &&
                <p className="hint">Пока никто не расписался</p>
            }

            {guestbook != null && guestbook.entries.length > 0 &&
                <div className="guestbook-list">
                    {guestbook.entries.map(entry => (
                        <GuestbookEntryRow key={`${entry.guestPlayerId}-${entry.date}`} entry={entry} now={now} />
                    ))}
                </div>
            }
        </section>
    );
};

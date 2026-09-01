import { useState } from 'react';
import { latestChangelogId } from '../constants/changelog';
import { ChangelogModal } from './ChangelogModal';
import { MechanicSprite } from './sprites';
import '../styles/changelog.css';

const STORAGE_KEY = 'changelog-last-seen';

export const ChangelogButton = ({ onOpen }: { onOpen?: () => void }) => {
    const [open, setOpen] = useState(false);
    const [lastSeen, setLastSeen] = useState(() => {
        const saved = localStorage.getItem(STORAGE_KEY);
        const parsed = saved == null ? 0 : Number(saved);
        return Number.isFinite(parsed) ? parsed : 0;
    });

    const unread = latestChangelogId > lastSeen;

    const close = () => {
        localStorage.setItem(STORAGE_KEY, String(latestChangelogId));
        setLastSeen(latestChangelogId);
        setOpen(false);
    };

    return (
        <>
            <button type="button" className="nav-link changelog-chip" title="Сельский вестник"
                aria-label="Сельский вестник – история изменений" onClick={() => { setOpen(true); onOpen?.(); }}>
                <MechanicSprite logicName="vestnik" size={24} className="nav-ico" aria-hidden="true" />
                Вестник
                {unread && <span className="hud-news-dot" aria-hidden="true" />}
            </button>
            {open && <ChangelogModal lastSeenId={lastSeen} onClose={close} />}
        </>
    );
};

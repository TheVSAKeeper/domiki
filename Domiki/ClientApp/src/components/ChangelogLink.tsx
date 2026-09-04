import { useSyncExternalStore } from 'react';
import { Link } from 'react-router-dom';
import { latestChangelogId } from '../constants/changelog';
import { getLastSeenChangelogId, subscribeChangelogSeen } from '../utils/changelogSeen';
import { MechanicSprite } from './sprites';

export const ChangelogLink = ({ onNavigate }: { onNavigate?: () => void }) => {
    const lastSeen = useSyncExternalStore(subscribeChangelogSeen, getLastSeenChangelogId);
    const unread = latestChangelogId > lastSeen;

    return (
        <Link className="nav-link changelog-chip" to="/vestnik" onClick={onNavigate}
            title="Сельский вестник" aria-label="Сельский вестник – история изменений">
            <MechanicSprite logicName="vestnik" size={24} className="nav-ico" aria-hidden="true" />
            <span className="nav-text">Вестник</span>
            {unread && <span className="hud-news-dot" aria-hidden="true" />}
        </Link>
    );
};

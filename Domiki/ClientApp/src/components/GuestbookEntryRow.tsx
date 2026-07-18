import type { GuestbookEntryDto } from '../types/api';
import { guestbookPhraseText } from '../constants/guestbookPhrases';
import { formatRelativeTime } from '../utils/time';
import { Crest } from './Crest';

interface GuestbookEntryRowProps {
    entry: GuestbookEntryDto;
    now: number;
}

export const GuestbookEntryRow = ({ entry, now }: GuestbookEntryRowProps) => (
    <div className="guestbook-entry">
        <Crest icon={entry.guestCrestIcon} color={entry.guestCrestColor} className="crest-badge-small" />
        <div className="guestbook-entry-body">
            <span className="guestbook-entry-name">{entry.guestVillageName}</span>
            <span className="guestbook-entry-phrase">«{guestbookPhraseText(entry.phraseId)}»</span>
        </div>
        <time className="guestbook-entry-time">{formatRelativeTime(entry.date, now)}</time>
    </div>
);

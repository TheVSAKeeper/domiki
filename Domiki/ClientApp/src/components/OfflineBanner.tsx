interface OfflineBannerProps {
    staleSince: number | null;
    subject?: string;
}

export const OfflineBanner = ({ staleSince, subject = 'деревня показана такой, какой была' }: OfflineBannerProps) => {
    if (staleSince == null) {
        return null;
    }

    const savedAt = new Date(staleSince).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });

    return (
        <div className="offline-banner" role="status" aria-live="polite">
            Связи нет – {subject} в {savedAt}
        </div>
    );
};

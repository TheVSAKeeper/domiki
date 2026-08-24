interface OfflineBannerProps {
    staleSince: number | null;
}

export const OfflineBanner = ({ staleSince }: OfflineBannerProps) => {
    if (staleSince == null) {
        return null;
    }

    const savedAt = new Date(staleSince).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });

    return (
        <div className="offline-banner" role="status" aria-live="polite">
            Связи нет – деревня показана такой, какой была в {savedAt}
        </div>
    );
};

interface OfflineBannerProps {
    staleSince: number | null;
    subject?: string;
    pendingCount?: number;
}

const pendingText = (count: number): string => {
    const tail = count % 100 >= 11 && count % 100 <= 14 ? 'дел' : [null, 'дело', 'дела', 'дела', 'дела'][count % 10] ?? 'дел';
    const verb = tail === 'дело' ? 'ждёт' : 'ждут';
    return `${String(count)} ${tail} ${verb} связи`;
};

export const OfflineBanner = ({ staleSince, subject = 'деревня показана такой, какой была', pendingCount = 0 }: OfflineBannerProps) => {
    if (staleSince == null) {
        return null;
    }

    const savedAt = new Date(staleSince).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });

    return (
        <div className="offline-banner" role="status" aria-live="polite">
            Связи нет – {subject} в {savedAt}
            {pendingCount > 0 && <>. {pendingText(pendingCount)} – отправим, как вернётся</>}
        </div>
    );
};

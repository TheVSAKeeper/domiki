export function formatDuration(totalSeconds: number): string {
    let total = Math.max(0, Math.round(totalSeconds));
    const days = Math.floor(total / 86400);
    total %= 86400;
    const hours = Math.floor(total / 3600);
    total %= 3600;
    const minutes = Math.floor(total / 60);
    const seconds = total % 60;

    const parts: string[] = [];
    if (days > 0) {
        parts.push(`${days}д`);
    }
    if (hours > 0) {
        parts.push(`${hours}ч`);
    }
    if (minutes > 0) {
        parts.push(`${minutes}м`);
    }
    if (seconds > 0) {
        parts.push(`${seconds}с`);
    }

    return parts.length > 0 ? parts.join(' ') : '0с';
}

export function formatDurationShort(totalSeconds: number): string {
    const total = Math.max(0, Math.round(totalSeconds));
    return total < 60 ? `${total}с` : formatDuration(total - (total % 60));
}

export function formatClock(totalSeconds: number): string {
    const total = Math.max(0, Math.round(totalSeconds));
    const days = Math.floor(total / 86400);
    if (days > 0) {
        return `${days}д ${Math.floor((total % 86400) / 3600)}ч`;
    }

    const hours = Math.floor(total / 3600);
    const minutes = Math.floor((total % 3600) / 60);
    const seconds = total % 60;
    const pad = (value: number) => String(value).padStart(2, '0');
    return hours > 0 ? `${hours}:${pad(minutes)}:${pad(seconds)}` : `${minutes}:${pad(seconds)}`;
}

export function remainingSeconds(finishDate: string, now: number): number {
    return (new Date(finishDate).getTime() - now) / 1000;
}

export function formatRelativeTime(dateIso: string, now: number): string {
    const seconds = Math.max(1, Math.floor((now - Date.parse(dateIso)) / 1000));
    if (seconds < 45) {
        return 'только что';
    }
    if (seconds < 3600) {
        return `${Math.floor(seconds / 60)} мин назад`;
    }
    if (seconds < 86400) {
        return `${Math.floor(seconds / 3600)} ч назад`;
    }
    return `${Math.floor(seconds / 86400)} дн назад`;
}

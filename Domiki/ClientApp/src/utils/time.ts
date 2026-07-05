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
        parts.push(days + 'д');
    }
    if (hours > 0) {
        parts.push(hours + 'ч');
    }
    if (minutes > 0) {
        parts.push(minutes + 'м');
    }
    if (seconds > 0) {
        parts.push(seconds + 'с');
    }

    return parts.length > 0 ? parts.join(' ') : '0с';
}

export function remainingSeconds(finishDate: string, now: number): number {
    return (new Date(finishDate).getTime() - now) / 1000;
}

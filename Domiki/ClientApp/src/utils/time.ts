function pad(value: number): string {
    return value < 10 ? '0' + value : String(value);
}

export function formatDuration(totalSeconds: number): string {
    const total = Math.round(totalSeconds);
    const seconds = total % 60;
    let minutes = Math.floor(total / 60);
    let hours = 0;
    let days = 0;

    if (minutes > 0) {
        hours = Math.floor(minutes / 60);
        minutes = minutes % 60;
    }

    if (hours > 0) {
        days = Math.floor(hours / 24);
        hours = hours % 24;
    }

    let result = '';

    if (days > 0) {
        result += pad(days) + 'д ';
    }

    if (hours > 0 || days > 0) {
        result += pad(hours) + 'ч ';
    }

    if (minutes > 0 || days > 0 || hours > 0) {
        result += pad(minutes) + 'м ';
    }

    if (days === 0) {
        result += pad(seconds) + 'с ';
    }

    return result;
}

export function remainingSeconds(finishDate: string, now: number): number {
    return (new Date(finishDate).getTime() - now) / 1000;
}

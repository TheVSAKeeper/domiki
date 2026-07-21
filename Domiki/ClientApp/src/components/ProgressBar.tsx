interface ProgressBarProps {
    value: number;
    max: number;
    label?: string;
    done?: boolean;
    className?: string;
}

export const ProgressBar = ({ value, max, label, done, className }: ProgressBarProps) => (
    <progress
        className={'progress-bar' + (done ? ' progress-done' : '') + (className == null ? '' : ' ' + className)}
        value={Math.max(0, Math.min(value, max))}
        max={Math.max(1, max)}
        data-label={label ?? ''}
    />
);

interface ProgressBarProps {
    value: number;
    max: number;
    label?: string;
    done?: boolean;
}

export const ProgressBar = ({ value, max, label, done }: ProgressBarProps) => (
    <progress
        className={'progress-bar' + (done ? ' progress-done' : '')}
        value={Math.max(0, Math.min(value, max))}
        max={Math.max(1, max)}
        data-label={label ?? ''}
    />
);

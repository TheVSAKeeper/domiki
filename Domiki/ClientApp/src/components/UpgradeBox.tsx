interface UpgradeBoxProps {
    durationSeconds: string | null;
    level: number;
}

export const UpgradeBox = ({ durationSeconds, level }: UpgradeBoxProps) => {
    const levelText = durationSeconds == null ? 'ур. ' + level : level + ' → ' + (level + 1);

    return (
        <span className="upgrade-box">
            {durationSeconds != null &&
                <span className="timer">{durationSeconds}</span>
            }
            <span className="domik-level">{levelText}</span>
        </span>
    );
};

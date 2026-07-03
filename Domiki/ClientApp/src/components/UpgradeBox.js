import React from 'react';

export const UpgradeBox = ({ durationSeconds, level }) => {
    let levelText = durationSeconds == null ? 'ур. ' + level : level + ' → ' + (level * 1 + 1);

    return (
        <span className="upgrade-box">
            {durationSeconds != null &&
                <span className="timer">{durationSeconds}</span>
            }
            <span className="domik-level">{levelText}</span>
        </span>
    );
};

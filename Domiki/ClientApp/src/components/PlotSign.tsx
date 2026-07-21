import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import RepeatIcon from 'pixelarticons/svg/repeat.svg?react';
import { ResourceSprite } from './sprites';

export type PlotStateKind = 'upgrading' | 'working' | 'upgradeable' | 'idle';

export interface PlotState {
    kind: PlotStateKind;
    label: string;
    output: string | null;
    timer: string | null;
    slots: number | null;
    repeat: boolean;
}

export const PlotSign = ({ kind, label, output, timer, slots, repeat }: PlotState) => (
    <span className={'plot-sign plot-sign-' + kind}>
        <span className="plot-sign-what">
            {output != null &&
                <ResourceSprite logicName={output} size={24} className="plot-sign-ico" aria-hidden="true" />
            }
            {output == null && (kind === 'upgrading' || kind === 'upgradeable') &&
                <ChevronUpIcon className="plot-sign-ico" aria-hidden="true" />
            }
            <span className="plot-sign-label">{label}</span>
            {slots != null &&
                <span className="plot-sign-slots">×{slots}</span>
            }
            {repeat &&
                <RepeatIcon className="plot-sign-repeat" aria-hidden="true" />
            }
        </span>
        {timer != null &&
            <span className="plot-sign-timer">{timer}</span>
        }
    </span>
);

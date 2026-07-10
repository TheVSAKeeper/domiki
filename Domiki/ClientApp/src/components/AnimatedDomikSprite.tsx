import { useEffect, useRef, useState } from 'react';
import { DomikSprite } from './sprites';

const LEVELUP_DURATION = 560;
const CYCLE_INTERVAL = 1400;
const CYCLE_FLASH_DURATION = 300;

interface AnimatedDomikSpriteProps {
    logicName: string;
    className?: string;
    level?: number;
    maxLevel?: number;
    mode?: 'levelup' | 'loop';
    active?: boolean;
    working?: boolean;
}

interface LoopingSpriteProps {
    logicName: string;
    className: string | undefined;
    maxLevel: number;
    working: boolean;
}

const LoopingSprite = ({ logicName, className, maxLevel, working }: LoopingSpriteProps) => {
    const [level, setLevel] = useState(1);
    const [isCycling, setIsCycling] = useState(false);

    useEffect(() => {
        let flashTimer: ReturnType<typeof setTimeout>;
        const interval = setInterval(() => {
            setLevel(prev => (prev >= maxLevel ? 1 : prev + 1));
            setIsCycling(true);
            flashTimer = setTimeout(() => setIsCycling(false), CYCLE_FLASH_DURATION);
        }, CYCLE_INTERVAL);
        return () => {
            clearInterval(interval);
            clearTimeout(flashTimer);
        };
    }, [maxLevel]);

    return (
        <span className={'domik-anim' + (isCycling ? ' is-cycling' : '')}>
            <DomikSprite logicName={logicName} level={level} className={className} working={working} />
        </span>
    );
};

export const AnimatedDomikSprite = ({ logicName, className, level = 1, maxLevel = 5, mode = 'levelup', active = false, working = false }: AnimatedDomikSpriteProps) => {
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const prevLevel = useRef<number | undefined>(undefined);
    const [isLevelup, setIsLevelup] = useState(false);

    useEffect(() => {
        if (mode !== 'levelup') {
            return;
        }
        const prev = prevLevel.current;
        prevLevel.current = level;
        if (prev === undefined || level <= prev || reduce) {
            return;
        }
        setIsLevelup(true);
        const timer = setTimeout(() => setIsLevelup(false), LEVELUP_DURATION);
        return () => clearTimeout(timer);
    }, [mode, level, reduce]);

    if (mode === 'loop') {
        if (!reduce && active) {
            return <LoopingSprite logicName={logicName} className={className} maxLevel={maxLevel} working={working} />;
        }

        return (
            <span className="domik-anim">
                <DomikSprite logicName={logicName} level={reduce ? maxLevel : 1} className={className} working={working} />
            </span>
        );
    }

    return (
        <span className={'domik-anim' + (isLevelup ? ' is-levelup' : '')}>
            <DomikSprite logicName={logicName} level={level} className={className} working={working} />
        </span>
    );
};

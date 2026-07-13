import { useEffect, useRef, useState } from 'react';
import type { ResourceTypeDto } from '../types/api';
import { ResourceSprite } from './sprites';

const GAIN_DURATION = 500;

interface HudResourceProps {
    resourceType: ResourceTypeDto;
    value: number;
}

export const HudResource = ({ resourceType, value }: HudResourceProps) => {
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const prevValue = useRef<number | undefined>(undefined);
    const [isGain, setIsGain] = useState(false);

    useEffect(() => {
        const prev = prevValue.current;
        prevValue.current = value;
        if (prev === undefined || value <= prev || reduce) {
            return;
        }
        setIsGain(true);
        const timer = setTimeout(() => setIsGain(false), GAIN_DURATION);
        return () => clearTimeout(timer);
    }, [value, reduce]);

    return (
        <div className="resource-box hud-resource" title={resourceType.name} aria-label={`${resourceType.name}: ${value}`}>
            <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />
            <span className="hud-resource-name" aria-hidden="true">{resourceType.name}</span>
            <span className={'resource-value' + (isGain ? ' res-gain' : '')}>{value}</span>
        </div>
    );
};

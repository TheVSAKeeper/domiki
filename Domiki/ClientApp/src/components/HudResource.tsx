import { useEffect, useRef, useState } from 'react';
import type { ResourceTypeDto } from '../types/api';

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

    const image = '/images/resourceTypes/' + resourceType.logicName + '.png';
    return (
        <div className="resource-box" title={resourceType.name}>
            <img src={image} alt={resourceType.name} />
            <span className={'resource-value' + (isGain ? ' res-gain' : '')}>{value}</span>
        </div>
    );
};

import type { ResourceTypeDto } from '../types/api';
import { ResourceSprite } from './sprites';

interface ResourceChipProps {
    resourceType: ResourceTypeDto;
    value?: number;
    min?: number;
    max?: number;
    rare?: boolean;
}

export const ResourceChip = ({ resourceType, value, min, max, rare }: ResourceChipProps) => (
    <span className={'resource-chip' + (rare ? ' resource-chip-rare' : '')} title={resourceType.name}>
        <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />
        {min != null && max != null ? `${min}–${max}` : value}
    </span>
);

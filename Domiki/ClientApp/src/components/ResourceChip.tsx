import type { ResourceTypeDto } from '../types/api';

interface ResourceChipProps {
    resourceType: ResourceTypeDto;
    value?: number;
    min?: number;
    max?: number;
    rare?: boolean;
}

export const ResourceChip = ({ resourceType, value, min, max, rare }: ResourceChipProps) => (
    <span className={'resource-chip' + (rare ? ' resource-chip-rare' : '')} title={resourceType.name}>
        <img src={'/images/resourceTypes/' + resourceType.logicName + '.png'} alt={resourceType.name} />
        {min != null && max != null ? `${min}–${max}` : value}
    </span>
);

import type { ResourceTypeDto } from '../types/api';
import { useResourceInfo } from './resourceInfoContext';
import { ResourceSprite } from './sprites';

interface ResourceChipProps {
    resourceType: ResourceTypeDto;
    value?: number;
    min?: number;
    max?: number;
    rare?: boolean;
}

export const ResourceChip = ({ resourceType, value, min, max, rare }: ResourceChipProps) => {
    const info = useResourceInfo();

    return (
        <span className={'resource-chip' + (rare ? ' resource-chip-rare' : '')}
            aria-label={min != null && max != null ? `${resourceType.name}: ${min}–${max}` : value != null ? `${resourceType.name}: ${value}` : resourceType.name}
            title={info == null ? resourceType.name : undefined}
            onMouseEnter={info == null ? undefined : event => { info.open(resourceType.id, event.currentTarget); }}
            onMouseLeave={info?.close}>
            <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />
            {min != null && max != null ? `${min}–${max}` : value}
        </span>
    );
};

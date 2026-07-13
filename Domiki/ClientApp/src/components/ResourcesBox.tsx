import type { ResourceDto, ResourceTypeDto } from '../types/api';
import { ResourceSprite } from './sprites';

interface ResourcesBoxProps {
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
    have?: ResourceDto[];
    showNames?: boolean;
}

export const ResourcesBox = ({ resources, resourceTypes, have, showNames = false }: ResourcesBoxProps) => {
    return (
        <div className="resources">
            {resources.map(res => {
                const resourceType = resourceTypes.find(x => x.id === res.typeId);
                if (resourceType == null) {
                    return null;
                }

                const owned = have?.find(x => x.typeId === res.typeId);
                const lacking = have != null && (owned == null || owned.value < res.value);
                return (
                    <div key={res.typeId} className={'resource-box' + (lacking ? ' resource-lack' : '')} title={resourceType.name}>
                        <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />
                        {showNames && <span className="resource-name">{resourceType.name}</span>}
                        <span className="resource-value">{res.value}</span>
                    </div>
                );
            })}
        </div>
    );
};

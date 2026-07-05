import type { ResourceDto, ResourceTypeDto } from '../types/api';

interface ResourcesBoxProps {
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
    have?: ResourceDto[];
}

export const ResourcesBox = ({ resources, resourceTypes, have }: ResourcesBoxProps) => {
    return (
        <div className="resources">
            {resources.map(res => {
                const resourceType = resourceTypes.find(x => x.id === res.typeId);
                if (resourceType == null) {
                    return null;
                }

                const owned = have?.find(x => x.typeId === res.typeId);
                const lacking = have != null && (owned == null || owned.value < res.value);
                const resImage = '/images/resourceTypes/' + resourceType.logicName + '.png';
                return (
                    <div key={res.typeId} className={'resource-box' + (lacking ? ' resource-lack' : '')} title={resourceType.name}>
                        <img src={resImage} alt={resourceType.name} />
                        <span className="resource-value">{res.value}</span>
                    </div>
                );
            })}
        </div>
    );
};

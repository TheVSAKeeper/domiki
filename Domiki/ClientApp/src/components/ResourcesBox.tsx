import { ResourceDto, ResourceTypeDto } from '../types/api';

interface ResourcesBoxProps {
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
}

export const ResourcesBox = ({ resources, resourceTypes }: ResourcesBoxProps) => {
    return (
        <div className="resources">
            {resources.map(res => {
                const resourceType = resourceTypes.find(x => x.id === res.typeId);
                if (resourceType == null) {
                    return null;
                }

                const resImage = '/images/resourceTypes/' + resourceType.logicName + '.png';
                return (
                    <div key={res.typeId} className="resource-box" title={resourceType.name}>
                        <img src={resImage} alt={resourceType.name} />
                        <span className="resource-value">{res.value}</span>
                    </div>
                );
            })}
        </div>
    );
};

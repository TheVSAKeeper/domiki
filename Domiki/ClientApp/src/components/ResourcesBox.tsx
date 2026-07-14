import type { ResourceDto, ResourceTypeDto } from '../types/api';
import { useResourceInfo } from './ResourceInfo';
import { ResourceSprite } from './sprites';

interface ResourcesBoxProps {
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
    have?: ResourceDto[];
    showNames?: boolean;
}

export const ResourcesBox = ({ resources, resourceTypes, have, showNames = false }: ResourcesBoxProps) => {
    const info = useResourceInfo();

    return (
        <div className="resources">
            {resources.map(res => {
                const resourceType = resourceTypes.find(x => x.id === res.typeId);
                if (resourceType == null) {
                    return null;
                }

                const owned = have?.find(x => x.typeId === res.typeId);
                const ownedValue = owned?.value ?? 0;
                const lacking = have != null && ownedValue < res.value;
                return (
                    <div key={res.typeId} className={'resource-box' + (lacking ? ' resource-lack' : '')}
                        aria-label={lacking ? `${resourceType.name}: ${ownedValue} из ${res.value}` : `${resourceType.name}: ${res.value}`}
                        title={info == null ? resourceType.name : undefined}
                        onMouseEnter={info == null ? undefined : event => { info.open(res.typeId, event.currentTarget); }}
                        onMouseLeave={info?.close}>
                        <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />
                        {showNames && <span className="resource-name">{resourceType.name}</span>}
                        <span className="resource-value">{lacking ? `${ownedValue}/${res.value}` : res.value}</span>
                    </div>
                );
            })}
        </div>
    );
};

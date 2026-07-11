import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import JournalIcon from 'pixelarticons/svg/article.svg?react';
import BackpackIcon from 'pixelarticons/svg/backpack.svg?react';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import BuildingCommunityIcon from 'pixelarticons/svg/building-community.svg?react';
import type { DecorTypeDto, DomikTypeDto, RecapEventDto, ResourceTypeDto } from '../types/api';
import { isNumber, isRecord, readLootEntry, readResource } from '../utils/recap';
import { EXPEDITION_LOOT_KIND_DECOR, EXPEDITION_LOOT_KIND_TRAIT_UPGRADE } from '../utils/game';
import { formatRelativeTime } from '../utils/time';
import { DomikSprite } from './sprites';
import { ResourceChip } from './ResourceChip';

interface JournalBoxProps {
    events: RecapEventDto[];
    resourceTypes: ResourceTypeDto[];
    domikTypes: DomikTypeDto[];
    decorTypes: DecorTypeDto[];
    now: number;
}

const findResourceType = (resourceTypes: ResourceTypeDto[], id: number) => resourceTypes.find(x => x.id === id);

const renderRow = (event: RecapEventDto, resourceTypes: ResourceTypeDto[], domikTypes: DomikTypeDto[], decorTypes: DecorTypeDto[]) => {
    const data = event.data;
    if (!isRecord(data)) {
        return null;
    }

    if (event.type === 'ManufactureFinished' && Array.isArray(data.resources)) {
        const resources = data.resources.flatMap(entry => {
            const parsed = readResource(entry);
            return parsed == null ? [] : [parsed];
        });
        return (
            <>
                <BuildingIcon aria-hidden="true" />
                <span className="journal-text">{isNumber(data.cycles) && data.cycles > 1 ? `Производство ×${data.cycles}` : 'Производство'}</span>
                <span className="journal-chips">
                    {resources.map(resource => {
                        const resourceType = findResourceType(resourceTypes, resource.typeId);
                        return resourceType == null ? null : <ResourceChip key={resource.typeId} resourceType={resourceType} value={resource.value} />;
                    })}
                </span>
            </>
        );
    }

    if (event.type === 'DomikUpgraded' && isNumber(data.domikTypeId) && isNumber(data.level)) {
        const domikType = domikTypes.find(x => x.id === data.domikTypeId);
        return (
            <>
                {domikType != null && <DomikSprite logicName={domikType.logicName} aria-hidden="true" />}
                <span className="journal-text">{(domikType?.name ?? `Постройка #${data.domikTypeId}`) + ` → ур. ${data.level}`}</span>
            </>
        );
    }

    if (event.type === 'ExpeditionReturned' && Array.isArray(data.loot)) {
        const loot = data.loot.flatMap(entry => readLootEntry(entry));
        return (
            <>
                <BackpackIcon aria-hidden="true" />
                <span className="journal-text">Экспедиция вернулась</span>
                <span className="journal-chips">
                    {loot.map((entry, index) => {
                        if (entry.kind === EXPEDITION_LOOT_KIND_DECOR) {
                            const decorType = decorTypes.find(x => x.id === entry.decorTypeId);
                            return <span key={index} className="journal-loot-rare">Нашли {decorType?.name ?? 'декор'}</span>;
                        }
                        if (entry.kind === EXPEDITION_LOOT_KIND_TRAIT_UPGRADE) {
                            return <span key={index} className="journal-loot-rare">{entry.workerName} закалился: {entry.newTrait}</span>;
                        }
                        const resourceType = entry.typeId == null ? null : findResourceType(resourceTypes, entry.typeId);
                        return resourceType == null || entry.value == null ? null : (
                            <span key={index} className={entry.isRare ? 'journal-loot-rare' : undefined}>
                                <ResourceChip resourceType={resourceType} value={entry.value} rare={entry.isRare} />
                            </span>
                        );
                    })}
                </span>
            </>
        );
    }

    if (event.type === 'LotSold') {
        const give = readResource({ resourceTypeId: data.giveResourceTypeId, value: data.giveValue });
        const want = readResource({ resourceTypeId: data.wantResourceTypeId, value: data.wantValue });
        const giveType = give == null ? null : findResourceType(resourceTypes, give.typeId);
        const wantType = want == null ? null : findResourceType(resourceTypes, want.typeId);
        return (
            <>
                <StoreIcon aria-hidden="true" />
                <span className="journal-text">Продано</span>
                <span className="journal-chips">
                    {give != null && giveType != null && <ResourceChip resourceType={giveType} value={give.value} />}
                    <span>→</span>
                    {want != null && wantType != null && <ResourceChip resourceType={wantType} value={want.value} />}
                </span>
            </>
        );
    }

    if (event.type === 'LotExpired') {
        const give = readResource({ resourceTypeId: data.giveResourceTypeId, value: data.giveValue });
        const giveType = give == null ? null : findResourceType(resourceTypes, give.typeId);
        return (
            <>
                <StoreIcon aria-hidden="true" />
                <span className="journal-text">Лот истёк</span>
                <span className="journal-chips">
                    {give != null && giveType != null && <ResourceChip resourceType={giveType} value={give.value} />}
                </span>
            </>
        );
    }

    if (event.type === 'TolokaCompleted' && isNumber(data.tolokaTypeId)) {
        return (
            <>
                <BuildingCommunityIcon aria-hidden="true" />
                <span className="journal-text">Толока завершена</span>
            </>
        );
    }

    return null;
};

export const JournalBox = ({ events, resourceTypes, domikTypes, decorTypes, now }: JournalBoxProps) => {
    return (
        <section className="journal-panel pixel-panel">
            <h3 className="panel-title mech-title"><JournalIcon className="panel-title-ico" aria-hidden="true" />Журнал</h3>
            <div className="journal-list">
                {events.length === 0 &&
                    <span className="hint">Пока событий нет. Стройте, производите, отправляйте экспедиции.</span>
                }
                {events.map((event, index) => {
                    const content = renderRow(event, resourceTypes, domikTypes, decorTypes);
                    if (content == null) {
                        return null;
                    }

                    return (
                        <div key={index} className="journal-row">
                            {content}
                            <span className="journal-time">{formatRelativeTime(event.date, now)}</span>
                        </div>
                    );
                })}
            </div>
        </section>
    );
};

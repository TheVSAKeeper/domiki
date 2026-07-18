import type { FC, ReactNode, SVGProps } from 'react';
import BackpackIcon from 'pixelarticons/svg/backpack.svg?react';
import StoreIcon from 'pixelarticons/svg/store.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import BuildingCommunityIcon from 'pixelarticons/svg/building-community.svg?react';
import GridIcon from 'pixelarticons/svg/grid-3x3.svg?react';
import CheckboxOnIcon from 'pixelarticons/svg/checkbox-on.svg?react';
import BookOpenIcon from 'pixelarticons/svg/book-open.svg?react';
import HandIcon from 'pixelarticons/svg/hand.svg?react';
import type { DecorTypeDto, DomikTypeDto, RecapEventDto, ResourceTypeDto } from '../types/api';
import { isNumber, isRecord, lootEntryKey, readLootEntry, readResource } from '../utils/recap';
import { EXPEDITION_LOOT_KIND_BLUEPRINT, EXPEDITION_LOOT_KIND_DECOR, EXPEDITION_LOOT_KIND_TRAIT_UPGRADE } from '../utils/game';
import { withStableKeys } from '../utils/keys';
import { formatDuration, formatRelativeTime } from '../utils/time';
import { genderForm, traitLabel } from '../utils/gender';
import { guestbookPhraseText } from '../constants/guestbookPhrases';
import { AbstractSprite, DomikSprite, MechanicSprite } from './sprites';
import { ResourceChip } from './ResourceChip';
import { Crest } from './Crest';

interface JournalBoxProps {
    events: RecapEventDto[];
    resourceTypes: ResourceTypeDto[];
    domikTypes: DomikTypeDto[];
    decorTypes: DecorTypeDto[];
    now: number;
}

type SvgIcon = FC<SVGProps<SVGSVGElement>>;

interface EntryContent {
    tone: string;
    Icon: SvgIcon;
    body: ReactNode;
}

const findResourceType = (resourceTypes: ResourceTypeDto[], id: number) => resourceTypes.find(x => x.id === id);

const pluralRu = (n: number, one: string, few: string, many: string) => {
    const mod10 = n % 10;
    const mod100 = n % 100;
    if (mod10 === 1 && mod100 !== 11) {
        return one;
    }
    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) {
        return few;
    }
    return many;
};

const startOfDay = (ms: number) => {
    const date = new Date(ms);
    date.setHours(0, 0, 0, 0);
    return date.getTime();
};

const dayLabel = (dateIso: string, now: number) => {
    const offset = Math.round((startOfDay(now) - startOfDay(Date.parse(dateIso))) / 86400000);
    if (offset <= 0) {
        return 'Сегодня';
    }
    if (offset === 1) {
        return 'Вчера';
    }
    return new Date(dateIso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'long' });
};

const renderContent = (event: RecapEventDto, resourceTypes: ResourceTypeDto[], domikTypes: DomikTypeDto[], decorTypes: DecorTypeDto[]): EntryContent | null => {
    const data = event.data;
    if (!isRecord(data)) {
        return null;
    }

    if (event.type === 'ManufactureFinished' && Array.isArray(data.resources)) {
        const domikType = isNumber(data.domikTypeId) ? domikTypes.find(x => x.id === data.domikTypeId) : undefined;
        const resources = data.resources.flatMap(entry => {
            const parsed = readResource(entry);
            return parsed == null ? [] : [parsed];
        });
        return {
            tone: 'prod',
            Icon: GridIcon,
            body: (
                <>
                    {domikType != null && <DomikSprite logicName={domikType.logicName} aria-hidden="true" />}
                    <span className="journal-text">{isNumber(data.cycles) && data.cycles > 1 ? `Производство ×${data.cycles}` : 'Производство'}</span>
                    <span className="journal-chips">
                        {resources.map(resource => {
                            const resourceType = findResourceType(resourceTypes, resource.typeId);
                            return resourceType == null ? null : <ResourceChip key={resource.typeId} resourceType={resourceType} value={resource.value} />;
                        })}
                    </span>
                </>
            ),
        };
    }

    if (event.type === 'DomikUpgraded' && isNumber(data.domikTypeId) && isNumber(data.level)) {
        const domikType = domikTypes.find(x => x.id === data.domikTypeId);
        return {
            tone: 'build',
            Icon: BuildingIcon,
            body: (
                <>
                    {domikType != null && <DomikSprite logicName={domikType.logicName} aria-hidden="true" />}
                    <span className="journal-text">{(domikType?.name ?? `Постройка #${data.domikTypeId}`) + ` → ур. ${data.level}`}</span>
                </>
            ),
        };
    }

    if (event.type === 'ExpeditionReturned' && Array.isArray(data.loot)) {
        const loot = data.loot.flatMap(entry => readLootEntry(entry));
        return {
            tone: 'exp',
            Icon: BackpackIcon,
            body: (
                <>
                    <MechanicSprite logicName="expeditions" aria-hidden="true" />
                    <span className="journal-text">Экспедиция вернулась</span>
                    <span className="journal-chips">
                        {withStableKeys(loot, lootEntryKey).map(({ key, item: entry }) => {
                            if (entry.kind === EXPEDITION_LOOT_KIND_DECOR) {
                                const decorType = decorTypes.find(x => x.id === entry.decorTypeId);
                                return <span key={key} className="journal-loot-rare">Нашли {decorType?.name ?? 'декор'}</span>;
                            }
                            if (entry.kind === EXPEDITION_LOOT_KIND_TRAIT_UPGRADE) {
                                return <span key={key} className="journal-loot-rare">{entry.workerName} {genderForm(entry.workerGender, 'закалился', 'закалилась')}: {traitLabel(entry.newTraitLogicName ?? '', entry.newTrait ?? '', entry.workerGender)}</span>;
                            }
                            if (entry.kind === EXPEDITION_LOOT_KIND_BLUEPRINT) {
                                return <span key={key} className="journal-loot-rare">Нашли {entry.blueprintName ?? 'чертёж'}</span>;
                            }
                            const resourceType = entry.typeId == null ? null : findResourceType(resourceTypes, entry.typeId);
                            return resourceType == null || entry.value == null ? null : (
                                <span key={key} className={entry.isRare ? 'journal-loot-rare' : undefined}>
                                    <ResourceChip resourceType={resourceType} value={entry.value} rare={entry.isRare} />
                                </span>
                            );
                        })}
                    </span>
                </>
            ),
        };
    }

    if (event.type === 'LotSold') {
        const give = readResource({ resourceTypeId: data.giveResourceTypeId, value: data.giveValue });
        const want = readResource({ resourceTypeId: data.wantResourceTypeId, value: data.wantValue });
        const giveType = give == null ? null : findResourceType(resourceTypes, give.typeId);
        const wantType = want == null ? null : findResourceType(resourceTypes, want.typeId);
        return {
            tone: 'market',
            Icon: StoreIcon,
            body: (
                <>
                    <span className="journal-text">Продано</span>
                    <span className="journal-chips">
                        {give != null && giveType != null && <ResourceChip resourceType={giveType} value={give.value} />}
                        <span>→</span>
                        {want != null && wantType != null && <ResourceChip resourceType={wantType} value={want.value} />}
                    </span>
                </>
            ),
        };
    }

    if (event.type === 'LotExpired') {
        const give = readResource({ resourceTypeId: data.giveResourceTypeId, value: data.giveValue });
        const giveType = give == null ? null : findResourceType(resourceTypes, give.typeId);
        return {
            tone: 'market',
            Icon: StoreIcon,
            body: (
                <>
                    <span className="journal-text">Лот истёк</span>
                    <span className="journal-chips">
                        {give != null && giveType != null && <ResourceChip resourceType={giveType} value={give.value} />}
                    </span>
                </>
            ),
        };
    }

    if (event.type === 'TolokaCompleted' && isNumber(data.tolokaTypeId)) {
        return {
            tone: 'toloka',
            Icon: BuildingCommunityIcon,
            body: <span className="journal-text">Толока завершена</span>,
        };
    }

    if (event.type === 'GuestbookEntryLeft' && typeof data.guestVillageName === 'string' && isNumber(data.guestCrestIcon) && isNumber(data.guestCrestColor) && isNumber(data.phraseId)) {
        return {
            tone: 'guestbook',
            Icon: BookOpenIcon,
            body: (
                <>
                    <Crest icon={data.guestCrestIcon} color={data.guestCrestColor} className="crest-badge-small" />
                    <span className="journal-text">{data.guestVillageName}: расписались в вашей книге гостей</span>
                    <span className="guestbook-entry-phrase">«{guestbookPhraseText(data.phraseId)}»</span>
                </>
            ),
        };
    }

    if (event.type === 'VillageHelped' && typeof data.guestVillageName === 'string' && isNumber(data.guestCrestIcon) && isNumber(data.guestCrestColor) && typeof data.domikTypeName === 'string' && isNumber(data.reducedSeconds)) {
        return {
            tone: 'help',
            Icon: HandIcon,
            body: (
                <>
                    <Crest icon={data.guestCrestIcon} color={data.guestCrestColor} className="crest-badge-small" />
                    <span className="journal-text">{data.guestVillageName} подсобила: {data.domikTypeName} освободится на {formatDuration(data.reducedSeconds)} раньше</span>
                </>
            ),
        };
    }

    if (event.type === 'GoalCompleted' && typeof data.name === 'string' && isNumber(data.rewardCoins)) {
        const coinType = findResourceType(resourceTypes, 1);
        return {
            tone: 'goal',
            Icon: CheckboxOnIcon,
            body: (
                <>
                    <span className="journal-text">Наказ выполнен: {data.name}</span>
                    {coinType != null && <ResourceChip resourceType={coinType} value={data.rewardCoins} />}
                </>
            ),
        };
    }

    return null;
};

export const JournalBox = ({ events, resourceTypes, domikTypes, decorTypes, now }: JournalBoxProps) => {
    const entries = withStableKeys(
        [...events]
            .sort((a, b) => Date.parse(b.date) - Date.parse(a.date))
            .flatMap(event => {
                const content = renderContent(event, resourceTypes, domikTypes, decorTypes);
                return content == null ? [] : [{ event, content }];
            }),
        entry => `${entry.event.type}-${entry.event.date}`,
    );

    const groups: { key: string; label: string; items: typeof entries }[] = [];
    entries.forEach(entry => {
        const label = dayLabel(entry.item.event.date, now);
        const last = groups[groups.length - 1];
        if (last != null && last.label === label) {
            last.items.push(entry);
        } else {
            groups.push({ key: entry.key, label, items: [entry] });
        }
    });

    return (
        <section className="journal-panel pixel-panel">
            <header className="journal-hero">
                <span className="journal-hero-emblem" aria-hidden="true"><AbstractSprite logicName="journal" size={40} /></span>
                <div className="journal-hero-text">
                    <h3 className="journal-hero-title panel-title">Журнал</h3>
                    <p className="journal-hero-sub">Летопись двора: что ни день – то новое дело.</p>
                </div>
                {entries.length > 0 &&
                    <span className="journal-hero-stat">
                        <b>{entries.length}</b>
                        <small>{pluralRu(entries.length, 'запись', 'записи', 'записей')}</small>
                    </span>
                }
            </header>

            {entries.length === 0
                ? (
                    <div className="journal-empty">
                        <AbstractSprite logicName="journal" size={48} className="journal-empty-ico" aria-hidden="true" />
                        <p className="journal-empty-title">Летопись пока пуста</p>
                        <p className="journal-empty-hint">Стройте, производите, шлите экспедиции – двор начнёт вести дневник сам.</p>
                    </div>
                )
                : (
                    <div className="journal-timeline">
                        {groups.map(group => (
                            <div key={group.key} className="journal-group">
                                <div className="journal-day"><span className="journal-day-label">{group.label}</span></div>
                                {group.items.map(entry => {
                                    const { tone, Icon, body } = entry.item.content;
                                    return (
                                        <article key={entry.key} className="journal-entry" data-tone={tone}>
                                            <span className="journal-node" aria-hidden="true"><Icon /></span>
                                            <div className="journal-card">
                                                {body}
                                                <time className="journal-time">{formatRelativeTime(entry.item.event.date, now)}</time>
                                            </div>
                                        </article>
                                    );
                                })}
                            </div>
                        ))}
                    </div>
                )
            }
        </section>
    );
};

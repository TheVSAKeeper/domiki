import { useEffect, useMemo, useState } from 'react';
import type { FC, ReactNode, SVGProps } from 'react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import type { DecorTypeDto, DomikTypeDto, JournalDigestEntryDto, NeighborReputationDto, RecapEventDto, ResourceTypeDto, VillageRunDto } from '../types/api';
import { getJournalPage } from '../services/api';
import { isNumber, isRecord, lootEntryKey, readLootEntry, readResource } from '../utils/recap';
import { EXPEDITION_LOOT_KIND_BLUEPRINT, EXPEDITION_LOOT_KIND_DECOR, EXPEDITION_LOOT_KIND_TRAIT_UPGRADE } from '../utils/game';
import { getErrandTemplate, getErrandThanks } from '../utils/errandTexts';
import { getIncidentTemplate, incidentText } from '../utils/incidentTexts';
import { domikIncidentText, getDomikIncidentTemplate } from '../utils/domikIncidentTexts';
import { getWorkerMilestoneTemplate, workerMilestoneText } from '../utils/workerMilestoneTexts';
import { getWorkerMealText } from '../utils/tavernMealTexts';
import { pickGiftText } from '../utils/giftTexts';
import { withStableKeys } from '../utils/keys';
import { formatDuration, formatRelativeTime } from '../utils/time';
import { genderForm, traitLabel } from '../utils/gender';
import { guestbookPhraseText } from '../constants/guestbookPhrases';
import { AbstractSprite, DomikSprite, MechanicSprite, ResourceSprite } from './sprites';
import { ResourceChip } from './ResourceChip';
import { Crest } from './Crest';
import '../styles/journal.css';

const JOURNAL_GROUPS = [
    { key: 'None', label: 'Всё' },
    { key: 'Household', label: 'Хозяйство' },
    { key: 'Workers', label: 'Трудяги' },
    { key: 'Village', label: 'Деревня' },
    { key: 'Guests', label: 'Гости' },
    { key: 'Market', label: 'Ярмарка' },
] as const;

const PAGE_SIZE = 30;

const groupLabel = (key: string) => JOURNAL_GROUPS.find(x => x.key === key)?.label ?? key;

const villageOf = (runs: VillageRunDto[], date: string): string | null => {
    const at = Date.parse(date);
    const run = runs.find(x => at >= Date.parse(x.startDate) && (x.endDate == null || at < Date.parse(x.endDate)));
    if (run == null) {
        return null;
    }

    return run.villageName ?? 'Деревня без имени';
};

interface JournalBoxProps {
    events: RecapEventDto[];
    resourceTypes: ResourceTypeDto[];
    domikTypes: DomikTypeDto[];
    decorTypes: DecorTypeDto[];
    neighbors: NeighborReputationDto[];
    now: number;
}

type SvgIcon = FC<SVGProps<SVGSVGElement>>;

const mechanicIcon = (logicName: string): SvgIcon => props => <MechanicSprite logicName={logicName} {...props} />;
const abstractIcon = (logicName: string): SvgIcon => props => <AbstractSprite logicName={logicName} {...props} />;
const domikIcon = (logicName: string): SvgIcon => props => <DomikSprite logicName={logicName} {...props} />;

interface EntryContent {
    tone: string;
    Icon: SvgIcon;
    body: ReactNode;
    fallback?: boolean;
}

const unrecognized: EntryContent = {
    tone: 'neutral',
    Icon: abstractIcon('journal'),
    fallback: true,
    body: <span className="journal-text">Запись не распознана этой версией игры</span>,
};

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

const renderContent = (event: RecapEventDto, resourceTypes: ResourceTypeDto[], domikTypes: DomikTypeDto[], decorTypes: DecorTypeDto[], neighbors: NeighborReputationDto[]): EntryContent => {
    const data = event.data;
    if (!isRecord(data)) {
        return unrecognized;
    }

    if (event.type === 'ManufactureFinished' && Array.isArray(data.resources)) {
        const domikType = isNumber(data.domikTypeId) ? domikTypes.find(x => x.id === data.domikTypeId) : undefined;
        const resources = data.resources.flatMap(entry => {
            const parsed = readResource(entry);
            return parsed == null ? [] : [parsed];
        });
        return {
            tone: 'prod',
            Icon: abstractIcon('production_recipe'),
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

    if (event.type === 'ManufactureRepeatFailed' && isNumber(data.domikTypeId) && typeof data.reason === 'string') {
        const domikType = domikTypes.find(x => x.id === data.domikTypeId);
        return {
            tone: 'prod',
            Icon: abstractIcon('production_recipe'),
            body: (
                <>
                    {domikType != null && <DomikSprite logicName={domikType.logicName} aria-hidden="true" />}
                    <span className="journal-text">
                        {isNumber(data.count) && data.count > 1 ? `Наряд заглох ×${data.count}: ` : 'Наряд заглох: '}
                        {data.reason}
                    </span>
                </>
            ),
        };
    }

    if (event.type === 'ManufactureMeasureMet' && isNumber(data.domikTypeId) && isNumber(data.resourceTypeId) && isNumber(data.value)) {
        const domikType = domikTypes.find(x => x.id === data.domikTypeId);
        const resourceType = findResourceType(resourceTypes, data.resourceTypeId);
        return {
            tone: 'prod',
            Icon: abstractIcon('production_recipe'),
            body: (
                <>
                    {domikType != null && <DomikSprite logicName={domikType.logicName} aria-hidden="true" />}
                    <span className="journal-text">
                        Наряд отстоял меру: {resourceType?.name ?? 'припас'} дошёл до {data.value} – наряд снят.
                    </span>
                </>
            ),
        };
    }

    if (event.type === 'ManufactureReserveHeld' && isNumber(data.domikTypeId) && isNumber(data.resourceTypeId)) {
        const domikType = domikTypes.find(x => x.id === data.domikTypeId);
        const resourceType = findResourceType(resourceTypes, data.resourceTypeId);
        return {
            tone: 'prod',
            Icon: abstractIcon('production_recipe'),
            body: (
                <>
                    {domikType != null && <DomikSprite logicName={domikType.logicName} aria-hidden="true" />}
                    <span className="journal-text">Наряд встал: {resourceType?.name ?? 'припас'} под заповедью.</span>
                </>
            ),
        };
    }

    if (event.type === 'ManufactureGoldCapReached' && isNumber(data.domikTypeId) && isNumber(data.mined) && isNumber(data.cap)) {
        const domikType = domikTypes.find(x => x.id === data.domikTypeId);
        return {
            tone: 'prod',
            Icon: abstractIcon('production_recipe'),
            body: (
                <>
                    {domikType != null && <DomikSprite logicName={domikType.logicName} aria-hidden="true" />}
                    <span className="journal-text">Жила на сегодня выбрана: намыто {data.mined} из {data.cap} – наряд снят.</span>
                </>
            ),
        };
    }

    if (event.type === 'DomikUpgraded' && isNumber(data.domikTypeId) && isNumber(data.level)) {
        const domikType = domikTypes.find(x => x.id === data.domikTypeId);
        return {
            tone: 'build',
            Icon: domikType != null ? domikIcon(domikType.logicName) : BuildingIcon,
            body: <span className="journal-text">{(domikType?.name ?? `Постройка #${data.domikTypeId}`) + ` → ур. ${data.level}`}</span>,
        };
    }

    if (event.type === 'Relocated' && isNumber(data.workers) && isNumber(data.blueprints) && isNumber(data.knots)) {
        return {
            tone: 'build',
            Icon: abstractIcon('prestige_new_valley'),
            body: (
                <span className="journal-text">
                    Собрались затемно, к полудню были на месте. Двор пустой, зато все свои: {data.workers} {pluralRu(data.workers, 'трудяга', 'трудяги', 'трудяг')},
                    {' '}{data.blueprints} {pluralRu(data.blueprints, 'чертёж', 'чертежа', 'чертежей')} и {data.knots} {pluralRu(data.knots, 'узелок', 'узелка', 'узелков')} на память.
                </span>
            ),
        };
    }

    if (event.type === 'CloakWornOut') {
        return {
            tone: 'errand',
            Icon: mechanicIcon('expeditions'),
            body: <><ResourceSprite logicName="cloak" aria-hidden="true" /><span className="journal-text">Плащ отслужил пятьдесят смен и рассыпался</span></>,
        };
    }

    if (event.type === 'ExpeditionReturned' && Array.isArray(data.loot)) {
        const loot = data.loot.flatMap(entry => readLootEntry(entry));
        return {
            tone: 'exp',
            Icon: mechanicIcon('expeditions'),
            body: (
                <>
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
            Icon: mechanicIcon('market'),
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
            Icon: mechanicIcon('market'),
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
            Icon: mechanicIcon('toloka'),
            body: <span className="journal-text">Толока завершена</span>,
        };
    }

    if (event.type === 'GuestbookEntryLeft' && typeof data.guestVillageName === 'string' && isNumber(data.guestCrestIcon) && isNumber(data.guestCrestColor) && isNumber(data.phraseId)) {
        return {
            tone: 'guestbook',
            Icon: mechanicIcon('guestbook'),
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
            Icon: mechanicIcon('village_help'),
            body: (
                <>
                    <Crest icon={data.guestCrestIcon} color={data.guestCrestColor} className="crest-badge-small" />
                    <span className="journal-text">{data.guestVillageName} подсобила: {data.domikTypeName} освободится на {formatDuration(data.reducedSeconds)} раньше</span>
                </>
            ),
        };
    }

    if (event.type === 'ErrandResolved' && isNumber(data.neighborId) && isNumber(data.templateId) && isNumber(data.clueId) && isNumber(data.coins) && isNumber(data.reputation)) {
        const template = getErrandTemplate(data.templateId);
        const resolution = template.resolutions[data.clueId] ?? '';
        const thanks = getErrandThanks(data.neighborId);
        const coinType = findResourceType(resourceTypes, 1);
        const bonusType = isNumber(data.bonusResourceTypeId) ? findResourceType(resourceTypes, data.bonusResourceTypeId) : undefined;
        return {
            tone: 'errand',
            Icon: mechanicIcon('errands'),
            body: (
                <>
                    <MechanicSprite logicName="orders" aria-hidden="true" />
                    <span className="journal-text">
                        {resolution}
                        <span className="journal-errand-thanks">«{thanks}»</span>
                    </span>
                    <span className="journal-chips">
                        {coinType != null && <ResourceChip resourceType={coinType} value={data.coins} />}
                        <span className="reputation-reward">
                            <AbstractSprite logicName="reputation" size={24} className="reputation-ico" aria-hidden="true" />
                            +{data.reputation} реп.
                        </span>
                        {bonusType != null && isNumber(data.bonusValue) && <ResourceChip resourceType={bonusType} value={data.bonusValue} rare />}
                    </span>
                </>
            ),
        };
    }

    if (event.type === 'WorkerMissing' && typeof data.workerName === 'string' && isNumber(data.workerGender) && isNumber(data.templateId)) {
        return { tone: 'errand', Icon: abstractIcon('incident'), body: <><MechanicSprite logicName="orders" aria-hidden="true" /><span className="journal-text">{data.workerName} {genderForm(data.workerGender, 'задержался', 'задержалась')} в походе – есть зацепки<span className="journal-errand-thanks">«{getIncidentTemplate(data.templateId).title}»</span></span></> };
    }

    if (event.type === 'IncidentResolved' && typeof data.workerName === 'string' && isNumber(data.workerGender) && isNumber(data.templateId) && typeof data.autoReturned === 'boolean') {
        const template = getIncidentTemplate(data.templateId);
        if (data.autoReturned) {
            return { tone: 'errand', Icon: abstractIcon('incident'), body: <><MechanicSprite logicName="orders" aria-hidden="true" /><span className="journal-text">{incidentText('{имя} вернул{ся|ась} сам{|а} – дорогу на{шёл|шла} без подмоги.', data.workerName, data.workerGender)}</span></> };
        }
        if (!isNumber(data.clueId)) {
            return unrecognized;
        }
        const resourceType = isNumber(data.resourceTypeId) ? findResourceType(resourceTypes, data.resourceTypeId) : undefined;
        return { tone: 'errand', Icon: abstractIcon('incident'), body: <><MechanicSprite logicName="orders" aria-hidden="true" /><span className="journal-text">{incidentText(template.resolutions[data.clueId] ?? '', data.workerName, data.workerGender)}<span className="journal-errand-thanks"><i>{incidentText(template.epilogue, data.workerName, data.workerGender)}</i></span></span><span className="journal-chips">{resourceType != null && isNumber(data.value) && <ResourceChip resourceType={resourceType} value={data.value} />}{data.traitUpgraded === true && typeof data.newTrait === 'string' && <span className="journal-loot-rare">Черта: {traitLabel(typeof data.newTraitLogicName === 'string' ? data.newTraitLogicName : '', data.newTrait, data.workerGender)}</span>}</span></> };
    }

    if (event.type === 'DomikIncidentStarted' && isNumber(data.domikTypeId) && isNumber(data.templateId)) {
        const domikName = domikTypes.find(type => type.id === data.domikTypeId)?.name ?? `Постройке #${data.domikTypeId}`;
        return { tone: 'errand', Icon: abstractIcon('incident'), body: <><MechanicSprite logicName="orders" aria-hidden="true" /><span className="journal-text">В {domikName} что-то неспокойно – есть зацепки<span className="journal-errand-thanks">«{getDomikIncidentTemplate(data.templateId).title}»</span></span></> };
    }

    if (event.type === 'DomikIncidentResolved' && isNumber(data.domikTypeId) && isNumber(data.templateId) && typeof data.autoResolved === 'boolean') {
        const domikName = domikTypes.find(type => type.id === data.domikTypeId)?.name ?? `Постройке #${data.domikTypeId}`;
        if (data.autoResolved) {
            return { tone: 'errand', Icon: abstractIcon('incident'), body: <><MechanicSprite logicName="orders" aria-hidden="true" /><span className="journal-text">Загадка в {domikName} разгадалась сама</span></> };
        }
        if (!isNumber(data.clueId) || typeof data.heroWorkerName !== 'string') {
            return unrecognized;
        }
        const template = getDomikIncidentTemplate(data.templateId);
        const heroGender = isNumber(data.heroWorkerGender) ? data.heroWorkerGender : undefined;
        const resourceType = isNumber(data.resourceTypeId) ? findResourceType(resourceTypes, data.resourceTypeId) : undefined;
        const upgradedWorkerName = typeof data.upgradedWorkerName === 'string' ? data.upgradedWorkerName : 'Трудяга';
        return { tone: 'errand', Icon: abstractIcon('incident'), body: <><MechanicSprite logicName="orders" aria-hidden="true" /><span className="journal-text">{domikIncidentText(template.resolutions[data.clueId] ?? '', domikName, data.heroWorkerName, heroGender)}<span className="journal-errand-thanks"><i>{domikIncidentText(template.epilogue, domikName, data.heroWorkerName, heroGender)}</i></span></span><span className="journal-chips">{resourceType != null && isNumber(data.value) && <ResourceChip resourceType={resourceType} value={data.value} />}{data.traitUpgraded === true && typeof data.newTrait === 'string' && <span className="journal-loot-rare">Черта: {upgradedWorkerName} – {traitLabel(typeof data.newTraitLogicName === 'string' ? data.newTraitLogicName : '', data.newTrait, heroGender)}</span>}</span></> };
    }

    if (event.type === 'WorkerMilestone' && isNumber(data.milestoneType) && isNumber(data.workerId) && typeof data.workerName === 'string' && isNumber(data.workerGender)) {
        const template = getWorkerMilestoneTemplate(data.milestoneType);
        const partnerName = typeof data.workerName2 === 'string' ? data.workerName2 : undefined;
        const partnerGender = isNumber(data.workerGender2) ? data.workerGender2 : undefined;
        const resourceType = isNumber(data.resourceTypeId) ? findResourceType(resourceTypes, data.resourceTypeId) : undefined;
        return { tone: 'goal', Icon: abstractIcon('worker_milestone'), body: <><span className="journal-text">{workerMilestoneText(template.journal, data.workerName, data.workerGender, partnerName, partnerGender)}<span className="journal-errand-thanks"><i>{workerMilestoneText(template.epilogue, data.workerName, data.workerGender, partnerName, partnerGender)}</i></span></span><span className="journal-chips">{resourceType != null && isNumber(data.value) && <ResourceChip resourceType={resourceType} value={data.value} />}{data.traitUpgraded === true && typeof data.newTrait === 'string' && <span className="journal-loot-rare">Черта: {data.workerName} – {traitLabel(typeof data.newTraitLogicName === 'string' ? data.newTraitLogicName : '', data.newTrait, data.workerGender)}</span>}</span></> };
    }

    if (event.type === 'WorkerMeal' && isNumber(data.count) && isNumber(data.variant) && Array.isArray(data.resources)) {
        const workerName = typeof data.workerName === 'string' ? data.workerName : null;
        const workerGender = isNumber(data.workerGender) ? data.workerGender : null;
        const reason = typeof data.reason === 'string' ? data.reason : null;
        const resources = data.resources.flatMap(entry => {
            const parsed = readResource(entry);
            return parsed == null ? [] : [parsed];
        });
        const foodResourceType = resources[0] == null ? undefined : findResourceType(resourceTypes, resources[0].typeId);
        const foodName = foodResourceType == null ? null : foodResourceType.name.toLocaleLowerCase('ru-RU');
        const text = getWorkerMealText({ count: data.count, workerName, workerGender, variant: data.variant, reason }, foodName);
        return {
            tone: 'errand',
            Icon: mechanicIcon('tavern'),
            body: (
                <>
                    <span className="journal-text">{text}</span>
                    {resources.length > 0 &&
                        <span className="journal-chips">
                            {resources.map(resource => {
                                const resourceType = findResourceType(resourceTypes, resource.typeId);
                                return resourceType == null ? null : <ResourceChip key={resource.typeId} resourceType={resourceType} value={resource.value} />;
                            })}
                        </span>
                    }
                </>
            ),
        };
    }

    if (event.type === 'NeighborGift' && isNumber(data.neighborId) && Array.isArray(data.resources) && isNumber(data.visitIndex) && typeof data.big === 'boolean') {
        const neighbor = neighbors.find(x => x.neighborId === data.neighborId);
        const neighborName = neighbor?.neighborName ?? `Сосед #${data.neighborId}`;
        const resources = data.resources.flatMap(entry => {
            const parsed = readResource(entry);
            return parsed == null ? [] : [parsed];
        });
        const decorTypeId = data.decorTypeId;
        const decorName = isNumber(decorTypeId) ? decorTypes.find(x => x.id === decorTypeId)?.name ?? `Декор #${decorTypeId}` : 'Декор не указан';
        return {
            tone: 'gift',
            Icon: mechanicIcon('gifts'),
            body: (
                <>
                    <span className="journal-text">
                        {neighborName}
                        <span className="journal-errand-thanks">{pickGiftText(data.neighborId, data.big, event.date)}</span>
                    </span>
                    {data.big
                        ? <span className="journal-loot-rare">Нашли {decorName}</span>
                        : (
                            <span className="journal-chips">
                                {resources.map(resource => {
                                    const resourceType = findResourceType(resourceTypes, resource.typeId);
                                    return resourceType == null ? null : <ResourceChip key={resource.typeId} resourceType={resourceType} value={resource.value} />;
                                })}
                            </span>
                        )
                    }
                </>
            ),
        };
    }

    if (event.type === 'GoalCompleted' && typeof data.name === 'string' && isNumber(data.rewardCoins)) {
        const coinType = findResourceType(resourceTypes, 1);
        return {
            tone: 'goal',
            Icon: abstractIcon('goal'),
            body: (
                <>
                    <span className="journal-text">Наказ выполнен: {data.name}</span>
                    {coinType != null && <ResourceChip resourceType={coinType} value={data.rewardCoins} />}
                </>
            ),
        };
    }

    return unrecognized;
};

export const JournalBox = ({ events, resourceTypes, domikTypes, decorTypes, neighbors, now }: JournalBoxProps) => {
    const [group, setGroup] = useState<string>('None');
    const [loaded, setLoaded] = useState<RecapEventDto[] | null>(null);
    const [villageRuns, setVillageRuns] = useState<VillageRunDto[]>([]);
    const [totalCount, setTotalCount] = useState<number | null>(null);
    const [digest, setDigest] = useState<JournalDigestEntryDto[]>([]);
    const [exhausted, setExhausted] = useState(false);
    const [loading, setLoading] = useState(true);
    const [failed, setFailed] = useState(false);

    // Фильтр «Всё» показывает первую страницу из снимка состояния, чтобы не ходить на сервер за уже полученным.
    const shown = useMemo(
        () => (group === 'None' ? [...events, ...(loaded ?? [])] : (loaded ?? [])),
        [group, events, loaded],
    );

    useEffect(() => {
        const controller = new AbortController();
        const wantsEvents = group !== 'None';
        getJournalPage(0, group, wantsEvents ? PAGE_SIZE : 1, controller.signal)
            .then(page => {
                setVillageRuns(page.villageRuns);
                setTotalCount(page.totalCount);
                setDigest(page.digest);
                if (wantsEvents) {
                    setLoaded(page.events);
                    setExhausted(page.events.length < PAGE_SIZE);
                }
            })
            .catch(() => {
                if (!controller.signal.aborted) {
                    setFailed(true);
                }
            })
            .finally(() => {
                if (!controller.signal.aborted) {
                    setLoading(false);
                }
            });
        return () => controller.abort();
    }, [group]);

    const selectGroup = (key: string) => {
        if (key === group) {
            return;
        }

        setGroup(key);
        setLoaded(null);
        setExhausted(false);
        setFailed(false);
        setLoading(true);
    };

    const loadMore = () => {
        const oldest = shown[shown.length - 1];
        if (oldest == null || loading) {
            return;
        }

        setLoading(true);
        setFailed(false);
        getJournalPage(oldest.id, group, PAGE_SIZE)
            .then(page => {
                setLoaded(previous => [...(previous ?? []), ...page.events]);
                setTotalCount(page.totalCount);
                setExhausted(page.events.length < PAGE_SIZE);
            })
            .catch(() => setFailed(true))
            .finally(() => setLoading(false));
    };

    const entries = withStableKeys(
        [...shown]
            .sort((a, b) => Date.parse(b.date) - Date.parse(a.date))
            .map(event => ({ event, content: renderContent(event, resourceTypes, domikTypes, decorTypes, neighbors) })),
        entry => `${entry.event.type}-${entry.event.date}-${entry.event.id}`,
    );

    const groups: { key: string; label: string; village: string | null; items: typeof entries }[] = [];
    entries.forEach(entry => {
        const label = dayLabel(entry.item.event.date, now);
        const village = villageOf(villageRuns, entry.item.event.date);
        const last = groups[groups.length - 1];
        if (last != null && last.label === label && last.village === village) {
            last.items.push(entry);
        } else {
            groups.push({ key: entry.key, label, village, items: [entry] });
        }
    });

    // Курсор берётся из последней показанной записи, поэтому у старого снимка без идентификаторов листать нечего.
    const oldestShown = shown[shown.length - 1];
    const canLoadMore = !exhausted && oldestShown != null && oldestShown.id > 0;

    return (
        <section className="journal-panel pixel-panel">
            <header className="journal-hero">
                <span className="journal-hero-emblem" aria-hidden="true"><AbstractSprite logicName="journal" size={40} /></span>
                <div className="journal-hero-text">
                    <h3 className="journal-hero-title panel-title">Журнал</h3>
                    <p className="journal-hero-sub">Летопись двора: что ни день – то новое дело.</p>
                </div>
                {(totalCount ?? entries.length) > 0 &&
                    <span className="journal-hero-stat">
                        <b>{totalCount ?? entries.length}</b>
                        <small>{pluralRu(totalCount ?? entries.length, 'запись', 'записи', 'записей')}</small>
                    </span>
                }
            </header>

            {digest.length > 0 &&
                <div className="journal-digest">
                    <span className="journal-digest-title">За неделю</span>
                    {digest.map(item => (
                        <span key={item.group} className="journal-digest-item">
                            {groupLabel(item.group)}
                            <b>{item.count}</b>
                        </span>
                    ))}
                </div>
            }

            <div className="journal-filters" role="group" aria-label="Отбор записей журнала">
                {JOURNAL_GROUPS.map(item => (
                    <button
                        key={item.key}
                        type="button"
                        className="journal-filter"
                        aria-pressed={group === item.key}
                        onClick={() => selectGroup(item.key)}
                    >
                        {item.label}
                    </button>
                ))}
            </div>

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
                        {groups.map((group, index) => (
                            <div key={group.key} className="journal-group">
                                {group.village !== groups[index - 1]?.village &&
                                    <div className="journal-village"><span className="journal-village-label">{group.village ?? 'Прежняя деревня'}</span></div>
                                }
                                <div className="journal-day"><span className="journal-day-label">{group.label}</span></div>
                                {group.items.map(entry => {
                                    const { tone, Icon, body, fallback } = entry.item.content;
                                    return (
                                        <article key={entry.key} className="journal-entry" data-tone={tone} data-fallback={fallback === true ? '' : undefined}>
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

            {failed &&
                <p className="journal-more-note">Не удалось получить летопись. Проверьте связь.</p>
            }

            {canLoadMore &&
                <button type="button" className="btn-game journal-more" onClick={loadMore} disabled={loading}>
                    {loading ? 'Читаем летопись…' : 'Показать раньше'}
                </button>
            }

            {exhausted && entries.length > 0 &&
                <p className="journal-more-note">Летопись хранит записи за последние 30 дней – это всё.</p>
            }
        </section>
    );
};

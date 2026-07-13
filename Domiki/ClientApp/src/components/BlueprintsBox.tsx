import CheckIcon from 'pixelarticons/svg/check.svg?react';
import FlagIcon from 'pixelarticons/svg/flag.svg?react';
import type { BlueprintDto, DecorTypeDto, DomikTypeDto, NeighborReputationDto } from '../types/api';
import { DecorSprite, DomikSprite, MechanicSprite, NeighborSprite } from './sprites';

interface BlueprintsBoxProps {
    blueprints: BlueprintDto[];
    domikTypes: DomikTypeDto[];
    decorTypes: DecorTypeDto[];
    reputations: NeighborReputationDto[];
}

interface Milestone {
    key: string;
    kind: 'blueprint' | 'decor';
    name: string;
    neighborId: number;
    neighborName: string;
    neighborLogicName: string;
    threshold: number;
    current: number;
    owned: boolean;
    building?: DomikTypeDto;
    decorLogicName?: string;
}

const neighborKeepsake: Record<string, string> = {
    glinischi: 'В Глинищах руки в глине, а сердце нараспашку – заветным поделятся.',
    kamenka: 'Каменский народ кремень: что пообещали, то и отсыплют своим.',
    zarechye: 'С того берега весточка: заслужишь доверие – переправят гостинец.',
    borovoe: 'В Боровом смолой да дружбой пахнет – заветное берегут для своих.',
    dubrava: 'Дубравские добро помнят годами: уважишь – зачтётся сторицей.',
};

const keepsakeFor = (logicName: string) => neighborKeepsake[logicName] ?? 'Соседи приберегли доброе – для доброго имени.';

const kindLabel = (kind: Milestone['kind']) => (kind === 'blueprint' ? 'чертёж стройки' : 'убранство двора');

export const BlueprintsBox = ({ blueprints, domikTypes, decorTypes, reputations }: BlueprintsBoxProps) => {
    if (blueprints.length === 0 && decorTypes.every(x => x.neighborId == null)) {
        return null;
    }

    const logicFor = (neighborId: number) => reputations.find(x => x.neighborId === neighborId)?.neighborLogicName ?? 'generic';

    const milestones: Milestone[] = [
        ...blueprints.map((blueprint): Milestone => ({
            key: `blueprint-${blueprint.id}`,
            kind: 'blueprint',
            name: blueprint.name,
            neighborId: blueprint.neighborId,
            neighborName: blueprint.neighborName,
            neighborLogicName: logicFor(blueprint.neighborId),
            threshold: blueprint.reputationThreshold,
            current: blueprint.currentReputation,
            owned: blueprint.owned,
            building: domikTypes.find(type => type.id === blueprint.domikTypeId),
        })),
        ...decorTypes.filter(x => x.neighborId != null).map((decorType): Milestone => {
            const points = reputations.find(x => x.neighborId === decorType.neighborId)?.points ?? 0;
            return {
                key: `decor-${decorType.id}`,
                kind: 'decor',
                name: decorType.name,
                neighborId: decorType.neighborId ?? 0,
                neighborName: decorType.neighborName ?? '',
                neighborLogicName: logicFor(decorType.neighborId ?? 0),
                threshold: decorType.reputationThreshold,
                current: points,
                owned: points >= decorType.reputationThreshold,
                decorLogicName: decorType.logicName,
            };
        }),
    ].sort((a, b) => Number(a.owned) - Number(b.owned) || (a.threshold - a.current) - (b.threshold - b.current));

    const opened = milestones.filter(x => x.owned).length;

    return (
        <section className="blueprints-panel pixel-panel">
            <div className="bp-hero">
                <div className="bp-hero-emblem">
                    <MechanicSprite logicName="blueprints" size={40} aria-hidden="true" />
                </div>
                <div className="bp-hero-text">
                    <h3 className="panel-title bp-hero-title">Вехи соседей</h3>
                    <p className="bp-hero-sub">Растёт доброе имя – и на каждой вехе сосед делится заветным: чертежом стройки да убранством для двора.</p>
                </div>
                <div className="bp-hero-stat" title="Открыто вех">
                    <span className="bp-hero-stat-num">{opened}<span className="bp-hero-stat-of">/{milestones.length}</span></span>
                    <span className="bp-hero-stat-label">вех открыто</span>
                </div>
            </div>
            <div className="bp-grid">
                {milestones.map(milestone => {
                    const progress = Math.min(milestone.current, milestone.threshold);
                    const remaining = Math.max(0, milestone.threshold - milestone.current);
                    const percent = milestone.threshold > 0 ? Math.round((progress / milestone.threshold) * 100) : 0;
                    const tone = (['a', 'b', 'c', 'd'] as const)[milestone.neighborId % 4] ?? 'a';
                    return (
                        <div key={milestone.key} className={'veha-card' + (milestone.owned ? ' veha-card--owned' : '')}>
                            <div className={'veha-postmark veha-postmark--' + (milestone.owned ? 'owned' : tone)}>
                                <span className="veha-neighbor-badge">
                                    <NeighborSprite logicName={milestone.neighborLogicName} size={24} className="neighbor-ico" aria-hidden="true" />
                                </span>
                                <span className="veha-neighbor-name">
                                    {milestone.neighborName}
                                    <span className="veha-neighbor-cue">{milestone.owned ? 'поделился' : 'приберёг'}</span>
                                </span>
                            </div>
                            <div className="veha-gift">
                                <span className="veha-gift-sprite">
                                    {milestone.building != null
                                        ? <DomikSprite logicName={milestone.building.logicName} />
                                        : milestone.decorLogicName != null
                                            ? <DecorSprite logicName={milestone.decorLogicName} size={64} />
                                            : <MechanicSprite logicName="blueprints" size={48} aria-hidden="true" />}
                                </span>
                                <span className="veha-gift-text">
                                    <span className="veha-gift-name">{milestone.name}</span>
                                    <span className="veha-gift-kind">{kindLabel(milestone.kind)}</span>
                                </span>
                            </div>
                            <p className="veha-plea">{keepsakeFor(milestone.neighborLogicName)}</p>
                            {milestone.owned
                                ? <div className="veha-open"><CheckIcon aria-hidden="true" />Веха взята – заветное открыто</div>
                                : <div className="veha-milestone">
                                    <div className="veha-track" aria-label={`${progress} из ${milestone.threshold} доброго имени`}>
                                        <span className="veha-track-fill" style={{ width: `${percent}%` }} />
                                        <FlagIcon className="veha-flag" aria-hidden="true" />
                                    </div>
                                    <span className="veha-goal">
                                        {progress} / {milestone.threshold} <span className="veha-cue">до вехи{remaining > 0 ? ` · ещё ${remaining}` : ''}</span>
                                    </span>
                                </div>}
                        </div>
                    );
                })}
            </div>
        </section>
    );
};

import { useState } from 'react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import type { ConvoyDto, DomikTypeDto, ErrandDto, NeighborReputationDto, OrderDto, ResourceDto, ResourceTypeDto, VillageDto, VillageLevelDto, VillageProfileDto, WorkerDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { getErrandTemplate } from '../utils/errandTexts';
import { getVillageProfileState, VILLAGE_PROFILE_LEVEL_REQUIREMENT, VILLAGE_PROFILE_REPUTATION_REQUIREMENT } from '../utils/villageProfile';
import { neighborPrepositionalName, profileGenitiveName } from '../utils/profileLore';
import { reputationTier, reputationTierAhead } from '../utils/reputationTiers';
import { SectionHero } from './SectionHero';
import { ResourcesBox } from './ResourcesBox';
import { ActionButton } from './ActionButton';
import { ConvoyTally } from './ConvoyTally';
import { ErrandAcceptModal } from './ErrandAcceptModal';
import { VillageProfileConfirmModal } from './VillageProfileConfirmModal';
import { TermTip } from './TermTip';
import { AbstractSprite, MechanicSprite, NeighborSprite, ResourceSprite, WorkerSprite } from './sprites';

interface OrdersBoxProps {
    orders: OrderDto[];
    errand: ErrandDto | null;
    workers: WorkerDto[];
    reputation: NeighborReputationDto[];
    convoys: ConvoyDto[];
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    domikTypes: DomikTypeDto[];
    villageProfiles: VillageProfileDto[];
    village: VillageDto | null;
    villageLevel: VillageLevelDto | null;
    now: number;
    onComplete: (orderId: number) => void;
    onCancel: (orderId: number) => void;
    onAcceptErrand: (errandId: number, clueId: number, workerIds: number[]) => Promise<boolean>;
    onCancelErrand: (errandId: number) => Promise<boolean>;
    onBuyFromConvoy: (neighborId: number, resourceTypeId: number, count: number) => Promise<boolean>;
    onSetFriend: (neighborId: number | null) => Promise<boolean>;
    onSetVillageProfile: (neighborId: number) => Promise<boolean>;
}

const neighborPlea: Record<string, string> = {
    glinischi: 'В Глинищах руки по локоть в глине – подсобишь, по-соседски не забудем.',
    kamenka: 'Каменский народ кремень, да и нам порой нужна подмога. Выручи, друг.',
    zarechye: 'С того берега кланяемся – подсобишь, и гостинец переправим.',
    borovoe: 'В Боровом смолой да дружбой пахнет – сделаешь, в долгу не останемся.',
    dubrava: 'Дубравские добро помнят годами. Уважишь – зачтётся сторицей.',
};

const pleaFor = (logicName: string) => neighborPlea[logicName] ?? 'Соседи будут рады подмоге – и добром отплатят.';

const FRIEND_HINT = 'Выбери, с кем деревня нынче водит дружбу: заказы этого соседа будут появляться на доске чаще. Дружить можно с одним, передумать – в любой день.';

const PROFILE_HINT = 'Выбери, чей уклад переймёт деревня: две постройки его ремесла станут работать быстрее на 15 %. Уклад один на деревню, сменить – не раньше чем через 7 суток.';

const URGENT_SECONDS = 3600;

const rewardResources = (order: OrderDto): ResourceDto[] => [
    { typeId: 1, value: order.rewardCoins },
    { typeId: 5, value: order.rewardGold },
].filter(resource => resource.value > 0);

interface ErrandCardProps {
    errand: ErrandDto;
    workers: WorkerDto[];
    now: number;
    onAccept: () => void;
    onCancel: () => void;
}

const ErrandCard = ({ errand, workers, now, onAccept, onCancel }: ErrandCardProps) => {
    const template = getErrandTemplate(errand.templateId);
    const tone = (['a', 'b', 'c', 'd'] as const)[errand.neighborId % 4] ?? 'a';
    const accepted = errand.acceptDate != null;
    const deadline = accepted ? errand.finishDate : errand.expireDate;
    const left = deadline == null ? 0 : remainingSeconds(deadline, now);
    const crew = accepted ? workers.filter(worker => errand.workerIds.includes(worker.id)) : [];
    const clue = errand.clueId == null ? null : template.clues[errand.clueId];

    return (
        <div className="order-card errand-card">
            <div className={'order-postmark order-postmark-' + tone}>
                <span className="order-neighbor-badge">
                    <NeighborSprite logicName={errand.neighborLogicName} size={24} className="neighbor-ico" aria-hidden="true" />
                </span>
                <span className="order-neighbor-name">
                    {errand.neighborName}<span className="order-asks">просит</span>
                </span>
                <span className={'order-timer timer' + (left <= URGENT_SECONDS ? ' order-timer-urgent' : '')}>
                    <ClockIcon aria-hidden="true" />{formatDuration(left)}
                </span>
            </div>
            <span className={'errand-badge' + (accepted ? ' errand-badge-accepted' : '')}>{accepted ? 'Идут поиски' : 'Поручение'}</span>
            <h4 className="errand-title">{template.title}</h4>
            {accepted
                ? <>
                    {clue != null && <p className="errand-clue-chip">{clue.label}</p>}
                    {crew.length > 0 &&
                        <div className="errand-crew">
                            {crew.map(worker => (
                                <span key={worker.id} className="errand-crew-member">
                                    <WorkerSprite name={worker.name} state="working" className="worker-avatar" aria-hidden="true" />
                                    {worker.name}
                                </span>
                            ))}
                        </div>
                    }
                    <ActionButton className="btn-game btn-ghost" onClick={onCancel}>Отозвать</ActionButton>
                </>
                : <>
                    <p className="order-plea errand-offer-text">{template.offer}</p>
                    <div className="errand-actions">
                        <ActionButton className="btn-game icon-chip-btn" onClick={onAccept}>
                            <MechanicSprite logicName="errands" size={24} className="btn-ico" aria-hidden="true" />
                            Помочь
                        </ActionButton>
                        <ActionButton className="btn-game btn-ghost" onClick={onCancel}>Отказаться</ActionButton>
                    </div>
                </>
            }
        </div>
    );
};

export const OrdersBox = ({ orders, errand, workers, reputation, convoys, resourceTypes, resources, domikTypes, villageProfiles, village, villageLevel, now, onComplete, onCancel, onAcceptErrand, onCancelErrand, onBuyFromConvoy, onSetFriend, onSetVillageProfile }: OrdersBoxProps) => {
    const [errandModalId, setErrandModalId] = useState<number | null>(null);
    const [profileModalNeighborId, setProfileModalNeighborId] = useState<number | null>(null);
    const profileModalNeighbor = profileModalNeighborId == null ? null : reputation.find(item => item.neighborId === profileModalNeighborId) ?? null;
    const profileModalBuildings = profileModalNeighbor == null ? [] : getVillageProfileState(villageProfiles, domikTypes, profileModalNeighbor, villageLevel?.level ?? 0, village?.profileNeighborId ?? null, village?.profileChangeAvailableDate ?? null, now).buildings;
    return (
        <section className="orders-panel pixel-panel">
            <SectionHero className="orders-hero">
                <div className="orders-hero-emblem sec-hero-emblem">
                    <MechanicSprite logicName="orders" size={40} aria-hidden="true" />
                </div>
                <div className="orders-hero-text">
                    <h3 className="panel-title orders-hero-title">Заказы от соседей</h3>
                    <p className="orders-hero-sub sec-hero-sub">Из окрестных выселок шлют весточки – сделайте, что просят, и заслужите доброе имя.</p>
                </div>
                <div className="orders-hero-stat sec-hero-stat" title="Весточек на столе">
                    <span className="orders-hero-stat-num sec-hero-stat-num">{orders.length}</span>
                    <span className="orders-hero-stat-label sec-hero-stat-label">весточек на столе</span>
                </div>
            </SectionHero>
            {reputation.length > 0 &&
                <div className="standing-board">
                    <div className="standing-board-head">
                        <h4 className="standing-board-title"><MechanicSprite logicName="friendship" size={24} aria-hidden="true" />Доброе имя по выселкам</h4>
                        <p className="standing-board-hint">Крепко завязан узелок у соседа, с которым деревня водит дружбу – его заказы приходят чаще. Дружить можно с одним, передумать – в любой день.</p>
                    </div>
                    <div className="standing-list">
                        {reputation.map(item => {
                            const next = item.nextThreshold;
                            const tier = reputationTier(item.points);
                            const fillPercent = next != null ? Math.min(100, Math.round((item.points / next) * 100)) : 100;
                            const title = !item.isOpen
                                ? 'Дорога ещё не открыта'
                                : next != null
                                    ? `До вехи ${next}: ещё ${next - item.points}${item.nextRewardName != null ? ` – ${item.nextRewardName}` : ''}`
                                    : tier.gloss;
                            const genitiveName = profileGenitiveName[item.neighborLogicName] ?? item.neighborName;
                            const profile = getVillageProfileState(villageProfiles, domikTypes, item, villageLevel?.level ?? 0, village?.profileNeighborId ?? null, village?.profileChangeAvailableDate ?? null, now);
                            const profileCooldownLeft = village?.profileChangeAvailableDate == null ? 0 : remainingSeconds(village.profileChangeAvailableDate, now);
                            return (
                                <div key={item.neighborId}
                                    className={'standing-badge'
                                        + (next == null ? ' standing-badge-honored' : '')
                                        + (item.isFriend ? ' standing-badge-friend' : '')
                                        + (!item.isOpen ? ' standing-badge-locked' : '')}
                                    title={title}>
                                    <NeighborSprite logicName={item.neighborLogicName} size={24} className="neighbor-ico" aria-hidden="true" />
                                    <div className="standing-badge-body">
                                        <span className="standing-badge-name">{item.neighborName}</span>
                                        {item.isOpen &&
                                            <TermTip term="reputation_tier" gloss={tier.gloss} className="standing-badge-tier">
                                                {`в ${neighborPrepositionalName[item.neighborLogicName] ?? item.neighborName} ты ${tier.name}`}
                                            </TermTip>}
                                        <div className="standing-track" aria-hidden="true">
                                            <span className="standing-track-fill" style={{ width: `${fillPercent}%` }} />
                                        </div>
                                        {item.isOpen &&
                                            <span className="standing-badge-goal">
                                                {next != null ? <>{item.points}/{next}</> : <>{item.points}</>}
                                            </span>}
                                        {item.isOpen
                                            ? next != null && item.nextRewardName != null &&
                                                <span className="standing-badge-cue" title={item.nextRewardName}>{item.nextRewardName}</span>
                                            : <span className="standing-badge-cue">дорога ещё не открыта</span>}
                                    </div>
                                    <div className="standing-marks">
                                        {item.isOpen
                                            ? <ActionButton className={'standing-friend-mark' + (item.isFriend ? ' standing-friend-mark-on' : '')}
                                                aria-pressed={item.isFriend}
                                                aria-label={item.isFriend ? `Дружим с выселком ${item.neighborName} – перестать` : `Водить дружбу с выселком ${item.neighborName}`}
                                                title={FRIEND_HINT}
                                                onClick={async () => void await onSetFriend(item.isFriend ? null : item.neighborId)}>
                                                <MechanicSprite logicName="friendship" size={24} aria-hidden="true" />
                                            </ActionButton>
                                            : <span className="standing-friend-mark standing-friend-mark-locked" aria-hidden="true"><LockIcon /></span>}
                                        {profile.state === 'need-level' || profile.state === 'need-reputation' || profile.state === 'closed'
                                            ? <span className="standing-friend-mark standing-friend-mark-locked"
                                                title={profile.state === 'need-reputation'
                                                    ? `Доброе имя ещё не доросло до уклада – ${item.points}/${VILLAGE_PROFILE_REPUTATION_REQUIREMENT}`
                                                    : profile.state === 'need-level'
                                                        ? `Мала ещё обжитость для уклада – ${villageLevel?.level ?? 0}/${VILLAGE_PROFILE_LEVEL_REQUIREMENT}`
                                                        : !item.isOpen ? 'Дорога ещё не открыта' : 'У этого соседа нет своего уклада'}
                                                aria-label={profile.state === 'need-reputation'
                                                    ? `Уклад ${genitiveName} закрыт: доброе имя ${item.points} из ${VILLAGE_PROFILE_REPUTATION_REQUIREMENT}`
                                                    : profile.state === 'need-level'
                                                        ? `Уклад ${genitiveName} закрыт: обжитость ${villageLevel?.level ?? 0} из ${VILLAGE_PROFILE_LEVEL_REQUIREMENT}`
                                                        : undefined}>
                                                <HomeIcon aria-hidden="true" />
                                            </span>
                                            : profile.state === 'adopted'
                                                ? <span className="standing-friend-mark standing-friend-mark-on"
                                                    aria-label={`Деревня живёт по укладу ${genitiveName}`}
                                                    title="живём по этому укладу">
                                                    <HomeIcon aria-hidden="true" />
                                                </span>
                                                : profile.state === 'cooldown'
                                                    ? <span className="standing-friend-mark standing-friend-mark-locked"
                                                        aria-label={`Сменить уклад можно через ${formatDuration(profileCooldownLeft)}`}
                                                        title={`Уклад меняли недавно – новый через ${formatDuration(profileCooldownLeft)}`}>
                                                        <HomeIcon aria-hidden="true" />
                                                    </span>
                                                    : <ActionButton className="standing-friend-mark"
                                                        aria-pressed={false}
                                                        aria-label={`Перенять уклад ${genitiveName}`}
                                                        title={PROFILE_HINT}
                                                        onClick={() => setProfileModalNeighborId(item.neighborId)}>
                                                        <HomeIcon aria-hidden="true" />
                                                    </ActionButton>}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </div>}
            {convoys.length > 0 &&
                <div className="convoy-board">
                    <div className="convoy-board-head">
                        <h4 className="convoy-board-title"><MechanicSprite logicName="convoy" size={24} aria-hidden="true" />Обозы</h4>
                        <p className="convoy-board-hint">Сосед пригоняет обоз со своим товаром. Уступит немного и не задёшево – зато сразу и без хлопот; новый обоз придёт через сутки после первой покупки.</p>
                    </div>
                    <div className="convoy-list">
                        {convoys.map(convoy => {
                            const left = convoy.windowResetDate == null ? 0 : remainingSeconds(convoy.windowResetDate, now);
                            return (
                                <div key={convoy.neighborId} className={'convoy-row' + (convoy.isLocked ? ' convoy-row-locked' : '')}>
                                    <span className="convoy-row-neighbor">
                                        <NeighborSprite logicName={convoy.neighborLogicName} size={24} className="neighbor-ico" aria-hidden="true" />
                                        {convoy.neighborName}
                                    </span>
                                    {convoy.isLocked
                                        ? <span className="convoy-row-locked-hint">
                                            <LockIcon aria-hidden="true" />
                                            Мало доверия для обоза – выручайте соседа заказами
                                        </span>
                                        : <>
                                            <div className="convoy-items">
                                                {convoy.items.map(item => {
                                                    const resourceType = resourceTypes.find(x => x.id === item.resourceTypeId);
                                                    if (resourceType == null) {
                                                        return null;
                                                    }

                                                    const soldOut = convoy.remaining <= 0;
                                                    return (
                                                        <ActionButton key={item.resourceTypeId} className="convoy-item-chip" disabled={soldOut}
                                                            title={soldOut ? 'Обоз на сегодня распродан' : `Купить ${resourceType.name} за ${item.price}`}
                                                            onClick={async () => void await onBuyFromConvoy(convoy.neighborId, item.resourceTypeId, 1)}>
                                                            <ResourceSprite logicName={resourceType.logicName} aria-hidden="true" />
                                                            <span className="convoy-item-price">
                                                                <ResourceSprite logicName="coin" aria-hidden="true" />{item.price}
                                                            </span>
                                                        </ActionButton>
                                                    );
                                                })}
                                            </div>
                                            <span className="convoy-row-status">
                                                <ConvoyTally remaining={convoy.remaining} limit={convoy.limit} />
                                                {convoy.remaining <= 0 && (
                                                    <span className="convoy-row-reset">
                                                        <ClockIcon aria-hidden="true" />{formatDuration(left)}
                                                    </span>
                                                )}
                                            </span>
                                        </>}
                                </div>
                            );
                        })}
                    </div>
                </div>}
            {errand != null &&
                <ErrandCard errand={errand} workers={workers} now={now}
                    onAccept={() => setErrandModalId(errand.id)}
                    onCancel={() => { void onCancelErrand(errand.id); }} />}
            {orders.length === 0
                ? <div className="orders-empty">
                    <MechanicSprite logicName="orders" size={48} aria-hidden="true" />
                    <p className="orders-empty-title">На столе пусто</p>
                    <p className="orders-empty-hint">Соседям пока нечего просить – загляните позже, весточки приходят сами.</p>
                </div>
                : <div className="orders-grid">
                    {orders.map(order => {
                        const canComplete = hasResourcesFor(order.required.map(x => ({ typeId: x.resourceTypeId, value: x.value })), resources);
                        const left = remainingSeconds(order.expireDate, now);
                        const tone = (['a', 'b', 'c', 'd'] as const)[order.neighborId % 4] ?? 'a';
                        const tierAhead = reputationTierAhead(reputation.find(item => item.neighborId === order.neighborId)?.points ?? 0, order.rewardReputation);
                        return (
                            <div key={order.id} className="order-card">
                                <div className={'order-postmark order-postmark-' + tone}>
                                    <span className="order-neighbor-badge">
                                        <NeighborSprite logicName={order.neighborLogicName} size={24} className="neighbor-ico" aria-hidden="true" />
                                    </span>
                                    <span className="order-neighbor-name">
                                        {order.neighborName}<span className="order-asks">просит</span>
                                    </span>
                                    <span className={'order-timer timer' + (left <= URGENT_SECONDS ? ' order-timer-urgent' : '')}>
                                        <ClockIcon aria-hidden="true" />{formatDuration(left)}
                                    </span>
                                </div>
                                <p className="order-plea">{pleaFor(order.neighborLogicName)}</p>
                                <div className="order-ask">
                                    <span className="panel-label">нужно</span>
                                    <ResourcesBox resources={order.required.map(x => ({ typeId: x.resourceTypeId, value: x.value }))}
                                        resourceTypes={resourceTypes} have={resources} />
                                </div>
                                <div className="order-reward">
                                    <span className="panel-label">в благодарность</span>
                                    <div className="order-reward-row">
                                        <ResourcesBox resources={rewardResources(order)} resourceTypes={resourceTypes} />
                                        <span className="reputation-reward">
                                            <AbstractSprite logicName="reputation" size={24} className="reputation-ico" aria-hidden="true" />
                                            +{order.rewardReputation} к имени
                                        </span>
                                    </div>
                                    {tierAhead != null &&
                                        <p className="order-reward-tier">
                                            и в {neighborPrepositionalName[order.neighborLogicName] ?? order.neighborName} ты {tierAhead.name}
                                        </p>}
                                </div>
                                <div className="order-actions">
                                    <ActionButton className="btn-game" disabled={!canComplete}
                                        title={canComplete ? undefined : 'Не хватает ресурсов'}
                                        onClick={() => onComplete(order.id)}>
                                        Сдать заказ
                                    </ActionButton>
                                    <ActionButton className="btn-game btn-ghost"
                                        title="Заказ уйдёт в другую деревню – без обиды, но и без награды. Новый спрос появится не сразу."
                                        onClick={() => onCancel(order.id)}>
                                        Уступить
                                    </ActionButton>
                                </div>
                            </div>
                        );
                    })}
                </div>}
            {errand != null && errandModalId === errand.id && errand.acceptDate == null &&
                <ErrandAcceptModal errand={errand} workers={workers} now={now}
                    neighborPoints={reputation.find(item => item.neighborId === errand.neighborId)?.points ?? 0}
                    onConfirm={onAcceptErrand}
                    onClose={() => setErrandModalId(null)} />}
            {profileModalNeighborId != null && profileModalNeighbor != null &&
                <VillageProfileConfirmModal
                    genitiveName={profileGenitiveName[profileModalNeighbor.neighborLogicName] ?? profileModalNeighbor.neighborName}
                    buildings={profileModalBuildings}
                    onConfirm={() => onSetVillageProfile(profileModalNeighborId)}
                    onClose={() => setProfileModalNeighborId(null)} />}
        </section>
    );
};

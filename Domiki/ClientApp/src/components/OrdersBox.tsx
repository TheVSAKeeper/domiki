import { useState } from 'react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import type { ConvoyDto, ErrandDto, NeighborReputationDto, OrderDto, ResourceDto, ResourceTypeDto, WorkerDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { getErrandTemplate } from '../utils/errandTexts';
import { ResourcesBox } from './ResourcesBox';
import { ActionButton } from './ActionButton';
import { ConvoyTally } from './ConvoyTally';
import { ErrandAcceptModal } from './ErrandAcceptModal';
import { AbstractSprite, MechanicSprite, NeighborSprite, ResourceSprite, WorkerSprite } from './sprites';

interface OrdersBoxProps {
    orders: OrderDto[];
    errand: ErrandDto | null;
    workers: WorkerDto[];
    reputation: NeighborReputationDto[];
    convoys: ConvoyDto[];
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    now: number;
    onComplete: (orderId: number) => void;
    onCancel: (orderId: number) => void;
    onAcceptErrand: (errandId: number, clueId: number, workerIds: number[]) => Promise<boolean>;
    onCancelErrand: (errandId: number) => Promise<boolean>;
    onBuyFromConvoy: (neighborId: number, resourceTypeId: number, count: number) => Promise<boolean>;
    onSetFriend: (neighborId: number | null) => Promise<boolean>;
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
                        <ActionButton className="btn-game" onClick={onAccept}>
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

export const OrdersBox = ({ orders, errand, workers, reputation, convoys, resourceTypes, resources, now, onComplete, onCancel, onAcceptErrand, onCancelErrand, onBuyFromConvoy, onSetFriend }: OrdersBoxProps) => {
    const [errandModalId, setErrandModalId] = useState<number | null>(null);
    return (
        <section className="orders-panel pixel-panel">
            <div className="orders-hero">
                <div className="orders-hero-emblem">
                    <MechanicSprite logicName="orders" size={40} aria-hidden="true" />
                </div>
                <div className="orders-hero-text">
                    <h3 className="panel-title orders-hero-title">Заказы от соседей</h3>
                    <p className="orders-hero-sub">Из окрестных выселок шлют весточки – сделайте, что просят, и заслужите доброе имя.</p>
                </div>
                <div className="orders-hero-stat" title="Весточек на столе">
                    <span className="orders-hero-stat-num">{orders.length}</span>
                    <span className="orders-hero-stat-label">весточек на столе</span>
                </div>
            </div>
            {reputation.length > 0 &&
                <div className="standing-board">
                    <div className="standing-board-head">
                        <h4 className="standing-board-title"><MechanicSprite logicName="friendship" size={24} aria-hidden="true" />Доброе имя по выселкам</h4>
                        <p className="standing-board-hint">Крепко завязан узелок у соседа, с которым деревня водит дружбу – его заказы приходят чаще. Дружить можно с одним, передумать – в любой день.</p>
                    </div>
                    <div className="standing-list">
                        {reputation.map(item => {
                            const next = item.nextThreshold;
                            const fillPercent = next != null ? Math.min(100, Math.round((item.points / next) * 100)) : 100;
                            const title = !item.isOpen
                                ? 'Дорога ещё не открыта'
                                : next != null
                                    ? `До вехи ${next}: ещё ${next - item.points}${item.nextRewardName != null ? ` – ${item.nextRewardName}` : ''}`
                                    : 'Доброе имя в почёте';
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
                                        <div className="standing-track" aria-hidden="true">
                                            <span className="standing-track-fill" style={{ width: `${fillPercent}%` }} />
                                        </div>
                                        {item.isOpen &&
                                            <span className="standing-badge-goal">
                                                {next != null ? <>{item.points}/{next}</> : <>{item.points} · в почёте</>}
                                            </span>}
                                        {item.isOpen
                                            ? next != null && item.nextRewardName != null &&
                                                <span className="standing-badge-cue" title={item.nextRewardName}>{item.nextRewardName}</span>
                                            : <span className="standing-badge-cue">дорога ещё не открыта</span>}
                                    </div>
                                    {item.isOpen
                                        ? <ActionButton className={'standing-friend-mark' + (item.isFriend ? ' standing-friend-mark-on' : '')}
                                            aria-pressed={item.isFriend}
                                            aria-label={item.isFriend ? `Дружим с выселком ${item.neighborName} – перестать` : `Водить дружбу с выселком ${item.neighborName}`}
                                            title={FRIEND_HINT}
                                            onClick={async () => void await onSetFriend(item.isFriend ? null : item.neighborId)}>
                                            <MechanicSprite logicName="friendship" size={24} aria-hidden="true" />
                                        </ActionButton>
                                        : <span className="standing-friend-mark standing-friend-mark-locked" aria-hidden="true"><LockIcon /></span>}
                                </div>
                            );
                        })}
                    </div>
                </div>}
            {convoys.length > 0 &&
                <div className="convoy-board">
                    <div className="convoy-board-head">
                        <h4 className="convoy-board-title"><MechanicSprite logicName="convoy" size={24} aria-hidden="true" />Обозы</h4>
                        <p className="convoy-board-hint">Раз в день сосед пригоняет обоз со своим товаром. Уступит немного и не задёшево – зато сразу и без хлопот.</p>
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
                                            +{order.rewardReputation} реп.
                                        </span>
                                    </div>
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
                    onConfirm={onAcceptErrand}
                    onClose={() => setErrandModalId(null)} />}
        </section>
    );
};

import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import HeartIcon from 'pixelarticons/svg/heart.svg?react';
import type { NeighborReputationDto, OrderDto, ResourceDto, ResourceTypeDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { ResourcesBox } from './ResourcesBox';
import { ActionButton } from './ActionButton';
import { AbstractSprite, MechanicSprite, NeighborSprite } from './sprites';

interface OrdersBoxProps {
    orders: OrderDto[];
    reputation: NeighborReputationDto[];
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    now: number;
    onComplete: (orderId: number) => void;
}

const neighborPlea: Record<string, string> = {
    glinischi: 'В Глинищах руки по локоть в глине – подсобишь, по-соседски не забудем.',
    kamenka: 'Каменский народ кремень, да и нам порой нужна подмога. Выручи, друг.',
    zarechye: 'С того берега кланяемся – подсобишь, и гостинец переправим.',
    borovoe: 'В Боровом смолой да дружбой пахнет – сделаешь, в долгу не останемся.',
    dubrava: 'Дубравские добро помнят годами. Уважишь – зачтётся сторицей.',
};

const pleaFor = (logicName: string) => neighborPlea[logicName] ?? 'Соседи будут рады подмоге – и добром отплатят.';

const reputationMilestones = [10, 25, 50];

const nextReputationMilestone = (points: number) => reputationMilestones.find(value => value > points);

const previousReputationMilestone = (points: number) =>
    [...reputationMilestones].reverse().find(value => value <= points) ?? 0;

const URGENT_SECONDS = 3600;

const rewardResources = (order: OrderDto): ResourceDto[] => [
    { typeId: 1, value: order.rewardCoins },
    { typeId: 5, value: order.rewardGold },
].filter(resource => resource.value > 0);

export const OrdersBox = ({ orders, reputation, resourceTypes, resources, now, onComplete }: OrdersBoxProps) => {
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
                    <span className="standing-board-label"><HeartIcon aria-hidden="true" />доброе имя<br />по выселкам</span>
                    <div className="standing-list">
                        {reputation.map(item => {
                            const next = nextReputationMilestone(item.points);
                            const floor = previousReputationMilestone(item.points);
                            const span = next != null ? next - floor : 1;
                            return (
                                <div key={item.neighborId} className={'standing-badge' + (next == null ? ' standing-badge-honored' : '')}
                                    title={next != null ? `До вехи ${next}: ещё ${next - item.points}` : 'Доброе имя в почёте'}>
                                    <NeighborSprite logicName={item.neighborLogicName} size={24} className="neighbor-ico" aria-hidden="true" />
                                    <div className="standing-badge-body">
                                        <span className="standing-badge-name">{item.neighborName}</span>
                                        <div className="standing-track" aria-hidden="true">
                                            <span className="standing-track-fill" style={{ width: `${Math.round(((item.points - floor) / span) * 100)}%` }} />
                                        </div>
                                        <span className="standing-badge-goal">
                                            {next != null ? <>{item.points} / {next} <span className="standing-badge-cue">до вехи</span></> : <>{item.points} · в почёте</>}
                                        </span>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </div>}
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
                                <ActionButton className="btn-game" disabled={!canComplete}
                                    title={canComplete ? undefined : 'Не хватает ресурсов'}
                                    onClick={() => onComplete(order.id)}>
                                    Сдать заказ
                                </ActionButton>
                            </div>
                        );
                    })}
                </div>}
        </section>
    );
};

import type { NeighborReputationDto, OrderDto, ResourceDto, ResourceTypeDto } from '../types/api';
import { hasResourcesFor } from '../utils/game';
import { formatDuration, remainingSeconds } from '../utils/time';
import { ResourcesBox } from './ResourcesBox';
import { AbstractSprite, MechanicSprite, NeighborSprite } from './sprites';

interface OrdersBoxProps {
    orders: OrderDto[];
    reputation: NeighborReputationDto[];
    resourceTypes: ResourceTypeDto[];
    resources: ResourceDto[];
    now: number;
    onComplete: (orderId: number) => void;
}

const reputationMilestones = [10, 25, 50];

const nextReputationMilestone = (points: number) => reputationMilestones.find(value => value > points);

export const OrdersBox = ({ orders, reputation, resourceTypes, resources, now, onComplete }: OrdersBoxProps) => {
    const rewardResources = (order: OrderDto): ResourceDto[] => [
        { typeId: 1, value: order.rewardCoins },
        { typeId: 5, value: order.rewardGold },
    ].filter(resource => resource.value > 0);

    return (
        <section className="orders-panel pixel-panel">
            <div className="orders-head">
                <h3 className="panel-title mech-title"><MechanicSprite logicName="orders" size={24} className="panel-title-ico" aria-hidden="true" />Заказы</h3>
                <div className="reputation-list">
                    {reputation.map(item => (
                        <span key={item.neighborId} className="reputation-chip">
                            <NeighborSprite logicName={item.neighborLogicName} size={24} className="neighbor-ico" aria-hidden="true" />
                            {item.neighborName}: {item.points}
                            {nextReputationMilestone(item.points) ? <> / {nextReputationMilestone(item.points)} ???</> : null}
                        </span>
                    ))}
                </div>
            </div>
            <div className="orders-grid">
                {orders.map(order => {
                    const canComplete = hasResourcesFor(order.required.map(x => ({ typeId: x.resourceTypeId, value: x.value })), resources);
                    return (
                        <div key={order.id} className="order-card">
                            <div className="order-topline">
                                <span className="order-neighbor">
                                    <NeighborSprite logicName={order.neighborLogicName} size={24} className="neighbor-ico" aria-hidden="true" />
                                    {order.neighborName}
                                </span>
                                <span className="timer">{formatDuration(remainingSeconds(order.expireDate, now))}</span>
                            </div>
                            <div className="order-row">
                                <span className="panel-label">Нужно</span>
                                <ResourcesBox resources={order.required.map(x => ({ typeId: x.resourceTypeId, value: x.value }))}
                                    resourceTypes={resourceTypes} have={resources} />
                            </div>
                            <div className="order-row">
                                <span className="panel-label">Даёт</span>
                                <div className="order-reward">
                                    <ResourcesBox resources={rewardResources(order)} resourceTypes={resourceTypes} />
                                    <span className="reputation-reward">
                                        <AbstractSprite logicName="reputation" size={24} className="reputation-ico" aria-hidden="true" />
                                        +{order.rewardReputation} реп.
                                    </span>
                                </div>
                            </div>
                            <button className="btn-game" disabled={!canComplete}
                                title={canComplete ? undefined : 'Не хватает ресурсов'}
                                onClick={() => onComplete(order.id)}>
                                Сдать
                            </button>
                        </div>
                    );
                })}
            </div>
        </section>
    );
};

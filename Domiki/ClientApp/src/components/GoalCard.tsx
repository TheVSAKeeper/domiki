import FlagIcon from 'pixelarticons/svg/flag.svg?react';
import type { GoalsStateDto, ResourceTypeDto } from '../types/api';
import { zealMultiplier } from '../utils/game';
import { ProgressBar } from './ProgressBar';
import { ResourceChip } from './ResourceChip';
import { StatChip } from './StatChip';
import { AbstractSprite } from './sprites';

interface GoalCardProps {
    goals: GoalsStateDto | null;
    resourceTypes: ResourceTypeDto[];
}

const shiftWord = (n: number): string => {
    const mod10 = n % 10;
    const mod100 = n % 100;
    if (mod10 === 1 && mod100 !== 11) return 'смена';
    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return 'смены';
    return 'смен';
};

const ZealChip = ({ charges }: { charges: number }) => {
    const multiplier = zealMultiplier(charges);
    return (
        <StatChip icon={<AbstractSprite logicName="untouched_deposits" size={24} className="stat-chip-ico" aria-hidden="true" />} title="Ускорение коротких производств">
            <span className="zeal-chip">
                <span className="zeal-chip-label">Нетронутые залежи</span>
                <span className="zeal-chip-count">{charges} {shiftWord(charges)}</span>
                <span className="zeal-chip-mult">×{multiplier}</span>
            </span>
        </StatChip>
    );
};

export const GoalCard = ({ goals, resourceTypes }: GoalCardProps) => {
    if (goals == null || (goals.active == null && goals.zealCharges <= 0)) {
        return null;
    }

    if (goals.active == null) {
        return <div className="goal-zeal-only"><ZealChip charges={goals.zealCharges} /></div>;
    }

    const coinType = resourceTypes.find(resourceType => resourceType.id === 1);
    const progress = Math.min(goals.completedCount + 1, goals.totalCount);

    return (
        <section className="goal-card pixel-panel">
            <div className="goal-hero">
                <div className="goal-emblem">
                    <FlagIcon className="goal-emblem-ico" aria-hidden="true" />
                </div>
                <div className="goal-hero-text">
                    <h3 className="goal-title">Наказ старосты</h3>
                    <p className="goal-quest">{goals.active.name}</p>
                </div>
                <div className="goal-stat" title="Наказ по счёту">
                    <span className="goal-stat-num">{progress}</span>
                    <span className="goal-stat-label">из {goals.totalCount}</span>
                </div>
            </div>
            <ProgressBar value={progress} max={goals.totalCount} label={`${progress} из ${goals.totalCount}`} />
            <div className="goal-card-footer">
                <span className="goal-reward-label">Награда старосты</span>
                <div className="goal-reward-chips">
                    {coinType != null && <ResourceChip resourceType={coinType} value={goals.active.rewardCoins} />}
                    {goals.zealCharges > 0 && <ZealChip charges={goals.zealCharges} />}
                </div>
            </div>
        </section>
    );
};

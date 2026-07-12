import FlagIcon from 'pixelarticons/svg/flag.svg?react';
import ZapIcon from 'pixelarticons/svg/zap.svg?react';
import type { GoalsStateDto, ResourceTypeDto } from '../types/api';
import { zealMultiplier } from '../utils/game';
import { ProgressBar } from './ProgressBar';
import { ResourceChip } from './ResourceChip';
import { StatChip } from './StatChip';

interface GoalCardProps {
    goals: GoalsStateDto | null;
    resourceTypes: ResourceTypeDto[];
}

const ZealChip = ({ charges }: { charges: number }) => {
    const multiplier = zealMultiplier(charges);
    return (
        <StatChip icon={<ZapIcon className="stat-chip-ico" aria-hidden="true" />} title="Ускорение коротких производств">
            Нетронутые залежи: {charges} быстрых смен (×{multiplier})
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
            <div className="goal-card-head">
                <h3 className="panel-title mech-title"><FlagIcon className="panel-title-ico" aria-hidden="true" />Наказ старосты</h3>
                <span className="goal-progress">{progress} из {goals.totalCount}</span>
            </div>
            <ProgressBar value={progress} max={goals.totalCount} label={`${progress} из ${goals.totalCount}`} />
            <p className="goal-name">{goals.active.name}</p>
            <div className="goal-card-footer">
                {coinType != null && <ResourceChip resourceType={coinType} value={goals.active.rewardCoins} />}
                {goals.zealCharges > 0 && <ZealChip charges={goals.zealCharges} />}
            </div>
        </section>
    );
};

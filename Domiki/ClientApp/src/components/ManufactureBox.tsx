import ZapIcon from 'pixelarticons/svg/zap.svg?react';
import type { ManufactureDto, ReceiptDto, ResourceTypeDto } from '../types/api';
import { canInstaFinish, instaFinishCost, manufactureProgressPercent } from '../utils/game';
import { ProgressBar } from './ProgressBar';
import { ResourceSprite } from './sprites';

interface ManufactureBoxProps {
    manufacture: ManufactureDto;
    receipt: ReceiptDto;
    now: number;
    remainingText: string;
    goldValue: number;
    goldType?: ResourceTypeDto | undefined;
    onHurry: (manufactureId: number) => void;
    onToggleAutoRepeat: (manufactureId: number, next: boolean) => void;
}

export const ManufactureBox = ({ manufacture, receipt, now, remainingText, goldValue, goldType, onHurry, onToggleAutoRepeat }: ManufactureBoxProps) => {
    const percent = manufactureProgressPercent(manufacture, receipt, now);
    const hurryCost = instaFinishCost(manufacture.finishDate, now);
    const tooFar = !canInstaFinish(manufacture.finishDate, now);
    const notEnoughGold = goldValue < hurryCost;
    const hurryTitle = tooFar
        ? `До конца ${remainingText}; ускорение доступно в последние 6 ч`
        : notEnoughGold ? `Не хватает золота: ${hurryCost - goldValue}` : undefined;

    return (
        <div className="manufacture-box">
            <ProgressBar value={percent} max={100} label={remainingText} />
            <div className="manufacture-info">
                <span className="manufacture-name">{receipt.name}</span>
                <span className="resource-box" title="Трудяги">
                    <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                    <span className="resource-value">{manufacture.plodderCount}</span>
                </span>
            </div>
            <button type="button" className="btn-game"
                disabled={tooFar || notEnoughGold}
                title={hurryTitle}
                onClick={() => onHurry(manufacture.id)}>
                <ZapIcon className="btn-ico" aria-hidden="true" />
                Поторопить – {Math.max(1, hurryCost)}
                {goldType != null &&
                    <ResourceSprite logicName={goldType.logicName} className="hurry-cost-ico" aria-hidden="true" />
                }
            </button>
            <label className="receipt-optional">
                <input type="checkbox" checked={manufacture.autoRepeat}
                    onChange={() => onToggleAutoRepeat(manufacture.id, !manufacture.autoRepeat)} />
                Повторять
            </label>
        </div>
    );
};

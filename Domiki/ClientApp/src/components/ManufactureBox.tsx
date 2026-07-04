import { ManufactureDto, ReceiptDto } from '../types/api';
import { manufactureProgressPercent } from '../utils/game';

interface ManufactureBoxProps {
    manufacture: ManufactureDto;
    receipt: ReceiptDto;
    now: number;
    remainingText: string;
}

export const ManufactureBox = ({ manufacture, receipt, now, remainingText }: ManufactureBoxProps) => {
    const percent = manufactureProgressPercent(manufacture, receipt, now);

    return (
        <div className="manufacture-box">
            <progress max={100} value={percent} data-label={remainingText}></progress>
            <div className="manufacture-info">
                <span className="manufacture-name">{receipt.name}</span>
                <span className="resource-box" title="Трудяги">
                    <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                    <span className="resource-value">{manufacture.plodderCount}</span>
                </span>
            </div>
        </div>
    );
};

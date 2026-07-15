import { useState } from 'react';
import ZapIcon from 'pixelarticons/svg/zap.svg?react';
import RepeatIcon from 'pixelarticons/svg/repeat.svg?react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import type { ManufactureDto, ReceiptDto, ResourceTypeDto } from '../types/api';
import { canInstaFinish, instaFinishCost, manufactureProgressPercent } from '../utils/game';
import { ProgressBar } from './ProgressBar';
import { ActionButton } from './ActionButton';
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
    const [repeatExpanded, setRepeatExpanded] = useState(false);
    const percent = manufactureProgressPercent(manufacture, receipt, now);
    const hurryCost = instaFinishCost(manufacture.finishDate, now);
    const tooFar = !canInstaFinish(manufacture.finishDate, now);
    const notEnoughGold = goldValue < hurryCost;
    const hurryTitle = tooFar
        ? `До конца ${remainingText}; ускорение доступно в последние 6 ч`
        : notEnoughGold ? `Не хватает золота: ${hurryCost - goldValue}` : undefined;
    const repeatAt = new Intl.DateTimeFormat('ru-RU', {
        day: 'numeric',
        month: 'short',
        hour: '2-digit',
        minute: '2-digit',
    }).format(new Date(manufacture.finishDate));

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
            <ActionButton className="btn-game"
                disabled={tooFar || notEnoughGold}
                title={hurryTitle}
                onClick={() => onHurry(manufacture.id)}>
                <ZapIcon className="btn-ico" aria-hidden="true" />
                Поторопить – {Math.max(1, hurryCost)}
                {goldType != null &&
                    <ResourceSprite logicName={goldType.logicName} className="hurry-cost-ico" aria-hidden="true" />
                }
            </ActionButton>
            <div className={'manufacture-repeat' + (manufacture.autoRepeat ? ' manufacture-repeat-on' : '')}>
                <button type="button" className="manufacture-repeat-toggle"
                    aria-expanded={repeatExpanded}
                    onClick={() => setRepeatExpanded(expanded => !expanded)}>
                    <RepeatIcon className="manufacture-repeat-ico" aria-hidden="true" />
                    <strong>{manufacture.autoRepeat ? 'Автоповтор включён' : 'Автоповтор выключен'}</strong>
                    <ChevronDownIcon className={'manufacture-repeat-caret' + (repeatExpanded ? ' manufacture-repeat-caret-open' : '')}
                        aria-hidden="true" />
                </button>
                {repeatExpanded &&
                    <div className="manufacture-repeat-body">
                        <p>
                            {manufacture.autoRepeat
                                ? <>Следующая попытка — {repeatAt}: снова запустится «{receipt.name}», если хватит ресурсов и трудяги смогут продолжить.</>
                                : <>После завершения «{receipt.name}» новая смена сама не запустится.</>}
                        </p>
                        <ActionButton className="btn-game btn-ghost manufacture-repeat-action"
                            onClick={() => onToggleAutoRepeat(manufacture.id, !manufacture.autoRepeat)}>
                            {manufacture.autoRepeat ? 'Остановить повторы' : 'Повторять эту смену'}
                        </ActionButton>
                        {manufacture.autoRepeat &&
                            <span className="manufacture-repeat-note">Текущая смена завершится как обычно</span>
                        }
                    </div>
                }
            </div>
        </div>
    );
};

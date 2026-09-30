import { useEffect, useId, useRef, useState } from 'react';
import type { ResourceTypeDto } from '../types/api';
import { INSTA_FINISH_CONFIRM_GOLD, INSTA_FINISH_MAX_SECONDS, INSTA_FINISH_MIN_SECONDS, instaFinishBlock, instaFinishCost } from '../utils/game';
import { pluralRu } from '../utils/plural';
import { remainingSeconds } from '../utils/time';
import { ActionButton } from './ActionButton';
import { AbstractSprite, ResourceSprite } from './sprites';

const CONFIRM_WINDOW_MS = 3000;
const CONFIRM_DELAY_MS = 400;

interface HurryButtonProps {
    finishDate: string;
    now: number;
    plodderCount: number;
    goldValue: number;
    goldType?: ResourceTypeDto | undefined;
    remainingText: string;
    onHurry: (confirmed: boolean) => void;
}

export const HurryButton = ({ finishDate, now, plodderCount, goldValue, goldType, remainingText, onHurry }: HurryButtonProps) => {
    const [armed, setArmed] = useState(false);
    const armedAt = useRef(0);
    const reasonId = useId();
    const remaining = remainingSeconds(finishDate, now);
    const hurryCost = instaFinishCost(remaining, plodderCount);
    const shownCost = Math.max(1, hurryCost);
    const block = instaFinishBlock(remaining);
    const notEnoughGold = goldValue < hurryCost;
    const disabled = block != null || notEnoughGold;
    const needConfirm = hurryCost > INSTA_FINISH_CONFIRM_GOLD;
    const confirming = armed && needConfirm && !disabled;
    const hurryReason = block === 'tooFar'
        ? `До конца ${remainingText} – поторопить можно только в последние ${INSTA_FINISH_MAX_SECONDS / 3600} ч`
        : block === 'tooSoon'
            ? `До конца меньше ${INSTA_FINISH_MIN_SECONDS / 60} минут – дождись, золото тут ни к чему`
            : notEnoughGold ? `Не хватает золота: ${hurryCost - goldValue}` : undefined;

    useEffect(() => {
        if (!armed) return;
        const timer = window.setTimeout(() => { setArmed(false); }, CONFIRM_WINDOW_MS);
        return () => { window.clearTimeout(timer); };
    }, [armed]);

    const handleClick = () => {
        if (needConfirm && !armed) {
            armedAt.current = Date.now();
            setArmed(true);
            return;
        }
        if (needConfirm && Date.now() - armedAt.current < CONFIRM_DELAY_MS) {
            return;
        }
        setArmed(false);
        onHurry(needConfirm);
    };

    return (
        <>
            <ActionButton className="btn-game hurry-btn"
                disabled={disabled}
                aria-describedby={hurryReason == null ? undefined : reasonId}
                aria-label={confirming ? `Подтвердить: поторопить за ${hurryCost} ${pluralRu(hurryCost, 'золотой', 'золотых', 'золотых')}` : undefined}
                onClick={handleClick}>
                <span className="hurry-btn-cell hurry-btn-ico">
                    <AbstractSprite logicName="hurry" size={24} aria-hidden="true" />
                </span>
                <span className="hurry-btn-label">{confirming ? 'Точно?' : 'Поторопить'}</span>
                <span className={'hurry-btn-cell hurry-btn-price' + (notEnoughGold ? ' hurry-btn-price--short' : '')}
                    aria-label={`цена ${shownCost}`}>
                    {goldType != null && <ResourceSprite logicName={goldType.logicName} size={24} aria-hidden="true" />}
                    {shownCost}
                </span>
            </ActionButton>
            {hurryReason != null && <p id={reasonId} className="hurry-btn-reason">{hurryReason}</p>}
        </>
    );
};

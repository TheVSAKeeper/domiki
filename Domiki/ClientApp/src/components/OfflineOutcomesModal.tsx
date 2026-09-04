import { useLayoutEffect, useRef } from 'react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import type { OfflineOutcome } from '../types/api';
import { OFFLINE_OUTCOMES_SUBTITLE, OFFLINE_OUTCOMES_TITLE, offlineOutcomeText } from '../utils/offlineOutcomeTexts';
import { withStableKeys } from '../utils/keys';
import { AbstractSprite } from './sprites';

interface OfflineOutcomesModalProps {
    outcomes: OfflineOutcome[];
    onClose: () => void;
}

export const OfflineOutcomesModal = ({ outcomes, onClose }: OfflineOutcomesModalProps) => {
    const dialogRef = useRef<HTMLDialogElement>(null);

    useLayoutEffect(() => {
        const dialog = dialogRef.current;
        if (dialog != null && !dialog.open) {
            dialog.showModal();
        }
    }, []);

    const rows = withStableKeys(outcomes, outcome => `${outcome.kind}-${outcome.reason}`);

    return (
        <dialog ref={dialogRef} className="recap-modal pixel-panel" aria-label={OFFLINE_OUTCOMES_TITLE} onClose={onClose}>
            <div className="recap-hero">
                <div className="recap-hero-head">
                    <div>
                        <h2 className="recap-title">{OFFLINE_OUTCOMES_TITLE}</h2>
                    </div>
                    <button type="button" className="recap-close" title="Закрыть" onClick={onClose}>
                        <CloseIcon aria-hidden="true" />
                    </button>
                </div>
                <p className="recap-subtitle">{OFFLINE_OUTCOMES_SUBTITLE}</p>
            </div>
            <div className="recap-section" data-tone="prod">
                {rows.map(({ key, item: outcome }) => (
                    <div key={key} className="recap-row">
                        <AbstractSprite logicName="incident" size={24} className="recap-row-ico" aria-hidden="true" />
                        <span className="recap-line">{offlineOutcomeText(outcome.kind, outcome.reason)}</span>
                    </div>
                ))}
            </div>
        </dialog>
    );
};

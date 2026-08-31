import { useLayoutEffect, useRef } from 'react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import type { DomikTypeDto } from '../types/api';
import { ActionButton } from './ActionButton';
import { MechanicSprite } from './sprites';

interface VillageProfileConfirmModalProps {
    genitiveName: string;
    buildings: DomikTypeDto[];
    onConfirm: () => Promise<boolean>;
    onClose: () => void;
}

const upperFirst = (value: string) => value.charAt(0).toUpperCase() + value.slice(1);
const lowerFirst = (value: string) => value.charAt(0).toLowerCase() + value.slice(1);

export const VillageProfileConfirmModal = ({ genitiveName, buildings, onConfirm, onClose }: VillageProfileConfirmModalProps) => {
    const dialogRef = useRef<HTMLDialogElement>(null);

    useLayoutEffect(() => {
        const dialog = dialogRef.current;
        if (dialog != null && !dialog.open) {
            dialog.showModal();
        }
    }, []);

    const [first, second] = buildings;
    const body = first != null && second != null
        ? `${upperFirst(first.name)} и ${lowerFirst(second.name)} переймут соседскую сноровку – смены в них станут короче на 15 %. Монет это не стоит, но сменить уклад снова выйдет лишь через 7 суток.`
        : '';
    const title = `Перенять уклад ${genitiveName}?`;

    const confirm = async () => {
        const ok = await onConfirm();
        if (ok) {
            onClose();
        }
    };

    return (
        <dialog ref={dialogRef} className="errand-modal pixel-panel" aria-label={title} onClose={onClose}>
            <div className="errand-modal-head">
                <h2 className="errand-modal-title">{title}</h2>
                <button type="button" className="errand-modal-close" title="Закрыть" onClick={onClose}>
                    <CloseIcon aria-hidden="true" />
                </button>
            </div>
            <p className="errand-modal-offer">{body}</p>
            <div className="errand-actions">
                <ActionButton className="btn-game icon-chip-btn" onClick={confirm}>
                    <MechanicSprite logicName="obzhitost" size={32} className="btn-ico" aria-hidden="true" />
                    Перенять уклад
                </ActionButton>
                <ActionButton className="btn-game btn-ghost" onClick={() => { onClose(); }}>
                    Погодить
                </ActionButton>
            </div>
        </dialog>
    );
};

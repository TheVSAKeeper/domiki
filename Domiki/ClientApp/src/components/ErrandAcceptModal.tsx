import { useLayoutEffect, useRef, useState } from 'react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import type { ErrandDto, WorkerDto } from '../types/api';
import { errandClueDurationsHours, errandCoinsPerWorkerHour, errandReputationByClue, getErrandTemplate } from '../utils/errandTexts';
import { isWorkerFree } from '../utils/game';
import { isSkilledWorker } from '../utils/worker';
import { ActionButton } from './ActionButton';
import { AbstractSprite, MechanicSprite, WorkerSprite } from './sprites';

const ERRAND_MAX_WORKERS = 2;

interface ErrandAcceptModalProps {
    errand: ErrandDto;
    workers: WorkerDto[];
    now: number;
    onConfirm: (errandId: number, clueId: number, workerIds: number[]) => Promise<boolean>;
    onClose: () => void;
}

export const ErrandAcceptModal = ({ errand, workers, now, onConfirm, onClose }: ErrandAcceptModalProps) => {
    const dialogRef = useRef<HTMLDialogElement>(null);
    const [clueId, setClueId] = useState<number | null>(null);
    const [workerIds, setWorkerIds] = useState<number[]>([]);

    useLayoutEffect(() => {
        const dialog = dialogRef.current;
        if (dialog != null && !dialog.open) {
            dialog.showModal();
        }
    }, []);

    const template = getErrandTemplate(errand.templateId);
    const freeWorkers = workers.filter(worker => isWorkerFree(worker, now));

    const toggleWorker = (workerId: number) => setWorkerIds(prev => {
        if (prev.includes(workerId)) {
            return prev.filter(id => id !== workerId);
        }
        if (prev.length >= ERRAND_MAX_WORKERS) {
            return prev;
        }
        return [...prev, workerId];
    });

    const canConfirm = clueId != null && workerIds.length > 0;
    const hours = clueId == null ? 0 : errandClueDurationsHours[clueId] ?? 0;
    const rewardCoins = errandCoinsPerWorkerHour * workerIds.length * hours;

    const confirm = async () => {
        if (clueId == null) {
            return;
        }
        const ok = await onConfirm(errand.id, clueId, workerIds);
        if (ok) {
            onClose();
        }
    };

    return (
        <dialog ref={dialogRef} className="errand-modal pixel-panel" aria-label={template.title} onClose={onClose}>
            <div className="errand-modal-head">
                <div>
                    <h2 className="errand-modal-title">{template.title}</h2>
                    <span className="errand-modal-sub">Выберите зацепку и трудяг для поисков</span>
                </div>
                <button type="button" className="errand-modal-close" title="Закрыть" onClick={onClose}>
                    <CloseIcon aria-hidden="true" />
                </button>
            </div>
            <p className="errand-modal-offer">{template.offer}</p>
            <span className="panel-label">зацепка</span>
            <div className="errand-clue-list">
                {template.clues.map((clue, index) => (
                    <button key={clue.label} type="button"
                        className={'errand-clue' + (clueId === index ? ' errand-clue-selected' : '')}
                        onClick={() => setClueId(index)}>
                        <span className="errand-clue-label">{clue.label}</span>
                        <span className="errand-clue-detail">{clue.detail}</span>
                        <span className="errand-clue-meta">
                            <span><ClockIcon aria-hidden="true" />≈{errandClueDurationsHours[index]} ч</span>
                            <span><AbstractSprite logicName="reputation" size={24} className="reputation-ico" aria-hidden="true" />+{errandReputationByClue[index]} реп.</span>
                        </span>
                    </button>
                ))}
            </div>
            <span className="panel-label">трудяги в поиск (1–2)</span>
            <div className="worker-picker">
                {freeWorkers.length === 0 && <span className="hint">Нет свободных трудяг</span>}
                {freeWorkers.map(worker => (
                    <button key={worker.id} type="button"
                        className={'worker-chip worker-chip-pick' + (workerIds.includes(worker.id) ? ' worker-chip-selected' : '')}
                        onClick={() => toggleWorker(worker.id)}>
                        <WorkerSprite name={worker.name} skilled={isSkilledWorker(worker)} className="worker-avatar" aria-hidden="true" />
                        <span className="worker-name">{worker.name}</span>
                    </button>
                ))}
            </div>
            {canConfirm &&
                <div className="errand-reward-preview">
                    <span className="panel-label">награда за поиски</span>
                    <span className="errand-reward-preview-value">+{rewardCoins} монет, +{errandReputationByClue[clueId]} реп.</span>
                </div>
            }
            <ActionButton className="btn-game" disabled={!canConfirm} onClick={confirm}>
                <MechanicSprite logicName="errands" size={24} className="btn-ico" aria-hidden="true" />
                Отправить на поиски
            </ActionButton>
        </dialog>
    );
};

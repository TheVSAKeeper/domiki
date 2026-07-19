import { useState } from 'react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import HandIcon from 'pixelarticons/svg/hand.svg?react';
import type { DomikIncidentDto, DomikTypeDto, WorkerDto } from '../types/api';
import { isWorkerFree } from '../utils/game';
import { domikIncidentText, getDomikIncidentTemplate, incidentClueDurationsHours, incidentClueFindMultiplier } from '../utils/domikIncidentTexts';
import { isSkilledWorker } from '../utils/worker';
import { formatDuration, remainingSeconds } from '../utils/time';
import { ActionButton } from './ActionButton';
import { ProgressBar } from './ProgressBar';
import { DomikSprite, WorkerSprite } from './sprites';

interface DomikIncidentCardProps {
    incident: DomikIncidentDto;
    workers: WorkerDto[];
    domikTypes: DomikTypeDto[];
    now: number;
    onStartSearch: (incidentId: number, clueId: number, workerIds: number[]) => Promise<boolean>;
}

const INCIDENT_MAX_WORKERS = 2;
const timeLabel = (date: string) => new Date(date).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

export const DomikIncidentCard = ({ incident, workers, domikTypes, now, onStartSearch }: DomikIncidentCardProps) => {
    const [clueId, setClueId] = useState<number | null>(null);
    const [workerIds, setWorkerIds] = useState<number[]>([]);
    const template = getDomikIncidentTemplate(incident.templateId);
    const domikType = domikTypes.find(type => type.id === incident.domikTypeId);
    const domikName = domikType?.name ?? 'Постройка';
    const selectableWorkers = workers;
    const freeWorkers = selectableWorkers.filter(worker => isWorkerFree(worker, now));
    const toggleWorker = (workerId: number) => setWorkerIds(previous => {
        if (previous.includes(workerId)) {
            return previous.filter(id => id !== workerId);
        }
        return previous.length >= INCIDENT_MAX_WORKERS ? previous : [...previous, workerId];
    });
    const startSearch = async () => {
        if (clueId == null) {
            return;
        }
        await onStartSearch(incident.id, clueId, workerIds);
    };

    if (incident.clueId != null && incident.searchEndDate != null) {
        const clue = template.clues[incident.clueId] ?? template.clues[0];
        const durationHours = incidentClueDurationsHours[incident.clueId] ?? 0;
        const end = Date.parse(incident.searchEndDate);
        const start = end - durationHours * 3600000;
        const crew = workers.filter(worker => incident.searchWorkerIds.includes(worker.id));
        return (
            <section className="incident-card goal-card pixel-panel">
                <div className="goal-hero incident-hero">
                    <div className="goal-emblem">{domikType != null && <DomikSprite logicName={domikType.logicName} className="goal-emblem-ico" aria-hidden="true" />}</div>
                    <div className="goal-hero-text"><span className="errand-badge">Происшествие</span><h3 className="goal-title">{template.title}</h3></div>
                </div>
                <p className="incident-clue"><b>{clue.label}</b><span>{domikIncidentText(clue.detail, domikName, '', undefined)}</span></p>
                <ProgressBar value={Math.min(Math.max(now - start, 0), end - start)} max={end - start} label={formatDuration(Math.max(remainingSeconds(incident.searchEndDate, now), 0))} />
                <span className="incident-return">Разбираются – вернутся к {timeLabel(incident.searchEndDate)}</span>
                <div className="errand-crew">
                    {crew.map(worker => <span key={worker.id} className="errand-crew-member"><WorkerSprite name={worker.name} skilled={isSkilledWorker(worker)} className="worker-avatar" aria-hidden="true" />{worker.name}</span>)}
                </div>
            </section>
        );
    }

    const canStart = clueId != null && workerIds.length > 0;
    return (
        <section className="incident-card goal-card pixel-panel">
            <div className="goal-hero incident-hero">
                <div className="goal-emblem">{domikType != null && <DomikSprite logicName={domikType.logicName} className="goal-emblem-ico" aria-hidden="true" />}</div>
                <div className="goal-hero-text"><span className="errand-badge">Происшествие</span><h3 className="goal-title">{template.title}</h3></div>
            </div>
            <p className="incident-hook">{domikIncidentText(template.hook, domikName, '', undefined)}</p>
            <span className="incident-return">Разгадается сама к {timeLabel(incident.autoResolveDate)}</span>
            <span className="panel-label">зацепка</span>
            <div className="errand-clue-list">
                {template.clues.map((clue, index) => (
                    <button key={clue.label} type="button" role="radio" aria-checked={clueId === index} className={'errand-clue' + (clueId === index ? ' errand-clue-selected' : '')} onClick={() => setClueId(index)}>
                        <span className="errand-clue-label">{clue.label}</span>
                        <span className="errand-clue-detail">{domikIncidentText(clue.detail, domikName, '', undefined)}</span>
                        <span className="errand-clue-meta"><span><ClockIcon aria-hidden="true" />{incidentClueDurationsHours[index]} ч</span><span>находка ×{incidentClueFindMultiplier[index]}</span></span>
                    </button>
                ))}
            </div>
            <span className="panel-label">трудяги разбираться (1–2)</span>
            <div className="worker-picker">
                {freeWorkers.length === 0 && <span className="hint">Нет свободных трудяг</span>}
                {selectableWorkers.map(worker => {
                    const free = isWorkerFree(worker, now);
                    return <button key={worker.id} type="button" disabled={!free} className={'worker-chip worker-chip-pick' + (workerIds.includes(worker.id) ? ' worker-chip-selected' : '')} onClick={() => toggleWorker(worker.id)}><WorkerSprite name={worker.name} skilled={isSkilledWorker(worker)} className="worker-avatar" aria-hidden="true" /><span className="worker-name">{worker.name}</span></button>;
                })}
            </div>
            <ActionButton className="btn-game" disabled={!canStart} onClick={startSearch}><HandIcon className="btn-ico" aria-hidden="true" />Отправить разбираться</ActionButton>
        </section>
    );
};

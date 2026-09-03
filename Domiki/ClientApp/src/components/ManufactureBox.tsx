import { Fragment, useState } from 'react';
import RepeatIcon from 'pixelarticons/svg/repeat.svg?react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import type { ManufactureDto, ReceiptDto, ResourceTypeDto } from '../types/api';
import { manufactureProgressPercent } from '../utils/game';
import { formatTimeOfDay } from '../utils/time';
import { ProgressBar } from './ProgressBar';
import { ActionButton } from './ActionButton';
import { HurryButton } from './HurryButton';
import { ResourceNameChip } from './ResourceNameChip';

interface ManufactureBoxProps {
    manufacture: ManufactureDto;
    receipt: ReceiptDto;
    now: number;
    remainingText: string;
    goldValue: number;
    goldType?: ResourceTypeDto | undefined;
    resourceTypes?: ResourceTypeDto[];
    measureUnlocked?: boolean;
    keyResourceTypeIds?: number[];
    pending?: boolean;
    onHurry: (manufactureId: number) => void;
    onToggleAutoRepeat: (manufactureId: number, next: boolean) => void;
    onSetMeasure?: (manufactureId: number, resourceTypeId: number | null, value: number | null) => void;
}

export const ManufactureBox = ({ manufacture, receipt, now, remainingText, goldValue, goldType, resourceTypes = [], measureUnlocked = false, keyResourceTypeIds = [], pending = false, onHurry, onToggleAutoRepeat, onSetMeasure }: ManufactureBoxProps) => {
    const [repeatExpanded, setRepeatExpanded] = useState(false);
    const measureDefaultTypeId = manufacture.measureResourceTypeId
        ?? receipt.outputResources[0]?.typeId
        ?? resourceTypes[0]?.id
        ?? 0;
    const [measureTypeId, setMeasureTypeId] = useState(measureDefaultTypeId);
    const [measureInput, setMeasureInput] = useState(String(manufacture.measureValue ?? ''));
    const measureType = resourceTypes.find(type => type.id === (manufacture.measureResourceTypeId ?? measureTypeId));
    const parsedMeasure = Math.trunc(Number(measureInput));
    const measureReady = Number.isFinite(parsedMeasure) && parsedMeasure > 0;
    const percent = manufactureProgressPercent(manufacture, now);
    const repeatAt = formatTimeOfDay(manufacture.finishDate, now);
    const keyInputs = receipt.inputResources.flatMap(input => {
        const type = resourceTypes.find(candidate => candidate.id === input.typeId);
        return keyResourceTypeIds.includes(input.typeId) && type != null ? [{ value: input.value, type }] : [];
    });

    const head = (
        <>
            <ProgressBar value={percent} max={100} label={remainingText} />
            <div className="manufacture-info">
                <span className="manufacture-name">{receipt.name}</span>
                <span className="resource-box" title="Трудяги">
                    <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                    <span className="resource-value">{manufacture.plodderCount}</span>
                </span>
            </div>
        </>
    );

    if (pending) {
        return (
            <div className="manufacture-box">
                {head}
                <p className="manufacture-pending">Смена записана, ждём ответа деревни</p>
            </div>
        );
    }

    return (
        <div className="manufacture-box">
            {head}
            <HurryButton finishDate={manufacture.finishDate} now={now} goldValue={goldValue} goldType={goldType}
                remainingText={remainingText} onHurry={() => { onHurry(manufacture.id); }} />
            <div className={'manufacture-repeat' + (manufacture.autoRepeat ? ' manufacture-repeat-on' : '')}>
                <button type="button" className="manufacture-repeat-toggle"
                    aria-expanded={repeatExpanded}
                    onClick={() => setRepeatExpanded(expanded => !expanded)}>
                    <RepeatIcon className="manufacture-repeat-ico" aria-hidden="true" />
                    <strong>{manufacture.autoRepeat ? 'Наряд поставлен' : 'Наряда нет'}</strong>
                    <ChevronDownIcon className={'manufacture-repeat-caret' + (repeatExpanded ? ' manufacture-repeat-caret-open' : '')}
                        aria-hidden="true" />
                </button>
                {repeatExpanded &&
                    <div className="manufacture-repeat-body">
                        <p>
                            {manufacture.autoRepeat
                                ? <>Следующая попытка в {repeatAt}: снова возьмутся за «{receipt.name}», если хватит припасов и трудяг.</>
                                : <>После завершения «{receipt.name}» новая смена сама не запустится.</>}
                        </p>
                        {keyInputs.length > 0 &&
                            <p className="manufacture-repeat-key">
                                Каждая смена забирает{' '}
                                {keyInputs.map((input, index) => (
                                    <Fragment key={input.type.id}>
                                        {index > 0 && ', '}
                                        <span className="manufacture-repeat-key-item">
                                            <ResourceNameChip resourceType={input.type} /> ×{input.value}
                                        </span>
                                    </Fragment>
                                ))}
                                {' '}– под нарядом ключи будут уходить один за другим.
                            </p>
                        }
                        <ActionButton className="btn-game btn-ghost manufacture-repeat-action"
                            onClick={() => onToggleAutoRepeat(manufacture.id, !manufacture.autoRepeat)}>
                            {manufacture.autoRepeat ? 'Снять наряд' : 'Поставить наряд'}
                        </ActionButton>
                        {manufacture.autoRepeat &&
                            <span className="manufacture-repeat-note">Текущая смена завершится как обычно</span>
                        }
                        {manufacture.autoRepeat && measureUnlocked && onSetMeasure != null &&
                            <div className="manufacture-measure">
                                <span className="panel-label">Мера наряда</span>
                                {manufacture.measureValue != null && measureType != null &&
                                    <p className="manufacture-measure-hint">
                                        Наряд снимется сам, когда <ResourceNameChip resourceType={measureType} /> дойдёт до {manufacture.measureValue}.
                                    </p>
                                }
                                {manufacture.measureValue == null &&
                                    <p className="manufacture-measure-hint">Без меры – пока хватает припасов.</p>
                                }
                                <label className="manufacture-measure-row">
                                    Повторять, пока
                                    <select value={measureTypeId} onChange={event => setMeasureTypeId(Number(event.target.value))}>
                                        {resourceTypes.map(type => <option key={type.id} value={type.id}>{type.name}</option>)}
                                    </select>
                                    меньше
                                    <input type="number" min={1} value={measureInput}
                                        onChange={event => setMeasureInput(event.target.value)} />
                                </label>
                                <div className="manufacture-measure-actions">
                                    <ActionButton className="btn-game btn-ghost" disabled={!measureReady}
                                        onClick={() => onSetMeasure(manufacture.id, measureTypeId, parsedMeasure)}>
                                        Поставить меру
                                    </ActionButton>
                                    {manufacture.measureValue != null &&
                                        <ActionButton className="btn-game btn-ghost"
                                            onClick={() => { setMeasureInput(''); onSetMeasure(manufacture.id, null, null); }}>
                                            Снять меру
                                        </ActionButton>
                                    }
                                </div>
                            </div>
                        }
                    </div>
                }
            </div>
        </div>
    );
};

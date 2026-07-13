import MinusIcon from 'pixelarticons/svg/minus.svg?react';
import PlusIcon from 'pixelarticons/svg/plus.svg?react';

interface NumberStepperProps {
    value: number;
    onChange: (value: number) => void;
    min?: number;
    max?: number;
    step?: number;
    className?: string;
}

export const NumberStepper = ({ value, onChange, min = 1, max, step = 1, className }: NumberStepperProps) => {
    const clamp = (next: number) => Math.min(max ?? Infinity, Math.max(min, next));

    return (
        <div className={'number-stepper' + (className == null ? '' : ' ' + className)}>
            <button type="button" className="number-stepper-btn" aria-label="Уменьшить значение" disabled={value <= min}
                onClick={() => onChange(clamp(value - step))}>
                <MinusIcon className="btn-ico" aria-hidden="true" />
            </button>
            <input type="number" min={min} max={max} step={step} value={value}
                onChange={event => onChange(clamp(Math.floor(Number(event.target.value) || min)))} />
            <button type="button" className="number-stepper-btn" aria-label="Увеличить значение" disabled={max != null && value >= max}
                onClick={() => onChange(clamp(value + step))}>
                <PlusIcon className="btn-ico" aria-hidden="true" />
            </button>
        </div>
    );
};

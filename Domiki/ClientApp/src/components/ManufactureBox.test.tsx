import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { ManufactureDto, ReceiptDto } from '../types/api';
import { ManufactureBox } from './ManufactureBox';

const manufacture: ManufactureDto = {
    id: 17,
    finishDate: '2026-07-13T12:30:00.000Z',
    durationSeconds: 3600,
    plodderCount: 2,
    receiptId: 4,
    autoRepeat: true,
};

const receipt = {
    id: 4,
    name: 'Обжечь кирпич',
    durationSeconds: 3600,
    inputResources: [{ typeId: 200, value: 2 }],
    outputResources: [{ typeId: 201, value: 1 }],
} as ReceiptDto;

const renderBox = (value: ManufactureDto, onToggle = vi.fn()) => {
    render(<ManufactureBox manufacture={value} receipt={receipt} now={Date.parse(value.finishDate) - 1000}
        remainingText="1 с" goldValue={0} onHurry={vi.fn()} onToggleAutoRepeat={onToggle} />);
    return onToggle;
};

describe('ManufactureBox наряд controls', () => {
    it('нарисованная смена не даёт ни ускорить, ни поставить наряд, пока сервер не ответил', () => {
        render(<ManufactureBox manufacture={{ ...manufacture, id: -1 }} receipt={receipt} now={Date.parse(manufacture.finishDate) - 1000}
            remainingText="1 с" goldValue={0} pending onHurry={vi.fn()} onToggleAutoRepeat={vi.fn()} />);

        expect(screen.getByText('Смена записана, ждём ответа деревни')).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Наряд поставлен' })).not.toBeInTheDocument();
    });

    it('explains the standing наряд and lets the player lift it', () => {
        const onToggle = renderBox(manufacture);

        expect(screen.getByText('Наряд поставлен')).toBeInTheDocument();
        expect(screen.queryByText(/снова возьмутся за «Обжечь кирпич»/)).not.toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: 'Наряд поставлен' }));
        expect(screen.getByText(/снова возьмутся за «Обжечь кирпич»/)).toBeInTheDocument();
        expect(screen.getByText('Текущая смена завершится как обычно')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: 'Снять наряд' }));
        expect(onToggle).toHaveBeenCalledWith(17, false);
    });

    it('warns that a standing наряд keeps eating the ремесленный ключ', () => {
        const keyReceipt = { ...receipt, inputResources: [{ typeId: 200, value: 2 }, { typeId: 300, value: 1 }] };
        render(<ManufactureBox manufacture={{ ...manufacture, autoRepeat: false }} receipt={keyReceipt}
            now={Date.parse(manufacture.finishDate) - 1000} remainingText="1 с" goldValue={0}
            resourceTypes={[{ id: 300, name: 'Кайло', logicName: 'pick', marketValue: 160, isFood: false }]}
            keyResourceTypeIds={[300]} onHurry={vi.fn()} onToggleAutoRepeat={vi.fn()} />);

        fireEvent.click(screen.getByRole('button', { name: 'Наряда нет' }));
        expect(screen.getByText(/Каждая смена забирает/)).toBeInTheDocument();
        expect(screen.getByText('×1')).toBeInTheDocument();
    });

    it('keeps the наряд hint clean when no key is spent', () => {
        renderBox({ ...manufacture, autoRepeat: false });

        fireEvent.click(screen.getByRole('button', { name: 'Наряда нет' }));
        expect(screen.queryByText(/Каждая смена забирает/)).not.toBeInTheDocument();
    });

    it('lets the player put a наряд on the current shift', () => {
        const onToggle = renderBox({ ...manufacture, autoRepeat: false });

        expect(screen.getByText('Наряда нет')).toBeInTheDocument();
        fireEvent.click(screen.getByRole('button', { name: 'Наряда нет' }));
        fireEvent.click(screen.getByRole('button', { name: 'Поставить наряд' }));
        expect(onToggle).toHaveBeenCalledWith(17, true);
    });
});

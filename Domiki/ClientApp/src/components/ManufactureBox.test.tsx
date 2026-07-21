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
} as ReceiptDto;

const renderBox = (value: ManufactureDto, onToggle = vi.fn()) => {
    render(<ManufactureBox manufacture={value} receipt={receipt} now={Date.parse(value.finishDate) - 1000}
        remainingText="1 с" goldValue={0} onHurry={vi.fn()} onToggleAutoRepeat={onToggle} />);
    return onToggle;
};

describe('ManufactureBox repeat controls', () => {
    it('explains the active repeat and lets the player stop it', () => {
        const onToggle = renderBox(manufacture);

        expect(screen.getByText('Автоповтор включён')).toBeInTheDocument();
        expect(screen.queryByText(/снова запустится «Обжечь кирпич»/)).not.toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: 'Автоповтор включён' }));
        expect(screen.getByText(/снова запустится «Обжечь кирпич»/)).toBeInTheDocument();
        expect(screen.getByText('Текущая смена завершится как обычно')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: 'Остановить повторы' }));
        expect(onToggle).toHaveBeenCalledWith(17, false);
    });

    it('lets the player enable repeat for the current shift', () => {
        const onToggle = renderBox({ ...manufacture, autoRepeat: false });

        expect(screen.getByText('Автоповтор выключен')).toBeInTheDocument();
        fireEvent.click(screen.getByRole('button', { name: 'Автоповтор выключен' }));
        fireEvent.click(screen.getByRole('button', { name: 'Повторять эту смену' }));
        expect(onToggle).toHaveBeenCalledWith(17, true);
    });
});

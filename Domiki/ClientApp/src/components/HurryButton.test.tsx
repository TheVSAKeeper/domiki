import { act, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { HurryButton } from './HurryButton';

const NOW = Date.parse('2026-09-30T12:00:00.000Z');

const renderButton = (remainingSeconds: number, plodderCount: number, goldValue: number, onHurry = vi.fn()) => {
    render(<HurryButton finishDate={new Date(NOW + remainingSeconds * 1000).toISOString()} now={NOW}
        plodderCount={plodderCount} goldValue={goldValue} remainingText="7 ч" onHurry={onHurry} />);
    return onHurry;
};

describe('HurryButton', () => {
    beforeEach(() => {
        vi.useFakeTimers();
        vi.setSystemTime(NOW);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('шлёт подтверждение только после «Точно?» и не принимает двойной тап', () => {
        const onHurry = renderButton(4380, 5, 30);
        const button = screen.getByRole('button');

        fireEvent.click(button);
        expect(screen.getByText('Точно?')).toBeInTheDocument();
        expect(button).toHaveAccessibleName('Подтвердить: поторопить за 7 золотых');

        act(() => { vi.advanceTimersByTime(399); });
        fireEvent.click(button);
        expect(onHurry).not.toHaveBeenCalled();

        act(() => { vi.advanceTimersByTime(1); });
        fireEvent.click(button);
        expect(onHurry).toHaveBeenCalledExactlyOnceWith(true);
        expect(screen.getByText('Поторопить')).toBeInTheDocument();
    });

    it('«Точно?» гаснет через 3 с, и следующий тап снова переспрашивает', () => {
        const onHurry = renderButton(4380, 5, 30);
        const button = screen.getByRole('button');

        fireEvent.click(button);
        act(() => { vi.advanceTimersByTime(3000); });
        expect(screen.getByText('Поторопить')).toBeInTheDocument();

        fireEvent.click(button);
        expect(onHurry).not.toHaveBeenCalled();
        expect(screen.getByText('Точно?')).toBeInTheDocument();
    });

    it('цену до 6 золотых торопит первым тапом без подтверждения', () => {
        const onHurry = renderButton(4320, 5, 30);

        fireEvent.click(screen.getByRole('button'));

        expect(onHurry).toHaveBeenCalledExactlyOnceWith(false);
    });

    it.each([
        [7 * 3600, 1, 30, 'До конца 7 ч – поторопить можно только в последние 6 ч'],
        [10 * 60, 1, 30, 'До конца меньше 15 минут – дождись, золото тут ни к чему'],
        [4380, 5, 3, 'Не хватает золота: 4'],
    ])('недоступная кнопка показывает причину текстом (остаток %i с, трудяг %i, золота %i)', (remaining, plodders, gold, reason) => {
        renderButton(remaining, plodders, gold);

        const button = screen.getByRole('button');
        expect(button).toBeDisabled();
        expect(screen.getByText(reason)).toBeVisible();
        expect(button).toHaveAccessibleDescription(reason);
    });

    it('доступная кнопка причину не показывает', () => {
        renderButton(4320, 5, 30);

        expect(screen.getByRole('button')).not.toHaveAttribute('aria-describedby');
    });
});

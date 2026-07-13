import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { NumberStepper } from './NumberStepper';

describe('NumberStepper', () => {
    it('gives its icon-only controls meaningful accessible names', () => {
        render(<NumberStepper value={2} onChange={vi.fn()} />);

        expect(screen.getByRole('button', { name: 'Уменьшить значение' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Увеличить значение' })).toBeInTheDocument();
    });
});

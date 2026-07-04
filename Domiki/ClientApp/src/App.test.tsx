import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import App from './App';
import { ToastProvider } from './services/toast';

describe('App', () => {
    it('renders without crashing', async () => {
        render(
            <MemoryRouter>
                <ToastProvider>
                    <App />
                </ToastProvider>
            </MemoryRouter>,
        );

        expect(await screen.findByText('Domiki')).toBeInTheDocument();
    });
});

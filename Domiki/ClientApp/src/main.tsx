import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import { ToastProvider } from './services/toast';

const rootElement = document.getElementById('root');

if (rootElement == null) {
    throw new Error('Root element not found.');
}

const root = createRoot(rootElement);

root.render(
    <StrictMode>
        <BrowserRouter basename={import.meta.env.BASE_URL}>
            <ToastProvider>
                <App />
            </ToastProvider>
        </BrowserRouter>
    </StrictMode>,
);

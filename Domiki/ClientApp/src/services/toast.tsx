import { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';

interface ToastItem {
    id: number;
    message: string;
    type: 'success' | 'error';
}

interface ToastContextValue {
    success: (message: string) => void;
    error: (message: string) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);
const ToastItemsContext = createContext<ToastItem[]>([]);
const ToastDismissContext = createContext<(id: number) => void>(() => {});

export const ToastProvider = ({ children }: { children: ReactNode }) => {
    const [toasts, setToasts] = useState<ToastItem[]>([]);
    const nextId = useRef(0);

    const dismiss = useCallback((id: number) => {
        setToasts(current => current.filter(toast => toast.id !== id));
    }, []);

    const push = useCallback((message: string, type: ToastItem['type'], timeout: number) => {
        const id = nextId.current++;
        setToasts(current => [...current, { id, message, type }]);
        setTimeout(() => dismiss(id), timeout);
    }, [dismiss]);

    const error = useCallback((message: string) => push(message, 'error', 4000), [push]);

    const success = useCallback((message: string) => push(message, 'success', 2500), [push]);

    const value = useMemo(() => ({ success, error }), [success, error]);

    return (
        <ToastContext.Provider value={value}>
            <ToastItemsContext.Provider value={toasts}>
                <ToastDismissContext.Provider value={dismiss}>
                    {children}
                </ToastDismissContext.Provider>
            </ToastItemsContext.Provider>
        </ToastContext.Provider>
    );
};

export const useToast = (): ToastContextValue => {
    const context = useContext(ToastContext);
    if (context == null) {
        throw new Error('useToast должен использоваться внутри ToastProvider.');
    }
    return context;
};

export const Toaster = () => {
    const toasts = useContext(ToastItemsContext);
    const dismiss = useContext(ToastDismissContext);

    return (
        <div className="toast-container" aria-live="assertive" aria-atomic="false">
            {toasts.map(toast => (
                <button type="button" key={toast.id} className={`toast toast-${toast.type}`} onClick={() => dismiss(toast.id)}>
                    {toast.message}
                </button>
            ))}
        </div>
    );
};

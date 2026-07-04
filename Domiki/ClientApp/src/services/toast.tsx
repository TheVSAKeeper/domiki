import { createContext, ReactNode, useCallback, useContext, useMemo, useRef, useState } from 'react';

interface ToastItem {
    id: number;
    message: string;
}

interface ToastContextValue {
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

    const error = useCallback((message: string) => {
        const id = nextId.current++;
        setToasts(current => [...current, { id, message }]);
        setTimeout(() => dismiss(id), 4000);
    }, [dismiss]);

    const value = useMemo(() => ({ error }), [error]);

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
                <div key={toast.id} className="toast" onClick={() => dismiss(toast.id)}>
                    {toast.message}
                </div>
            ))}
        </div>
    );
};

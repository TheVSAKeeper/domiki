import { createContext, useContext } from 'react';

export interface ToastItem {
    id: number;
    message: string;
    type: 'success' | 'error';
}

export interface ToastContextValue {
    success: (message: string) => void;
    error: (message: string) => void;
}

export const ToastContext = createContext<ToastContextValue | null>(null);
export const ToastItemsContext = createContext<ToastItem[]>([]);
export const ToastDismissContext = createContext<(id: number) => void>(() => {});

export const useToast = (): ToastContextValue => {
    const context = useContext(ToastContext);
    if (context == null) {
        throw new Error('useToast должен использоваться внутри ToastProvider.');
    }
    return context;
};

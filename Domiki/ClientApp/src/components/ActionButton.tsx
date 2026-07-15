import { createContext, useContext, useId, useState } from 'react';
import type { ComponentPropsWithoutRef, MouseEvent, ReactNode } from 'react';

interface ActionBusyValue {
    busyKey: string | null;
    setBusyKey: (key: string | null) => void;
}

const ActionBusyContext = createContext<ActionBusyValue>({ busyKey: null, setBusyKey: () => {} });

// ponytail: single global busyKey locks every ActionButton while one action runs (backend serializes the player anyway); split per-key if two independent actions ever need to overlap
export const ActionBusyProvider = ({ children }: { children: ReactNode }) => {
    const [busyKey, setBusyKey] = useState<string | null>(null);
    return <ActionBusyContext.Provider value={{ busyKey, setBusyKey }}>{children}</ActionBusyContext.Provider>;
};

type ActionButtonProps = Omit<ComponentPropsWithoutRef<'button'>, 'onClick'> & {
    onClick?: (event: MouseEvent<HTMLButtonElement>) => void | Promise<void>;
};

export const ActionButton = ({ onClick, disabled, className, type, children, ...rest }: ActionButtonProps) => {
    const id = useId();
    const { busyKey, setBusyKey } = useContext(ActionBusyContext);
    const busy = busyKey === id;
    const run = async (event: MouseEvent<HTMLButtonElement>) => {
        if (busyKey != null) return;
        setBusyKey(id);
        try {
            await Promise.resolve(onClick?.(event));
        } finally {
            setBusyKey(null);
        }
    };
    return (
        <button {...rest} type={type ?? 'button'}
            className={busy ? `${className ?? ''} is-busy` : className}
            disabled={disabled === true || busyKey != null}
            aria-busy={busy}
            onClick={onClick == null ? undefined : event => { void run(event); }}>
            {busy && <span className="btn-spin" aria-hidden="true" />}
            {children}
        </button>
    );
};

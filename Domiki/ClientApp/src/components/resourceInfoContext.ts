import { createContext, useContext } from 'react';

export interface ResourceInfoContextValue {
    open: (typeId: number, el: HTMLElement) => void;
    close: () => void;
}

export const ResourceInfoContext = createContext<ResourceInfoContextValue | null>(null);

export const useResourceInfo = () => useContext(ResourceInfoContext);

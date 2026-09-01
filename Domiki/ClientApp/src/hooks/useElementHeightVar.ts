import { useLayoutEffect } from 'react';
import type { RefObject } from 'react';

export function useElementHeightVar(ref: RefObject<HTMLElement | null>, name: string): void {
    useLayoutEffect(() => {
        const element = ref.current;
        if (element == null) {
            return;
        }

        const write = () => { document.documentElement.style.setProperty(name, `${element.offsetHeight}px`); };
        write();
        const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(write);
        observer?.observe(element);
        return () => {
            observer?.disconnect();
            document.documentElement.style.setProperty(name, '0px');
        };
    }, [ref, name]);
}

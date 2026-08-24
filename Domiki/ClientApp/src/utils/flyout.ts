import { useLayoutEffect, useRef, useState } from 'react';
import type { RefObject } from 'react';

export const FLYOUT_EDGE_GAP = 8;
const ANCHOR_GAP = 6;

export const flyoutLeft = (anchorLeft: number, width: number) =>
    Math.max(FLYOUT_EDGE_GAP, Math.min(anchorLeft, window.innerWidth - width - FLYOUT_EDGE_GAP));

export const flyoutWidth = (preferred: number) =>
    Math.min(preferred, window.innerWidth - FLYOUT_EDGE_GAP * 2);

export const flyoutTop = (anchor: { top: number; bottom: number }, height: number) => {
    const below = anchor.bottom + ANCHOR_GAP;
    if (below + height + FLYOUT_EDGE_GAP <= window.innerHeight) {
        return below;
    }

    const above = anchor.top - ANCHOR_GAP - height;
    return above >= FLYOUT_EDGE_GAP ? above : Math.max(FLYOUT_EDGE_GAP, window.innerHeight - height - FLYOUT_EDGE_GAP);
};

type FlyoutPlacement<T extends HTMLElement> = readonly [ref: RefObject<T | null>, top: number, hidden: boolean];

export const useFlyoutTop = <T extends HTMLElement>(anchor: DOMRect | null): FlyoutPlacement<T> => {
    const ref = useRef<T>(null);
    const [top, setTop] = useState<number | null>(null);

    useLayoutEffect(() => {
        const element = ref.current;
        if (anchor == null || element == null) {
            setTop(null);
            return;
        }

        setTop(flyoutTop(anchor, element.offsetHeight));
    }, [anchor]);

    return [ref, top ?? (anchor == null ? 0 : anchor.bottom + ANCHOR_GAP), anchor != null && top == null];
};

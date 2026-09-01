import { useEffect, useId, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { flyoutLeft, flyoutWidth, useFlyoutTop } from '../utils/flyout';
import { termLore } from '../utils/termLore';

const POP_WIDTH = 260;

interface TermTipProps {
    term: string;
    children: ReactNode;
    className?: string;
}

export const TermTip = ({ term, children, className }: TermTipProps) => {
    const gloss = termLore[term];
    const buttonRef = useRef<HTMLButtonElement>(null);
    const [shown, setShown] = useState<{ rect: DOMRect; pinned: boolean } | null>(null);
    const anchor = shown?.rect ?? null;
    const [popRef, popTop, popHidden] = useFlyoutTop<HTMLDivElement>(anchor);
    const glossId = useId();

    useEffect(() => {
        if (anchor == null) {
            return;
        }

        const onInteract = (event: Event) => {
            const node = event.target instanceof Node ? event.target : null;
            if (node != null && (buttonRef.current?.contains(node) === true || popRef.current?.contains(node) === true)) {
                return;
            }

            setShown(null);
        };
        const onKey = (event: KeyboardEvent) => {
            if (event.key === 'Escape') {
                setShown(null);
                buttonRef.current?.focus();
            }
        };
        const onShift = () => { setShown(null); };

        document.addEventListener('pointerdown', onInteract);
        document.addEventListener('keydown', onKey);
        window.addEventListener('scroll', onShift, { capture: true, passive: true });
        window.addEventListener('resize', onShift);
        return () => {
            document.removeEventListener('pointerdown', onInteract);
            document.removeEventListener('keydown', onKey);
            window.removeEventListener('scroll', onShift, { capture: true });
            window.removeEventListener('resize', onShift);
        };
    }, [anchor, popRef]);

    if (gloss == null) {
        return <>{children}</>;
    }

    const show = (pinned: boolean) => {
        const rect = buttonRef.current?.getBoundingClientRect();
        setShown(rect == null ? null : { rect, pinned });
    };

    return (
        <>
            <button ref={buttonRef} type="button" className={'term-tip' + (className == null ? '' : ' ' + className)}
                aria-describedby={glossId}
                onClick={() => { if (shown?.pinned === true) { setShown(null); } else { show(true); } }}
                onPointerEnter={event => { if (event.pointerType === 'mouse' && shown == null) { show(false); } }}
                onPointerLeave={event => { if (event.pointerType === 'mouse' && shown?.pinned === false) { setShown(null); } }}
                onFocus={event => { if (event.target.matches(':focus-visible')) { show(false); } }}
                onBlur={() => { setShown(null); }}>
                {children}
            </button>
            <span className="term-tip-gloss" id={glossId}>{gloss}</span>
            {anchor != null && createPortal(
                <div ref={popRef} className="term-tip-pop" role="tooltip"
                    style={{
                        top: popTop,
                        left: flyoutLeft(anchor.left, flyoutWidth(POP_WIDTH)),
                        width: flyoutWidth(POP_WIDTH),
                        visibility: popHidden ? 'hidden' : undefined,
                    }}>
                    {gloss}
                </div>,
                document.body)}
        </>
    );
};

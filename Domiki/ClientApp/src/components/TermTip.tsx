import { useEffect, useId, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { Link } from 'react-router-dom';
import { flyoutLeft, flyoutWidth, useFlyoutTop } from '../utils/flyout';
import { termLore } from '../utils/termLore';
import { termArticles, termBuildings, wikiArticleHref, wikiBuildingHref } from '../utils/wikiLinks';

const POP_WIDTH = 260;

interface TermTipProps {
    term: string;
    children: ReactNode;
    className?: string;
    gloss?: string;
}

export const TermTip = ({ term, children, className, gloss: ownGloss }: TermTipProps) => {
    const gloss = ownGloss ?? termLore[term];
    const article = termArticles[term];
    const building = termBuildings[term];
    const moreHref = article != null ? wikiArticleHref(article) : building != null ? wikiBuildingHref(building) : null;
    const buttonRef = useRef<HTMLButtonElement>(null);
    const moreRef = useRef<HTMLAnchorElement>(null);
    const [shown, setShown] = useState<{ rect: DOMRect; pinned: boolean; host: Element } | null>(null);
    const anchor = shown?.rect ?? null;
    const pinned = shown?.pinned === true;
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
                return;
            }

            if (event.key === 'Tab' && !event.shiftKey && pinned && moreRef.current != null && document.activeElement === buttonRef.current) {
                event.preventDefault();
                moreRef.current.focus();
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
    }, [anchor, pinned, popRef]);

    if (gloss == null) {
        return <>{children}</>;
    }

    const show = (asPinned: boolean) => {
        const button = buttonRef.current;
        setShown(button == null ? null : { rect: button.getBoundingClientRect(), pinned: asPinned, host: button.closest('dialog') ?? document.body });
    };

    return (
        <>
            <button ref={buttonRef} type="button" className={'term-tip' + (className == null ? '' : ' ' + className)}
                aria-describedby={glossId}
                onClick={() => { if (pinned) { setShown(null); } else { show(true); } }}
                onPointerEnter={event => { if (event.pointerType === 'mouse' && shown == null) { show(false); } }}
                onPointerLeave={event => { if (event.pointerType === 'mouse' && shown?.pinned === false) { setShown(null); } }}
                onFocus={event => { if (event.target.matches(':focus-visible')) { show(false); } }}
                onBlur={() => { if (!pinned) { setShown(null); } }}>
                {children}
            </button>
            <span className="term-tip-gloss" id={glossId}>{gloss}</span>
            {shown != null && createPortal(
                <div ref={popRef} className={'term-tip-pop' + (pinned ? ' term-tip-pop--pinned' : '')} role={pinned ? undefined : 'tooltip'}
                    style={{
                        top: popTop,
                        left: flyoutLeft(shown.rect.left, flyoutWidth(POP_WIDTH)),
                        width: flyoutWidth(POP_WIDTH),
                        visibility: popHidden ? 'hidden' : undefined,
                    }}>
                    {gloss}
                    {moreHref != null && (
                        <Link ref={moreRef} className="term-tip-more" to={moreHref} aria-describedby={glossId}
                            onClick={() => { setShown(null); }}
                            onKeyDown={event => {
                                if (event.key === 'Tab' && event.shiftKey) {
                                    event.preventDefault();
                                    buttonRef.current?.focus();
                                }
                            }}
                            onBlur={event => {
                                const next = event.relatedTarget;
                                if (next == null || (buttonRef.current?.contains(next) !== true && popRef.current?.contains(next) !== true)) {
                                    setShown(null);
                                }
                            }}>
                            подробнее
                        </Link>
                    )}
                </div>,
                shown.host)}
        </>
    );
};

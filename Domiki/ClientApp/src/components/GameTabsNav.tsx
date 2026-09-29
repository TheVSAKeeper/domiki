import { useEffect, useRef, useState } from 'react';
import type { MouseEvent as ReactMouseEvent, PointerEvent as ReactPointerEvent, ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useElementHeightVar } from '../hooks/useElementHeightVar';
import { BOARD_TAB_KEY, tabPath } from '../utils/gameTabs';
import { MechanicSprite } from './sprites';

interface GameTabEntry {
    key: string;
    label: string;
    icon: ReactNode;
}

interface GameTabsNavProps {
    tabs: GameTabEntry[];
    activeKey: string;
}

export const GameTabsNav = ({ tabs, activeKey }: GameTabsNavProps) => {
    const gameTabsRef = useRef<HTMLElement>(null);
    const listRef = useRef<HTMLUListElement>(null);
    const dragRef = useRef({ active: false, startX: 0, startScroll: 0, moved: false });
    const [tabsOverflow, setTabsOverflow] = useState({ left: false, right: false });

    const navTabs: GameTabEntry[] = [
        { key: BOARD_TAB_KEY, label: 'Домики', icon: <MechanicSprite logicName="domiki" size={32} className="game-tab-ico" aria-hidden="true" /> },
        ...tabs,
    ];

    useElementHeightVar(gameTabsRef, '--game-tabs-height');

    useEffect(() => {
        const tabsEl = listRef.current;
        if (tabsEl == null) {
            return;
        }

        const updateOverflow = () => {
            const max = tabsEl.scrollWidth - tabsEl.clientWidth;
            setTabsOverflow({ left: tabsEl.scrollLeft > 2, right: tabsEl.scrollLeft < max - 2 });
        };
        updateOverflow();
        tabsEl.addEventListener('scroll', updateOverflow, { passive: true });
        const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(updateOverflow);
        observer?.observe(tabsEl);
        return () => {
            tabsEl.removeEventListener('scroll', updateOverflow);
            observer?.disconnect();
        };
    }, [navTabs.length]);

    useEffect(() => {
        const tabsEl = listRef.current;
        const active = tabsEl?.querySelector<HTMLElement>(`#game-tab-${activeKey}`);
        if (tabsEl == null || active == null) {
            return;
        }

        const left = active.getBoundingClientRect().left - tabsEl.getBoundingClientRect().left + tabsEl.scrollLeft;
        const right = left + active.offsetWidth;
        if (left < tabsEl.scrollLeft || right > tabsEl.scrollLeft + tabsEl.clientWidth) {
            tabsEl.scrollLeft = Math.max(0, left - 12);
        }
    }, [activeKey, navTabs.length]);

    const beginDrag = (event: ReactPointerEvent<HTMLElement>) => {
        const tabsEl = listRef.current;
        if (tabsEl == null || event.pointerType === 'touch' || event.button !== 0 || tabsEl.scrollWidth <= tabsEl.clientWidth) {
            return;
        }
        dragRef.current = { active: true, startX: event.clientX, startScroll: tabsEl.scrollLeft, moved: false };
    };

    const moveDrag = (event: ReactPointerEvent<HTMLElement>) => {
        const tabsEl = listRef.current;
        const drag = dragRef.current;
        if (tabsEl == null || !drag.active) {
            return;
        }
        const dx = event.clientX - drag.startX;
        if (!drag.moved && Math.abs(dx) > 4) {
            drag.moved = true;
            tabsEl.setPointerCapture(event.pointerId);
            tabsEl.style.cursor = 'grabbing';
        }
        if (drag.moved) {
            tabsEl.scrollLeft = drag.startScroll - dx;
        }
    };

    const endDrag = () => {
        dragRef.current.active = false;
        if (listRef.current != null) {
            listRef.current.style.cursor = '';
        }
    };

    const suppressDragClick = (event: ReactMouseEvent<HTMLElement>) => {
        if (dragRef.current.moved) {
            event.preventDefault();
            event.stopPropagation();
            dragRef.current.moved = false;
        }
    };

    return (
        <nav className={'game-tabs' + (tabsOverflow.left ? ' game-tabs-overflow-left' : '') + (tabsOverflow.right ? ' game-tabs-overflow-right' : '')}
            ref={gameTabsRef} aria-label="Разделы деревни">
            <span className="game-tabs-affordance game-tabs-affordance-left" aria-hidden="true">‹</span>
            <ul className="game-tabs-list" ref={listRef}
                onPointerDown={beginDrag} onPointerMove={moveDrag} onPointerUp={endDrag} onPointerCancel={endDrag}
                onClickCapture={suppressDragClick}>
                {navTabs.map(tab => {
                    const active = tab.key === activeKey;
                    return (
                        <li key={tab.key} className="game-tabs-item">
                            <Link id={`game-tab-${tab.key}`} to={tabPath(tab.key)}
                                data-game-tab={tab.key}
                                aria-current={active ? 'page' : undefined}
                                className={'game-tab icon-chip-btn' + (active ? ' game-tab-active' : '') + (tab.key === BOARD_TAB_KEY ? ' game-tab-board' : '')}>
                                {tab.icon}
                                {tab.label}
                            </Link>
                        </li>
                    );
                })}
            </ul>
            <span className="game-tabs-affordance game-tabs-affordance-right" aria-hidden="true">›</span>
        </nav>
    );
};

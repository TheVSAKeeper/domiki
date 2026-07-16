import { useEffect, useRef, useState } from 'react';
import type { KeyboardEvent, ReactNode } from 'react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';

interface GameTabEntry {
    key: string;
    label: string;
    icon: ReactNode;
}

interface GameTabsNavProps {
    tabs: GameTabEntry[];
    activeKey: string | undefined;
    onSelect: (key: string) => void;
    onScrollToPanel: () => void;
}

export const GameTabsNav = ({ tabs, activeKey, onSelect, onScrollToPanel }: GameTabsNavProps) => {
    const gameTabsRef = useRef<HTMLDivElement>(null);
    const [tabsOverflow, setTabsOverflow] = useState({ left: false, right: false });

    useEffect(() => {
        const tabsEl = gameTabsRef.current;
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
    }, [tabs.length]);

    useEffect(() => {
        const tabsEl = gameTabsRef.current;
        const active = activeKey == null ? null : tabsEl?.querySelector<HTMLElement>(`#game-tab-${activeKey}`);
        if (tabsEl == null || active == null) {
            return;
        }

        const left = active.offsetLeft;
        const right = left + active.offsetWidth;
        if (left < tabsEl.scrollLeft || right > tabsEl.scrollLeft + tabsEl.clientWidth) {
            tabsEl.scrollTo({ left: Math.max(0, left - 12), behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
        }
    }, [activeKey]);

    const activateTabByKeyboard = (event: KeyboardEvent<HTMLButtonElement>, index: number) => {
        if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
            return;
        }
        event.preventDefault();
        const nextIndex = event.key === 'Home'
            ? 0
            : event.key === 'End'
                ? tabs.length - 1
                : (index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
        const next = tabs[nextIndex];
        if (next != null) {
            onSelect(next.key);
            requestAnimationFrame(() => document.getElementById(`game-tab-${next.key}`)?.focus());
        }
    };

    return (
        <nav className={'game-tabs' + (tabsOverflow.left ? ' game-tabs-overflow-left' : '') + (tabsOverflow.right ? ' game-tabs-overflow-right' : '')}
            ref={gameTabsRef} aria-label="Разделы деревни">
            <button type="button" className="game-tab game-tab-home" onClick={() => { window.scrollTo({ top: 0 }); }}>
                <BuildingIcon className="game-tab-ico" aria-hidden="true" />
                Домики
            </button>
            <div className="game-tabs-list" role="tablist" aria-label="Игровые разделы">
                {tabs.map((tab, index) => {
                    const active = tab.key === activeKey;
                    return (
                        <button type="button" role="tab" key={tab.key} id={`game-tab-${tab.key}`}
                            data-game-tab={tab.key}
                            aria-selected={active}
                            aria-controls="game-tab-panel"
                            tabIndex={active ? 0 : -1}
                            className={'game-tab' + (active ? ' game-tab-active' : '')}
                            onKeyDown={event => activateTabByKeyboard(event, index)}
                            onClick={() => { onSelect(tab.key); onScrollToPanel(); }}>
                            {tab.icon}
                            {tab.label}
                        </button>
                    );
                })}
            </div>
            <span className="game-tabs-affordance" aria-hidden="true">›</span>
        </nav>
    );
};

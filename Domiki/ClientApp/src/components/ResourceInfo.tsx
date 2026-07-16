import { useCallback, useMemo, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import type { DomikTypeDto, ReceiptDto, ResourceTypeDto } from '../types/api';
import { resourceSourceMap } from '../utils/game';
import { resourceLore } from '../utils/resourceLore';
import { ResourceInfoContext } from './resourceInfoContext';
import { ResourceSprite } from './sprites';

const FLYOUT_WIDTH = 260;

interface ResourceInfoProviderProps {
    resourceTypes: ResourceTypeDto[];
    domikTypes: DomikTypeDto[];
    receipts: ReceiptDto[];
    children: ReactNode;
}

export const ResourceInfoProvider = ({ resourceTypes, domikTypes, receipts, children }: ResourceInfoProviderProps) => {
    const sources = useMemo(() => resourceSourceMap(domikTypes, receipts), [domikTypes, receipts]);
    const typeById = useMemo(() => new Map(resourceTypes.map(type => [type.id, type])), [resourceTypes]);
    const [flyout, setFlyout] = useState<{ typeId: number; top: number; left: number } | null>(null);

    const open = useCallback((typeId: number, el: HTMLElement) => {
        const rect = el.getBoundingClientRect();
        const left = Math.max(12, Math.min(rect.left, window.innerWidth - FLYOUT_WIDTH - 12));
        setFlyout({ typeId, top: rect.bottom + 6, left });
    }, []);
    const close = useCallback(() => { setFlyout(null); }, []);
    const value = useMemo(() => ({ open, close }), [open, close]);

    const type = flyout == null ? null : typeById.get(flyout.typeId) ?? null;
    const lore = type == null ? null : resourceLore[type.logicName] ?? null;
    const producers = flyout == null ? [] : sources.get(flyout.typeId) ?? [];

    return (
        <ResourceInfoContext.Provider value={value}>
            {children}
            {flyout != null && type != null && createPortal(
                <div className="res-info-pop pixel-panel" role="tooltip" style={{ top: flyout.top, left: flyout.left, width: FLYOUT_WIDTH }}>
                    <div className="res-info-head">
                        <ResourceSprite logicName={type.logicName} size={40} aria-hidden="true" />
                        <span className="res-info-name">{type.name}</span>
                    </div>
                    {lore != null && <p className="res-info-flavor">{lore.flavor}</p>}
                    {(lore != null || producers.length > 0) &&
                        <dl className="res-info-facts">
                            {lore != null && <><dt>Откуда</dt><dd>{lore.source}</dd></>}
                            {producers.length > 0 && <><dt>Производят</dt><dd>{producers.map(producer => producer.name).join(', ')}</dd></>}
                            {lore != null && <><dt>Зачем</dt><dd>{lore.use}</dd></>}
                        </dl>
                    }
                </div>,
                document.body)}
        </ResourceInfoContext.Provider>
    );
};

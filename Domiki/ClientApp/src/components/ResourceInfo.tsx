import { useCallback, useMemo, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import type { DomikTypeDto, ReceiptDto, ResourceTypeDto } from '../types/api';
import { resourceSourceMap } from '../utils/game';
import { resourceLore } from '../utils/resourceLore';
import { flyoutLeft, flyoutWidth, useFlyoutTop } from '../utils/flyout';
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
    const [flyout, setFlyout] = useState<{ typeId: number; rect: DOMRect } | null>(null);
    const [popRef, popTop, popHidden] = useFlyoutTop<HTMLDivElement>(flyout?.rect ?? null);

    const open = useCallback((typeId: number, el: HTMLElement) => {
        setFlyout({ typeId, rect: el.getBoundingClientRect() });
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
                <div ref={popRef} className="res-info-pop pixel-panel" role="tooltip"
                    style={{
                        top: popTop,
                        left: flyoutLeft(flyout.rect.left, flyoutWidth(FLYOUT_WIDTH)),
                        width: flyoutWidth(FLYOUT_WIDTH),
                        visibility: popHidden ? 'hidden' : undefined,
                    }}>
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

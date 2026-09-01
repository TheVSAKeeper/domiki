import { useMemo, useRef, useState } from 'react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import type { LedgerDto, ResourceDto, ResourceReserveDto, ResourceTypeDto } from '../types/api';
import type { HudDigest, StockEntry } from '../utils/hud';
import { groupStockByDen } from '../utils/hud';
import { useElementHeightVar } from '../hooks/useElementHeightVar';
import { useNarrowScreen } from '../hooks/useNarrowScreen';
import { COIN_RESOURCE_TYPE_ID, GOLD_RESOURCE_TYPE_ID } from '../utils/game';
import { useResourceInfo } from './resourceInfoContext';
import { ResourceSprite } from './sprites';
import '../styles/stock.css';

const CURRENCY_TYPE_IDS = new Set([COIN_RESOURCE_TYPE_ID, GOLD_RESOURCE_TYPE_ID]);

const STACK_LIMIT = 4;

interface StockRow {
    type: ResourceTypeDto;
    value: number;
}

interface StockChipProps {
    row: StockRow;
    reserve: number;
    den: string;
    denLabel: string;
    alarm: string | null;
    focused: boolean;
}

const StockChip = ({ row, reserve, den, denLabel, alarm, focused }: StockChipProps) => {
    const info = useResourceInfo();
    const notes = [denLabel];
    if (alarm != null) {
        notes.push(alarm);
    }
    if (reserve > 0) {
        notes.push(`заповедано ${reserve}`);
    }

    return (
        <div className="stock-chip" data-den={den}
            data-alarm={alarm == null ? 'false' : 'true'}
            data-kept={reserve > 0 ? 'true' : 'false'}
            data-focused={focused ? 'true' : 'false'}
            aria-label={`${row.type.name}: ${row.value}, ${notes.join(', ')}`}
            title={info == null ? row.type.name : undefined}
            onMouseEnter={info == null ? undefined : event => { info.open(row.type.id, event.currentTarget); }}
            onMouseLeave={info?.close}
            onClick={info == null ? undefined : event => { info.toggle(row.type.id, event.currentTarget); }}>
            <ResourceSprite logicName={row.type.logicName} size={32} className="stock-chip-ico" aria-hidden="true" />
            <span className="stock-chip-value">{row.value}</span>
        </div>
    );
};

interface StockRailProps {
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
    digest: HudDigest;
    ledger: LedgerDto | null;
    reserves: ResourceReserveDto[];
    focusTypeIds: number[];
}

export const StockRail = ({ resources, resourceTypes, digest, ledger, reserves, focusTypeIds }: StockRailProps) => {
    const narrow = useNarrowScreen();
    const [wideOpen, setWideOpen] = useState(true);
    const [narrowOpen, setNarrowOpen] = useState(false);
    const railRef = useRef<HTMLElement>(null);

    useElementHeightVar(railRef, '--stock-rail-height');

    const open = narrow ? narrowOpen : wideOpen;

    const typeById = useMemo(() => new Map(resourceTypes.map(type => [type.id, type])), [resourceTypes]);
    const reserveById = useMemo(() => new Map(reserves.map(item => [item.resourceTypeId, item.reserve])), [reserves]);
    const focusedIds = useMemo(() => new Set(focusTypeIds), [focusTypeIds]);
    const shortage = ledger?.shortage ?? null;

    const lackingIds = useMemo(
        () => new Set(digest.blockedBuildings.flatMap(building => building.missing.map(item => item.typeId))),
        [digest.blockedBuildings],
    );

    const dens = useMemo(() => {
        const valueById = new Map(resources.map(resource => [resource.typeId, resource.value]));
        const ids = new Set([...valueById.keys(), ...lackingIds]);
        CURRENCY_TYPE_IDS.forEach(id => ids.delete(id));
        const stock = [...ids]
            .filter(id => (valueById.get(id) ?? 0) > 0 || lackingIds.has(id))
            .map(id => ({ type: typeById.get(id), value: valueById.get(id) ?? 0 }))
            .filter((entry): entry is StockEntry => entry.type != null);
        return groupStockByDen(stock);
    }, [resources, typeById, lackingIds]);

    const yardRows = useMemo(
        () => dens.flatMap(den => den.items.map(row => ({ row, den: den.key, denLabel: den.label }))),
        [dens],
    );

    const kinds = yardRows.filter(item => item.row.value > 0).length;
    const stackRows = yardRows.slice(0, STACK_LIMIT);

    const alarmOf = (row: StockRow) => {
        if (lackingIds.has(row.type.id)) {
            return 'не хватает';
        }
        if (shortage?.resourceTypeId !== row.type.id) {
            return null;
        }
        return shortage.hours <= 0 ? 'на исходе' : `хватит на ${shortage.hours} ч`;
    };

    return (
        <section ref={railRef} className="stock-rail" aria-label="Закрома деревни" data-open={open}>
            <button type="button" className="stock-rail-head" aria-expanded={open}
                onClick={() => { (narrow ? setNarrowOpen : setWideOpen)(prev => !prev); }}>
                <span className="stock-rail-title">Закрома</span>
                {!open &&
                    <span className="stock-rail-stack" aria-hidden="true">
                        {stackRows.map(item =>
                            <ResourceSprite key={item.row.type.id} logicName={item.row.type.logicName} size={24} />,
                        )}
                    </span>
                }
                <span className="stock-rail-tally">{kinds}</span>
                {open
                    ? <ChevronDownIcon className="stock-rail-caret" aria-hidden="true" />
                    : <ChevronUpIcon className="stock-rail-caret" aria-hidden="true" />
                }
            </button>

            {open &&
                <div className="stock-rail-body">
                    {yardRows.length === 0
                        ? <p className="stock-rail-empty">Закрома пусты.</p>
                        : <div className="stock-yard">
                            {yardRows.map(item =>
                                <StockChip key={item.row.type.id} row={item.row} den={item.den} denLabel={item.denLabel}
                                    reserve={reserveById.get(item.row.type.id) ?? 0}
                                    alarm={alarmOf(item.row)}
                                    focused={focusedIds.has(item.row.type.id)} />,
                            )}
                        </div>
                    }
                </div>
            }
        </section>
    );
};

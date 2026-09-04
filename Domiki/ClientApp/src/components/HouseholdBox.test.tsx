import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { LedgerDto, ResourceReserveDto, ResourceTypeDto } from '../types/api';
import type { HudDigest } from '../utils/hud';
import { HouseholdBox } from './HouseholdBox';

const resourceTypes: ResourceTypeDto[] = [
    { id: 200, name: 'Глина', logicName: 'clay', marketValue: 1, isFood: false },
];

const emptyDigest: HudDigest = {
    idleDomiks: 0,
    soonestOrder: null,
    expeditionsBack: 0,
    workersResting: 0,
    workersSick: 0,
    idleBuildings: [],
    blockedBuildings: [],
    restingEarliest: null,
    sickEarliest: null,
    upgradeableBuildings: [],
    standingShifts: [],
    workersFree: 3,
    workersAway: 0,
    handsFreeEarliest: null,
    runningShifts: 0,
    runningEarliest: null,
};

const NOW = Date.parse('2026-07-25T12:00:00.000Z');

const renderBox = (digest: HudDigest, ledger: LedgerDto | null = null, reserves: ResourceReserveDto[] = [], intentReserves: ResourceReserveDto[] = []) => {
    const onSelectDomik = vi.fn();
    const onOpenTab = vi.fn();
    const onToggleRepeat = vi.fn();
    const onSetReserve = vi.fn();
    render(<HouseholdBox digest={digest} resourceTypes={resourceTypes} resources={[{ typeId: 200, value: 240 }]}
        reserves={reserves} intentReserves={intentReserves} ledger={ledger} now={NOW} onSetReserve={onSetReserve}
        onSelectDomik={onSelectDomik} onOpenTab={onOpenTab} onToggleRepeat={onToggleRepeat} />);
    return { onSelectDomik, onOpenTab, onToggleRepeat, onSetReserve };
};

describe('HouseholdBox empty states', () => {
    it('renders the calm line when nothing needs attention and no наряды stand', () => {
        renderBox(emptyDigest);

        expect(screen.getByText('В деревне всё при деле – староста доволен.')).toBeInTheDocument();
        expect(screen.getByText('Нарядов не поставлено – каждая смена запускается вручную.')).toBeInTheDocument();
    });
});

describe('HouseholdBox idle building row', () => {
    it('renders the building and navigates to it without starting any work', () => {
        const digest: HudDigest = {
            ...emptyDigest,
            idleBuildings: [{ domikId: 7, typeId: 10, logicName: 'forge', displayName: 'Кузница', level: 1 }],
        };
        const { onSelectDomik, onToggleRepeat } = renderBox(digest);

        fireEvent.click(screen.getByRole('button', { name: /Кузница/ }));

        expect(onSelectDomik).toHaveBeenCalledWith(7, 'forge');
        expect(onToggleRepeat).not.toHaveBeenCalled();
    });
});

describe('HouseholdBox idle building chip expander', () => {
    it('shows 5 chips by default and reveals the rest via «ещё N», toggling aria-expanded', () => {
        const idleBuildings = Array.from({ length: 7 }, (_, i) => ({
            domikId: i + 1, typeId: 10, logicName: 'forge', displayName: `Кузница ${i + 1}`, level: 1,
        }));
        const digest: HudDigest = { ...emptyDigest, idleBuildings };
        renderBox(digest);

        expect(screen.getAllByRole('button', { name: /^Кузница \d$/ })).toHaveLength(5);
        const moreButton = screen.getByRole('button', { name: /ещё 2/ });
        expect(moreButton).toHaveAttribute('aria-expanded', 'false');

        fireEvent.click(moreButton);

        expect(screen.getAllByRole('button', { name: /^Кузница \d$/ })).toHaveLength(7);
        expect(screen.getByRole('button', { name: /свернуть/ })).toHaveAttribute('aria-expanded', 'true');
    });
});

describe('HouseholdBox idle buildings without free hands', () => {
    const idleBuildings = [{ domikId: 7, typeId: 10, logicName: 'forge', displayName: 'Кузница', level: 1 }];

    it('replaces the idle address list with a calm line when nobody is free', () => {
        const digest: HudDigest = {
            ...emptyDigest, idleBuildings, workersFree: 0, handsFreeEarliest: '2026-07-25T12:40:00.000Z',
        };
        renderBox(digest);

        expect(screen.getByText(/Свободных рук нет/)).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: /Кузница/ })).not.toBeInTheDocument();
    });

    it('blames the beds when the hands are missing only because of отход', () => {
        renderBox({ ...emptyDigest, idleBuildings, workersFree: 0, workersAway: 2 });

        expect(screen.getByText(/Свободных рук нет/).textContent).toContain('2 трудяги в отходе – не хватает коек');
    });

    it('keeps the idle address list while at least one трудяга is free', () => {
        renderBox({ ...emptyDigest, idleBuildings, workersFree: 1 });

        expect(screen.queryByText(/Свободных рук нет/)).not.toBeInTheDocument();
        expect(screen.getByRole('button', { name: /Кузница/ })).toBeInTheDocument();
    });
});

describe('HouseholdBox running shifts summary', () => {
    it('sums the running shifts into one row instead of listing them', () => {
        renderBox({ ...emptyDigest, runningShifts: 4, runningEarliest: '2026-07-25T12:20:00.000Z' });

        expect(screen.getByText(/В работе 4 смены, ближайшая поспеет в/)).toBeInTheDocument();
    });
});

describe('HouseholdBox blocked building row', () => {
    it('names the missing resource and still navigates on click without lifting any наряд', () => {
        const digest: HudDigest = {
            ...emptyDigest,
            blockedBuildings: [{ domikId: 3, typeId: 10, logicName: 'forge', displayName: 'Гончарня', level: 1, missing: [{ typeId: 200, value: 4 }] }],
        };
        const { onSelectDomik, onToggleRepeat } = renderBox(digest);

        expect(screen.getByText(/Гончарня стоит:/)).toBeInTheDocument();
        expect(screen.getByText('не хватает')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: /Гончарня стоит/ }));
        expect(onSelectDomik).toHaveBeenCalledWith(3, 'forge');
        expect(onToggleRepeat).not.toHaveBeenCalled();
    });
});

describe('HouseholdBox blocked building cap', () => {
    it('caps blocked rows at 3 and reveals the rest via «ещё N»', () => {
        const blockedBuildings = Array.from({ length: 5 }, (_, i) => ({
            domikId: i + 1, typeId: 10, logicName: 'forge', displayName: `Домик ${i + 1}`, level: 1,
            missing: [{ typeId: 200, value: 1 }],
        }));
        const digest: HudDigest = { ...emptyDigest, blockedBuildings };
        renderBox(digest);

        expect(screen.getAllByRole('button', { name: /стоит: не хватает/ })).toHaveLength(3);
        const moreButton = screen.getByRole('button', { name: 'ещё 2 постройки стоят без припасов' });
        expect(moreButton).toHaveAttribute('aria-expanded', 'false');

        fireEvent.click(moreButton);

        expect(screen.getAllByRole('button', { name: /стоит: не хватает/ })).toHaveLength(5);
        expect(screen.getByRole('button', { name: /свернуть/ })).toHaveAttribute('aria-expanded', 'true');
    });
});

describe('HouseholdBox наряды block', () => {
    const shiftDigest: HudDigest = {
        ...emptyDigest,
        standingShifts: [{
            manufactureId: 5, domikId: 3, domikLogicName: 'pottery', domikName: 'Гончарня',
            receiptId: 2, receiptName: 'Обжечь кирпич', finishDate: '2026-07-25T15:40:00.000Z', starving: false,
            measure: null, inputTypeIds: [200],
        }],
    };

    it('lets the player lift a standing наряд', () => {
        const { onToggleRepeat, onSelectDomik } = renderBox(shiftDigest);

        expect(screen.getByText(/Наряд: Обжечь кирпич · Гончарня · снова в/)).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: 'Снять наряд' }));
        expect(onToggleRepeat).toHaveBeenCalledWith(5, false);
        expect(onSelectDomik).not.toHaveBeenCalled();
    });

    it('warns that the наряд has nothing left for the next round', () => {
        const digest: HudDigest = {
            ...shiftDigest,
            standingShifts: shiftDigest.standingShifts.map(shift => ({ ...shift, starving: true })),
        };
        renderBox(digest);

        expect(screen.getByText('Припасов на следующий круг нет – и никто их сейчас не делает.')).toBeInTheDocument();
    });

    it('navigates to the building of the наряд without lifting it', () => {
        const { onSelectDomik, onToggleRepeat } = renderBox(shiftDigest);

        fireEvent.click(screen.getByRole('button', { name: /Наряд: Обжечь кирпич/ }));

        expect(onSelectDomik).toHaveBeenCalledWith(3, 'pottery');
        expect(onToggleRepeat).not.toHaveBeenCalled();
    });
});

describe('HouseholdBox счётная книга', () => {
    const ledger: LedgerDto = {
        level: 1,
        hasEntries: true,
        flows: [{ resourceTypeId: 200, gained: 60, spent: 20 }],
        shortage: { resourceTypeId: 200, hours: 6 },
        idlePercent: 41,
    };

    it('stays closed while the player has no Изба старосты', () => {
        renderBox(emptyDigest);

        expect(screen.queryByText('Счётная книга')).not.toBeInTheDocument();
    });

    it('shows the net flow, the first shortage and the idle share', () => {
        renderBox(emptyDigest, ledger);

        expect(screen.getByText('глина +40')).toBeInTheDocument();
        expect(screen.getByText('Глина: хватит на 6 ч при нынешнем расходе')).toBeInTheDocument();
        expect(screen.getByText('41 % суток')).toBeInTheDocument();
    });

    it('calls a spent resource that ran out already', () => {
        renderBox(emptyDigest, { ...ledger, shortage: { resourceTypeId: 200, hours: 0 } });

        expect(screen.getByText('Глина: уже на исходе')).toBeInTheDocument();
    });

    it.each([
        ['книга только заведена', 41, 'Книга только заведена – староста считает с этого часа.'],
        ['двор простоял сутки', 100, 'За сутки ни прихода, ни расхода – двор стоял.'],
    ])('tells %s apart by the idle share', (_case, idlePercent, expected) => {
        renderBox(emptyDigest, { ...ledger, hasEntries: false, flows: [], shortage: null, idlePercent });

        expect(screen.getByText(expected)).toBeInTheDocument();
        expect(screen.getByText('Ничего не убывает – припасов хватает на всё, что стоит нарядом.')).toBeInTheDocument();
    });
});

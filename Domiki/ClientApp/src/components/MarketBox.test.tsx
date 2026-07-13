import { render, screen, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { MarketStateDto, ResourceTypeDto } from '../types/api';
import { MarketBox } from './MarketBox';

const resourceTypes: ResourceTypeDto[] = [
    { id: 1, name: 'Монеты', logicName: 'coins', marketValue: 1 },
    { id: 2, name: 'Дерево', logicName: 'wood', marketValue: 2 },
];

const market: MarketStateDto = {
    lots: [],
    myLots: [],
    buildingLevel: 1,
    commissionRate: 0.1,
    commissionMin: 1,
    nextCommissionRate: null,
    maxLots: 1,
};

const lot = (id: number, wantResourceTypeId: number, wantValue: number): MarketStateDto['lots'][number] => ({
    id,
    sellerId: id,
    sellerVillageName: 'Соседи',
    sellerCrestIcon: 0,
    sellerCrestColor: 0,
    giveResourceTypeId: 2,
    giveValue: 10,
    wantResourceTypeId,
    wantValue,
    commissionCoins: 0,
    expireDate: '2999-01-01T00:00:00Z',
});

describe('MarketBox', () => {
    it('names resource radio options after their resources', () => {
        render(
            <MarketBox
                market={market}
                resourceTypes={resourceTypes}
                resources={[]}
                now={0}
                onPost={vi.fn()}
                onAccept={vi.fn()}
                onCancel={vi.fn()}
            />,
        );

        const givePicker = screen.getByRole('radiogroup', { name: 'Ресурс, который даю' });
        expect(within(givePicker).getByRole('radio', { name: 'Монеты' })).toBeChecked();
        expect(within(givePicker).getByRole('radio', { name: 'Дерево' })).not.toBeChecked();
    });

    it('blocks accepting a lot the player cannot pay for and names the shortfall', () => {
        render(
            <MarketBox
                market={{ ...market, lots: [lot(1, 1, 5)] }}
                resourceTypes={resourceTypes}
                resources={[{ typeId: 1, value: 3 }]}
                now={0}
                onPost={vi.fn()}
                onAccept={vi.fn()}
                onCancel={vi.fn()}
            />,
        );

        expect(screen.getByRole('button', { name: /Принять/ })).toBeDisabled();
        expect(screen.getByText(/не хватает/)).toBeInTheDocument();
    });

    it('surfaces affordable lots first and enables their accept button', () => {
        render(
            <MarketBox
                market={{ ...market, lots: [lot(1, 1, 5), lot(2, 2, 2)] }}
                resourceTypes={resourceTypes}
                resources={[{ typeId: 1, value: 3 }, { typeId: 2, value: 10 }]}
                now={0}
                onPost={vi.fn()}
                onAccept={vi.fn()}
                onCancel={vi.fn()}
            />,
        );

        const accepts = screen.getAllByRole('button', { name: /Принять/ });
        expect(accepts[0]).toBeEnabled();
        expect(accepts[1]).toBeDisabled();
        expect(screen.getByText(/по карману$/)).toBeInTheDocument();
    });
});

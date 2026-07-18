import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { MarketStateDto, ResourceTypeDto } from '../types/api';
import { MarketBox } from './MarketBox';

const resourceTypes: ResourceTypeDto[] = [
    { id: 1, name: 'Монеты', logicName: 'coins', marketValue: 1 },
    { id: 2, name: 'Дерево', logicName: 'wood', marketValue: 2 },
    { id: 5, name: 'Золото', logicName: 'gold', marketValue: 10 },
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

const lot = (id: number, wantResourceTypeId: number, wantValue: number, kind = 1): MarketStateDto['lots'][number] => ({
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
    kind,
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

    it('posts a buy lot paying gold and wanting a non-currency resource', () => {
        const onPost = vi.fn();
        render(
            <MarketBox
                market={market}
                resourceTypes={resourceTypes}
                resources={[{ typeId: 5, value: 20 }, { typeId: 1, value: 20 }]}
                now={0}
                onPost={onPost}
                onAccept={vi.fn()}
                onCancel={vi.fn()}
            />,
        );

        fireEvent.click(screen.getByRole('radio', { name: 'Куплю' }));
        expect(screen.getByText('плачу золотом')).toBeInTheDocument();
        const wantPicker = screen.getByRole('radiogroup', { name: 'Ресурс, который покупаю' });
        expect(within(wantPicker).queryByRole('radio', { name: 'Золото' })).not.toBeInTheDocument();
        expect(within(wantPicker).getByRole('radio', { name: 'Дерево' })).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: /Выставить лот/ }));
        expect(onPost).toHaveBeenCalledWith(2, 5, expect.any(Number), 2, expect.any(Number));
    });

    it('marks a buy lot with the Куплю badge among other lots', () => {
        const { container } = render(
            <MarketBox
                market={{ ...market, lots: [lot(1, 2, 5, 2)] }}
                resourceTypes={resourceTypes}
                resources={[{ typeId: 2, value: 10 }]}
                now={0}
                onPost={vi.fn()}
                onAccept={vi.fn()}
                onCancel={vi.fn()}
            />,
        );

        const badge = container.querySelector('.market-lot-badge-buy');
        expect(badge).not.toBeNull();
        expect(badge).toHaveTextContent('Куплю');
    });

    it('filters other lots down to buy requests only', () => {
        const { container } = render(
            <MarketBox
                market={{ ...market, lots: [lot(1, 2, 5, 1), lot(2, 2, 5, 2)] }}
                resourceTypes={resourceTypes}
                resources={[{ typeId: 2, value: 10 }]}
                now={0}
                onPost={vi.fn()}
                onAccept={vi.fn()}
                onCancel={vi.fn()}
            />,
        );

        expect(container.querySelectorAll('.market-lot-badge')).toHaveLength(2);

        fireEvent.click(screen.getByRole('radio', { name: 'Покупка' }));
        expect(container.querySelectorAll('.market-lot-badge-sell')).toHaveLength(0);
        expect(container.querySelectorAll('.market-lot-badge-buy')).toHaveLength(1);
    });
});

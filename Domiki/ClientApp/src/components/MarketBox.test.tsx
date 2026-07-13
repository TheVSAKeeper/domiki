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
});

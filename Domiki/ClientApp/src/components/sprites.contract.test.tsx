import { render } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import {
    AbstractSprite,
    DecorSprite,
    DomikSprite,
    ResourceSprite,
} from './sprites';

describe('sprite registry contract', () => {
    it('renders a registered logicName with its runtime state', () => {
        const { container } = render(<DomikSprite logicName="forge" level={3} working />);

        expect(container.querySelector('svg')).toHaveAttribute('data-level', '3');
        expect(container.querySelector('svg')).toHaveAttribute('data-working', 'true');
    });

    it('keeps every dedicated decor added to the registry renderable', () => {
        const { container } = render(
            <>
                <DecorSprite logicName="brick_arch" />
                <DecorSprite logicName="lantern" />
                <DecorSprite logicName="carved_gate" />
                <DecorSprite logicName="crane_well" />
                <DecorSprite logicName="gazebo" />
                <DecorSprite logicName="carp_pond" />
            </>,
        );

        expect(container.querySelectorAll('svg')).toHaveLength(6);
    });

    it('renders the cheese resource and tavern from the registry', () => {
        const { container } = render(<><ResourceSprite logicName="cheese" /><DomikSprite logicName="tavern" /></>);

        expect(container.querySelectorAll('svg')).toHaveLength(2);
    });

    it('renders nothing for an unknown strict-registry logicName', () => {
        const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
        const { container: domik } = render(<DomikSprite logicName="missing_domik" />);
        const { container: abstract } = render(<AbstractSprite logicName="missing_abstract" />);
        const { container: decor } = render(<DecorSprite logicName="missing_decor" />);

        expect(domik).toBeEmptyDOMElement();
        expect(abstract).toBeEmptyDOMElement();
        expect(decor).toBeEmptyDOMElement();
        expect(warn).toHaveBeenCalledWith('[sprites] Unknown domik logicName: "missing_domik"');
        expect(warn).toHaveBeenCalledWith('[sprites] Unknown abstract logicName: "missing_abstract"');
        expect(warn).toHaveBeenCalledWith('[sprites] Unknown decor logicName: "missing_decor"');
        warn.mockRestore();
    });
});

describe('inline sprite accessibility contract', () => {
    it('hides an unlabeled decorative sprite from assistive technologies', () => {
        const { container } = render(<ResourceSprite logicName="coin" />);
        const sprite = container.querySelector('svg');

        expect(sprite).toHaveAttribute('aria-hidden', 'true');
        expect(sprite).not.toHaveAttribute('role');
        expect(sprite).toHaveAttribute('focusable', 'false');
    });

    it('exposes an explicitly labeled semantic sprite and removes source metadata', () => {
        const { container } = render(<ResourceSprite logicName="coin" aria-label="Монеты" />);
        const sprite = container.querySelector('svg');

        expect(sprite).toHaveAttribute('role', 'img');
        expect(sprite).toHaveAccessibleName('Монеты');
        expect(sprite).not.toHaveAttribute('aria-hidden');
        expect(sprite).not.toHaveAttribute('aria-labelledby');
        expect(sprite?.querySelector('title, desc')).not.toBeInTheDocument();
    });
});

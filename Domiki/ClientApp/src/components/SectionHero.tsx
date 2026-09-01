import { useState } from 'react';
import type { ReactNode } from 'react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import '../styles/section-hero.css';

interface SectionHeroProps {
    className: string;
    children: ReactNode;
}

export const SectionHero = ({ className, children }: SectionHeroProps) => {
    const [open, setOpen] = useState(false);

    return (
        <header className={`sec-hero ${className}` + (open ? ' sec-hero--open' : '')}>
            {children}
            <button type="button" className="sec-hero-fold" aria-expanded={open}
                aria-label={open ? 'Свернуть описание раздела' : 'Развернуть описание раздела'}
                onClick={() => { setOpen(value => !value); }}>
                <ChevronDownIcon className="sec-hero-fold-ico" aria-hidden="true" />
            </button>
        </header>
    );
};

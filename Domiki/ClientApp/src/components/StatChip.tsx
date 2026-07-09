import type { ReactNode } from 'react';

interface StatChipProps {
    icon: ReactNode;
    children: ReactNode;
    title?: string;
    tone?: 'default' | 'gold';
}

export const StatChip = ({ icon, children, title, tone = 'default' }: StatChipProps) => (
    <span className={'stat-chip' + (tone === 'gold' ? ' stat-chip-gold' : '')} title={title}>
        {icon}
        <span className="stat-chip-value">{children}</span>
    </span>
);

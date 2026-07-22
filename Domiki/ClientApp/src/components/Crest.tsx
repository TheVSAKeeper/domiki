import { DEFAULT_VILLAGE_ICON, VILLAGE_CREST_COLORS, VILLAGE_CREST_ICONS, VILLAGE_CREST_INKS } from '../constants/village';

interface CrestProps {
    icon: number;
    color: number;
    className?: string;
}

export const Crest = ({ icon, color, className }: CrestProps) => {
    const Icon = VILLAGE_CREST_ICONS[icon] ?? DEFAULT_VILLAGE_ICON;
    const backgroundColor = VILLAGE_CREST_COLORS[color] ?? VILLAGE_CREST_COLORS[0];
    const ink = VILLAGE_CREST_INKS[color] ?? VILLAGE_CREST_INKS[0];

    return (
        <span className={className == null ? 'crest-badge' : `crest-badge ${className}`} style={{ backgroundColor, color: ink }}>
            <Icon className="crest-ico" aria-hidden="true" />
        </span>
    );
};

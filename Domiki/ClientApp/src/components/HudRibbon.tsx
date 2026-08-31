import type { ReactNode } from 'react';
import ClockIcon from 'pixelarticons/svg/clock.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import type { HudDigest } from '../utils/hud';
import { pluralRu } from '../utils/plural';
import { MechanicSprite, NeighborSprite } from './sprites';

interface HudRibbonProps {
    digest: HudDigest;
    onOpenTab: (tab: string) => void;
}

const RIBBON_LIMIT = 2;

export const HudRibbon = ({ digest, onOpenTab }: HudRibbonProps) => {
    const items: { key: string; text: string; tab: string; node: ReactNode }[] = [];

    if (digest.soonestOrder != null) {
        items.push({
            key: 'order',
            text: `заказ ${digest.soonestOrder.neighborName}, ${digest.soonestOrder.hours}ч`,
            tab: 'orders',
            node: (
                <button type="button" className="hud-ribbon-item hud-ribbon-order"
                    onClick={() => { onOpenTab('orders'); }}
                    title={`Ближайший заказ истекает через ${digest.soonestOrder.hours} ч`}>
                    <NeighborSprite logicName={digest.soonestOrder.neighborLogicName} size={24} className="hud-ribbon-ico" aria-hidden="true" />
                    <span className="hud-ribbon-label">заказ {digest.soonestOrder.neighborName}</span>
                    <ClockIcon className="hud-ribbon-clock" aria-hidden="true" />{digest.soonestOrder.hours}ч
                </button>
            ),
        });
    }

    if (digest.expeditionsBack > 0) {
        items.push({
            key: 'expeditions',
            text: digest.expeditionsBack === 1 ? 'поход вернулся' : `${digest.expeditionsBack} похода вернулись`,
            tab: 'expeditions',
            node: (
                <button type="button" className="hud-ribbon-item"
                    onClick={() => { onOpenTab('expeditions'); }} title="Поход вернулся – загляните за добычей">
                    <MechanicSprite logicName="expeditions" size={24} className="hud-ribbon-ico" aria-hidden="true" />
                    {digest.expeditionsBack > 1 && <b>{digest.expeditionsBack}</b>}
                    <span className="hud-ribbon-label">
                        {digest.expeditionsBack === 1
                            ? 'поход вернулся'
                            : `${pluralRu(digest.expeditionsBack, 'поход', 'похода', 'походов')} вернулись`}
                    </span>
                </button>
            ),
        });
    }

    if (digest.idleDomiks > 0 && digest.workersFree > 0) {
        items.push({
            key: 'idle',
            text: `${digest.idleDomiks} ${pluralRu(digest.idleDomiks, 'домик', 'домика', 'домиков')} в простое`,
            tab: 'household',
            node: (
                <button type="button" className="hud-ribbon-item"
                    onClick={() => { onOpenTab('household'); }}
                    title="Домики без запущенного производства – докиньте входы и запустите смену">
                    <HomeIcon className="hud-ribbon-ico" aria-hidden="true" />
                    <b>{digest.idleDomiks}</b>
                    <span className="hud-ribbon-label">в простое</span>
                </button>
            ),
        });
    }

    if (items.length === 0) {
        return null;
    }

    const shown = items.slice(0, RIBBON_LIMIT);
    const hidden = items.slice(RIBBON_LIMIT);

    return (
        <div className="hud-ribbon">
            {shown.map(item => <span key={item.key} className="hud-ribbon-slot">{item.node}</span>)}
            {hidden.length > 0 &&
                <button type="button" className="hud-ribbon-more" title={hidden.map(item => item.text).join(', ')}
                    onClick={() => { onOpenTab(hidden[0]?.tab ?? 'household'); }}>
                    +{hidden.length}
                </button>}
        </div>
    );
};

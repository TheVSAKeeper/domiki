import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import type { DomikTypeDto, PlodderCount, ResourceDto, ResourceTypeDto, VillageLevelDto, WeatherStateDto } from '../types/api';
import { COIN_RESOURCE_TYPE_ID, strongestWeatherEffect } from '../utils/game';
import { remainingSeconds } from '../utils/time';
import { DomikSprite, MechanicSprite, WeatherSprite } from './sprites';
import { HudResource } from './HudResource';
import { ProgressBar } from './ProgressBar';
import { GiftVisitDots } from './GiftVisitDots';
import { ChangelogButton } from './ChangelogButton';

interface VillageHudProps {
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
    domikTypes: DomikTypeDto[];
    plodder: PlodderCount;
    villageLevel: VillageLevelDto | null;
    weather: WeatherStateDto | null;
    now: number;
    onStickyOffsetChange: (offset: number) => void;
}

export const VillageHud = ({ resources, resourceTypes, domikTypes, plodder, villageLevel, weather, now, onStickyOffsetChange }: VillageHudProps) => {
    const hudSentinelRef = useRef<HTMLDivElement>(null);
    const hudRef = useRef<HTMLElement>(null);
    const [hudAway, setHudAway] = useState(false);
    const [hudPinnedOpen, setHudPinnedOpen] = useState(false);
    const [levelFlyout, setLevelFlyout] = useState<{ top: number; left: number; width: number } | null>(null);
    const villageLevelRef = useRef<HTMLDivElement>(null);
    const openLevelFlyout = () => {
        const rect = villageLevelRef.current?.getBoundingClientRect();
        if (rect != null) {
            setLevelFlyout({ top: rect.bottom + 6, left: rect.left, width: rect.width });
        }
    };
    const closeLevelFlyout = () => setLevelFlyout(null);

    useEffect(() => {
        const sentinel = hudSentinelRef.current;
        if (sentinel == null) {
            return;
        }

        const observer = new IntersectionObserver(entries => {
            const entry = entries[0];
            if (entry == null) {
                return;
            }

            setHudAway(!entry.isIntersecting);
            if (entry.isIntersecting) {
                setHudPinnedOpen(false);
            }
        });

        observer.observe(sentinel);
        return () => { observer.disconnect(); };
    }, []);

    useEffect(() => {
        const hud = hudRef.current;
        if (hud == null) {
            return;
        }
        const updateOffset = () => onStickyOffsetChange(hud.offsetHeight + 16);
        updateOffset();
        const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(updateOffset);
        observer?.observe(hud);
        return () => { observer?.disconnect(); };
    }, [onStickyOffsetChange]);

    const hudCollapsed = hudAway && !hudPinnedOpen;
    const coinType = resourceTypes.find(t => t.id === COIN_RESOURCE_TYPE_ID);
    const coinValue = resources.find(r => r.typeId === COIN_RESOURCE_TYPE_ID)?.value;
    const currentWeather = weather?.current ?? null;
    const nextGoal = villageLevel?.unlocks.find((unlock): unlock is typeof unlock & { level: number } => !unlock.unlocked && unlock.level != null);
    const effectChips = currentWeather?.effects.filter(effect => effect.outputPercent !== 100) ?? [];

    return (
        <>
            <div className="hud-sentinel" ref={hudSentinelRef} aria-hidden="true" />
            <header ref={hudRef} className={'hud pixel-panel' + (hudCollapsed ? ' hud-collapsed' : '')}>
                <button type="button" className="hud-compact" onClick={() => { setHudPinnedOpen(true); }} title="Развернуть панель">
                    {coinType != null && coinValue != null && <HudResource resourceType={coinType} value={coinValue} />}
                    <div className="resource-box">
                        <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                        <span className="resource-value">{plodder.free}/{plodder.max}</span>
                    </div>
                    {villageLevel != null &&
                        <span className="resource-box">
                            <MechanicSprite logicName="obzhitost" size={24} className="village-level-ico" aria-hidden="true" />
                            <span className="resource-value">{villageLevel.level}</span>
                        </span>}
                    {currentWeather != null && <WeatherSprite logicName={currentWeather.logicName} className="weather-ico" aria-hidden="true" />}
                    <ChevronDownIcon className="btn-ico hud-compact-caret" aria-hidden="true" />
                </button>
                <div className="resources">
                    {resourceTypes.length > 0 &&
                        resources.map(resource => {
                            const resourceType = resourceTypes.find(x => x.id === resource.typeId);
                            if (resourceType == null) {
                                return null;
                            }

                            return <HudResource key={resource.typeId} resourceType={resourceType} value={resource.value} />;
                        })
                    }
                </div>
                {domikTypes.length > 0 &&
                    <div className="resource-box hud-plodder" title="Трудяги">
                        <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                        <span className="resource-value">{plodder.free}/{plodder.max}</span>
                    </div>
                }
                {villageLevel != null &&
                    <>
                        <div className="village-level" ref={villageLevelRef}
                            onMouseEnter={openLevelFlyout} onMouseLeave={closeLevelFlyout}
                            onFocus={openLevelFlyout} onBlur={closeLevelFlyout}>
                            <button type="button" className="village-level-box"
                                title={`Постройки ${villageLevel.buildings}, жители ${villageLevel.residents}, репутация ${villageLevel.reputation}, уют ${villageLevel.comfort}`}>
                                <MechanicSprite logicName="obzhitost" size={24} className="village-level-ico" aria-hidden="true" />
                                <span className="village-level-label">Обжитость</span>
                                <span className="village-level-value">{villageLevel.level}</span>
                            </button>
                        </div>
                        {levelFlyout != null && createPortal(
                            <div className="village-level-flyout" style={{ top: levelFlyout.top, left: levelFlyout.left, width: levelFlyout.width }}>
                                <span>Постройки: {villageLevel.buildings}</span>
                                <span>Жители: {villageLevel.residents}</span>
                                <span>Репутация: {villageLevel.reputation}</span>
                                <span>Уют: {villageLevel.comfort}</span>
                                {[
                                    ...villageLevel.unlocks.filter(unlock => !unlock.unlocked && unlock.level != null).slice(0, 3),
                                    ...villageLevel.unlocks.filter(unlock => !unlock.unlocked && unlock.level == null),
                                ].map(unlock => (
                                    <span key={`${unlock.label}-${unlock.level ?? unlock.requirement}`} className="village-level-next">
                                        {unlock.label} – {unlock.level != null ? `при обжитости ${unlock.level}` : unlock.requirement}
                                    </span>
                                ))}
                            </div>,
                            document.body)}
                        {villageLevel.visitsSinceBigGift > 0 &&
                            <div className="hud-gift-strip" role="img"
                                aria-label={`До большого гостинца визитов: ${7 - villageLevel.visitsSinceBigGift}`}
                                title={`До большого гостинца визитов: ${7 - villageLevel.visitsSinceBigGift}`}>
                                <GiftVisitDots visitIndex={villageLevel.visitsSinceBigGift} />
                            </div>}
                        {nextGoal != null &&
                            <div className="hud-goal"
                                title={`Откроется при обжитости ${nextGoal.level}: ${nextGoal.label}`}>
                                <LockIcon className="hud-goal-ico" aria-hidden="true" />
                                <span className="hud-goal-label">{nextGoal.label}</span>
                                <ProgressBar value={villageLevel.level} max={nextGoal.level}
                                    label={`${villageLevel.level}/${nextGoal.level}`} />
                            </div>}
                    </>
                }
                {weather != null && currentWeather != null &&
                    <div className="weather-strip" title={currentWeather.weatherName}>
                        <WeatherSprite logicName={currentWeather.logicName} className="weather-ico" aria-hidden="true" />
                        <span className="weather-name">{currentWeather.weatherName}</span>
                        {effectChips.length > 0 &&
                            <div className="weather-effects">
                                {effectChips.map(effect => {
                                    const domikType = domikTypes.find(type => type.id === effect.domikTypeId);
                                    if (domikType == null) {
                                        return null;
                                    }

                                    const delta = effect.outputPercent - 100;
                                    const buff = delta > 0;
                                    return (
                                        <span key={effect.domikTypeId}
                                            className={'weather-effect' + (buff ? ' weather-effect-buff' : ' weather-effect-nerf')}
                                            title={`${domikType.name}: ${buff ? "+" : ""}${delta}% выход`}>
                                            <DomikSprite className="weather-effect-ico" logicName={domikType.logicName} />
                                            {buff ? '+' : ''}{delta}%
                                        </span>
                                    );
                                })}
                            </div>
                        }
                        {weather.forecast.length > 0 &&
                        <div className="weather-forecast">
                            <span className="weather-forecast-label">далее</span>
                            {weather.forecast.map(period => {
                                const hoursAhead = Math.max(1, Math.round(remainingSeconds(period.startDate, now) / 3600));
                                const hint = strongestWeatherEffect(period.effects, domikTypes);
                                return (
                                    <span key={period.startDate} className="weather-chip" title={period.weatherName}>
                                        <WeatherSprite logicName={period.logicName} size={24} className="weather-chip-ico" aria-hidden="true" />
                                        через {hoursAhead}ч
                                        {hint != null &&
                                            <span className={'weather-effect' + (hint.delta > 0 ? ' weather-effect-buff' : ' weather-effect-nerf')}
                                                title={`${hint.domikType.name}: ${hint.delta > 0 ? '+' : ''}${hint.delta}% выход`}>
                                                <DomikSprite className="weather-effect-ico" logicName={hint.domikType.logicName} />
                                                {hint.delta > 0 ? '+' : ''}{hint.delta}%
                                            </span>}
                                    </span>
                                );
                            })}
                        </div>
                        }
                    </div>
                }
                <ChangelogButton />
                {hudAway && hudPinnedOpen &&
                    <button type="button" className="hud-fold" onClick={() => { setHudPinnedOpen(false); }} title="Свернуть панель">
                        <ChevronUpIcon className="btn-ico" aria-hidden="true" />
                    </button>}
            </header>
        </>
    );
};

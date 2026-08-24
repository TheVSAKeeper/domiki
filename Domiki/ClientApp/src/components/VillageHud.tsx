import { useEffect, useRef, useState } from 'react';
import type { CSSProperties, ReactNode } from 'react';
import { createPortal } from 'react-dom';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import type { DomikTypeDto, PlodderCount, ResourceDto, ResourceTypeDto, VillageLevelDto, WeatherStateDto } from '../types/api';
import { COIN_RESOURCE_TYPE_ID, GOLD_RESOURCE_TYPE_ID, strongestWeatherEffect } from '../utils/game';
import type { HudDigest } from '../utils/hud';
import { pluralRu } from '../utils/plural';
import { remainingSeconds } from '../utils/time';
import { useFlyoutTop } from '../utils/flyout';
import { AbstractSprite, DomikSprite, MechanicSprite, NeighborSprite, WeatherSprite } from './sprites';
import { HudResource } from './HudResource';
import { HudRibbon } from './HudRibbon';
import { ProgressBar } from './ProgressBar';
import { GiftVisitDots } from './GiftVisitDots';

interface VillageHudProps {
    resources: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
    domikTypes: DomikTypeDto[];
    plodder: PlodderCount;
    digest: HudDigest;
    villageLevel: VillageLevelDto | null;
    weather: WeatherStateDto | null;
    now: number;
    onStickyOffsetChange: (offset: number) => void;
    villageProfile?: { logicName: string; name: string; buildings: string[] } | null;
    nav: ReactNode;
    onOpenHousehold: () => void;
}

const flyoutStyle = (anchor: DOMRect, top: number, hidden: boolean) =>
    ({
        top,
        '--flyout-right': `${window.innerWidth - anchor.right}px`,
        visibility: hidden ? 'hidden' : undefined,
    }) as CSSProperties;

const hoursLeft = (finishDate: string, now: number) => Math.max(1, Math.ceil(remainingSeconds(finishDate, now) / 3600));

export const VillageHud = ({ resources, resourceTypes, domikTypes, plodder, digest, villageLevel, weather, now, onStickyOffsetChange, villageProfile, nav, onOpenHousehold }: VillageHudProps) => {
    const hudRef = useRef<HTMLElement>(null);
    const [levelFlyout, setLevelFlyout] = useState<DOMRect | null>(null);
    const [weatherFlyout, setWeatherFlyout] = useState<DOMRect | null>(null);
    const villageLevelRef = useRef<HTMLDivElement>(null);
    const weatherCapsuleRef = useRef<HTMLButtonElement>(null);
    const [levelFlyoutRef, levelFlyoutTop, levelFlyoutHidden] = useFlyoutTop<HTMLDivElement>(levelFlyout);
    const [weatherFlyoutRef, weatherFlyoutTop, weatherFlyoutHidden] = useFlyoutTop<HTMLDivElement>(weatherFlyout);
    const openLevelFlyout = () => {
        const rect = villageLevelRef.current?.getBoundingClientRect();
        if (rect != null) {
            setLevelFlyout(rect);
        }
    };
    const closeLevelFlyout = () => setLevelFlyout(null);
    const toggleWeatherFlyout = () => {
        setWeatherFlyout(open => open != null ? null : weatherCapsuleRef.current?.getBoundingClientRect() ?? null);
    };

    useEffect(() => {
        if (weatherFlyout == null) {
            return;
        }

        const onDocInteract = (event: Event) => {
            if (!weatherCapsuleRef.current?.contains(event.target as Node)) {
                setWeatherFlyout(null);
            }
        };
        const onKey = (event: KeyboardEvent) => {
            if (event.key === 'Escape') {
                setWeatherFlyout(null);
            }
        };
        document.addEventListener('mousedown', onDocInteract);
        document.addEventListener('keydown', onKey);
        return () => {
            document.removeEventListener('mousedown', onDocInteract);
            document.removeEventListener('keydown', onKey);
        };
    }, [weatherFlyout]);

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

    const coinType = resourceTypes.find(t => t.id === COIN_RESOURCE_TYPE_ID);
    const coinValue = resources.find(r => r.typeId === COIN_RESOURCE_TYPE_ID)?.value;
    const goldType = resourceTypes.find(t => t.id === GOLD_RESOURCE_TYPE_ID);
    const goldValue = resources.find(r => r.typeId === GOLD_RESOURCE_TYPE_ID)?.value;
    const currentWeather = weather?.current ?? null;
    const nextGoal = villageLevel?.unlocks.find((unlock): unlock is typeof unlock & { level: number } => !unlock.unlocked && unlock.level != null);
    const effectChips = currentWeather?.effects.filter(effect => effect.outputPercent !== 100) ?? [];
    const villageProfileBuildingsText = villageProfile == null ? '' : villageProfile.buildings.join(' и ');
    const weatherLeftHours = currentWeather != null ? hoursLeft(currentWeather.endDate, now) : 0;
    const nextPeriod = weather?.forecast[0] ?? null;
    const laterPeriods = weather?.forecast.slice(1) ?? [];

    const plodderState: string[] = [];
    if (digest.workersSick > 0) {
        plodderState.push(`${digest.workersSick} ${pluralRu(digest.workersSick, 'хворает', 'хворают', 'хворают')}`);
    }
    if (digest.workersResting > 0) {
        plodderState.push(`${digest.workersResting} ${pluralRu(digest.workersResting, 'отдыхает', 'отдыхают', 'отдыхают')}`);
    }
    const plodderTitle = plodderState.length > 0
        ? `Трудяги: ${plodder.free}/${plodder.max} свободно · ${plodderState.join(' · ')}`
        : `Трудяги: ${plodder.free}/${plodder.max} свободно`;

    return (
        <>
            <header ref={hudRef} className="hud pixel-panel">
                <div className="hud-bar">
                    <div className="hud-left">
                        <div className="hud-casna">
                            {coinType != null && coinValue != null && <HudResource resourceType={coinType} value={coinValue} />}
                            {goldType != null && goldValue != null && <HudResource resourceType={goldType} value={goldValue} />}
                        </div>

                        {domikTypes.length > 0 &&
                            <>
                                <span className="hud-div" aria-hidden="true" />
                                <div className="hud-plodders" title={plodderTitle}>
                                    <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                                    <span className="resource-value">{plodder.free}/{plodder.max}</span>
                                    <span className="hud-plodders-word">свободно</span>
                                    {plodderState.length > 0 &&
                                        <span className="hud-plodders-alert" aria-label={plodderState.join(', ')}>
                                            {digest.workersSick + digest.workersResting}
                                        </span>}
                                </div>
                            </>}

                        <HudRibbon digest={digest} onOpenHousehold={onOpenHousehold} />
                    </div>

                    <div className="hud-right">
                        {weather != null && currentWeather != null &&
                            <button type="button" ref={weatherCapsuleRef}
                                className={'weather-capsule' + (weatherFlyout != null ? ' is-open' : '')}
                                onClick={toggleWeatherFlyout} aria-expanded={weatherFlyout != null}
                                title={`${currentWeather.weatherName}, ещё ${weatherLeftHours} ч`}>
                                <WeatherSprite logicName={currentWeather.logicName} className="weather-ico" aria-hidden="true" />
                                <span className="weather-left">ещё {weatherLeftHours}ч</span>
                                {weatherFlyout != null
                                    ? <ChevronUpIcon className="btn-ico weather-capsule-caret" aria-hidden="true" />
                                    : <ChevronDownIcon className="btn-ico weather-capsule-caret" aria-hidden="true" />}
                            </button>}

                        {villageLevel != null &&
                            <div className="village-level" ref={villageLevelRef}
                                onMouseEnter={openLevelFlyout} onMouseLeave={closeLevelFlyout}
                                onFocus={openLevelFlyout} onBlur={closeLevelFlyout}>
                                <button type="button" className="village-level-box"
                                    title={`Постройки ${villageLevel.buildings}, жители ${villageLevel.residents}, репутация ${villageLevel.reputation}, уют ${villageLevel.comfort}`}>
                                    <MechanicSprite logicName="obzhitost" size={24} className="village-level-ico" aria-hidden="true" />
                                    <span className="village-level-label">Обжитость</span>
                                    <span className="village-level-value">{villageLevel.level}</span>
                                </button>
                            </div>}
                    </div>
                </div>

                <div className="hud-deck">
                    <div className="hud-deck-nav">{nav}</div>
                </div>

                {weatherFlyout != null && currentWeather != null && createPortal(
                    <div ref={weatherFlyoutRef} className="weather-flyout" style={flyoutStyle(weatherFlyout, weatherFlyoutTop, weatherFlyoutHidden)}>
                        <div className="wf-head">
                            <WeatherSprite logicName={currentWeather.logicName} className="weather-ico" aria-hidden="true" />
                            <span className="weather-name">{currentWeather.weatherName}</span>
                            <span className="weather-left">ещё {weatherLeftHours}ч</span>
                        </div>
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
                                            title={`${domikType.name}: ${buff ? '+' : ''}${delta}% выход`}>
                                            <DomikSprite className="weather-effect-ico" logicName={domikType.logicName} />
                                            {buff ? '+' : ''}{delta}%
                                        </span>
                                    );
                                })}
                            </div>}
                        {nextPeriod != null &&
                            <div className="wf-forecast-title">Впереди</div>}
                        {nextPeriod != null &&
                            <div className="weather-forecast">
                                {[nextPeriod, ...laterPeriods].map(period => {
                                    const hint = strongestWeatherEffect(period.effects, domikTypes);
                                    return (
                                        <span key={period.startDate} className="weather-chip" title={period.weatherName}>
                                            <WeatherSprite logicName={period.logicName} size={24} className="weather-chip-ico" aria-hidden="true" />
                                            через {hoursLeft(period.startDate, now)}ч
                                            {hint != null &&
                                                <span className={'weather-effect' + (hint.delta > 0 ? ' weather-effect-buff' : ' weather-effect-nerf')}>
                                                    <DomikSprite className="weather-effect-ico" logicName={hint.domikType.logicName} />
                                                    {hint.delta > 0 ? '+' : ''}{hint.delta}%
                                                </span>}
                                        </span>
                                    );
                                })}
                            </div>}
                    </div>,
                    document.body)}

                {levelFlyout != null && createPortal(
                    <div ref={levelFlyoutRef} className="village-level-flyout" style={flyoutStyle(levelFlyout, levelFlyoutTop, levelFlyoutHidden)}>
                        <div className="vlf-stats">
                            <span className="vlf-stat"><span className="vlf-stat-label">Постройки</span><span className="vlf-stat-value">{villageLevel?.buildings}</span></span>
                            <span className="vlf-stat"><span className="vlf-stat-label">Жители</span><span className="vlf-stat-value">{villageLevel?.residents}</span></span>
                            <span className="vlf-stat"><span className="vlf-stat-label">Репутация</span><span className="vlf-stat-value">{villageLevel?.reputation}</span></span>
                            <span className="vlf-stat"><span className="vlf-stat-label">Уют</span><span className="vlf-stat-value">{villageLevel?.comfort}</span></span>
                        </div>
                        {villageLevel != null && nextGoal != null &&
                            <div className="vlf-goal">
                                <div className="vlf-goal-head">
                                    <LockIcon className="vlf-goal-ico" aria-hidden="true" />
                                    <span className="vlf-goal-name">{nextGoal.label}</span>
                                </div>
                                <ProgressBar value={villageLevel.level} max={nextGoal.level}
                                    label={`обжитость ${villageLevel.level}/${nextGoal.level}`} />
                            </div>}
                        {villageProfile != null &&
                            <div className="vlf-uklad" title={`Деревня живёт по укладу ${villageProfile.name}: ${villageProfileBuildingsText} управляются быстрее на 15 %.`}>
                                <NeighborSprite logicName={villageProfile.logicName} size={24} className="vlf-uklad-ico" aria-hidden="true" />
                                <span className="vlf-uklad-label">уклад {villageProfile.name}</span>
                            </div>}
                        {villageLevel != null && villageLevel.visitsSinceBigGift > 0 &&
                            <div className="vlf-gift">
                                <span className="vlf-gift-label">До большого гостинца</span>
                                <GiftVisitDots visitIndex={villageLevel.visitsSinceBigGift} />
                            </div>}
                        {(() => {
                            const rows = [
                                ...(villageLevel?.unlocks.filter(unlock => !unlock.unlocked && unlock.level != null).slice(0, 3) ?? []),
                                ...(villageLevel?.unlocks.filter(unlock => !unlock.unlocked && unlock.level == null) ?? []),
                            ];
                            if (rows.length === 0) {
                                return null;
                            }

                            return (
                                <div className="vlf-ahead">
                                    <span className="vlf-ahead-title">Впереди</span>
                                    <ul className="vlf-unlocks">
                                        {rows.map(unlock => (
                                            <li key={`${unlock.label}-${unlock.level ?? unlock.requirement ?? ''}`} className="vlf-row">
                                                {unlock.kind === 'building'
                                                    ? <DomikSprite logicName={unlock.logicName ?? ''} className="vlf-ico" aria-hidden="true" />
                                                    : unlock.kind === 'neighbor'
                                                        ? <NeighborSprite logicName={unlock.logicName ?? ''} size={24} className="vlf-ico" aria-hidden="true" />
                                                        : unlock.logicName === 'smart_artel'
                                                            ? <AbstractSprite logicName="smart_artel" size={24} className="vlf-ico" aria-hidden="true" />
                                                            : <HomeIcon className="vlf-ico" aria-hidden="true" />}
                                                <span className="vlf-body">
                                                    <span className="vlf-name">{unlock.label}</span>
                                                    {unlock.level == null && unlock.requirement != null &&
                                                        <span className="vlf-req">{unlock.requirement}</span>}
                                                </span>
                                                {unlock.level != null &&
                                                    <span className="vlf-badge">обж {unlock.level}</span>}
                                            </li>
                                        ))}
                                    </ul>
                                </div>
                            );
                        })()}
                    </div>,
                    document.body)}

            </header>
        </>
    );
};

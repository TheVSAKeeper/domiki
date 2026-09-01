import { useEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import ChevronDownIcon from 'pixelarticons/svg/chevron-down.svg?react';
import ChevronUpIcon from 'pixelarticons/svg/chevron-up.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import LockIcon from 'pixelarticons/svg/lock.svg?react';
import type { DomikTypeDto, PlodderCount, ResourceDto, ResourceTypeDto, VillageLevelDto, WeatherStateDto } from '../types/api';
import { COIN_RESOURCE_TYPE_ID, GOLD_RESOURCE_TYPE_ID, weatherEffects } from '../utils/game';
import type { HudDigest } from '../utils/hud';
import { pluralRu } from '../utils/plural';
import { remainingSeconds } from '../utils/time';
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
    onOpenTab: (tab: string) => void;
}

const hoursLeft = (finishDate: string, now: number) => Math.max(1, Math.ceil(remainingSeconds(finishDate, now) / 3600));

const WeatherEffectChip = ({ domikType, delta }: { domikType: DomikTypeDto; delta: number }) => (
    <span className={'weather-effect' + (delta > 0 ? ' weather-effect-buff' : ' weather-effect-nerf')}
        title={`${domikType.name}: ${delta > 0 ? '+' : ''}${delta}% выход`}>
        <DomikSprite className="weather-effect-ico" logicName={domikType.logicName} />
        {delta > 0 ? '+' : ''}{delta}%
    </span>
);

export const VillageHud = ({ resources, resourceTypes, domikTypes, plodder, digest, villageLevel, weather, now, onStickyOffsetChange, villageProfile, nav, onOpenTab }: VillageHudProps) => {
    const hudRef = useRef<HTMLDivElement>(null);
    const [flyout, setFlyout] = useState<'weather' | 'level' | null>(null);
    const levelFlyout = flyout === 'level';
    const weatherFlyout = flyout === 'weather';
    const toggleFlyout = (name: 'weather' | 'level') => { setFlyout(open => open === name ? null : name); };

    useEffect(() => {
        if (flyout == null) {
            return;
        }

        const onDocInteract = (event: Event) => {
            const element = event.target instanceof Element ? event.target : null;
            if (element?.closest('.weather-capsule, .village-level-box, .hud-flyout') == null) {
                setFlyout(null);
            }
        };
        const onKey = (event: KeyboardEvent) => {
            if (event.key === 'Escape') {
                setFlyout(null);
            }
        };
        document.addEventListener('mousedown', onDocInteract);
        document.addEventListener('keydown', onKey);
        return () => {
            document.removeEventListener('mousedown', onDocInteract);
            document.removeEventListener('keydown', onKey);
        };
    }, [flyout]);

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
    const effectChips = currentWeather == null ? [] : weatherEffects(currentWeather.effects, domikTypes);
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
            <div ref={hudRef} className="hud-shell">
            <header className="hud pixel-panel">
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

                        <HudRibbon digest={digest} onOpenTab={onOpenTab} />
                    </div>

                    <div className="hud-right">
                        {weather != null && currentWeather != null &&
                            <button type="button"
                                className={'weather-capsule' + (weatherFlyout ? ' is-open' : '')}
                                onClick={() => { toggleFlyout('weather'); }} aria-expanded={weatherFlyout}
                                title={`${currentWeather.weatherName}, ещё ${weatherLeftHours} ч`}>
                                <WeatherSprite logicName={currentWeather.logicName} className="weather-ico" aria-hidden="true" />
                                <span className="weather-capsule-name">{currentWeather.weatherName}</span>
                                <span className="weather-left">ещё {weatherLeftHours}ч</span>
                                {weatherFlyout
                                    ? <ChevronUpIcon className="btn-ico hud-capsule-caret" aria-hidden="true" />
                                    : <ChevronDownIcon className="btn-ico hud-capsule-caret" aria-hidden="true" />}
                            </button>}

                        {villageLevel != null &&
                            <div className="village-level">
                                <button type="button" className={'village-level-box' + (levelFlyout ? ' is-open' : '')}
                                    onClick={() => { toggleFlyout('level'); }} aria-expanded={levelFlyout}
                                    title={`Постройки ${villageLevel.buildings}, жители ${villageLevel.residents}, доброе имя ${villageLevel.reputation}, уют ${villageLevel.comfort}`}>
                                    <MechanicSprite logicName="obzhitost" size={24} className="village-level-ico" aria-hidden="true" />
                                    <span className="village-level-label">Обжитость</span>
                                    <span className="village-level-value">{villageLevel.level}</span>
                                    {levelFlyout
                                        ? <ChevronUpIcon className="btn-ico hud-capsule-caret" aria-hidden="true" />
                                        : <ChevronDownIcon className="btn-ico hud-capsule-caret" aria-hidden="true" />}
                                </button>
                            </div>}
                    </div>
                </div>

                <div className="hud-deck">
                    <div className="hud-deck-nav">{nav}</div>
                </div>
            </header>

                {weatherFlyout && currentWeather != null &&
                    <div className="hud-flyout weather-flyout">
                        <div className="wf-head">
                            <WeatherSprite logicName={currentWeather.logicName} className="weather-ico" aria-hidden="true" />
                            <span className="weather-name">{currentWeather.weatherName}</span>
                            <span className="weather-left">ещё {weatherLeftHours}ч</span>
                        </div>
                        {effectChips.length > 0 &&
                            <div className="weather-effects">
                                {effectChips.map(row =>
                                    <WeatherEffectChip key={row.domikType.id} domikType={row.domikType} delta={row.delta} />)}
                            </div>}
                        {nextPeriod != null &&
                            <div className="wf-forecast-title">Впереди</div>}
                        {nextPeriod != null &&
                            <div className="weather-forecast">
                                {[nextPeriod, ...laterPeriods].map(period => (
                                    <span key={period.startDate} className="weather-chip" title={period.weatherName}>
                                        <WeatherSprite logicName={period.logicName} size={24} className="weather-chip-ico" aria-hidden="true" />
                                        <span className="weather-chip-when">{period.weatherName}, через {hoursLeft(period.startDate, now)}ч</span>
                                        {weatherEffects(period.effects, domikTypes).map(row =>
                                            <WeatherEffectChip key={row.domikType.id} domikType={row.domikType} delta={row.delta} />)}
                                    </span>
                                ))}
                            </div>}
                    </div>}

                {levelFlyout &&
                    <div className="hud-flyout village-level-flyout">
                        <div className="vlf-stats">
                            <span className="vlf-stat"><span className="vlf-stat-label">Постройки</span><span className="vlf-stat-value">{villageLevel?.buildings}</span></span>
                            <span className="vlf-stat"><span className="vlf-stat-label">Жители</span><span className="vlf-stat-value">{villageLevel?.residents}</span></span>
                            <span className="vlf-stat"><span className="vlf-stat-label">Доброе имя</span><span className="vlf-stat-value">{villageLevel?.reputation}</span></span>
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
                    </div>}
            </div>
        </>
    );
};

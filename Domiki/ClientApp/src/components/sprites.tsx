import type { FC, SVGProps } from 'react';
import BarracksSprite from '../assets/domikTypes/barracks.svg?react';
import ClayMineSprite from '../assets/domikTypes/clay_mine.svg?react';
import FieldSprite from '../assets/domikTypes/field.svg?react';
import FairSprite from '../assets/domikTypes/fair.svg?react';
import ForgeSprite from '../assets/domikTypes/forge.svg?react';
import GatheringSprite from '../assets/domikTypes/gathering.svg?react';
import GoldMineSprite from '../assets/domikTypes/gold_mine.svg?react';
import LumberMillSprite from '../assets/domikTypes/lumber_mill.svg?react';
import MarketSprite from '../assets/domikTypes/market.svg?react';
import MarketYardSprite from '../assets/domikTypes/market_yard.svg?react';
import MillSprite from '../assets/domikTypes/mill.svg?react';
import PotterySprite from '../assets/domikTypes/pottery.svg?react';
import BakerySprite from '../assets/domikTypes/bakery.svg?react';
import ScoutHutSprite from '../assets/domikTypes/scout_hut.svg?react';
import SheepfoldSprite from '../assets/domikTypes/sheepfold.svg?react';
import StoneMineSprite from '../assets/domikTypes/stone_mine.svg?react';
import StonecutterSprite from '../assets/domikTypes/stonecutter.svg?react';
import WorkshopSprite from '../assets/domikTypes/workshop.svg?react';
import BridgeSprite from '../assets/tolokaTypes/bridge.svg?react';
import GranarySprite from '../assets/tolokaTypes/granary.svg?react';
import KilnSprite from '../assets/tolokaTypes/kiln.svg?react';
import WorkerPortrait from '../assets/workers/portrait.svg?react';
import WeatherClearSprite from '../assets/weather/clear.svg?react';
import WeatherRainSprite from '../assets/weather/rain.svg?react';
import WeatherDroughtSprite from '../assets/weather/drought.svg?react';
import WeatherFairDaySprite from '../assets/weather/fair_day.svg?react';
import WeatherFrostSprite from '../assets/weather/frost.svg?react';
import WeatherWindSprite from '../assets/weather/wind.svg?react';
import DecorBenchSprite from '../assets/decorTypes/bench.svg?react';
import DecorFenceSprite from '../assets/decorTypes/fence.svg?react';
import DecorFlagSprite from '../assets/decorTypes/flag.svg?react';
import DecorFlowersSprite from '../assets/decorTypes/flowers.svg?react';
import DecorFountainSprite from '../assets/decorTypes/fountain.svg?react';
import DecorGardenSprite from '../assets/decorTypes/garden.svg?react';
import DecorTrophySprite from '../assets/decorTypes/trophy.svg?react';
import DecorBrickArchSprite from '../assets/decorTypes/brick_arch.svg?react';
import DecorLanternSprite from '../assets/decorTypes/lantern.svg?react';
import TraitOrdinarySprite from '../assets/traits/ordinary.svg?react';
import TraitNimbleSprite from '../assets/traits/nimble.svg?react';
import TraitDiligentSprite from '../assets/traits/diligent.svg?react';
import TraitSonyaSprite from '../assets/traits/sonya.svg?react';
import TraitLuckySprite from '../assets/traits/lucky.svg?react';
import TraitThriftySprite from '../assets/traits/thrifty.svg?react';
import NeighborGenericSprite from '../assets/neighbors/generic.svg?react';
import NeighborZarechyeSprite from '../assets/neighbors/zarechye.svg?react';
import NeighborBorovoeSprite from '../assets/neighbors/borovoe.svg?react';
import NeighborKamenkaSprite from '../assets/neighbors/kamenka.svg?react';
import NeighborGlinischiSprite from '../assets/neighbors/glinischi.svg?react';
import NeighborDubravaSprite from '../assets/neighbors/dubrava.svg?react';
import ReputationSprite from '../assets/abstract/reputation.svg?react';
import WorkerSkillSprite from '../assets/abstract/worker_skill.svg?react';
import FatigueRestSprite from '../assets/abstract/fatigue_rest.svg?react';
import PrestigeSprite from '../assets/abstract/prestige_new_valley.svg?react';
import ProductionRecipeSprite from '../assets/abstract/production_recipe.svg?react';
import NearSortieSprite from '../assets/abstract/near_sortie.svg?react';
import LongExpeditionSprite from '../assets/abstract/long_expedition.svg?react';
import WalkingSortieSprite from '../assets/abstract/walking_sortie.svg?react';
import RareExpeditionFindSprite from '../assets/abstract/rare_expedition_find.svg?react';
import ExpeditionHardeningSprite from '../assets/abstract/expedition_hardening.svg?react';
import BlueprintSprite from '../assets/abstract/blueprint.svg?react';
import UntouchedDepositsSprite from '../assets/abstract/untouched_deposits.svg?react';
import JournalAbstractSprite from '../assets/abstract/journal.svg?react';
import ElderOrderSprite from '../assets/abstract/elder_order.svg?react';
import MechObzhitostSprite from '../assets/mechanics/obzhitost.svg?react';
import MechOrdersSprite from '../assets/mechanics/orders.svg?react';
import MechWorkersSprite from '../assets/mechanics/workers.svg?react';
import MechWeatherSprite from '../assets/mechanics/weather.svg?react';
import MechBlueprintsSprite from '../assets/mechanics/blueprints.svg?react';
import MechExpeditionsSprite from '../assets/mechanics/expeditions.svg?react';
import MechMarketSprite from '../assets/mechanics/market.svg?react';
import MechTolokaSprite from '../assets/mechanics/toloka.svg?react';
import MechDecorSprite from '../assets/mechanics/decor.svg?react';
import MechGiftsSprite from '../assets/mechanics/gifts.svg?react';
import ClayResSprite from '../assets/resourceTypes/clay.svg?react';
import CoinResSprite from '../assets/resourceTypes/coin.svg?react';
import GoldResSprite from '../assets/resourceTypes/gold.svg?react';
import StoneResSprite from '../assets/resourceTypes/stone.svg?react';
import WoodResSprite from '../assets/resourceTypes/wood.svg?react';
import BrickResSprite from '../assets/resourceTypes/brick.svg?react';
import BoardResSprite from '../assets/resourceTypes/board.svg?react';
import ToolResSprite from '../assets/resourceTypes/tool.svg?react';
import FurnitureResSprite from '../assets/resourceTypes/furniture.svg?react';
import BlockResSprite from '../assets/resourceTypes/block.svg?react';
import MillstoneResSprite from '../assets/resourceTypes/millstone.svg?react';
import DishesResSprite from '../assets/resourceTypes/dishes.svg?react';
import GrainResSprite from '../assets/resourceTypes/grain.svg?react';
import FlourResSprite from '../assets/resourceTypes/flour.svg?react';
import BreadResSprite from '../assets/resourceTypes/bread.svg?react';
import OreResSprite from '../assets/resourceTypes/ore.svg?react';
import IronResSprite from '../assets/resourceTypes/iron.svg?react';
import WoolResSprite from '../assets/resourceTypes/wool.svg?react';
import ClothResSprite from '../assets/resourceTypes/cloth.svg?react';

type SpriteComponent = FC<SVGProps<SVGSVGElement>>;

const prepareInlineSprite = (node: SVGSVGElement | null) => {
    if (node == null) {
        return;
    }

    node.querySelectorAll('title, desc').forEach(element => element.remove());
};

const cleanSpriteProps = (props: SVGProps<SVGSVGElement>): SVGProps<SVGSVGElement> => {
    const hidden = props['aria-hidden'] ?? (props['aria-label'] == null ? true : undefined);
    return {
        ...props,
        ref: prepareInlineSprite,
        role: hidden ? undefined : (props.role ?? 'img'),
        'aria-hidden': hidden,
        'aria-labelledby': undefined,
        focusable: 'false',
    };
};

const domikSprites: Record<string, SpriteComponent> = {
    bakery: BakerySprite,
    barracks: BarracksSprite,
    clay_mine: ClayMineSprite,
    field: FieldSprite,
    fair: FairSprite,
    forge: ForgeSprite,
    gathering: GatheringSprite,
    gold_mine: GoldMineSprite,
    lumber_mill: LumberMillSprite,
    market: MarketSprite,
    market_yard: MarketYardSprite,
    mill: MillSprite,
    pottery: PotterySprite,
    scout_hut: ScoutHutSprite,
    sheepfold: SheepfoldSprite,
    stone_mine: StoneMineSprite,
    stonecutter: StonecutterSprite,
    workshop: WorkshopSprite,
};

const tolokaSprites: Record<string, SpriteComponent> = {
    bridge: BridgeSprite,
    granary: GranarySprite,
    kiln: KilnSprite,
    // TODO: караван – заглушка эмблемой толоки, нарисовать canon-спрайт (ASSETS.md арт-долг)
    caravan: MechTolokaSprite,
};

const weatherSprites: Record<string, SpriteComponent> = {
    clear: WeatherClearSprite,
    rain: WeatherRainSprite,
    drought: WeatherDroughtSprite,
    frost: WeatherFrostSprite,
    wind: WeatherWindSprite,
    fair_day: WeatherFairDaySprite,
};

const decorSprites: Record<string, SpriteComponent> = {
    fence: DecorFenceSprite,
    flowerbed: DecorFlowersSprite,
    garden: DecorGardenSprite,
    fountain: DecorFountainSprite,
    bench: DecorBenchSprite,
    trail_idol: DecorTrophySprite,
    wanderer_banner: DecorFlagSprite,
    brick_arch: DecorBrickArchSprite,
    lantern: DecorLanternSprite,
};

const traitSprites: Record<string, SpriteComponent> = {
    ordinary: TraitOrdinarySprite,
    nimble: TraitNimbleSprite,
    diligent: TraitDiligentSprite,
    sonya: TraitSonyaSprite,
    lucky: TraitLuckySprite,
    thrifty: TraitThriftySprite,
};

const neighborSprites: Record<string, SpriteComponent> = {
    generic: NeighborGenericSprite,
    zarechye: NeighborZarechyeSprite,
    borovoe: NeighborBorovoeSprite,
    kamenka: NeighborKamenkaSprite,
    glinischi: NeighborGlinischiSprite,
    dubrava: NeighborDubravaSprite,
};

const abstractSprites: Record<string, SpriteComponent> = {
    reputation: ReputationSprite,
    worker_skill: WorkerSkillSprite,
    fatigue_rest: FatigueRestSprite,
    prestige_new_valley: PrestigeSprite,
    production_recipe: ProductionRecipeSprite,
    near_sortie: NearSortieSprite,
    long_expedition: LongExpeditionSprite,
    walking_sortie: WalkingSortieSprite,
    rare_expedition_find: RareExpeditionFindSprite,
    expedition_hardening: ExpeditionHardeningSprite,
    blueprint: BlueprintSprite,
    untouched_deposits: UntouchedDepositsSprite,
    journal: JournalAbstractSprite,
    elder_order: ElderOrderSprite,
};

const resourceSprites: Record<string, SpriteComponent> = {
    clay: ClayResSprite,
    coin: CoinResSprite,
    gold: GoldResSprite,
    stone: StoneResSprite,
    wood: WoodResSprite,
    brick: BrickResSprite,
    board: BoardResSprite,
    tool: ToolResSprite,
    furniture: FurnitureResSprite,
    block: BlockResSprite,
    millstone: MillstoneResSprite,
    dishes: DishesResSprite,
    grain: GrainResSprite,
    flour: FlourResSprite,
    bread: BreadResSprite,
    ore: OreResSprite,
    iron: IronResSprite,
    wool: WoolResSprite,
    cloth: ClothResSprite,
};

interface IconSpriteProps extends SVGProps<SVGSVGElement> {
    logicName: string;
    size?: 24 | 32 | 40 | 48 | 64;
}

const warnedUnknownSprites = new Set<string>();

const warnUnknownSprite = (kind: string, logicName: string) => {
    if (!import.meta.env.DEV) {
        return;
    }

    const key = `${kind}:${logicName}`;
    if (!warnedUnknownSprites.has(key)) {
        warnedUnknownSprites.add(key);
        console.warn(`[sprites] Unknown ${kind} logicName: "${logicName}"`);
    }
};

const renderIconSprite = (kind: string, sprites: Record<string, SpriteComponent>, fallback: SpriteComponent | undefined, { logicName, size = 32, ...props }: IconSpriteProps) => {
    const mappedSprite = sprites[logicName];
    if (mappedSprite == null) {
        warnUnknownSprite(kind, logicName);
    }
    const Sprite = mappedSprite ?? fallback;
    return Sprite == null ? null : <Sprite data-size={size} {...cleanSpriteProps(props)} />;
};

const mechanicSprites: Record<string, SpriteComponent> = {
    obzhitost: MechObzhitostSprite,
    orders: MechOrdersSprite,
    workers: MechWorkersSprite,
    weather: MechWeatherSprite,
    blueprints: MechBlueprintsSprite,
    expeditions: MechExpeditionsSprite,
    market: MechMarketSprite,
    toloka: MechTolokaSprite,
    decor: MechDecorSprite,
    gifts: MechGiftsSprite,
};

export const MechanicSprite = (props: IconSpriteProps) => <>{renderIconSprite('mechanic', mechanicSprites, undefined, props)}</>;
export const WeatherSprite = (props: IconSpriteProps) => <>{renderIconSprite('weather', weatherSprites, undefined, props)}</>;
export const DecorSprite = (props: IconSpriteProps) => <>{renderIconSprite('decor', decorSprites, undefined, props)}</>;
export const TraitSprite = (props: IconSpriteProps) => <>{renderIconSprite('trait', traitSprites, TraitOrdinarySprite, props)}</>;
export const NeighborSprite = (props: IconSpriteProps) => <>{renderIconSprite('neighbor', neighborSprites, NeighborGenericSprite, props)}</>;
export const AbstractSprite = (props: IconSpriteProps) => <>{renderIconSprite('abstract', abstractSprites, undefined, props)}</>;
export const ResourceSprite = (props: IconSpriteProps) => <>{renderIconSprite('resource', resourceSprites, undefined, props)}</>;

interface SpriteProps extends SVGProps<SVGSVGElement> {
    logicName: string;
    level?: number;
    working?: boolean;
}

const clampLevel = (level: number) => Math.min(5, Math.max(1, Math.floor(level)));

export const DomikSprite = ({ logicName, level = 1, working = false, ...props }: SpriteProps) => {
    const Sprite = domikSprites[logicName];
    if (Sprite == null) {
        warnUnknownSprite('domik', logicName);
    }
    return Sprite == null ? null : <Sprite data-level={clampLevel(level)} data-working={working ? 'true' : 'false'} {...cleanSpriteProps(props)} />;
};

export const TolokaSprite = ({ logicName, level = 1, ...props }: SpriteProps) => {
    const Sprite = tolokaSprites[logicName];
    if (Sprite == null) {
        warnUnknownSprite('toloka', logicName);
    }
    return Sprite == null ? null : <Sprite data-level={clampLevel(level)} {...cleanSpriteProps(props)} />;
};

type WorkerLook = [skin: number, hair: number, style: string, beard: number, hat: number, shirt: number, extra: number];

const workerLooks: Record<string, WorkerLook> = {
    'Аким': [1, 1, 'm1', 1, 0, 1, 0],
    'Агата': [1, 1, 'f2', 0, 0, 1, 0],
    'Бажен': [2, 2, 'm4', 0, 4, 4, 2],
    'Борис': [1, 5, 'm3', 3, 0, 2, 0],
    'Варвара': [1, 2, 'f1', 0, 0, 3, 3],
    'Велена': [2, 1, 'f4', 0, 4, 2, 2],
    'Глеб': [1, 3, 'm2', 0, 0, 3, 1],
    'Гордей': [2, 1, 'm2', 2, 0, 5, 0],
    'Дарья': [1, 3, 'f3', 0, 0, 1, 1],
    'Демьян': [1, 4, 'm4', 1, 0, 2, 0],
    'Егор': [1, 4, 'm1', 0, 1, 4, 1],
    'Есения': [1, 2, 'f4', 0, 0, 5, 0],
    'Ждан': [2, 1, 'm4', 0, 0, 1, 0],
    'Захар': [1, 2, 'm1', 2, 0, 3, 0],
    'Злата': [1, 3, 'f1', 0, 0, 4, 0],
    'Илья': [1, 1, 'm2', 0, 1, 2, 0],
    'Кира': [2, 1, 'f2', 0, 4, 3, 0],
    'Лада': [1, 2, 'f3', 0, 4, 2, 3],
    'Лукерья': [1, 5, 'f2', 0, 0, 5, 0],
    'Марта': [1, 4, 'f2', 0, 0, 2, 1],
    'Мирон': [2, 5, 'm4', 1, 0, 4, 0],
    'Назар': [2, 1, 'm1', 2, 1, 5, 0],
    'Нина': [2, 1, 'f1', 0, 0, 5, 0],
    'Остап': [1, 3, 'm3', 1, 0, 5, 0],
    'Пелагея': [1, 5, 'f2', 0, 3, 4, 0],
    'Прасковья': [2, 5, 'f1', 0, 3, 2, 0],
    'Роман': [1, 2, 'm2', 0, 2, 1, 0],
    'Сава': [1, 3, 'm4', 0, 0, 2, 3],
    'Тая': [1, 1, 'f3', 0, 0, 4, 3],
    'Ульяна': [1, 4, 'f1', 0, 4, 3, 0],
    'Фёдор': [1, 5, 'm1', 2, 1, 3, 0],
    'Ярина': [2, 2, 'f4', 0, 0, 1, 2],
};

const fallbackLook = (name: string): WorkerLook => {
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
        hash = (hash * 31 + name.charCodeAt(i)) | 0;
    }
    hash = Math.abs(hash);
    return [1 + hash % 2, 1 + Math.floor(hash / 2) % 5, Math.floor(hash / 16) % 2 === 0 ? 'm1' : 'm4', 0, 0, 1 + Math.floor(hash / 32) % 5, 0];
};

interface WorkerSpriteProps extends SVGProps<SVGSVGElement> {
    name: string;
    state?: 'idle' | 'working' | 'resting' | 'sick';
    skilled?: boolean;
}

export const WorkerSprite = ({ name, state = 'idle', skilled = false, ...props }: WorkerSpriteProps) => {
    const [skin, hair, style, beard, hat, shirt, extra] = workerLooks[name] ?? fallbackLook(name);
    return (
        <WorkerPortrait data-skin={skin} data-hair={hair} data-style={style} data-state={state}
            data-skilled={skilled ? 'true' : 'false'}
            data-beard={beard} data-hat={hat} data-shirt={shirt} data-extra={extra} {...cleanSpriteProps(props)} />
    );
};

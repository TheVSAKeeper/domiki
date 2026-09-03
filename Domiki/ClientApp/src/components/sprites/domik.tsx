import { memo } from 'react';
import BarracksSprite from '../../assets/domikTypes/barracks.svg?react';
import ClayMineSprite from '../../assets/domikTypes/clay_mine.svg?react';
import ElderHouseSprite from '../../assets/domikTypes/elder_house.svg?react';
import FieldSprite from '../../assets/domikTypes/field.svg?react';
import FairSprite from '../../assets/domikTypes/fair.svg?react';
import ForgeSprite from '../../assets/domikTypes/forge.svg?react';
import GatheringSprite from '../../assets/domikTypes/gathering.svg?react';
import GoldMineSprite from '../../assets/domikTypes/gold_mine.svg?react';
import LumberMillSprite from '../../assets/domikTypes/lumber_mill.svg?react';
import MarketSprite from '../../assets/domikTypes/market.svg?react';
import MarketYardSprite from '../../assets/domikTypes/market_yard.svg?react';
import MillSprite from '../../assets/domikTypes/mill.svg?react';
import PostHouseSprite from '../../assets/domikTypes/post_house.svg?react';
import PotterySprite from '../../assets/domikTypes/pottery.svg?react';
import BakerySprite from '../../assets/domikTypes/bakery.svg?react';
import ScoutHutSprite from '../../assets/domikTypes/scout_hut.svg?react';
import SheepfoldSprite from '../../assets/domikTypes/sheepfold.svg?react';
import TavernSprite from '../../assets/domikTypes/tavern.svg?react';
import StoneMineSprite from '../../assets/domikTypes/stone_mine.svg?react';
import StonecutterSprite from '../../assets/domikTypes/stonecutter.svg?react';
import WorkshopSprite from '../../assets/domikTypes/workshop.svg?react';
import { cleanSpriteProps, clampLevel, warnUnknownSprite } from './core';
import type { SpriteComponent, SpriteProps } from './core';

const domikSprites: Record<string, SpriteComponent> = {
    bakery: BakerySprite,
    barracks: BarracksSprite,
    clay_mine: ClayMineSprite,
    elder_house: ElderHouseSprite,
    field: FieldSprite,
    fair: FairSprite,
    forge: ForgeSprite,
    gathering: GatheringSprite,
    gold_mine: GoldMineSprite,
    lumber_mill: LumberMillSprite,
    market: MarketSprite,
    market_yard: MarketYardSprite,
    mill: MillSprite,
    post_house: PostHouseSprite,
    pottery: PotterySprite,
    scout_hut: ScoutHutSprite,
    sheepfold: SheepfoldSprite,
    tavern: TavernSprite,
    stone_mine: StoneMineSprite,
    stonecutter: StonecutterSprite,
    workshop: WorkshopSprite,
};

export const DomikSprite = memo(({ logicName, level = 1, working = false, ...props }: SpriteProps) => {
    const Sprite = domikSprites[logicName];
    if (Sprite == null) {
        warnUnknownSprite('domik', logicName);
    }
    return Sprite == null ? null : <Sprite data-level={clampLevel(level)} data-working={working ? 'true' : 'false'} {...cleanSpriteProps(props)} />;
});

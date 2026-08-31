import ClayResSprite from '../../assets/resourceTypes/clay.svg?react';
import CoinResSprite from '../../assets/resourceTypes/coin.svg?react';
import GoldResSprite from '../../assets/resourceTypes/gold.svg?react';
import StoneResSprite from '../../assets/resourceTypes/stone.svg?react';
import WoodResSprite from '../../assets/resourceTypes/wood.svg?react';
import BrickResSprite from '../../assets/resourceTypes/brick.svg?react';
import BoardResSprite from '../../assets/resourceTypes/board.svg?react';
import ToolResSprite from '../../assets/resourceTypes/tool.svg?react';
import FurnitureResSprite from '../../assets/resourceTypes/furniture.svg?react';
import BlockResSprite from '../../assets/resourceTypes/block.svg?react';
import MillstoneResSprite from '../../assets/resourceTypes/millstone.svg?react';
import DishesResSprite from '../../assets/resourceTypes/dishes.svg?react';
import GrainResSprite from '../../assets/resourceTypes/grain.svg?react';
import FlourResSprite from '../../assets/resourceTypes/flour.svg?react';
import BreadResSprite from '../../assets/resourceTypes/bread.svg?react';
import OreResSprite from '../../assets/resourceTypes/ore.svg?react';
import IronResSprite from '../../assets/resourceTypes/iron.svg?react';
import WoolResSprite from '../../assets/resourceTypes/wool.svg?react';
import ClothResSprite from '../../assets/resourceTypes/cloth.svg?react';
import CloakResSprite from '../../assets/resourceTypes/cloak.svg?react';
import CheeseResSprite from '../../assets/resourceTypes/cheese.svg?react';
import PickResSprite from '../../assets/resourceTypes/pick.svg?react';
import TongsResSprite from '../../assets/resourceTypes/tongs.svg?react';
import { renderIconSprite } from './core';
import type { IconSpriteProps, SpriteComponent } from './core';

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
    cloak: CloakResSprite,
    cheese: CheeseResSprite,
    pick: PickResSprite,
    tongs: TongsResSprite,
};

export const ResourceSprite = (props: IconSpriteProps) => <>{renderIconSprite('resource', resourceSprites, undefined, props)}</>;

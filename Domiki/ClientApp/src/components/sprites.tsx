import type { FC, SVGProps } from 'react';
import BarracksSprite from '../assets/domikTypes/barracks.svg?react';
import ClayMineSprite from '../assets/domikTypes/clay_mine.svg?react';
import FairSprite from '../assets/domikTypes/fair.svg?react';
import ForgeSprite from '../assets/domikTypes/forge.svg?react';
import GoldMineSprite from '../assets/domikTypes/gold_mine.svg?react';
import LumberMillSprite from '../assets/domikTypes/lumber_mill.svg?react';
import MarketSprite from '../assets/domikTypes/market.svg?react';
import StoneMineSprite from '../assets/domikTypes/stone_mine.svg?react';
import WorkshopSprite from '../assets/domikTypes/workshop.svg?react';
import BridgeSprite from '../assets/tolokaTypes/bridge.svg?react';
import GranarySprite from '../assets/tolokaTypes/granary.svg?react';

type SpriteComponent = FC<SVGProps<SVGSVGElement>>;

const domikSprites: Record<string, SpriteComponent> = {
    barracks: BarracksSprite,
    clay_mine: ClayMineSprite,
    fair: FairSprite,
    forge: ForgeSprite,
    gold_mine: GoldMineSprite,
    lumber_mill: LumberMillSprite,
    market: MarketSprite,
    stone_mine: StoneMineSprite,
    workshop: WorkshopSprite,
};

const tolokaSprites: Record<string, SpriteComponent> = {
    bridge: BridgeSprite,
    granary: GranarySprite,
};

interface SpriteProps extends SVGProps<SVGSVGElement> {
    logicName: string;
    level?: number;
}

const clampLevel = (level: number) => Math.min(5, Math.max(1, Math.floor(level)));

export const DomikSprite = ({ logicName, level = 1, ...props }: SpriteProps) => {
    const Sprite = domikSprites[logicName];
    return Sprite == null ? null : <Sprite data-level={clampLevel(level)} {...props} />;
};

export const TolokaSprite = ({ logicName, level = 1, ...props }: SpriteProps) => {
    const Sprite = tolokaSprites[logicName];
    return Sprite == null ? null : <Sprite data-level={clampLevel(level)} {...props} />;
};

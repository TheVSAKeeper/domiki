import type { FC, SVGProps } from 'react';
import BarracksSprite from '../assets/domikTypes/barracks.svg?react';
import ClayMineSprite from '../assets/domikTypes/clay_mine.svg?react';
import FairSprite from '../assets/domikTypes/fair.svg?react';
import ForgeSprite from '../assets/domikTypes/forge.svg?react';
import GatheringSprite from '../assets/domikTypes/gathering.svg?react';
import GoldMineSprite from '../assets/domikTypes/gold_mine.svg?react';
import LumberMillSprite from '../assets/domikTypes/lumber_mill.svg?react';
import MarketSprite from '../assets/domikTypes/market.svg?react';
import MarketYardSprite from '../assets/domikTypes/market_yard.svg?react';
import ScoutHutSprite from '../assets/domikTypes/scout_hut.svg?react';
import StoneMineSprite from '../assets/domikTypes/stone_mine.svg?react';
import WorkshopSprite from '../assets/domikTypes/workshop.svg?react';
import BridgeSprite from '../assets/tolokaTypes/bridge.svg?react';
import GranarySprite from '../assets/tolokaTypes/granary.svg?react';
import WorkerPortrait from '../assets/workers/portrait.svg?react';

type SpriteComponent = FC<SVGProps<SVGSVGElement>>;

const domikSprites: Record<string, SpriteComponent> = {
    barracks: BarracksSprite,
    clay_mine: ClayMineSprite,
    fair: FairSprite,
    forge: ForgeSprite,
    gathering: GatheringSprite,
    gold_mine: GoldMineSprite,
    lumber_mill: LumberMillSprite,
    market: MarketSprite,
    market_yard: MarketYardSprite,
    scout_hut: ScoutHutSprite,
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
}

export const WorkerSprite = ({ name, ...props }: WorkerSpriteProps) => {
    const [skin, hair, style, beard, hat, shirt, extra] = workerLooks[name] ?? fallbackLook(name);
    return (
        <WorkerPortrait data-skin={skin} data-hair={hair} data-style={style}
            data-beard={beard} data-hat={hat} data-shirt={shirt} data-extra={extra} {...props} />
    );
};

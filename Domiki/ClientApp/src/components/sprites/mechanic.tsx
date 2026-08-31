import MechObzhitostSprite from '../../assets/mechanics/obzhitost.svg?react';
import MechOrdersSprite from '../../assets/mechanics/orders.svg?react';
import MechWorkersSprite from '../../assets/mechanics/workers.svg?react';
import MechWeatherSprite from '../../assets/mechanics/weather.svg?react';
import MechBlueprintsSprite from '../../assets/mechanics/blueprints.svg?react';
import MechExpeditionsSprite from '../../assets/mechanics/expeditions.svg?react';
import MechMarketSprite from '../../assets/mechanics/market.svg?react';
import MechTolokaSprite from '../../assets/mechanics/toloka.svg?react';
import MechDecorSprite from '../../assets/mechanics/decor.svg?react';
import MechGiftsSprite from '../../assets/mechanics/gifts.svg?react';
import MechGuestbookSprite from '../../assets/mechanics/guestbook.svg?react';
import MechVestnikSprite from '../../assets/mechanics/vestnik.svg?react';
import MechErrandsSprite from '../../assets/mechanics/errands.svg?react';
import MechConvoySprite from '../../assets/mechanics/convoy.svg?react';
import MechFriendshipSprite from '../../assets/mechanics/friendship.svg?react';
import MechWikiSprite from '../../assets/mechanics/wiki.svg?react';
import MechWorldSprite from '../../assets/mechanics/world.svg?react';
import MechShopSprite from '../../assets/mechanics/shop.svg?react';
import MechVillageHelpSprite from '../../assets/mechanics/village_help.svg?react';
import MechSeasonSprite from '../../assets/mechanics/season.svg?react';
import MechIncidentSprite from '../../assets/mechanics/incident.svg?react';
import MechTavernSprite from '../../assets/mechanics/tavern.svg?react';
import MechUkladSprite from '../../assets/mechanics/uklad.svg?react';
import MechAilmentsSprite from '../../assets/mechanics/ailments.svg?react';
import MechDomikiSprite from '../../assets/mechanics/domiki.svg?react';
import { renderIconSprite } from './core';
import type { IconSpriteProps, SpriteComponent } from './core';

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
    guestbook: MechGuestbookSprite,
    vestnik: MechVestnikSprite,
    errands: MechErrandsSprite,
    convoy: MechConvoySprite,
    friendship: MechFriendshipSprite,
    wiki: MechWikiSprite,
    world: MechWorldSprite,
    shop: MechShopSprite,
    village_help: MechVillageHelpSprite,
    season: MechSeasonSprite,
    incident: MechIncidentSprite,
    tavern: MechTavernSprite,
    uklad: MechUkladSprite,
    ailments: MechAilmentsSprite,
    domiki: MechDomikiSprite,
};

export const MechanicSprite = (props: IconSpriteProps) => <>{renderIconSprite('mechanic', mechanicSprites, undefined, props)}</>;

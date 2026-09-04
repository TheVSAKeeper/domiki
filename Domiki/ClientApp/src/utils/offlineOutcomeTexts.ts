import type { GameCommand } from '../types/api';

export const OFFLINE_OUTCOMES_TITLE = 'Дела не сложились';

export const OFFLINE_OUTCOMES_SUBTITLE = 'Пока связи не было, деревня не стояла на месте – и эти дела ко двору не пришлись.';

export const EXPIRED_COMMAND_TEXT = 'Это дело ждало связи слишком долго – деревня за это время ушла вперёд. Начните заново.';

const DEED_LABELS: Record<GameCommand['kind'], string> = {
    BuyDomik: 'Стройка не началась.',
    UpgradeDomik: 'Улучшение не началось.',
    StartManufacture: 'Смена не началась.',
    HurryManufacture: 'Поторопить не вышло.',
    SetManufactureAutoRepeat: 'Наряд остался как был.',
    CompleteOrder: 'Заказ не сдали.',
};

export const deedLabel = (kind: GameCommand['kind']): string => DEED_LABELS[kind];

export const offlineOutcomeText = (kind: GameCommand['kind'], reason: string): string =>
    reason === EXPIRED_COMMAND_TEXT ? reason : `${deedLabel(kind)} ${reason}`;

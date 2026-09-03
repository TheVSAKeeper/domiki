export const MIN_DURATION_SHARE = 0.6;
export const ZEAL_MAX_RECIPE_SECONDS = 3600;
export const ZEAL_X4_THRESHOLD = 16;

export interface ManufactureDurationInput {
    receiptDurationSeconds: number;
    traitDurationPercents: number[];
    skillBonusPercents: number[];
    profilePercent: number;
    perkPercent: number;
    isMarketDomik: boolean;
    zealCharges: number;
}

export interface ManufactureDurationResult {
    seconds: number;
    zealSpent: boolean;
}

function average(values: number[]): number {
    return values.reduce((sum, value) => sum + value, 0) / values.length;
}

export function computeManufactureDuration(input: ManufactureDurationInput): ManufactureDurationResult {
    let duration = input.receiptDurationSeconds;

    duration = Math.ceil((duration * (100 + average(input.traitDurationPercents))) / 100);
    duration = Math.ceil((duration * (100 - average(input.skillBonusPercents))) / 100);
    duration = Math.ceil((duration * input.profilePercent) / 100);
    duration = Math.ceil((duration * input.perkPercent) / 100);
    duration = Math.max(duration, Math.ceil(input.receiptDurationSeconds * MIN_DURATION_SHARE));

    const zealAllowed = input.receiptDurationSeconds <= ZEAL_MAX_RECIPE_SECONDS && !input.isMarketDomik;
    if (!zealAllowed || input.zealCharges <= 0) {
        return { seconds: duration, zealSpent: false };
    }

    const multiplier = input.zealCharges > ZEAL_X4_THRESHOLD ? 4 : 2;
    return { seconds: Math.max(1, Math.trunc(duration / multiplier)), zealSpent: true };
}

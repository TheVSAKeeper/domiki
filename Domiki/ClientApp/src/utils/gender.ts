export const GENDER_FEMALE = 2;

export const isFemale = (gender: number | undefined): boolean => gender === GENDER_FEMALE;

export const genderForm = (gender: number | undefined, male: string, female: string): string => isFemale(gender) ? female : male;

const TRAIT_FORMS: Record<string, readonly [string, string]> = {
    ordinary: ['Обычный', 'Обычная'],
    nimble: ['Проворный', 'Проворная'],
    diligent: ['Работящий', 'Работящая'],
    sonya: ['Соня', 'Соня'],
    lucky: ['Везучий', 'Везучая'],
};

export const traitLabel = (logicName: string, fallback: string, gender: number | undefined): string => {
    const forms = TRAIT_FORMS[logicName];
    return forms == null ? fallback : (isFemale(gender) ? forms[1] : forms[0]);
};

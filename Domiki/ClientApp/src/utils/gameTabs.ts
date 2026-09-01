export const BOARD_TAB_KEY = 'board';

export const tabPath = (key: string) => key === BOARD_TAB_KEY ? '/domiki-page' : `/domiki-page/${key}`;

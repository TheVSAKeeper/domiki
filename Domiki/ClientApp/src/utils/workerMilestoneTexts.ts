import { genderForm } from './gender';

export interface WorkerMilestoneTemplate {
    milestoneType: number;
    journal: string;
    epilogue: string;
    toast: string;
}

const template1: WorkerMilestoneTemplate = {
    milestoneType: 1,
    journal: 'Первая смена всегда длинней остальных. {имя} трижды {переспросил|переспросила}, куда класть готовое, разок {уронил|уронила} рукавицу – а к вечеру {сдал|сдала} работу без единой подсказки. Старшие переглянулись: наш человек.',
    epilogue: '«Завтра приду раньше всех. Ну… почти раньше всех.»',
    toast: '{имя} – первая смена позади!',
};

export const templates: WorkerMilestoneTemplate[] = [
    template1,
    {
        milestoneType: 2,
        journal: 'Сотую смену {имя} {отработал|отработала} тихо, как обычную. А вот артель считала: на притолоке – зарубка за каждую смену. Вечером сотую обвели угольком и устроили посиделки: пирог, черёмуховый чай и сто добрых слов – по слову за смену.',
        epilogue: '«Сто? А я и не {считал|считала}. Руки сами помнят.»',
        toast: 'У {имя} – сотая смена. Артель гуляет!',
    },
    {
        milestoneType: 3,
        journal: 'Пятьдесят смен в одном деле – и {имя} больше не примеряется: мерка на глаз, инструмент сам ложится в ладонь, а брак сошёл, как снег с крыши. В деревне про такое говорят коротко: рука набита. Теперь это – про {имя}.',
        epilogue: '«Секрет? Пятьдесят раз подряд – вот и весь секрет.»',
        toast: '{имя} – мастер своего дела!',
    },
    {
        milestoneType: 4,
        journal: 'Месяц назад {имя} впервые {повесил|повесила} котомку на гвоздь у входа в барак. К юбилею барак сговорился: отскребли нары, застелили свежее сено – а под половицей нашлась жестянка прежнего жильца: огниво, леденцы и записка «кто нашёл – того и счастье».',
        epilogue: '«Гвоздь у входа теперь мой. Это, считай, главная должность.»',
        toast: '{имя} – месяц в деревне!',
    },
    {
        milestoneType: 5,
        journal: '{имя} и {имя2} месяц спорили, чья смена ловчей, – до зарубок на верстаке доспорились. Сочли зарубки: ровно поровну. Тогда сдвинули верстаки вместе и работают теперь парой, а спорят об одном – кому идти ставить чай.',
        epilogue: '«Спорить – спорим. Но верстак теперь один: так сподручней.»',
        toast: '{имя} и {имя2} – напарники!',
    },
    {
        milestoneType: 6,
        journal: 'Десятую дорогу {имя} {прошагал|прошагала} – и окрестные тропы теперь здороваются в ответ: заречный паромщик машет с середины реки, боровские охотники окликают по имени, а в Глинищах на заборе держат кружку – «для своих». Дальше околицы – а всё как дома.',
        epilogue: '«Дорога – та же смена. Только небо выше.»',
        toast: '{имя} – десять дорог за плечами!',
    },
];

export function getWorkerMilestoneTemplate(milestoneType: number): WorkerMilestoneTemplate {
    return templates.find(template => template.milestoneType === milestoneType) ?? template1;
}

export function workerMilestoneText(text: string, heroName: string, heroGender: number | undefined, partnerName?: string, partnerGender?: number): string {
    return text
        .replaceAll('{имя2}', partnerName ?? '')
        .replaceAll('{имя}', heroName)
        .replaceAll('{м2|ж2}', genderForm(partnerGender, 'м', 'ж'))
        .replace(/\{([^{}|]*)\|([^{}|]*)\}/g, (_, male: string, female: string) => genderForm(heroGender, male, female));
}

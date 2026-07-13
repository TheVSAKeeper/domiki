import { type CSSProperties, type FC, type SVGProps, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import PlayIcon from 'pixelarticons/svg/play.svg?react';
import LoginIcon from 'pixelarticons/svg/login.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import { authService } from '../services/auth';
import {
    AbstractSprite,
    DomikSprite,
    MechanicSprite,
    NeighborSprite,
    ResourceSprite,
    WorkerSprite,
} from './sprites';

const usePrefersReducedMotion = (): boolean => {
    const [reduce, setReduce] = useState(false);
    useEffect(() => {
        const mq = window.matchMedia('(prefers-reduced-motion: reduce)');
        const update = () => setReduce(mq.matches);
        update();
        mq.addEventListener('change', update);
        return () => { mq.removeEventListener('change', update); };
    }, []);
    return reduce;
};

interface Building {
    name: string;
    level: number;
    working: boolean;
}

const village: Building[] = [
    { name: 'lumber_mill', level: 3, working: true },
    { name: 'clay_mine', level: 2, working: true },
    { name: 'stone_mine', level: 4, working: false },
    { name: 'forge', level: 3, working: true },
    { name: 'market', level: 2, working: false },
    { name: 'workshop', level: 4, working: true },
    { name: 'gold_mine', level: 2, working: false },
    { name: 'barracks', level: 5, working: false },
];

interface Villager {
    name: string;
    state: 'idle' | 'working' | 'resting';
    left: number;
    delay: number;
}

const villagers: Villager[] = [
    { name: 'Дарья', state: 'working', left: 20, delay: 0 },
    { name: 'Аким', state: 'idle', left: 52, delay: .6 },
    { name: 'Велена', state: 'working', left: 78, delay: 1.1 },
];

interface Drift {
    resource: string;
    left: number;
    delay: number;
}

const drifts: Drift[] = [
    { resource: 'wood', left: 16, delay: 0 },
    { resource: 'clay', left: 42, delay: 2.4 },
    { resource: 'tool', left: 66, delay: 1.2 },
    { resource: 'coin', left: 88, delay: 3.1 },
];

interface Ledger {
    coins: number;
    wood: number;
    stone: number;
    tick: number;
}

const COIN_PER_TICK = 9;
const awayLines = ['+18 досок пока вкладка спала', '2 экспедиции вернулись за ночь', '+240 монет, пока ты отходил', 'толока закрыта – всем бафф'];

const LiveTreasury = () => {
    const reduce = usePrefersReducedMotion();
    const [ledger, setLedger] = useState<Ledger>({ coins: 12480, wood: 340, stone: 210, tick: 0 });
    const [awayIndex, setAwayIndex] = useState(0);

    useEffect(() => {
        if (reduce) {
            return;
        }
        const tick = setInterval(() => {
            setLedger(l => ({
                coins: l.coins + COIN_PER_TICK,
                wood: l.wood + (l.tick % 3 === 0 ? 1 : 0),
                stone: l.stone + (l.tick % 5 === 0 ? 1 : 0),
                tick: l.tick + 1,
            }));
        }, 1900);
        const rotate = setInterval(() => setAwayIndex(i => (i + 1) % awayLines.length), 3800);
        return () => { clearInterval(tick); clearInterval(rotate); };
    }, [reduce]);

    const num = (value: number) => value.toLocaleString('ru-RU');

    return (
        <div className="treasury">
            <span className="treasury-label">живая казна</span>
            <span className="treasury-item"><ResourceSprite logicName="coin" size={24} aria-hidden="true" />{num(ledger.coins)}</span>
            <span className="treasury-item"><ResourceSprite logicName="wood" size={24} aria-hidden="true" />{num(ledger.wood)}</span>
            <span className="treasury-item"><ResourceSprite logicName="stone" size={24} aria-hidden="true" />{num(ledger.stone)}</span>
            {ledger.tick > 0 && <span key={ledger.tick} className="coin-pop" aria-hidden="true">{`+${COIN_PER_TICK}`}</span>}
            {!reduce && <span key={awayIndex} className="treasury-away">{awayLines[awayIndex]}</span>}
        </div>
    );
};

const Diorama = () => {
    const reduce = usePrefersReducedMotion();
    return (
        <div className="scene">
            <div className="diorama" aria-hidden="true">
                <div className="sky">
                    <span className="cloud cloud-1" />
                    <span className="cloud cloud-2" />
                    <span className="cloud cloud-3" />
                    <span className="sun" />
                </div>
                <div className="hills" />
                <div className="village-strip">
                    {village.map((b, i) => (
                        <div key={b.name} className="lot" style={{ '--i': i } as CSSProperties}>
                            {b.working && !reduce && <span className="smoke" />}
                            <DomikSprite logicName={b.name} level={b.level} working={b.working} />
                        </div>
                    ))}
                    {!reduce && drifts.map(d => (
                        <span key={d.resource} className="drift" style={{ '--l': `${d.left}%`, '--d': `${d.delay}s` } as CSSProperties}>
                            <ResourceSprite logicName={d.resource} size={24} aria-hidden="true" />
                        </span>
                    ))}
                    <div className="path">
                        {villagers.map(v => (
                            <span key={v.name} className="villager" style={{ '--l': `${v.left}%`, '--d': `${v.delay}s` } as CSSProperties}>
                                <WorkerSprite name={v.name} state={v.state} />
                            </span>
                        ))}
                    </div>
                </div>
            </div>
            <LiveTreasury />
        </div>
    );
};

interface Trophy {
    Icon: FC<SVGProps<SVGSVGElement>>;
    num: string;
    cap: string;
    tone: string;
}

const trophies: Trophy[] = [
    { Icon: props => <AbstractSprite logicName="production_recipe" {...props} />, num: '128', cap: 'ресурсов наварили', tone: 'prod' },
    { Icon: props => <MechanicSprite logicName="expeditions" {...props} />, num: '2', cap: 'похода вернулись', tone: 'exp' },
    { Icon: props => <DomikSprite logicName="forge" level={3} {...props} />, num: '1', cap: 'постройка выросла', tone: 'build' },
    { Icon: props => <MechanicSprite logicName="toloka" {...props} />, num: '+40%', cap: 'бафф от толоки', tone: 'toloka' },
];

const RecapBand = () => (
    <section className="recap-band">
        <div className="recap-band-ribbon" aria-hidden="true">
            {Array.from({ length: 20 }, (_, i) => <i key={i} />)}
        </div>
        <div className="recap-band-head">
            <span className="recap-band-eyebrow">idle без обмана</span>
            <h2 className="recap-band-title">Пока вас не было</h2>
            <p className="recap-band-text">Деревня трудится, даже когда вкладка закрыта. Возвращаешься – и тебя встречает честный отчёт: сколько наварили домовята, кто пришёл из похода, что достроили. Не «+1 к цифре», а живой пересказ ночи.</p>
        </div>
        <div className="recap-band-trophies">
            {trophies.map(t => (
                <span key={t.cap} className="recap-band-trophy" data-tone={t.tone}>
                    <span className="tr-ico"><t.Icon aria-hidden="true" /></span>
                    <span className="tr-num">{t.num}</span>
                    <span className="tr-cap">{t.cap}</span>
                </span>
            ))}
        </div>
        <div className="recap-band-detail">
            <span className="recap-band-line"><WorkerSprite name="Дарья" state="idle" className="recap-face" /> Дарья вернулась из похода с редким сундуком</span>
            <span className="recap-band-line"><DomikSprite logicName="forge" level={3} className="recap-face" /> Кузница выросла до 3 уровня</span>
        </div>
        <div className="recap-band-ribbon recap-band-ribbon-bottom" aria-hidden="true">
            {Array.from({ length: 20 }, (_, i) => <i key={i} />)}
        </div>
    </section>
);

interface Inhabitant {
    resident: string;
    mech: string;
    title: string;
    text: string;
}

const inhabitants: Inhabitant[] = [
    { resident: 'соседи', mech: 'orders', title: 'Заказы', text: 'Соседи просят кирпич, доски, зерно. Выполнил – монеты, золото и репутация. Репутация открывает новые рецепты и чертежи.' },
    { resident: 'артель', mech: 'workers', title: 'Трудяги', text: 'У каждого имя, характер и любимое дело. Растут в навыке до +15%, устают и отдыхают в бараке. Жители, а не счётчик.' },
    { resident: 'небо', mech: 'weather', title: 'Погода', text: 'Дождь, сушь, ярмарочный день двигают выработку на ±25–50%. Прогноз виден на сутки вперёд – планируй, а не гадай.' },
    { resident: 'разведка', mech: 'expeditions', title: 'Экспедиции', text: 'Снаряди артель на 4–24 часа – вернутся с сундуком: редкие ресурсы, чертежи, декор, новые трудяги.' },
    { resident: 'сходка', mech: 'toloka', title: 'Толока', text: 'Всем миром на общий проект – мост, амбар, печь. Достроили сообща – каждому +40% к делу на время.' },
    { resident: 'торг', mech: 'market', title: 'Ярмарка', text: 'Выставил лот излишков – и пошёл дальше. Кто-то из соседей примет позже. Обмен без спешки и без чата.' },
    { resident: 'мастер', mech: 'blueprints', title: 'Чертежи', text: 'Пекарня, гончарня, мельница – их не купить. Только найти в походе или заслужить репутацией у соседа.' },
    { resident: 'уют', mech: 'decor', title: 'Декор', text: 'Заборы, фонтаны, сады – сливай излишки в уют. Обжитая деревня даёт трудягам отдохнуть быстрее.' },
];

const Inhabitants = () => (
    <section className="inhabitants">
        <div className="section-head">
            <span className="section-eyebrow">живой организм</span>
            <h2 className="section-title">Кто живёт в деревне</h2>
        </div>
        <div className="obzhit-banner">
            <MechanicSprite logicName="obzhitost" size={48} className="obzhit-ico" aria-hidden="true" />
            <div className="obzhit-body">
                <h3 className="obzhit-title">Обжитость</h3>
                <p className="obzhit-text">Уровень деревни – не полоска опыта, а зеркало. Он считается из того, что ты реально построил, кого поселил, как обжился. Гриндить нечего: просто обустраивай – и деревня оживает сама.</p>
            </div>
        </div>
        <div className="pillar-grid">
            {inhabitants.map((p, i) => (
                <div key={p.title} className="pillar" style={{ '--i': i } as CSSProperties}>
                    <span className="pillar-resident">{p.resident}</span>
                    <div className="pillar-head">
                        <MechanicSprite logicName={p.mech} size={40} className="pillar-ico" aria-hidden="true" />
                        <h3 className="pillar-title">{p.title}</h3>
                    </div>
                    <p className="pillar-text">{p.text}</p>
                </div>
            ))}
        </div>
    </section>
);

const steps = [
    { title: 'Строй и заселяй', text: 'Покупай домики, поднимай уровни, селись. Новые жители, рецепты и постройки открываются по мере обжитости.' },
    { title: 'Запускай производства', text: 'Трудяги варят ресурсы по рецептам. Ушёл – всё копится само, даже с закрытой вкладкой. Вернулся – экран «Пока вас не было».' },
    { title: 'Торгуй и странствуй', text: 'Бери заказы соседей, шли артель в походы, вкладывайся в толоку, лови погоду. Деревня растёт не ввысь, а вглубь.' },
];

const HowTo = () => (
    <section className="steps-section">
        <div className="section-head">
            <span className="section-eyebrow">каденция 2–3 захода в день</span>
            <h2 className="section-title">Как играть</h2>
        </div>
        <div className="steps">
            {steps.map((step, i) => (
                <div key={step.title} className="step">
                    <span className="step-num">{`0${i + 1}`}</span>
                    <h3 className="step-title">{step.title}</h3>
                    <p className="step-text">{step.text}</p>
                </div>
            ))}
        </div>
        <p className="cadence-note">Заглядывай на пять минут утром и вечером – игра под ритм жизни, а не наоборот.</p>
    </section>
);

const neighbors = [
    { logic: 'zarechye', name: 'Заречье', want: 'любит кирпич' },
    { logic: 'borovoe', name: 'Боровое', want: 'просит доски' },
    { logic: 'kamenka', name: 'Каменка', want: 'берёт камень' },
    { logic: 'glinischi', name: 'Глинищи', want: 'ждёт глину' },
    { logic: 'dubrava', name: 'Дубрава', want: 'ценит дерево' },
];

const SharedWorld = () => (
    <section className="shared-world pixel-panel">
        <div className="section-head">
            <span className="section-eyebrow">асинхронный мир</span>
            <h2 className="section-title">Живёшь не один</h2>
        </div>
        <p className="shared-text">Мир общий, но без суеты: ни чата, ни PvP. Одна погода на всех, толока всем миром, ярмарка обмена, сезонные рейтинги деревень и визиты в гости – по-соседски, в своём темпе.</p>
        <div className="neighbor-row">
            {neighbors.map(n => (
                <div key={n.logic} className="neighbor">
                    <NeighborSprite logicName={n.logic} size={48} className="neighbor-ico" aria-hidden="true" />
                    <span className="neighbor-name">{n.name}</span>
                    <span className="neighbor-want">{n.want}</span>
                </div>
            ))}
        </div>
    </section>
);

export const Home = () => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);

    useEffect(() => {
        const update = () => {
            void authService.isAuthenticated().then(setIsAuthenticated);
        };
        const subscription = authService.subscribe(update);
        update();
        return () => { authService.unsubscribe(subscription); };
    }, []);

    const loginDemo = async () => {
        const ok = await authService.loginDemo();
        if (ok) {
            window.location.assign('/domiki-page');
        }
    };

    const cta = isAuthenticated
        ? <Link className="btn-game" to="/domiki-page"><BuildingIcon className="btn-ico" aria-hidden="true" />В деревню</Link>
        : <>
            <button className="btn-game" onClick={() => void loginDemo()}><PlayIcon className="btn-ico" aria-hidden="true" />Играть демо</button>
            <a className="btn-ghost" href="/authentication/login"><LoginIcon className="btn-ico" aria-hidden="true" />Войти</a>
        </>;

    return (
        <div className="home">
            <section className="hero pixel-panel">
                <p className="hero-eyebrow">Уютная idle-деревня в браузере</p>
                <h1 className="hero-title">DOMIKI</h1>
                <p className="hero-tagline">Деревенька, что живёт сама. Строишь домики, селишь трудяг, берёшь заказы соседей – а ресурсы копятся даже с закрытой вкладкой.</p>
                <div className="hero-cta">{cta}</div>
                <Diorama />
            </section>

            <RecapBand />
            <Inhabitants />
            <HowTo />
            <SharedWorld />

            <section className="final-cta">
                <h2 className="final-title">Деревня уже топит печи</h2>
                <p className="final-text">Загляни – она давно тебя ждёт.</p>
                <div className="hero-cta">{cta}</div>
            </section>
        </div>
    );
};

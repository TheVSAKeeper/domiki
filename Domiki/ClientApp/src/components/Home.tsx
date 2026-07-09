import { type CSSProperties, type FC, type MouseEvent, type SVGProps, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import PlayIcon from 'pixelarticons/svg/play.svg?react';
import LoginIcon from 'pixelarticons/svg/login.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import UsersIcon from 'pixelarticons/svg/users.svg?react';
import HumanIcon from 'pixelarticons/svg/human.svg?react';
import FlagIcon from 'pixelarticons/svg/flag.svg?react';
import CloudSunIcon from 'pixelarticons/svg/cloud-sun.svg?react';
import ClipboardIcon from 'pixelarticons/svg/clipboard-note.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import { authService } from '../services/auth';
import { DomikSprite } from './sprites';

const buildings = ['market', 'lumber_mill', 'clay_mine', 'stone_mine', 'gold_mine', 'forge', 'workshop', 'barracks'];

const steps = [
    { title: 'Строй и обживай', text: 'Покупай домики и мастерскую, поднимай уровни. Деревня растёт – новые жители, рецепты и постройки по чертежам.' },
    { title: 'Запускай производство', text: 'Трудяги варят ресурсы по рецептам. Уходи – всё копится само, даже с закрытой вкладкой.' },
    { title: 'Торгуй и исследуй', text: 'Выполняй заказы соседей за репутацию, шли артель в экспедиции, лови погоду, обустраивай уют.' },
];

interface Pillar {
    resident: string;
    title: string;
    text: string;
    Icon: FC<SVGProps<SVGSVGElement>>;
}

const pillars: Pillar[] = [
    { resident: 'соседи', title: 'Заказы', text: 'Соседи просят ресурсы – платят монетой, золотом и репутацией.', Icon: UsersIcon },
    { resident: 'артель', title: 'Трудяги', text: 'У каждого имя, черта и навык. Растут на любимой работе.', Icon: HumanIcon },
    { resident: 'разведчики', title: 'Экспедиции', text: 'Отправь артель за случайным сундуком. Риск – трудяги устают.', Icon: FlagIcon },
    { resident: 'небо', title: 'Погода', text: 'Дожди и вёдро меняют выработку. Планируй под прогноз.', Icon: CloudSunIcon },
    { resident: 'мастер', title: 'Чертежи', text: 'Репутация соседа открывает мастерскую и новые постройки.', Icon: ClipboardIcon },
    { resident: 'уют', title: 'Декор', text: 'Сливай излишки в декор – деревня обживается, отдых быстрее.', Icon: HomeIcon },
];

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

interface Ledger {
    coins: number;
    wood: number;
    stone: number;
    tick: number;
}

const COIN_PER_TICK = 9;

const LiveTreasury = () => {
    const reduce = usePrefersReducedMotion();
    const [ledger, setLedger] = useState<Ledger>({ coins: 12480, wood: 340, stone: 210, tick: 0 });

    useEffect(() => {
        if (reduce) {
            return;
        }
        const id = setInterval(() => {
            setLedger(l => ({
                coins: l.coins + COIN_PER_TICK,
                wood: l.wood + (l.tick % 3 === 0 ? 1 : 0),
                stone: l.stone + (l.tick % 5 === 0 ? 1 : 0),
                tick: l.tick + 1,
            }));
        }, 1900);
        return () => { clearInterval(id); };
    }, [reduce]);

    const num = (value: number) => value.toLocaleString('ru-RU');

    return (
        <div className="treasury">
            <span className="treasury-label">живая казна</span>
            <span className="treasury-item"><img src="/images/resourceTypes/coin.png" alt="" />{num(ledger.coins)}</span>
            <span className="treasury-item"><img src="/images/resourceTypes/wood.png" alt="" />{num(ledger.wood)}</span>
            <span className="treasury-item"><img src="/images/resourceTypes/stone.png" alt="" />{num(ledger.stone)}</span>
            {ledger.tick > 0 && <span key={ledger.tick} className="coin-pop" aria-hidden="true">{`+${COIN_PER_TICK}`}</span>}
        </div>
    );
};

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

    const loginDemo = async (e: MouseEvent) => {
        e.preventDefault();
        const ok = await authService.loginDemo();
        if (ok) {
            window.location.assign('/domiki-page');
        }
    };

    return (
        <div className="home">
            <section className="hero pixel-panel">
                <p className="hero-eyebrow">Браузерная деревня-ферма</p>
                <h1 className="hero-title">DOMIKI</h1>
                <p className="hero-tagline">Деревня, которая работает на тебя. Строй домики, нанимай трудяг, бери заказы соседей – ресурсы копятся сами, даже с закрытой вкладкой.</p>
                <div className="hero-cta">
                    {isAuthenticated
                        ? <Link className="btn-game" to="/domiki-page"><BuildingIcon className="btn-ico" aria-hidden="true" />В деревню</Link>
                        : <>
                            <button className="btn-game" onClick={loginDemo}><PlayIcon className="btn-ico" aria-hidden="true" />Играть демо</button>
                            <a className="btn-ghost" href="/authentication/login"><LoginIcon className="btn-ico" aria-hidden="true" />Войти</a>
                        </>
                    }
                </div>
                <div className="scene">
                    <div className="village-strip" aria-hidden="true">
                        {buildings.map((name, i) => (
                            <DomikSprite key={name} logicName={name} level={(i % 5) + 1} style={{ '--i': i } as CSSProperties} />
                        ))}
                    </div>
                    <LiveTreasury />
                </div>
            </section>

            <section className="pillars">
                <h2 className="section-head">Кто живёт в деревне</h2>
                <div className="pillar-grid">
                    {pillars.map((pillar, i) => (
                        <div key={pillar.title} className="pillar" style={{ '--i': i } as CSSProperties}>
                            <span className="pillar-resident">{pillar.resident}</span>
                            <div className="pillar-head">
                                <pillar.Icon className="pillar-ico" aria-hidden="true" />
                                <h3 className="pillar-title">{pillar.title}</h3>
                            </div>
                            <p className="pillar-text">{pillar.text}</p>
                        </div>
                    ))}
                </div>
            </section>

            <section className="steps-section">
                <h2 className="section-head">Как играть</h2>
                <div className="steps">
                    {steps.map((step, i) => (
                        <div key={step.title} className="step">
                            <span className="step-num">{`0${i + 1}`}</span>
                            <h3 className="step-title">{step.title}</h3>
                            <p className="step-text">{step.text}</p>
                        </div>
                    ))}
                </div>
            </section>
        </div>
    );
};

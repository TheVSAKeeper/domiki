import { type CSSProperties, type MouseEvent, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import PlayIcon from 'pixelarticons/svg/play.svg?react';
import LoginIcon from 'pixelarticons/svg/login.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import { authService } from '../services/auth';

const buildings = ['market', 'lumber_mill', 'clay_mine', 'stone_mine', 'gold_mine', 'forge', 'barracks'];

const steps = [
    { title: 'Стройка', text: 'Покупай домики – рынок, шахты, кузницу. Каждый занимает своё место в деревне.' },
    { title: 'Улучшение', text: 'Вкладывай ресурсы в уровни. Выше уровень – больше трудяг и новых рецептов.' },
    { title: 'Производство', text: 'Запусти рецепт и уходи. Ресурсы копятся сами, даже с закрытой вкладкой.' },
];

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
                <p className="hero-tagline">Маленькая деревня, которая работает на тебя. Строй домики, запускай производства и собирай ресурсы – хоть с закрытой вкладкой.</p>
                <div className="hero-cta">
                    {isAuthenticated
                        ? <Link className="btn-game" to="/domiki-page"><BuildingIcon className="btn-ico" aria-hidden="true" />В деревню</Link>
                        : <>
                            <button className="btn-game" onClick={loginDemo}><PlayIcon className="btn-ico" aria-hidden="true" />Играть демо</button>
                            <a className="btn-ghost" href="/authentication/login"><LoginIcon className="btn-ico" aria-hidden="true" />Войти</a>
                        </>
                    }
                </div>
                <div className="village-strip" aria-hidden="true">
                    {buildings.map((name, i) => (
                        <img key={name} src={'/images/domikTypes/' + name + '.png'} alt="" style={{ '--i': i } as CSSProperties} />
                    ))}
                </div>
            </section>

            <section className="steps">
                {steps.map((step, i) => (
                    <div key={step.title} className="step">
                        <span className="step-num">{`0${i + 1}`}</span>
                        <h2 className="step-title">{step.title}</h2>
                        <p className="step-text">{step.text}</p>
                    </div>
                ))}
            </section>
        </div>
    );
};

import { useState } from 'react';
import { Link } from 'react-router-dom';
import MenuIcon from 'pixelarticons/svg/menu.svg?react';
import HomeIcon from 'pixelarticons/svg/home.svg?react';
import { LoginMenu } from './api-authorization/LoginMenu';
import { ChangelogLink } from './ChangelogLink';
import { InstallHint } from './InstallHint';
import { PushToggle } from './PushToggle';
import { MechanicSprite } from './sprites';

export const NavMenu = () => {
    const [open, setOpen] = useState(false);
    const close = () => setOpen(false);

    return (
        <header className="site-header">
            <nav className="topnav">
                <div className="topnav-inner">
                    <Link className="brand" to="/" onClick={close}>Domiki</Link>
                    <div id="village-slot" className="nav-village" />
                    <button
                        type="button"
                        className="nav-toggle"
                        aria-label="Меню"
                        aria-expanded={open}
                        onClick={() => setOpen(value => !value)}
                    >
                        <MenuIcon className="nav-ico" aria-hidden="true" />
                    </button>
                    <ul className={'nav-links' + (open ? ' nav-links-open' : '')}>
                        <li>
                            <Link className="nav-link" to="/" onClick={close}>
                                <HomeIcon className="nav-ico" aria-hidden="true" />
                                Главная
                            </Link>
                        </li>
                        <li>
                            <Link className="nav-link" to="/domiki-page" onClick={close}>
                                <MechanicSprite logicName="domiki" size={24} className="nav-ico" aria-hidden="true" />
                                Домики
                            </Link>
                        </li>
                        <li>
                            <Link className="nav-link" to="/world" onClick={close}>
                                <MechanicSprite logicName="world" size={24} className="nav-ico" aria-hidden="true" />
                                Мир
                            </Link>
                        </li>
                        <li>
                            <Link className="nav-link" to="/wiki" onClick={close}>
                                <MechanicSprite logicName="wiki" size={24} className="nav-ico" aria-hidden="true" />
                                Справочник
                            </Link>
                        </li>
                        <li>
                            <ChangelogLink onNavigate={close} />
                        </li>
                        <li>
                            <PushToggle />
                        </li>
                        <li>
                            <InstallHint />
                        </li>
                        <LoginMenu />
                    </ul>
                </div>
            </nav>
        </header>
    );
};

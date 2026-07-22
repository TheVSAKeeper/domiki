import { useState } from 'react';
import { Link } from 'react-router-dom';
import MenuIcon from 'pixelarticons/svg/menu.svg?react';
import BuildingIcon from 'pixelarticons/svg/building.svg?react';
import { LoginMenu } from './api-authorization/LoginMenu';
import { MechanicSprite } from './sprites';

export const NavMenu = () => {
    const [open, setOpen] = useState(false);
    const close = () => setOpen(false);

    return (
        <header className="site-header">
            <nav className="topnav">
                <div className="topnav-inner">
                    <Link className="brand" to="/" onClick={close}>Domiki</Link>
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
                            <Link className="nav-link" to="/domiki-page" onClick={close}>
                                <BuildingIcon className="nav-ico" aria-hidden="true" />
                                Домики
                            </Link>
                        </li>
                        <li>
                            <Link className="nav-link" to="/wiki" onClick={close}>
                                <MechanicSprite logicName="wiki" size={24} className="nav-ico" aria-hidden="true" />
                                Справочник
                            </Link>
                        </li>
                        <LoginMenu />
                    </ul>
                </div>
            </nav>
        </header>
    );
};

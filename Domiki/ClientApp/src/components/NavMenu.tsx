import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Collapse, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink } from 'reactstrap';
import { LoginMenu } from './api-authorization/LoginMenu';
import './NavMenu.css';

export const NavMenu = () => {
    const [collapsed, setCollapsed] = useState(true);

    return (
        <header>
            <Navbar className="navbar-expand-sm navbar-toggleable-sm border-bottom box-shadow mb-3" container light>
                <NavbarBrand tag={Link} to="/">Domiki</NavbarBrand>
                <NavbarToggler onClick={() => setCollapsed(!collapsed)} className="mr-2" />
                <Collapse className="d-sm-inline-flex flex-sm-row-reverse" isOpen={!collapsed} navbar>
                    <ul className="navbar-nav flex-grow">
                        <NavItem>
                            <NavLink tag={Link} to="/domiki-page">Домики</NavLink>
                        </NavItem>
                        <LoginMenu />
                    </ul>
                </Collapse>
            </Navbar>
        </header>
    );
};

import React, { Component, Fragment } from 'react';
import { NavItem, NavLink } from 'reactstrap';
import authService from './AuthorizeService';

export class LoginMenu extends Component {
  constructor(props) {
    super(props);

    this.state = {
      isAuthenticated: false,
      userName: null
    };
  }

  componentDidMount() {
    this._subscription = authService.subscribe(() => this.populateState());
    this.populateState();
  }

  componentWillUnmount() {
    authService.unsubscribe(this._subscription);
  }

  async populateState() {
    const [isAuthenticated, user] = await Promise.all([authService.isAuthenticated(), authService.getUser()])
    this.setState({
      isAuthenticated,
      userName: user && user.name
    });
  }

  render() {
    const { isAuthenticated, userName } = this.state;
    if (!isAuthenticated) {
      return this.anonymousView();
    }
    return this.authenticatedView(userName);
  }

  authenticatedView(userName) {
    return (<Fragment>
      <NavItem>
        <span className="nav-link text-dark">{userName}</span>
      </NavItem>
      <NavItem>
        <NavLink tag="a" className="text-dark" href="/authentication/logout">Выйти</NavLink>
      </NavItem>
    </Fragment>);
  }

  anonymousView() {
    return (<Fragment>
      <NavItem>
        <NavLink tag="a" className="text-dark" href="/authentication/login">Войти</NavLink>
      </NavItem>
    </Fragment>);
  }
}

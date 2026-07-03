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
        <span className="nav-user">{userName}</span>
      </NavItem>
      <NavItem>
        <NavLink tag="a" href="/authentication/logout">Выйти</NavLink>
      </NavItem>
    </Fragment>);
  }

  loginDemo = async (e) => {
    e.preventDefault();
    const response = await fetch('/authentication/demo', { method: 'POST', credentials: 'same-origin' });
    if (response.ok) {
      window.location.assign('/domiki-page');
    }
  };

  anonymousView() {
    return (<Fragment>
      <NavItem>
        <button type="button" className="nav-cta" onClick={this.loginDemo}>Играть демо</button>
      </NavItem>
      <NavItem>
        <NavLink tag="a" href="/authentication/login">Войти</NavLink>
      </NavItem>
    </Fragment>);
  }
}

import { Route, Routes, type RouteProps } from 'react-router-dom';
import AppRoutes from './AppRoutes';
import { AuthorizeRoute } from './components/api-authorization/AuthorizeRoute';
import { Layout } from './components/Layout';
import './custom.css';

const App = () => {
    return (
        <Layout>
            <Routes>
                {AppRoutes.map((route, index) => {
                    const { element, requireAuth, ...rest } = route;
                    return (
                        <Route
                            key={index}
                            {...(rest as RouteProps)}
                            element={requireAuth ? <AuthorizeRoute {...rest} element={element} /> : element}
                        />
                    );
                })}
            </Routes>
        </Layout>
    );
};

export default App;

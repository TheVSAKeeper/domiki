import { Route, Routes } from 'react-router-dom';
import AppRoutes from './AppRoutes';
import { AuthorizeRoute } from './components/api-authorization/AuthorizeRoute';
import { Layout } from './components/Layout';
import './styles/index.css';

const App = () => {
    return (
        <Layout>
            <Routes>
                {AppRoutes.map(route => {
                    const { element, requireAuth, ...rest } = route;
                    return (
                        <Route
                            key={route.path ?? '/'}
                            {...rest}
                            element={requireAuth ? <AuthorizeRoute {...rest} element={element} /> : element}
                        />
                    );
                })}
            </Routes>
        </Layout>
    );
};

export default App;

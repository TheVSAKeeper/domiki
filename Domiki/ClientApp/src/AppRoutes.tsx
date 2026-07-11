import { lazy, Suspense } from 'react';
import type { ReactNode } from 'react';
import { PixelLoader } from './components/PixelLoader';

const Home = lazy(() => import('./components/Home').then(({ Home }) => ({ default: Home })));
const DomikiPage = lazy(() => import('./components/DomikiPage').then(({ DomikiPage }) => ({ default: DomikiPage })));
const Wiki = lazy(() => import('./components/Wiki').then(({ Wiki }) => ({ default: Wiki })));
const WorldPage = lazy(() => import('./components/WorldPage').then(({ WorldPage }) => ({ default: WorldPage })));

const lazyPage = (page: ReactNode) => <Suspense fallback={<PixelLoader label="Загрузка…" />}>{page}</Suspense>;

export interface AppRouteConfig {
    index?: boolean;
    path?: string;
    element: ReactNode;
    requireAuth?: boolean;
}

const AppRoutes: AppRouteConfig[] = [
    {
        index: true,
        element: lazyPage(<Home />),
    },
    {
        path: '/domiki-page',
        element: lazyPage(<DomikiPage />),
    },
    {
        path: '/wiki',
        element: lazyPage(<Wiki />),
    },
    {
        path: '/world',
        element: lazyPage(<WorldPage />),
    },
];

export default AppRoutes;

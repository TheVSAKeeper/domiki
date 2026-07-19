import type { ReactNode } from 'react';
import { Toaster } from '../services/toast';
import { NavMenu } from './NavMenu';
import { UpdateBanner } from './UpdateBanner';

interface LayoutProps {
    children: ReactNode;
}

export const Layout = ({ children }: LayoutProps) => {
    return (
        <div>
            <NavMenu />
            <main className="app-container">
                {children}
            </main>
            <UpdateBanner />
            <Toaster />
        </div>
    );
};

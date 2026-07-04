import { ReactNode } from 'react';
import { Toaster } from '../services/toast';
import { NavMenu } from './NavMenu';

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
            <Toaster />
        </div>
    );
};

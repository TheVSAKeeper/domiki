import { ReactNode } from 'react';
import { Container } from 'reactstrap';
import { Toaster } from '../services/toast';
import { NavMenu } from './NavMenu';

interface LayoutProps {
    children: ReactNode;
}

export const Layout = ({ children }: LayoutProps) => {
    return (
        <div>
            <NavMenu />
            <Container>
                {children}
            </Container>
            <Toaster />
        </div>
    );
};

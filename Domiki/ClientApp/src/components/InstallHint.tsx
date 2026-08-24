import { useEffect, useState } from 'react';
import DownloadIcon from 'pixelarticons/svg/download.svg?react';
import { useToast } from '../services/toastContext';

interface InstallPromptEvent extends Event {
    prompt: () => Promise<void>;
}

const isStandalone = () =>
    window.matchMedia('(display-mode: standalone)').matches
    || (navigator as Navigator & { standalone?: boolean }).standalone === true;

const isIos = () => /iP(?:hone|ad|od)/.test(navigator.userAgent);

export const InstallHint = () => {
    const toast = useToast();
    const [promptEvent, setPromptEvent] = useState<InstallPromptEvent | null>(null);
    const [installed, setInstalled] = useState(isStandalone);

    useEffect(() => {
        const catchPrompt = (event: Event) => {
            event.preventDefault();
            setPromptEvent(event as InstallPromptEvent);
        };
        const markInstalled = () => {
            setInstalled(true);
        };

        window.addEventListener('beforeinstallprompt', catchPrompt);
        window.addEventListener('appinstalled', markInstalled);
        return () => {
            window.removeEventListener('beforeinstallprompt', catchPrompt);
            window.removeEventListener('appinstalled', markInstalled);
        };
    }, []);

    if (installed || (promptEvent == null && !isIos())) {
        return null;
    }

    const install = async () => {
        if (promptEvent == null) {
            toast.success('Нажмите «Поделиться» внизу экрана, затем «На экран «Домой»»');
            return;
        }

        await promptEvent.prompt();
        setPromptEvent(null);
    };

    return (
        <button type="button" className="nav-link install-hint"
            title="Поставить «Домики» на домашний экран"
            onClick={() => void install()}>
            <DownloadIcon className="nav-ico" aria-hidden="true" />
            На экран
        </button>
    );
};

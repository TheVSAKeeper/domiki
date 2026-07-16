import { useEffect, useState } from 'react';
import BellIcon from 'pixelarticons/svg/bell.svg?react';
import BellOffIcon from 'pixelarticons/svg/bell-off.svg?react';
import { useToast } from '../services/toastContext';
import { disablePush, enablePush, getPushState } from '../services/push';
import type { PushState } from '../services/push';

export const PushToggle = () => {
    const toast = useToast();
    const [pushState, setPushState] = useState<PushState>('unsupported');
    const [pushBusy, setPushBusy] = useState(false);

    useEffect(() => {
        void getPushState().then(setPushState);
    }, []);

    const togglePush = async () => {
        if (pushState === 'denied') {
            toast.error('Уведомления заблокированы в настройках браузера');
            return;
        }

        setPushBusy(true);
        try {
            if (pushState === 'on') {
                await disablePush();
                toast.success('Уведомления выключены');
            } else {
                await enablePush();
                toast.success('Уведомления включены');
            }
        } catch (err) {
            toast.error(err instanceof Error ? err.message : 'Не удалось изменить настройку уведомлений');
        } finally {
            setPushState(await getPushState());
            setPushBusy(false);
        }
    };

    if (pushState === 'unsupported') {
        return null;
    }

    return (
        <button type="button" className={`btn-game btn-ghost btn-icon push-toggle push-toggle-${pushState}`}
            title={pushState === 'on' ? 'Push-уведомления включены' : pushState === 'denied' ? 'Push-уведомления заблокированы браузером' : 'Push-уведомления выключены'}
            aria-label={pushState === 'on' ? 'Выключить push-уведомления' : 'Включить push-уведомления'}
            disabled={pushBusy} onClick={() => void togglePush()}>
            {pushState === 'on'
                ? <BellIcon className="btn-ico" aria-hidden="true" />
                : <BellOffIcon className="btn-ico" aria-hidden="true" />}
        </button>
    );
};

import { useUpdateAvailable } from '../services/appVersion';

export const UpdateBanner = () => {
    const updateAvailable = useUpdateAvailable();
    if (!updateAvailable) {
        return null;
    }

    return (
        <div className="update-banner" role="status" aria-live="polite">
            <span className="update-banner-text">🆕 Вышла новая версия</span>
            <button type="button" className="update-banner-button" onClick={() => location.reload()}>
                Обновить
            </button>
        </div>
    );
};

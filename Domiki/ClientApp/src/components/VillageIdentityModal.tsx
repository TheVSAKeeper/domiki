import { useState } from 'react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import SaveIcon from 'pixelarticons/svg/save.svg?react';
import type { VillageDto } from '../types/api';
import { VILLAGE_CREST_COLORS, VILLAGE_CREST_ICONS } from '../constants/village';

const CREST_OPTIONS = VILLAGE_CREST_ICONS.map((Icon, index) => ({ Icon, index, label: `Герб ${String(index + 1)}` }));

interface VillageIdentityModalProps {
    village: VillageDto | null;
    onSave: (name: string, crestIcon: number, crestColor: number) => Promise<void>;
    onClose: () => void;
}

export const VillageIdentityModal = ({ village, onSave, onClose }: VillageIdentityModalProps) => {
    const [draftVillageName, setDraftVillageName] = useState(() => village?.villageName ?? '');
    const [draftCrestIcon, setDraftCrestIcon] = useState(() => village?.crestIcon ?? 0);
    const [draftCrestColor, setDraftCrestColor] = useState(() => village?.crestColor ?? 0);

    return (
        <div className="modal-backdrop" role="presentation">
            <form className="identity-modal pixel-panel" onSubmit={event => { event.preventDefault(); void onSave(draftVillageName, draftCrestIcon, draftCrestColor); }}>
                <div className="identity-modal-head">
                    <h2 className="panel-title">Деревня</h2>
                    <button type="button" className="identity-button" title="Закрыть" onClick={onClose}>
                        <CloseIcon className="btn-ico" aria-hidden="true" />
                    </button>
                </div>
                <label className="identity-field">
                    <span className="panel-label">Название деревни</span>
                    <input value={draftVillageName} maxLength={24} onChange={event => setDraftVillageName(event.target.value)} />
                </label>
                <div className="identity-field">
                    <span className="panel-label">Герб</span>
                    <div className="crest-options">
                        {CREST_OPTIONS.map(({ Icon, index, label }) =>
                            <button key={label} type="button" aria-label={label}
                                className={'crest-option' + (draftCrestIcon === index ? ' crest-option-selected' : '')}
                                onClick={() => setDraftCrestIcon(index)}>
                                <Icon className="crest-ico" aria-hidden="true" />
                            </button>,
                        )}
                    </div>
                </div>
                <div className="identity-field">
                    <span className="panel-label">Цвет</span>
                    <div className="color-options">
                        {VILLAGE_CREST_COLORS.map((color, index) =>
                            <button key={color} type="button"
                                className={'color-option' + (draftCrestColor === index ? ' color-option-selected' : '')}
                                style={{ backgroundColor: color }}
                                aria-label={`Цвет ${index + 1}`}
                                onClick={() => setDraftCrestColor(index)} />,
                        )}
                    </div>
                </div>
                <button className="btn-game" type="submit">
                    <SaveIcon className="btn-ico" aria-hidden="true" />
                    Сохранить
                </button>
            </form>
        </div>
    );
};

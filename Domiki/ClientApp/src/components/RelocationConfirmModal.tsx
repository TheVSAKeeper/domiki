import { useLayoutEffect, useRef, useState } from 'react';
import CloseIcon from 'pixelarticons/svg/close.svg?react';
import type { RelocationPlanDto } from '../types/api';
import { pluralRu } from '../utils/plural';
import { ActionButton } from './ActionButton';
import { AbstractSprite } from './sprites';

interface RelocationConfirmModalProps {
    plan: RelocationPlanDto;
    villageName: string;
    onConfirm: (valleyId: number, villageName: string | null, valleyName: string) => Promise<boolean>;
    onClose: () => void;
}

export const RelocationConfirmModal = ({ plan, villageName, onConfirm, onClose }: RelocationConfirmModalProps) => {
    const dialogRef = useRef<HTMLDialogElement>(null);
    const [harnessed, setHarnessed] = useState(false);
    const [valleyId, setValleyId] = useState(() => plan.valleys[0]?.id ?? 0);
    const [newVillageName, setNewVillageName] = useState(villageName);

    useLayoutEffect(() => {
        const dialog = dialogRef.current;
        if (dialog != null && !dialog.open) {
            dialog.showModal();
        }
    }, []);

    const summary = plan.summary;
    const valley = plan.valleys.find(item => item.id === valleyId);
    const knots = plan.knotsOnRelocate;
    const knotsWord = pluralRu(knots, 'узелок', 'узелка', 'узелков');
    const title = harnessed ? 'Обоз запряжён' : 'Переехать в новую долину?';

    const confirm = async () => {
        const renamed = newVillageName.trim();
        const ok = await onConfirm(valleyId, renamed === villageName || renamed === '' ? null : renamed, valley?.name ?? 'новую долину');
        if (ok) {
            onClose();
        }
    };

    return (
        <dialog ref={dialogRef} className="errand-modal relocation-modal pixel-panel" aria-label={title} onClose={onClose}>
            <div className="errand-modal-head">
                <h2 className="errand-modal-title">{title}</h2>
                <button type="button" className="errand-modal-close" title="Закрыть" onClick={onClose}>
                    <CloseIcon aria-hidden="true" />
                </button>
            </div>

            {!harnessed &&
                <>
                    <p className="errand-modal-offer">
                        Деревню «{villageName}» вы оставляете насовсем – не потому, что здесь стало тесно, а потому,
                        что решили начать заново. Постройки, склады, казна и весь двор остаются здесь, и вернуться сюда нельзя.
                    </p>
                    <div className="relocation-columns">
                        <div className="relocation-column relocation-column-carried">
                            <h3 className="relocation-column-title">Едет с вами</h3>
                            <ul className="relocation-column-list">
                                <li>артель – {summary.workers} {pluralRu(summary.workers, 'трудяга', 'трудяги', 'трудяг')} со всей выучкой</li>
                                <li>чертежи – {summary.blueprints}</li>
                                <li>золото – {summary.gold} из {summary.goldTotal} (остальное останется)</li>
                                <li>доброе имя у соседей – половина</li>
                                <li>имя деревни и герб</li>
                            </ul>
                        </div>
                        <div className="relocation-column relocation-column-left">
                            <h3 className="relocation-column-title">Остаётся здесь</h3>
                            <ul className="relocation-column-list">
                                <li>все постройки и уровни – {summary.buildings}</li>
                                <li>{summary.resources} {pluralRu(summary.resources, 'припас', 'припаса', 'припасов')} на складе</li>
                                <li>{summary.coins} {pluralRu(summary.coins, 'монета', 'монеты', 'монет')}</li>
                                <li>весь декор и украсы</li>
                                <li>уклад и дружба</li>
                            </ul>
                        </div>
                    </div>
                    <p className="relocation-knots-line">
                        За прожитое деревня оставит на памятном столбе <strong>{knots} {knotsWord} памяти</strong>.
                    </p>
                    <div className="errand-actions">
                        <ActionButton className="btn-game icon-chip-btn" onClick={() => { setHarnessed(true); }}>
                            <AbstractSprite logicName="prestige_new_valley" size={32} className="btn-ico" aria-hidden="true" />
                            Собрать узелок
                        </ActionButton>
                        <ActionButton className="btn-game btn-ghost" onClick={() => { onClose(); }}>
                            Погодить
                        </ActionButton>
                    </div>
                </>
            }

            {harnessed &&
                <>
                    <div className="relocation-valleys">
                        {plan.valleys.map(item => (
                            <button type="button" key={item.id}
                                className={'relocation-valley' + (item.id === valleyId ? ' relocation-valley-selected' : '')}
                                aria-pressed={item.id === valleyId}
                                onClick={() => { setValleyId(item.id); }}>
                                <span className={`relocation-valley-view relocation-valley-${item.logicName}`} aria-hidden="true" />
                                <span className="relocation-valley-name">{item.name}</span>
                                <span className="relocation-valley-lore">{item.description}</span>
                            </button>
                        ))}
                    </div>
                    <label className="relocation-rename">
                        <span className="panel-label">Как назовём деревню на новом месте</span>
                        <input value={newVillageName} maxLength={24} onChange={event => { setNewVillageName(event.target.value); }} />
                    </label>
                    <p className="errand-modal-offer">
                        Последнее слово за вами. Скажете «трогай» – «{villageName}» останется на памятном столбе,
                        а вы проснётесь в долине {valley?.name ?? ''}: двор пустой, артель в сборе,
                        в кошеле {summary.startingCoins} {pluralRu(summary.startingCoins, 'монета', 'монеты', 'монет')} и {knots} {knotsWord} памяти.
                    </p>
                    <p className="relocation-warning">Обратной дороги нет.</p>
                    <div className="errand-actions">
                        <ActionButton className="btn-game icon-chip-btn" onClick={confirm}>
                            <AbstractSprite logicName="prestige_new_valley" size={32} className="btn-ico" aria-hidden="true" />
                            Трогай
                        </ActionButton>
                        <ActionButton className="btn-game btn-ghost" onClick={() => { onClose(); }}>
                            Отложить до утра
                        </ActionButton>
                    </div>
                </>
            }
        </dialog>
    );
};

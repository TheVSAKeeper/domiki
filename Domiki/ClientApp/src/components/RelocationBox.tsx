import { useEffect, useState } from 'react';
import { ApiError, getMemorialPost, getRelocationPlan } from '../services/api';
import { useToast } from '../services/toastContext';
import type { MemorialPostDto, RelocationDto, RelocationPlanDto } from '../types/api';
import { pluralRu } from '../utils/plural';
import { ActionButton } from './ActionButton';
import { Crest } from './Crest';
import { PixelLoader } from './PixelLoader';
import { RelocationConfirmModal } from './RelocationConfirmModal';
import { AbstractSprite, MechanicSprite } from './sprites';

interface RelocationBoxProps {
    relocation: RelocationDto | null;
    villageName: string;
    onRelocate: (valleyId: number, villageName: string | null, valleyName: string) => Promise<boolean>;
    onBuyPerk: (perkType: number) => Promise<boolean>;
}

const knotsWord = (count: number) => pluralRu(count, 'узелок', 'узелка', 'узелков');

const formatDate = (iso: string) => new Date(iso).toLocaleDateString('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' });

export const RelocationBox = ({ relocation, villageName, onRelocate, onBuyPerk }: RelocationBoxProps) => {
    const toast = useToast();
    const [post, setPost] = useState<MemorialPostDto | null>(null);
    const [plan, setPlan] = useState<RelocationPlanDto | null>(null);
    const [confirmOpen, setConfirmOpen] = useState(false);
    const [reloadSeq, setReloadSeq] = useState(0);

    useEffect(() => {
        const controller = new AbortController();

        void (async () => {
            try {
                const [nextPost, nextPlan] = await Promise.all([
                    getMemorialPost(controller.signal),
                    getRelocationPlan(controller.signal),
                ]);
                setPost(nextPost);
                setPlan(nextPlan);
            } catch (err) {
                if (err instanceof DOMException && err.name === 'AbortError') {
                    return;
                }
                if (err instanceof ApiError) {
                    toast.error(err.message);
                }
            }
        })();

        return () => { controller.abort(); };
    }, [toast, reloadSeq]);

    if (relocation == null) {
        return (
            <section className="relocation-panel pixel-panel">
                <PixelLoader label="Загрузка памятного столба…" />
            </section>
        );
    }

    const days = relocation.estimatedDays;
    const knots = relocation.knots;
    const gatePercent = relocation.threshold > 0
        ? Math.min(100, Math.round(relocation.level / relocation.threshold * 100))
        : 100;
    const relocateAndReload = async (valleyId: number, name: string | null, valleyName: string) => {
        const ok = await onRelocate(valleyId, name, valleyName);
        if (ok) {
            setReloadSeq(seq => seq + 1);
        }
        return ok;
    };

    return (
        <section className="relocation-panel pixel-panel">
            <header className="relocation-hero">
                <span className="relocation-hero-emblem" aria-hidden="true"><AbstractSprite logicName="prestige_new_valley" /></span>
                <div className="relocation-hero-text">
                    <h3 className="relocation-hero-title panel-title">Переезд в новую долину</h3>
                    <p className="relocation-hero-valley">Нынче стоим в долине {relocation.valleyName}</p>
                    <p className="relocation-hero-sub">
                        Всё, что здесь умели, уже поставлено, а за перевалом земля не пахана.
                        Двор и припас останутся, трудяги и чертежи поедут с вами.
                    </p>
                </div>
            </header>

            <div className="relocation-gate">
                <div className="relocation-gate-meter">
                    <div className="relocation-gate-track" aria-label={`Обжитость ${relocation.level} из ${relocation.threshold}`}>
                        <span className="relocation-gate-fill" style={{ width: `${String(gatePercent)}%` }} />
                    </div>
                    <span className="relocation-gate-goal">
                        {relocation.level} / {relocation.threshold} <span className="relocation-gate-cue">обжитость</span>
                    </span>
                </div>
                {relocation.canRelocate &&
                    <ActionButton className="btn-game icon-chip-btn" disabled={plan == null} onClick={() => { setConfirmOpen(true); }}>
                        <AbstractSprite logicName="prestige_new_valley" size={24} className="btn-ico" aria-hidden="true" />
                        Собраться в дорогу
                    </ActionButton>
                }
                {!relocation.canRelocate && relocation.level < relocation.threshold &&
                    <p className="relocation-gate-line">
                        Переезд – когда деревня встанет на ноги.
                        {days != null && ` По нынешнему ходу это ещё около ${String(days)} суток.`}
                    </p>
                }
                {!relocation.canRelocate && relocation.level >= relocation.threshold && relocation.blockReason != null &&
                    <p className="relocation-gate-line relocation-gate-blocked">{relocation.blockReason}</p>
                }
                {relocation.relocationCount > 0 &&
                    <p className="relocation-gate-note">Каждая новая долина дальше прежней.</p>
                }
            </div>

            <div className="relocation-ladder">
                <h4 className="relocation-section-title panel-title">
                    <MechanicSprite logicName="obzhitost" size={24} className="relocation-section-ico" aria-hidden="true" />
                    Узелки памяти: {knots}
                </h4>
                <div className="relocation-perks">
                    {relocation.perks.map(perk => {
                        const nextCost = perk.costs[perk.level];
                        return (
                            <div key={perk.perkType} className="relocation-perk">
                                <div className="relocation-perk-body">
                                    <span className="relocation-perk-name">{perk.name}</span>
                                    <span className="relocation-perk-lore">{perk.description}</span>
                                    <span className="relocation-perk-steps" aria-label={`Ступеней взято ${perk.level} из ${perk.costs.length}`}>
                                        {perk.costs.map((cost, step) => (
                                            <span key={cost} className={'relocation-perk-step' + (step < perk.level ? ' relocation-perk-step-taken' : '')}>
                                                {cost}
                                            </span>
                                        ))}
                                    </span>
                                </div>
                                {nextCost == null
                                    ? <span className="relocation-perk-done">все ступени взяты</span>
                                    : (
                                        <ActionButton className="btn-game" disabled={knots < nextCost} onClick={async () => { await onBuyPerk(perk.perkType); }}>
                                            {nextCost} {knotsWord(nextCost)}
                                        </ActionButton>
                                    )
                                }
                            </div>
                        );
                    })}
                </div>
            </div>

            <div className="relocation-post">
                <h4 className="relocation-section-title panel-title">
                    <AbstractSprite logicName="prestige_new_valley" size={24} className="relocation-section-ico" aria-hidden="true" />
                    Памятный столб
                </h4>
                <p className="relocation-post-sub">Деревни, что были</p>
                {post == null && <PixelLoader label="Загрузка столба…" />}
                {post != null &&
                    <>
                        <div className="relocation-post-stats">
                            <span>Прожито деревень: {post.relocationCount}</span>
                            <span>Суммарная обжитость: {post.levelSum}</span>
                            {post.firstDayDate != null && <span>Первый день: {formatDate(post.firstDayDate)}</span>}
                        </div>
                        {post.villages.length === 0
                            ? <p className="hint">Пока прожита одна деревня – эта.</p>
                            : (
                                <div className="relocation-post-list">
                                    {post.villages.map(village => (
                                        <div key={`${village.date}-${village.villageName ?? ''}`} className="relocation-post-row">
                                            <Crest icon={village.crestIcon} color={village.crestColor} />
                                            <span className="relocation-post-name">{village.villageName ?? 'Безымянная деревня'}</span>
                                            <span className="relocation-post-meta">
                                                {village.valleyName} · обжитость {village.level} · {village.livedDays} суток ·
                                                {' '}{village.knots} {knotsWord(village.knots)} · {formatDate(village.date)}
                                            </span>
                                        </div>
                                    ))}
                                </div>
                            )
                        }
                    </>
                }
            </div>

            {confirmOpen && plan != null &&
                <RelocationConfirmModal plan={plan} villageName={villageName}
                    onConfirm={relocateAndReload} onClose={() => { setConfirmOpen(false); }} />
            }
        </section>
    );
};

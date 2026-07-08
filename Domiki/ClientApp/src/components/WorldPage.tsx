import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import ArrowLeftIcon from 'pixelarticons/svg/arrow-left.svg?react';
import { ApiError, getWorld, visitVillage } from '../services/api';
import { useToast } from '../services/toast';
import { DEFAULT_VILLAGE_ICON, VILLAGE_CREST_COLORS, VILLAGE_CREST_ICONS } from '../constants/village';
import type { VillageVisitDto, WorldDto, WorldVillageDto } from '../types/api';

const Crest = ({ icon, color }: { icon: number; color: number }) => {
    const Icon = VILLAGE_CREST_ICONS[icon] ?? DEFAULT_VILLAGE_ICON;
    const backgroundColor = VILLAGE_CREST_COLORS[color] ?? VILLAGE_CREST_COLORS[0];

    return (
        <span className="crest-badge" style={{ backgroundColor }}>
            <Icon className="crest-ico" aria-hidden="true" />
        </span>
    );
};

const LevelBreakdown = ({ visit }: { visit: VillageVisitDto }) => (
    <div className="world-level-grid">
        <span>Постройки: {visit.level.buildings}</span>
        <span>Жители: {visit.level.residents}</span>
        <span>Репутация: {visit.level.reputation}</span>
        <span>Уют: {visit.level.comfort}</span>
    </div>
);

export const WorldPage = () => {
    const toast = useToast();
    const [world, setWorld] = useState<WorldDto | null>(null);
    const [selectedVillage, setSelectedVillage] = useState<WorldVillageDto | null>(null);
    const [visit, setVisit] = useState<VillageVisitDto | null>(null);
    const [visitLoading, setVisitLoading] = useState(false);
    const visitControllerRef = useRef<AbortController | null>(null);

    useEffect(() => () => { visitControllerRef.current?.abort(); }, []);

    useEffect(() => {
        const controller = new AbortController();

        void (async () => {
            try {
                setWorld(await getWorld(controller.signal));
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
    }, [toast]);

    const openVillage = async (village: WorldVillageDto) => {
        visitControllerRef.current?.abort();

        if (village.playerId == null) {
            visitControllerRef.current = null;
            setSelectedVillage(village);
            setVisit(null);
            setVisitLoading(false);
            return;
        }

        const controller = new AbortController();
        visitControllerRef.current = controller;
        setSelectedVillage(village);
        setVisit(null);
        setVisitLoading(true);
        try {
            setVisit(await visitVillage(village.playerId, controller.signal));
        } catch (err) {
            if (err instanceof DOMException && err.name === 'AbortError') {
                return;
            }
            if (err instanceof ApiError) {
                toast.error(err.message);
                return;
            }
            throw err;
        } finally {
            if (visitControllerRef.current === controller) {
                setVisitLoading(false);
            }
        }
    };

    if (world == null) {
        return <div className="wiki"><p className="wiki-loading">Загрузка мира…</p></div>;
    }

    return (
        <div className="wiki world-page">
            <section className="wiki-intro pixel-panel world-head">
                <div>
                    <h1 className="wiki-title">Мир</h1>
                    <div className="world-count">{world.villages.length} деревень</div>
                </div>
                <Link className="btn-game" to="/domiki-page">
                    <ArrowLeftIcon className="btn-ico" aria-hidden="true" />
                    В игру
                </Link>
            </section>

            <section className="wiki-section world-layout">
                <div className="world-list pixel-panel">
                    {world.villages.map((village, index) => (
                        <button
                            type="button"
                            key={`${village.playerId ?? 'npc'}-${village.villageName}`}
                            className={'world-row' + (village.isMe ? ' world-row-me' : '') + (selectedVillage === village ? ' world-row-selected' : '')}
                            onClick={() => { void openVillage(village); }}
                            title={village.isNpc ? 'Сосед' : 'Визит'}
                        >
                            <span className="world-rank">{index + 1}</span>
                            <Crest icon={village.crestIcon} color={village.crestColor} />
                            <span className="world-name">
                                {village.villageName}
                                {village.isMe && <span className="world-tag">моя</span>}
                                {village.isNpc && <span className="world-tag">NPC</span>}
                            </span>
                            <span className="world-level">{village.level}</span>
                        </button>
                    ))}
                </div>

                <aside className="world-visit pixel-panel">
                    {selectedVillage == null && <p className="hint">Выбери деревню</p>}
                    {selectedVillage != null && selectedVillage.isNpc &&
                        <div className="world-visit-head">
                            <Crest icon={selectedVillage.crestIcon} color={selectedVillage.crestColor} />
                            <div>
                                <h2 className="panel-title">{selectedVillage.villageName}</h2>
                                <p className="hint">Торгует ресурсом #{selectedVillage.npcResourceTypeId}</p>
                            </div>
                        </div>
                    }
                    {visitLoading && <p className="hint">Загрузка визита…</p>}
                    {visit != null &&
                        <>
                            <div className="world-visit-head">
                                <Crest icon={visit.crestIcon} color={visit.crestColor} />
                                <div>
                                    <h2 className="panel-title">{visit.villageName}</h2>
                                    <div className="world-visit-level">Обжитость {visit.level.level}</div>
                                </div>
                            </div>
                            <LevelBreakdown visit={visit} />
                            <div className="world-buildings">
                                {visit.buildings.length === 0 && <p className="hint">Построек нет</p>}
                                {visit.buildings.map((building, index) => (
                                    <div key={`${building.typeName}-${index}`} className="world-building-row">
                                        <span>{building.typeName}</span>
                                        <span>ур. {building.level}</span>
                                    </div>
                                ))}
                            </div>
                        </>
                    }
                </aside>
            </section>
        </div>
    );
};

import { useEffect, useState } from 'react';
import { ApiError, getGameState } from '../services/api';
import { useToast } from '../services/toast';
import { formatDuration } from '../utils/time';
import type { DomikTypeDto, ReceiptDto, ResourceDto, ResourceTypeDto } from '../types/api';
import { DomikSprite } from './sprites';

interface Catalog {
    domikTypes: DomikTypeDto[];
    resourceTypes: ResourceTypeDto[];
    receipts: ReceiptDto[];
}

interface ResChipsProps {
    items: ResourceDto[];
    resourceTypes: ResourceTypeDto[];
}

const ResChips = ({ items, resourceTypes }: ResChipsProps) => (
    <span className="wiki-chips">
        {items.map(res => {
            const type = resourceTypes.find(x => x.id === res.typeId);
            if (type == null) {
                return null;
            }
            return (
                <span key={res.typeId} className="wiki-chip" title={type.name}>
                    <img src={'/images/resourceTypes/' + type.logicName + '.png'} alt={type.name} />
                    {res.value}
                </span>
            );
        })}
    </span>
);

interface RecipeCardProps {
    receipt: ReceiptDto;
    resourceTypes: ResourceTypeDto[];
}

const RecipeCard = ({ receipt, resourceTypes }: RecipeCardProps) => (
    <div className="wiki-recipe">
        <div className="wiki-recipe-name">{receipt.name}</div>
        <div className="wiki-recipe-flow">
            <ResChips items={receipt.inputResources} resourceTypes={resourceTypes} />
            <span className="wiki-arrow" aria-hidden="true">→</span>
            <ResChips items={receipt.outputResources} resourceTypes={resourceTypes} />
        </div>
        {receipt.optionalInputResources.length > 0 && (
            <div className="wiki-recipe-opt">
                ускорение: <ResChips items={receipt.optionalInputResources} resourceTypes={resourceTypes} />
                {receipt.speedupPercent > 0 && <span> (−{receipt.speedupPercent}% времени)</span>}
            </div>
        )}
        <div className="wiki-recipe-meta">
            <span>{formatDuration(receipt.durationSeconds)}</span>
            <span>{receipt.plodderCount} трудяг</span>
        </div>
    </div>
);

export const Wiki = () => {
    const toast = useToast();
    const [catalog, setCatalog] = useState<Catalog | null>(null);

    useEffect(() => {
        const controller = new AbortController();

        void (async () => {
            try {
                const state = await getGameState(controller.signal);
                setCatalog({
                    domikTypes: state.domikTypes,
                    resourceTypes: state.resourceTypes,
                    receipts: state.receipts,
                });
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

    if (catalog == null) {
        return <div className="wiki"><p className="wiki-loading">Загрузка справочника…</p></div>;
    }

    const { domikTypes, resourceTypes, receipts } = catalog;
    const receiptById = (id: number) => receipts.find(x => x.id === id);
    const buildings = [...domikTypes].sort((a, b) => a.unlockLevel - b.unlockLevel || a.id - b.id);

    return (
        <div className="wiki">
            <section className="wiki-intro pixel-panel">
                <h1 className="wiki-title">Справочник</h1>
                <p>Domiki – уютная idle-деревня. Заходи на пару минут: строй домики, запускай производства, бери заказы соседей. Ресурсы копятся сами, даже с закрытой вкладкой.</p>
                <p>Ниже – ресурсы, постройки и рецепты. Данные живые, прямо из игры, так что не устаревают.</p>
            </section>

            <section className="wiki-section">
                <h2 className="section-head">Ресурсы</h2>
                <div className="wiki-res-grid">
                    {resourceTypes.map(type => (
                        <div key={type.id} className="wiki-res-cell pixel-panel" title={type.name}>
                            <img src={'/images/resourceTypes/' + type.logicName + '.png'} alt={type.name} />
                            <span>{type.name}</span>
                        </div>
                    ))}
                </div>
            </section>

            <section className="wiki-section">
                <h2 className="section-head">Постройки</h2>
                <div className="wiki-buildings">
                    {buildings.map(type => (
                        <div key={type.id} className="wiki-building pixel-panel">
                            <div className="wiki-building-head">
                                <DomikSprite logicName={type.logicName} />
                                <div>
                                    <h3 className="wiki-building-name">{type.name}</h3>
                                    <p className="wiki-building-meta">
                                        до {type.maxLevel} ур. · макс. {type.maxCount} шт.
                                        {type.unlockLevel > 0 && ` · с ${type.unlockLevel} ур. деревни`}
                                        {type.blueprintId != null && ' · по чертежу'}
                                    </p>
                                </div>
                            </div>
                            <div className="wiki-levels">
                                {type.levels.map(level => {
                                    const levelReceipts = level.receiptIds.map(receiptById).filter((r): r is ReceiptDto => r != null);
                                    if (level.resources.length === 0 && levelReceipts.length === 0) {
                                        return null;
                                    }
                                    return (
                                        <div key={level.value} className="wiki-level">
                                            <div className="wiki-level-head">
                                                <span className="wiki-level-badge">Ур. {level.value}</span>
                                                {level.resources.length > 0 && (
                                                    <span className="wiki-level-cost">апгрейд: <ResChips items={level.resources} resourceTypes={resourceTypes} /></span>
                                                )}
                                            </div>
                                            {levelReceipts.map(receipt => (
                                                <RecipeCard key={receipt.id} receipt={receipt} resourceTypes={resourceTypes} />
                                            ))}
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    ))}
                </div>
            </section>
        </div>
    );
};

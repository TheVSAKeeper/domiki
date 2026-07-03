import React from 'react';

export const ManufactureBox = ({ manufacture, receipts }) => {
    let receipt = receipts.filter(x => x.id === manufacture.receiptId)[0];
    var total = receipt.durationSeconds;
    var current = manufacture.durationSeconds;
    var percent = 100 - parseInt(current * 100 / total);

    return (
        <div className="manufacture-box">
            <progress max="100" value={percent} data-label={manufacture.durationSecondsText}></progress>
            <div className="manufacture-info">
                <span className="manufacture-name">{receipt.name}</span>
                <span className="resource-box" title="Трудяги">
                    <img src="/images/modificatorTypes/plodder.png" alt="Трудяги" />
                    <span className="resource-value">{manufacture.plodderCount}</span>
                </span>
            </div>
        </div>
    );
};

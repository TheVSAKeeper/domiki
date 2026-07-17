interface GiftVisitDotsProps {
    visitIndex: number;
    big?: boolean;
}

export const GiftVisitDots = ({ visitIndex, big = false }: GiftVisitDotsProps) => (
    <span className="gift-visit-dots" aria-hidden="true">
        {Array.from({ length: 7 }, (_, index) => <span key={index} className={big || index < visitIndex ? 'gift-visit-dot is-filled' : 'gift-visit-dot'} />)}
    </span>
);

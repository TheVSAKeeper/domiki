interface ConvoyTallyProps {
    remaining: number;
    limit: number;
}

export const ConvoyTally = ({ remaining, limit }: ConvoyTallyProps) => {
    const label = remaining > 0 ? `осталось ${remaining} из ${limit}` : 'обоз на сегодня распродан';
    const spent = limit - remaining;
    return (
        <span className={'convoy-tally' + (remaining <= 0 ? ' convoy-tally-empty' : '')} role="img" aria-label={label} title={label}>
            {Array.from({ length: limit }, (_, index) => (
                <span key={index} className={index < spent ? 'convoy-notch convoy-notch-spent' : 'convoy-notch'} />
            ))}
        </span>
    );
};

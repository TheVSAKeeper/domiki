interface PixelLoaderProps {
    label: string;
}

export const PixelLoader = ({ label }: PixelLoaderProps) => (
    <div className="pixel-loader" role="status">
        <span className="pixel-loader-dots" aria-hidden="true">
            <span></span>
            <span></span>
            <span></span>
        </span>
        <p className="pixel-loader-label">{label}</p>
    </div>
);

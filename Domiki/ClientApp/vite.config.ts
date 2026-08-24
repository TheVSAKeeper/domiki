/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import svgr from 'vite-plugin-svgr';
import { VitePWA } from 'vite-plugin-pwa';
import fs from 'node:fs';
import path from 'node:path';
import child_process from 'node:child_process';
import crypto from 'node:crypto';
import { brotliCompressSync, constants, gzipSync } from 'node:zlib';
import { env } from 'node:process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = 'Domiki';
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

function isDevCertificateValid() {
    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        return false;
    }

    try {
        const certificate = new crypto.X509Certificate(fs.readFileSync(certFilePath));
        return Date.parse(certificate.validTo) > Date.now();
    } catch {
        return false;
    }
}

function ensureDevCertificate() {
    if (isDevCertificateValid()) {
        return;
    }

    fs.rmSync(certFilePath, { force: true });
    fs.rmSync(keyFilePath, { force: true });

    const result = child_process.spawnSync(
        'dotnet',
        ['dev-certs', 'https', '--export-path', certFilePath, '--format', 'Pem', '--no-password'],
        { stdio: 'inherit' },
    );
    if (result.status !== 0) {
        throw new Error('Не удалось создать HTTPS-сертификат разработки ASP.NET Core.');
    }
}

const target = env.ASPNETCORE_HTTPS_PORT
    ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
    : env.ASPNETCORE_URLS
        ? env.ASPNETCORE_URLS.split(';')[0]
        : 'https://localhost:7146';

const backendPaths = [
    '/Domiki',
    '/Push',
    '/authentication',
    '/_configuration',
    '/.well-known',
    '/Identity',
    '/connect',
    '/ApplyDatabaseMigrations',
    '/_framework',
];

const proxy = Object.fromEntries(backendPaths.map((p) => [p, { target, secure: false }]));

const precompress = () => ({
    name: 'precompress-static-assets',
    apply: 'build' as const,
    closeBundle() {
        const visit = (directory: string) => {
            for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
                const fileName = path.join(directory, entry.name);
                if (entry.isDirectory()) {
                    visit(fileName);
                    continue;
                }
                if (!/\.(?:css|html|js|json|svg|txt|xml)$/.test(entry.name)) continue;
                const contents = fs.readFileSync(fileName);
                if (contents.length < 1024) continue;
                fs.writeFileSync(`${fileName}.br`, brotliCompressSync(contents, { params: { [constants.BROTLI_PARAM_QUALITY]: 9 } }));
                fs.writeFileSync(`${fileName}.gz`, gzipSync(contents, { level: 9 }));
            }
        };
        visit(path.resolve('build'));
    },
});

const spriteFamilies: Record<string, string> = {
    abstract: 'abstract',
    actors: 'actor',
    decorTypes: 'decor',
    domikTypes: 'domik',
    mechanics: 'mechanic',
    neighbors: 'neighbor',
    resourceTypes: 'resource',
    tolokaTypes: 'toloka',
    traits: 'trait',
    weather: 'weather',
    workers: 'worker',
};

const spriteChunk = (id: string) => {
    const asset = /\/src\/assets\/([^/]+)\//.exec(id);
    if (asset?.[1] != null && spriteFamilies[asset[1]] != null) {
        return `sprites-${spriteFamilies[asset[1]]}`;
    }

    const family = /\/src\/components\/sprites\/([^/.]+)\./.exec(id);
    if (family?.[1] != null && family[1] !== 'index' && family[1] !== 'core') {
        return `sprites-${family[1]}`;
    }

    return undefined;
};

export default defineConfig(({ command, mode }) => {
    const useDevServer = command === 'serve' && mode !== 'test';

    if (useDevServer) {
        ensureDevCertificate();
    }

    return {
        base: '/',
        plugins: [
            react(),
            svgr(),
            VitePWA({
                strategies: 'injectManifest',
                srcDir: 'src',
                filename: 'sw.js',
                injectRegister: false,
                manifest: false,
                injectManifest: {
                    globPatterns: ['**/*.{js,css,html,svg,png,ico,woff2}'],
                    globIgnores: ['**/*.{br,gz}'],
                    maximumFileSizeToCacheInBytes: 4 * 1024 * 1024,
                },
                devOptions: { enabled: false },
            }),
            precompress(),
        ],
        build: {
            outDir: 'build',
            emptyOutDir: true,
            modulePreload: {
                resolveDependencies: (_filename, deps, { hostType }) =>
                    hostType === 'html' ? deps.filter(dep => !/sprites-(?!mechanic)/.test(dep)) : deps,
            },
            rollupOptions: {
                output: {
                    manualChunks: (id) => id.includes('/node_modules/react') ? 'react' : spriteChunk(id),
                },
            },
        },
        server: useDevServer
            ? {
                host: '127.0.0.1',
                port: 44444,
                strictPort: true,
                proxy,
                https: {
                    key: fs.readFileSync(keyFilePath),
                    cert: fs.readFileSync(certFilePath),
                },
            }
            : undefined,
        test: {
            globals: true,
            environment: 'jsdom',
            setupFiles: './src/setupTests.ts',
            css: false,
        },
    };
});

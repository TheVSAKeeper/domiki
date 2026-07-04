/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import fs from 'node:fs';
import path from 'node:path';
import child_process from 'node:child_process';
import { env } from 'node:process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = 'Domiki';
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
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
    '/System',
    '/authentication',
    '/_configuration',
    '/.well-known',
    '/Identity',
    '/connect',
    '/ApplyDatabaseMigrations',
    '/_framework',
];

const proxy = Object.fromEntries(backendPaths.map((p) => [p, { target, secure: false }]));

export default defineConfig({
    base: '/',
    plugins: [react()],
    build: {
        outDir: 'build',
        emptyOutDir: true,
    },
    server: {
        host: '127.0.0.1',
        port: 44444,
        strictPort: true,
        proxy,
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        },
    },
    test: {
        globals: true,
        environment: 'jsdom',
        setupFiles: './src/setupTests.ts',
        css: false,
    },
});

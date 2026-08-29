import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const serviceWorkerSource = readFileSync(resolve(fileURLToPath(import.meta.url), '../sw.js'), 'utf8');

describe('service-worker push tag contract', () => {
    it('prefixes non-empty payload tags and keeps the legacy fallback', () => {
        expect(serviceWorkerSource).toMatch(
            /const notificationTag = typeof data\.tag === ['"]string['"]\s*&&\s*data\.tag\.length > 0\s*\?\s*`domiki-\$\{data\.tag\}`\s*:\s*['"]domiki['"];/,
        );
        expect(serviceWorkerSource).toContain('tag: notificationTag');
        expect(serviceWorkerSource).toContain("tag: 'domiki'");
    });
});

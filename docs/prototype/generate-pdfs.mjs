/**
 * NexTraceOne — Prototype PDF Generator
 * Usage: node generate-pdfs.mjs
 * Requires: npm install puppeteer (or npx puppeteer)
 */
import puppeteer from 'puppeteer-core';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import { mkdirSync } from 'fs';

const __dir = dirname(fileURLToPath(import.meta.url));
const outDir = join(__dir, 'pdfs');
mkdirSync(outDir, { recursive: true });

const pages = [
  { file: 'index.html',     name: '01-brand-identity' },
  { file: 'login.html',     name: '02-login' },
  { file: 'dashboard.html', name: '03-dashboard' },
  { file: 'services.html',  name: '04-service-catalog' },
  { file: 'changes.html',   name: '05-change-governance' },
  { file: 'incidents.html', name: '06-incidents' },
];

(async () => {
  console.log('🚀 Launching browser…');
  const browser = await puppeteer.launch({
    executablePath: '/root/.cache/ms-playwright/chromium-1194/chrome-linux/chrome',
    args: ['--no-sandbox', '--disable-setuid-sandbox'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 1440, height: 900 });

  for (const { file, name } of pages) {
    const url = `file://${join(__dir, file)}`;
    console.log(`📄 Rendering ${file}…`);
    await page.goto(url, { waitUntil: 'networkidle0' });
    await page.pdf({
      path: join(outDir, `${name}.pdf`),
      format: 'A3',
      landscape: true,
      printBackground: true,
      margin: { top: '10mm', right: '10mm', bottom: '10mm', left: '10mm' },
    });
    console.log(`   ✅ Saved: pdfs/${name}.pdf`);
  }

  await browser.close();
  console.log('\n✨ Done! PDFs saved to docs/prototype/pdfs/');
})();

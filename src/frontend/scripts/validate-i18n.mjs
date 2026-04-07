#!/usr/bin/env node

/**
 * i18n Validation Script — NexTraceOne
 *
 * Compara chaves de tradução entre en.json e os outros locales.
 * Detecta: chaves em falta, chaves extra, e chaves não usadas no código.
 *
 * Uso: node scripts/validate-i18n.mjs
 * Exit code 1 se houver chaves em falta.
 */

import { readFileSync, readdirSync, statSync } from 'fs';
import { join, extname } from 'path';

const LOCALES_DIR = 'src/locales';
const SRC_DIR = 'src';
const REFERENCE_LOCALE = 'en.json';

// ─── Helpers ────────────────────────────────────────────────────────────────

function flatten(obj, prefix = '') {
  const items = [];
  for (const [key, value] of Object.entries(obj)) {
    const fullKey = prefix ? `${prefix}.${key}` : key;
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      items.push(...flatten(value, fullKey));
    } else {
      items.push(fullKey);
    }
  }
  return items;
}

function readJson(path) {
  return JSON.parse(readFileSync(path, 'utf-8'));
}

function walkDir(dir, extensions) {
  const results = [];
  for (const entry of readdirSync(dir)) {
    const fullPath = join(dir, entry);
    const stat = statSync(fullPath);
    if (stat.isDirectory()) {
      if (entry === 'node_modules' || entry === '.git' || entry === 'locales') continue;
      results.push(...walkDir(fullPath, extensions));
    } else if (extensions.includes(extname(entry))) {
      results.push(fullPath);
    }
  }
  return results;
}

// ─── Main ───────────────────────────────────────────────────────────────────

console.log('🔍 NexTraceOne i18n Validation\n');

// Load reference locale
const refData = readJson(join(LOCALES_DIR, REFERENCE_LOCALE));
const refKeys = new Set(flatten(refData));
console.log(`📦 Reference (en.json): ${refKeys.size} keys\n`);

// Compare with other locales
const localeFiles = readdirSync(LOCALES_DIR).filter(f => f.endsWith('.json') && f !== REFERENCE_LOCALE);
let hasMissing = false;

for (const file of localeFiles) {
  const data = readJson(join(LOCALES_DIR, file));
  const keys = new Set(flatten(data));

  const missing = [...refKeys].filter(k => !keys.has(k));
  const extra = [...keys].filter(k => !refKeys.has(k));

  if (missing.length > 0) {
    hasMissing = true;
    console.log(`❌ ${file}: ${missing.length} missing keys`);
    for (const k of missing.slice(0, 20)) {
      console.log(`   - ${k}`);
    }
    if (missing.length > 20) console.log(`   ... and ${missing.length - 20} more`);
  } else {
    console.log(`✅ ${file}: all ${keys.size} keys present`);
  }

  if (extra.length > 0) {
    console.log(`   ⚠️  ${extra.length} extra keys not in en.json`);
  }
}

// Check for unused keys (basic heuristic)
console.log('\n🔎 Checking for potentially unused keys...');
const sourceFiles = walkDir(SRC_DIR, ['.tsx', '.ts']);
const allSourceCode = sourceFiles.map(f => readFileSync(f, 'utf-8')).join('\n');

// Extract first-level key prefixes used in t() calls
const usedPrefixes = new Set();
const tCallRegex = /t\(['"`]([a-zA-Z0-9_.]+)['"`]/g;
let match;
while ((match = tCallRegex.exec(allSourceCode)) !== null) {
  usedPrefixes.add(match[1]);
}

// Check top-level keys
const topLevelKeys = Object.keys(refData);
const unusedTopLevel = topLevelKeys.filter(k => {
  // Check if any t() call starts with this prefix
  return ![...usedPrefixes].some(p => p === k || p.startsWith(k + '.'));
});

if (unusedTopLevel.length > 0) {
  console.log(`⚠️  ${unusedTopLevel.length} potentially unused top-level key groups:`);
  for (const k of unusedTopLevel) {
    console.log(`   - ${k}`);
  }
} else {
  console.log('✅ All top-level key groups appear to be referenced in code');
}

console.log(`\n📊 Summary: ${refKeys.size} keys, ${localeFiles.length + 1} locales, ${sourceFiles.length} source files`);

if (hasMissing) {
  console.log('\n💥 FAIL: Missing keys detected. Please add translations.');
  process.exit(1);
} else {
  console.log('\n✅ PASS: All locales have complete translations.');
  process.exit(0);
}

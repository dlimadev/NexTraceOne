#!/usr/bin/env node
// =============================================================================
// check-i18n-coverage.sh
// NexTraceOne — i18n Coverage Verification
//
// Verifica que todos os locale files (pt-BR, pt-PT, es) têm as mesmas keys
// que o locale base (en). Falha com exit code 1 se alguma key estiver em falta.
//
// Uso:
//   node scripts/quality/check-i18n-coverage.sh
//   node scripts/quality/check-i18n-coverage.sh --warn-only
//
// Opções:
//   --warn-only   Reporta keys em falta mas não falha (exit code 0)
// =============================================================================

"use strict";

const fs = require("fs");
const path = require("path");

const LOCALES_DIR = path.join(
  __dirname,
  "../../src/frontend/src/locales"
);

const BASE_LOCALE = "en.json";
const LOCALES_TO_CHECK = ["pt-BR.json", "pt-PT.json", "es.json"];

const warnOnly = process.argv.includes("--warn-only");

// ── Helpers ──────────────────────────────────────────────────────────────────

function loadJson(filePath) {
  try {
    return JSON.parse(fs.readFileSync(filePath, "utf8"));
  } catch (err) {
    console.error(`❌ Failed to load ${filePath}: ${err.message}`);
    process.exit(1);
  }
}

/**
 * Extrai todas as chaves leaf (caminho completo com ponto como separador)
 * de um objecto JSON aninhado.
 */
function extractLeafKeys(obj, prefix = "") {
  const keys = [];
  for (const [k, v] of Object.entries(obj)) {
    const fullKey = prefix ? `${prefix}.${k}` : k;
    if (v !== null && typeof v === "object" && !Array.isArray(v)) {
      keys.push(...extractLeafKeys(v, fullKey));
    } else {
      keys.push(fullKey);
    }
  }
  return keys;
}

/**
 * Encontra keys presentes em baseKeys mas ausentes em targetKeys.
 */
function findMissingKeys(baseKeys, targetKeys) {
  const targetSet = new Set(targetKeys);
  return baseKeys.filter((k) => !targetSet.has(k));
}

/**
 * Encontra keys presentes em targetKeys mas ausentes em baseKeys (extra keys).
 */
function findExtraKeys(baseKeys, targetKeys) {
  const baseSet = new Set(baseKeys);
  return targetKeys.filter((k) => !baseSet.has(k));
}

// ── Main ─────────────────────────────────────────────────────────────────────

const baseFile = path.join(LOCALES_DIR, BASE_LOCALE);
const baseJson = loadJson(baseFile);
const baseKeys = extractLeafKeys(baseJson);

console.log(`\n📖 i18n Coverage Check — NexTraceOne`);
console.log(`   Base locale : ${BASE_LOCALE} (${baseKeys.length} keys)`);
console.log(`   Checking    : ${LOCALES_TO_CHECK.join(", ")}`);
console.log(`   Mode        : ${warnOnly ? "warn-only" : "strict (fail on missing)"}`);
console.log("");

let totalMissing = 0;
let totalExtra = 0;
let hasFailures = false;

for (const localeFile of LOCALES_TO_CHECK) {
  const filePath = path.join(LOCALES_DIR, localeFile);

  if (!fs.existsSync(filePath)) {
    console.error(`❌ Locale file not found: ${localeFile}`);
    if (!warnOnly) hasFailures = true;
    continue;
  }

  const localeJson = loadJson(filePath);
  const localeKeys = extractLeafKeys(localeJson);

  const missing = findMissingKeys(baseKeys, localeKeys);
  const extra = findExtraKeys(baseKeys, localeKeys);

  totalMissing += missing.length;
  totalExtra += extra.length;

  if (missing.length === 0 && extra.length === 0) {
    console.log(
      `✅ ${localeFile.padEnd(12)} — ${localeKeys.length}/${baseKeys.length} keys — 100% coverage`
    );
  } else {
    const coverage = (
      ((baseKeys.length - missing.length) / baseKeys.length) *
      100
    ).toFixed(1);
    const status = missing.length > 0 ? "❌" : "⚠️ ";
    console.log(
      `${status} ${localeFile.padEnd(12)} — ${localeKeys.length}/${baseKeys.length} keys — ${coverage}% coverage`
    );

    if (missing.length > 0) {
      console.log(`   Missing ${missing.length} key(s):`);
      missing.slice(0, 20).forEach((k) => console.log(`     - ${k}`));
      if (missing.length > 20) {
        console.log(`     ... and ${missing.length - 20} more`);
      }
      if (!warnOnly) hasFailures = true;
    }

    if (extra.length > 0) {
      console.log(`   Extra ${extra.length} key(s) not in base locale:`);
      extra.slice(0, 10).forEach((k) => console.log(`     + ${k}`));
      if (extra.length > 10) {
        console.log(`     ... and ${extra.length - 10} more`);
      }
    }
  }
}

console.log("");
console.log(`📊 Summary:`);
console.log(`   Total missing keys : ${totalMissing}`);
console.log(`   Total extra keys   : ${totalExtra}`);
console.log(`   Locales checked    : ${LOCALES_TO_CHECK.length}`);

if (hasFailures) {
  console.log("");
  console.log(
    `❌ i18n coverage check FAILED — ${totalMissing} missing key(s) across ${LOCALES_TO_CHECK.length} locales.`
  );
  console.log(
    `   Add missing translations or run with --warn-only to skip this check.`
  );
  process.exit(1);
} else if (totalMissing > 0) {
  console.log("");
  console.log(
    `⚠️  i18n coverage check completed with ${totalMissing} warning(s) (--warn-only mode).`
  );
} else {
  console.log("");
  console.log(`✅ i18n coverage check PASSED — all locales are complete.`);
}

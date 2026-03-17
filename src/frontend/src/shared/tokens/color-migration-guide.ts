/**
 * NexTraceOne — Design Tokens Reference
 *
 * Mapeamento de referência entre cores Tailwind genéricas e tokens NTO.
 * Usar este ficheiro como guia durante a migração de cores hardcoded.
 *
 * FONTE DA VERDADE: docs/DESIGN-SYSTEM.md §2.1
 *
 * ══════════════════════════════════════════════════════════════════
 * CORES A ELIMINAR (Tailwind genérico)  →  TOKEN NTO CORRECTO
 * ══════════════════════════════════════════════════════════════════
 *
 * --- Critical / Danger ---
 * text-red-300, text-red-400         →  text-critical
 * bg-red-900/40                      →  bg-critical/15 ou bg-critical-muted
 * border-red-700/50                  →  border-critical/25
 *
 * --- Warning ---
 * text-amber-300, text-amber-400     →  text-warning
 * text-orange-300, text-orange-400   →  text-warning
 * bg-amber-900/40, bg-orange-900/40  →  bg-warning/15 ou bg-warning-muted
 * border-amber-700/50                →  border-warning/25
 *
 * --- Success ---
 * text-emerald-300, text-emerald-400 →  text-success
 * text-green-300, text-green-400     →  text-success
 * bg-emerald-900/40, bg-green-900/40 →  bg-success/15 ou bg-success-muted
 * border-emerald-700/50              →  border-success/25
 *
 * --- Info ---
 * text-blue-300, text-blue-400       →  text-info
 * bg-blue-900/40                     →  bg-info/15 ou bg-info-muted
 * border-blue-700/50                 →  border-info/25
 *
 * --- Neutral ---
 * text-gray-300, text-gray-400       →  text-faded
 * text-slate-300, text-slate-400     →  text-muted ou text-faded
 * bg-slate-800/40, bg-slate-900/40   →  bg-elevated
 * border-slate-700/50                →  border-edge
 *
 * --- Accent / Special ---
 * text-indigo-300                    →  text-info (ou text-accent se for CTA)
 * bg-indigo-900/40                   →  bg-info/15
 * text-purple-300                    →  text-info (NTO não usa purple)
 * bg-purple-900/40                   →  bg-info/15
 *
 * ══════════════════════════════════════════════════════════════════
 * PADRÃO DE BADGE CORRECTO
 * ══════════════════════════════════════════════════════════════════
 *
 * Em vez de:
 *   <span className="bg-red-900/40 text-red-300 border border-red-700/50">
 *
 * Usar:
 *   <Badge variant="danger">Critical</Badge>
 *
 * Variantes disponíveis: default, success, warning, danger, info
 */

export {};

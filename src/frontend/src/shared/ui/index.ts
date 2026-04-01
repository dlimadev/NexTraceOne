/**
 * NexTraceOne Design System — UI Primitives
 *
 * Barrel export para componentes base do design system.
 * Componentes aqui são agnósticos de domínio e reutilizáveis em todo o produto.
 *
 * Migração incremental: re-exportar componentes existentes de src/components/
 * até que estejam totalmente migrados para esta pasta.
 *
 * @see docs/DESIGN-SYSTEM.md
 */

// ── Brand ────────────────────────────────────────────────────────────────────
export { NexTraceLogo, NexTraceIcon, NexTraceWordmark } from '../../components/NexTraceLogo';

// ── Interactive ─────────────────────────────────────────────────────────────
export { Button } from '../../components/Button';
export { IconButton } from '../../components/IconButton';

// ── Form Fields ─────────────────────────────────────────────────────────────
export { TextField } from '../../components/TextField';
export { PasswordInput } from '../../components/PasswordInput';
export { TextArea } from '../../components/TextArea';
export { Select } from '../../components/Select';
export { SearchInput } from '../../components/SearchInput';
export { Checkbox } from '../../components/Checkbox';
export { Toggle } from '../../components/Toggle';
export { Radio } from '../../components/Radio';

// ── Data Display ────────────────────────────────────────────────────────────
export { Badge } from '../../components/Badge';
export { Card, CardHeader, CardBody } from '../../components/Card';
export { Tabs } from '../../components/Tabs';
export { Divider } from '../../components/Divider';

// ── Typography ──────────────────────────────────────────────────────────────
export { Heading, Text, Label, MonoText } from '../../components/Typography';

// ── Feedback ────────────────────────────────────────────────────────────────
export { Skeleton } from '../../components/Skeleton';
export { EmptyState } from '../../components/EmptyState';
export { ErrorState } from '../../components/ErrorState';
export { Loader } from '../../components/Loader';
export { Tooltip } from '../../components/Tooltip';
export { DemoBanner } from '../../components/DemoBanner';

// ── Theme ───────────────────────────────────────────────────────────────────
export { ThemeToggle } from './ThemeToggle';

// ── Overlay ─────────────────────────────────────────────────────────────────
export { Modal } from '../../components/Modal';
export { Drawer } from '../../components/Drawer';

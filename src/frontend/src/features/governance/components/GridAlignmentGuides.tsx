/**
 * GridAlignmentGuides — linhas-guia de alinhamento tipo Figma/Grafana no canvas.
 * Detecta widgets sendo arrastados/redimensionados via classe CSS do RGL
 * e mostra linhas tracejadas quando bordas se alinham.
 */
import { useEffect, useRef, useState, useCallback } from 'react';

export interface GuideLine {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  label?: string;
}

export interface GridAlignmentGuidesProps {
  containerRef: React.RefObject<HTMLDivElement | null>;
  /** Grid config: cols, rowHeight, margin[x,y], containerPadding[x,y] */
  cols: number;
  rowHeight: number;
  marginX: number;
  marginY: number;
  paddingX: number;
  paddingY: number;
}

function getWidgetRects(container: HTMLDivElement): Array<{
  id: string;
  left: number;
  top: number;
  right: number;
  bottom: number;
  width: number;
  height: number;
  centerX: number;
  centerY: number;
}> {
  const items = container.querySelectorAll<HTMLElement>('.react-grid-item');
  const result = [];
  for (const item of items) {
    if (item.classList.contains('react-grid-item-dragging')) continue;
    if (item.classList.contains('react-grid-item-resizing')) continue;
    if (item.classList.contains('react-grid-placeholder')) continue;
    const rect = item.getBoundingClientRect();
    const containerRect = container.getBoundingClientRect();
    result.push({
      id: item.getAttribute('data-grid-i') || item.getAttribute('key') || '',
      left: rect.left - containerRect.left,
      top: rect.top - containerRect.top,
      right: rect.right - containerRect.left,
      bottom: rect.bottom - containerRect.top,
      width: rect.width,
      height: rect.height,
      centerX: rect.left - containerRect.left + rect.width / 2,
      centerY: rect.top - containerRect.top + rect.height / 2,
    });
  }
  return result;
}

function getActiveRect(container: HTMLDivElement): {
  left: number;
  top: number;
  right: number;
  bottom: number;
  width: number;
  height: number;
  centerX: number;
  centerY: number;
} | null {
  const active = container.querySelector<HTMLElement>(
    '.react-grid-item-dragging, .react-grid-item-resizing'
  );
  if (!active) return null;
  const rect = active.getBoundingClientRect();
  const containerRect = container.getBoundingClientRect();
  return {
    left: rect.left - containerRect.left,
    top: rect.top - containerRect.top,
    right: rect.right - containerRect.left,
    bottom: rect.bottom - containerRect.top,
    width: rect.width,
    height: rect.height,
    centerX: rect.left - containerRect.left + rect.width / 2,
    centerY: rect.top - containerRect.top + rect.height / 2,
  };
}

const SNAP_THRESHOLD_PX = 6;

function computeGuides(
  active: NonNullable<ReturnType<typeof getActiveRect>>,
  others: ReturnType<typeof getWidgetRects>,
  containerWidth: number,
  containerHeight: number
): GuideLine[] {
  const guides: GuideLine[] = [];
  const activeEdges = [
    { pos: active.left, name: 'left' },
    { pos: active.right, name: 'right' },
    { pos: active.centerX, name: 'centerX' },
  ];
  const activeYEdges = [
    { pos: active.top, name: 'top' },
    { pos: active.bottom, name: 'bottom' },
    { pos: active.centerY, name: 'centerY' },
  ];

  // Align with other widgets
  for (const other of others) {
    const otherXEdges = [
      { pos: other.left, name: 'left' },
      { pos: other.right, name: 'right' },
      { pos: other.centerX, name: 'centerX' },
    ];
    const otherYEdges = [
      { pos: other.top, name: 'top' },
      { pos: other.bottom, name: 'bottom' },
      { pos: other.centerY, name: 'centerY' },
    ];

    for (const ae of activeEdges) {
      for (const oe of otherXEdges) {
        if (Math.abs(ae.pos - oe.pos) <= SNAP_THRESHOLD_PX) {
          guides.push({
            x1: ae.pos,
            y1: Math.min(active.top, other.top) - 8,
            x2: ae.pos,
            y2: Math.max(active.bottom, other.bottom) + 8,
            label: `${Math.round(ae.pos)}px`,
          });
        }
      }
    }

    for (const ae of activeYEdges) {
      for (const oe of otherYEdges) {
        if (Math.abs(ae.pos - oe.pos) <= SNAP_THRESHOLD_PX) {
          guides.push({
            x1: Math.min(active.left, other.left) - 8,
            y1: ae.pos,
            x2: Math.max(active.right, other.right) + 8,
            y2: ae.pos,
            label: `${Math.round(ae.pos)}px`,
          });
        }
      }
    }
  }

  // Align with container edges
  for (const ae of activeEdges) {
    if (Math.abs(ae.pos) <= SNAP_THRESHOLD_PX) {
      guides.push({
        x1: 0,
        y1: Math.min(active.top, 0) - 4,
        x2: 0,
        y2: Math.max(active.bottom, containerHeight) + 4,
      });
    }
    if (Math.abs(ae.pos - containerWidth) <= SNAP_THRESHOLD_PX) {
      guides.push({
        x1: containerWidth,
        y1: Math.min(active.top, 0) - 4,
        x2: containerWidth,
        y2: Math.max(active.bottom, containerHeight) + 4,
      });
    }
  }

  for (const ae of activeYEdges) {
    if (Math.abs(ae.pos) <= SNAP_THRESHOLD_PX) {
      guides.push({
        x1: Math.min(active.left, 0) - 4,
        y1: 0,
        x2: Math.max(active.right, containerWidth) + 4,
        y2: 0,
      });
    }
    if (Math.abs(ae.pos - containerHeight) <= SNAP_THRESHOLD_PX) {
      guides.push({
        x1: Math.min(active.left, 0) - 4,
        y1: containerHeight,
        x2: Math.max(active.right, containerWidth) + 4,
        y2: containerHeight,
      });
    }
  }

  // Deduplicate lines that are very close
  const deduped: GuideLine[] = [];
  for (const g of guides) {
    const isDuplicate = deduped.some((d) => {
      const dx1 = Math.abs(d.x1 - g.x1);
      const dy1 = Math.abs(d.y1 - g.y1);
      const dx2 = Math.abs(d.x2 - g.x2);
      const dy2 = Math.abs(d.y2 - g.y2);
      return dx1 < 2 && dy1 < 2 && dx2 < 2 && dy2 < 2;
    });
    if (!isDuplicate) deduped.push(g);
  }

  return deduped;
}

export function GridAlignmentGuides({
  containerRef,
}: GridAlignmentGuidesProps) {
  const [guides, setGuides] = useState<GuideLine[]>([]);
  const rafRef = useRef<number>(0);
  const lastActiveRef = useRef<boolean>(false);

  const tick = useCallback(() => {
    const container = containerRef.current;
    if (!container) {
      setGuides([]);
      // eslint-disable-next-line react-hooks/immutability
      rafRef.current = requestAnimationFrame(tick);
      return;
    }

    const active = getActiveRect(container);
    const hasActive = active !== null;

    if (!hasActive) {
      if (lastActiveRef.current) {
        setGuides([]);
      }
      lastActiveRef.current = false;
      // eslint-disable-next-line react-hooks/immutability
      rafRef.current = requestAnimationFrame(tick);
      return;
    }

    lastActiveRef.current = true;
    const others = getWidgetRects(container);
    const containerRect = container.getBoundingClientRect();
    const newGuides = computeGuides(
      active,
      others,
      containerRect.width,
      containerRect.height
    );

    setGuides((prev) => {
      if (prev.length !== newGuides.length) return newGuides;
      for (let i = 0; i < prev.length; i++) {
        const p = prev[i];
        const n = newGuides[i];
        if (
          Math.abs(p.x1 - n.x1) > 1 ||
          Math.abs(p.y1 - n.y1) > 1 ||
          Math.abs(p.x2 - n.x2) > 1 ||
          Math.abs(p.y2 - n.y2) > 1
        ) {
          return newGuides;
        }
      }
      return prev;
    });

    // eslint-disable-next-line react-hooks/immutability
    rafRef.current = requestAnimationFrame(tick);
  }, [containerRef]);

  useEffect(() => {
    rafRef.current = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(rafRef.current);
  }, [tick]);

  if (guides.length === 0) return null;

  return (
    <svg className="alignment-guides" width="100%" height="100%">
      {guides.map((g, i) => (
        <g key={i}>
          <line
            x1={g.x1}
            y1={g.y1}
            x2={g.x2}
            y2={g.y2}
            className="alignment-guide-line"
          />
          {g.label && (
            <text
              x={g.x1 === g.x2 ? g.x1 + 4 : g.x2 - 4}
              y={g.y1 === g.y2 ? g.y1 - 4 : g.y2 + 10}
              className="alignment-guide-label"
              textAnchor={g.x1 === g.x2 ? 'start' : 'end'}
            >
              {g.label}
            </text>
          )}
        </g>
      ))}
    </svg>
  );
}

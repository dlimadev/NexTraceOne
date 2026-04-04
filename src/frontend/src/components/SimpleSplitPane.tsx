import React, { useRef, useState } from 'react';

interface SimpleSplitPaneProps {
  left: React.ReactNode;
  right: React.ReactNode;
  initialLeftPercent?: number;
  minLeftPercent?: number;
  minRightPercent?: number;
  className?: string;
}

export const SimpleSplitPane: React.FC<SimpleSplitPaneProps> = ({
  left,
  right,
  initialLeftPercent = 50,
  minLeftPercent = 0,
  minRightPercent = 0,
  className = '',
}) => {
  const [leftPercent, setLeftPercent] = useState(initialLeftPercent);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const draggingRef = useRef(false);
  const prevLeftRef = useRef<number | null>(null);

  function onPointerDown(e: React.PointerEvent) {
    draggingRef.current = true;
    try { (e.target as Element).setPointerCapture(e.pointerId); } catch { /* no-op */ }
  }

  function onSeparatorDoubleClick() {
    // toggle collapse/restore
    if (leftPercent > 0) {
      prevLeftRef.current = leftPercent;
      setLeftPercent(0);
    } else {
      setLeftPercent(prevLeftRef.current ?? initialLeftPercent);
      prevLeftRef.current = null;
    }
  }

  function onPointerMove(e: React.PointerEvent) {
    if (!draggingRef.current || !containerRef.current) return;
    const rect = containerRef.current.getBoundingClientRect();
    const x = e.clientX - rect.left;
    let pct = (x / rect.width) * 100;
    pct = Math.max(pct, minLeftPercent);
    pct = Math.min(pct, 100 - minRightPercent);
    setLeftPercent(pct);
  }

  function onPointerUp(e: React.PointerEvent) {
    draggingRef.current = false;
    try { (e.target as Element).releasePointerCapture(e.pointerId); } catch { /* no-op */ }
  }

  return (
    <div ref={containerRef} className={`flex h-full min-h-0 ${className}`}>
      <div style={{ width: `${leftPercent}%` }} className="flex flex-col min-w-0">
        {left}
      </div>
      <div
        role="separator"
        className="w-1.5 cursor-col-resize bg-edge hover:bg-accent/40 flex items-center justify-center"
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={onPointerUp}
        onDoubleClick={onSeparatorDoubleClick}
      >
        <div style={{ width: 1 }} />
      </div>
      <div style={{ width: `${100 - leftPercent}%` }} className="flex flex-col min-w-0">
        {right}
      </div>
    </div>
  );
};

export default SimpleSplitPane;

import { describe, it, expect, vi } from 'vitest';

// Verifica o seam de onSuccess isoladamente: dado um draftId, quando onCreated
// existe chama-o; caso contrário navega para o studio.
function onDraftCreated(
  data: { draftId: string },
  onCreated: ((id: string) => void) | undefined,
  navigate: (path: string) => void,
) {
  if (onCreated) onCreated(data.draftId);
  else navigate(`/contracts/studio/${data.draftId}`);
}

describe('useContractDraftForm onCreated seam', () => {
  it('calls onCreated when provided', () => {
    const onCreated = vi.fn();
    const navigate = vi.fn();
    onDraftCreated({ draftId: 'd1' }, onCreated, navigate);
    expect(onCreated).toHaveBeenCalledWith('d1');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('navigates to studio when onCreated absent', () => {
    const navigate = vi.fn();
    onDraftCreated({ draftId: 'd2' }, undefined, navigate);
    expect(navigate).toHaveBeenCalledWith('/contracts/studio/d2');
  });
});

import { useState } from 'react'
import { useTranslation } from 'react-i18next'

interface AgentFeedbackWidgetProps {
  executionId: string
  onFeedbackSubmitted?: () => void
}

interface FeedbackPayload {
  rating: number
  outcome: 'resolved' | 'partial' | 'incorrect'
  comment?: string
  submittedBy: string
  tenantId: string
  wasCorrect: boolean
}

export function AgentFeedbackWidget({ executionId, onFeedbackSubmitted }: AgentFeedbackWidgetProps) {
  const { t } = useTranslation()
  const [rating, setRating] = useState(0)
  const [outcome, setOutcome] = useState<FeedbackPayload['outcome'] | null>(null)
  const [comment, setComment] = useState('')
  const [submitted, setSubmitted] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async () => {
    if (rating === 0 || !outcome) return
    setSubmitting(true)
    try {
      await fetch(`/api/v1/ai/executions/${executionId}/feedback`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          rating,
          outcome,
          comment: comment || undefined,
          wasCorrect: outcome === 'resolved',
          submittedBy: 'current-user',
          tenantId: 'current-tenant',
        } satisfies FeedbackPayload),
      })
      setSubmitted(true)
      onFeedbackSubmitted?.()
    } catch {
      // silent fail — feedback is best-effort
    } finally {
      setSubmitting(false)
    }
  }

  if (submitted) {
    return (
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <span>✓</span>
        <span>{t('ai.feedback.submitted')}</span>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-2 pt-2 border-t border-border/50">
      <p className="text-xs text-muted-foreground">{t('ai.feedback.prompt')}</p>

      <div className="flex gap-1" role="group" aria-label={t('ai.feedback.ratingLabel')}>
        {[1, 2, 3, 4, 5].map((star) => (
          <button
            key={star}
            type="button"
            onClick={() => setRating(star)}
            className={`text-lg transition-colors ${star <= rating ? 'text-yellow-400' : 'text-muted-foreground/30 hover:text-yellow-300'}`}
            aria-label={t('ai.feedback.star', { count: star })}
          >
            ★
          </button>
        ))}
      </div>

      <div className="flex gap-2">
        {(['resolved', 'partial', 'incorrect'] as const).map((o) => (
          <button
            key={o}
            type="button"
            onClick={() => setOutcome(o)}
            className={`px-2 py-1 text-xs rounded border transition-colors ${
              outcome === o
                ? 'bg-primary text-primary-foreground border-primary'
                : 'border-border hover:border-primary/50'
            }`}
          >
            {t(`ai.feedback.outcome.${o}`)}
          </button>
        ))}
      </div>

      <textarea
        value={comment}
        onChange={(e) => setComment(e.target.value)}
        placeholder={t('ai.feedback.commentPlaceholder')}
        className="w-full text-xs resize-none rounded border border-border bg-background p-2 focus:outline-none focus:ring-1 focus:ring-ring"
        rows={2}
        maxLength={500}
      />

      <button
        type="button"
        onClick={handleSubmit}
        disabled={rating === 0 || !outcome || submitting}
        className="self-start px-3 py-1 text-xs rounded bg-primary text-primary-foreground hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {submitting ? t('common.saving') : t('ai.feedback.submit')}
      </button>
    </div>
  )
}

export default AgentFeedbackWidget

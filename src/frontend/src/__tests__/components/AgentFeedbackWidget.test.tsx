import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { AgentFeedbackWidget } from '../../components/ai/AgentFeedbackWidget'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => {
      const parts = key.split('.')
      const last = parts[parts.length - 1]
      return opts ? `${last}:${JSON.stringify(opts)}` : last
    },
  }),
}))

describe('AgentFeedbackWidget', () => {
  it('renders rating stars', () => {
    render(<AgentFeedbackWidget executionId="test-exec-id" />)
    const stars = screen.getAllByRole('button').filter(b => b.textContent === '★')
    expect(stars).toHaveLength(5)
  })

  it('renders outcome buttons', () => {
    render(<AgentFeedbackWidget executionId="test-exec-id" />)
    expect(screen.getByText('resolved')).toBeDefined()
    expect(screen.getByText('partial')).toBeDefined()
    expect(screen.getByText('incorrect')).toBeDefined()
  })

  it('submit button is disabled when no rating and outcome selected', () => {
    render(<AgentFeedbackWidget executionId="test-exec-id" />)
    const submitBtn = screen.getByText('submit')
    expect(submitBtn.closest('button')?.disabled).toBe(true)
  })

  it('submit button enables when rating and outcome selected', () => {
    render(<AgentFeedbackWidget executionId="test-exec-id" />)
    fireEvent.click(screen.getAllByText('★')[4])
    fireEvent.click(screen.getByText('resolved'))
    const submitBtn = screen.getByText('submit')
    expect(submitBtn.closest('button')?.disabled).toBe(false)
  })
})

import i18n from '../i18n';
import type { AxiosError } from 'axios';

/**
 * Estrutura padronizada de erro retornada pelo backend via Problem Details.
 *
 * O campo `code` contém a chave i18n (ex.: "Identity.Auth.InvalidCredentials")
 * que pode ser usada para resolver a mensagem de UX via catálogo de tradução.
 * O campo `detail` contém a mensagem técnica em inglês para logs/debug.
 */
interface ApiProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  type?: string;
}

/**
 * Mapeamento de códigos de erro do backend para chaves i18n do frontend.
 * Permite resolução automática da mensagem de UX a partir do code retornado pela API.
 */
const ERROR_CODE_MAP: Record<string, string> = {
  'Identity.Auth.InvalidCredentials': 'auth.invalidCredentials',
  'Identity.Auth.AccountLocked': 'errors.forbidden',
  'Identity.Auth.AccountDeactivated': 'errors.forbidden',
  'Identity.Auth.NotAuthenticated': 'auth.notAuthenticated',
  'Identity.Auth.InsufficientPermissions': 'authorization.insufficientPermissions',
  'Identity.Session.Expired': 'auth.sessionExpired',
  'Identity.Session.Revoked': 'auth.sessionExpired',
  'Identity.Session.InvalidRefreshToken': 'auth.sessionExpired',
  'Identity.User.NotFound': 'errors.notFound',
  'Identity.User.EmailAlreadyExists': 'errors.conflict',
  'Identity.Tenant.NotFound': 'errors.notFound',
  'Identity.TenantMembership.NotFound': 'errors.notFound',

  // ── AI Governance errors (E-M04) ──────────────────────────────────────
  'AiGovernance.Budget.QuotaExceeded': 'aiHub.errors.quotaExceeded',
  'AiGovernance.Model.Blocked': 'aiHub.errors.modelBlocked',
  'AiGovernance.Model.Inactive': 'aiHub.errors.modelInactive',
  'AiGovernance.Model.NotFound': 'aiHub.errors.modelNotFound',
  'AiGovernance.ExternalAI.NotAllowed': 'aiHub.errors.externalAiNotAllowed',
  'AiGovernance.Agent.AccessDenied': 'aiHub.errors.agentAccessDenied',
  'AiGovernance.Agent.NotFound': 'aiHub.errors.agentNotFound',
  'AiGovernance.Guardrail.Violation': 'aiHub.errors.guardrailViolation',
  'AiGovernance.Guardrail.NotFound': 'aiHub.errors.guardrailNotFound',
  'AiGovernance.Guardrail.DuplicateName': 'aiHub.errors.guardrailDuplicateName',
  'AiGovernance.Conversation.NotFound': 'aiHub.errors.conversationNotFound',
  'AiGovernance.Conversation.AccessDenied': 'aiHub.errors.conversationAccessDenied',
  'AiGovernance.Conversation.NotActive': 'aiHub.errors.conversationNotActive',
  'AiGovernance.KnowledgeSource.NotFound': 'aiHub.errors.knowledgeSourceNotFound',
  'AiGovernance.RoutingStrategy.NotFound': 'aiHub.errors.routingStrategyNotFound',
  'AiGovernance.Policy.NotFound': 'aiHub.errors.policyNotFound',
  'AiGovernance.IDE.InvalidClientType': 'aiHub.errors.ideInvalidClientType',
};

/**
 * Extrai e resolve a mensagem de erro de uma resposta Axios para exibição na UI.
 *
 * Prioridade de resolução:
 * 1. Mapeia code do backend para chave i18n via ERROR_CODE_MAP.
 * 2. Mapeia status HTTP para mensagem genérica.
 * 3. Fallback para mensagem genérica de erro.
 *
 * A mensagem técnica (detail) NÃO é exibida ao usuário final — apenas em console/log.
 */
export function resolveApiError(error: unknown): string {
  const axiosError = error as AxiosError<ApiProblemDetails>;
  const data = axiosError?.response?.data;

  if (data?.code) {
    const translationKey = ERROR_CODE_MAP[data.code];
    if (translationKey) {
      return i18n.t(translationKey);
    }
  }

  const status = axiosError?.response?.status;
  if (status) {
    switch (status) {
      case 400: return i18n.t('errors.validation');
      case 401: return i18n.t('auth.notAuthenticated');
      case 403: return i18n.t('errors.forbidden');
      case 404: return i18n.t('errors.notFound');
      case 409: return i18n.t('errors.conflict');
      case 500: return i18n.t('errors.serverError');
    }
  }

  if (axiosError?.code === 'ERR_NETWORK') {
    return i18n.t('errors.networkError');
  }

  return i18n.t('errors.generic');
}

/**
 * Resolve mensagens de erro específicas do módulo de IA para exibição na UI.
 * Enriquece o resolveApiError padrão com contexto específico do AI Hub.
 * (E-M04)
 *
 * @param error - Erro capturado em operações de IA (chat, agentes, modelos)
 * @param context - Contexto da operação (ex: 'chat', 'agent', 'model', 'guardrail')
 * @returns Mensagem traduzida para exibição ao utilizador
 */
export function mapAiError(error: unknown, context?: string): string {
  const axiosError = error as AxiosError<ApiProblemDetails>;
  const code = axiosError?.response?.data?.code;

  // Erros AI específicos com mensagem contextualizada
  if (code) {
    const mapped = ERROR_CODE_MAP[code];
    if (mapped) {
      return i18n.t(mapped);
    }
  }

  // Fallback por contexto de operação
  const status = axiosError?.response?.status;
  if (status === 429) {
    return i18n.t('aiHub.errors.quotaExceeded');
  }

  if (status === 403 && context === 'agent') {
    return i18n.t('aiHub.errors.agentAccessDenied');
  }

  if (status === 403) {
    return i18n.t('aiHub.errors.externalAiNotAllowed');
  }

  if (status === 400 && context === 'guardrail') {
    return i18n.t('aiHub.errors.guardrailViolation');
  }

  // Fallback genérico para erros de IA
  if (context === 'chat') {
    return i18n.t('aiHub.errorSendingMessage');
  }

  return resolveApiError(error);
}

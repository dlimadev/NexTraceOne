/**
 * Política de contratos por tipo de interface — espelho do InterfaceContractPolicy do backend.
 * Define quais ContractTypes são permitidos para cada InterfaceType.
 * Usado para validação no frontend antes de criar um contrato binding.
 */
import type { InterfaceType } from '../../../types';
import type { ContractTypeValue } from './constants';

/**
 * Mapeamento oficial: InterfaceType → ContractType(s) permitidos.
 * Interfaces com lista vazia não expõem contratos de interface pública.
 */
export const INTERFACE_CONTRACT_POLICY: Record<InterfaceType, ContractTypeValue[]> = {
  RestApi:          ['RestApi'],
  SoapService:      ['Soap'],
  KafkaProducer:    ['Event', 'SharedSchema'],
  KafkaConsumer:    ['Event', 'SharedSchema'],
  GrpcService:      [],  // ContractType.Grpc não exposto no frontend ainda
  GraphqlApi:       ['RestApi'],
  BackgroundWorker: [],
  ScheduledJob:     [],
  WebhookProducer:  ['RestApi', 'Event'],
  WebhookConsumer:  [],
  ZosConnectApi:    ['RestApi'],
  MqQueue:          ['MqMessage'],
  IntegrationBridge: ['RestApi', 'Soap', 'Event'],
};

/**
 * Indica se um tipo de interface suporta contratos.
 */
export function supportsContractsForInterface(interfaceType: InterfaceType): boolean {
  const allowed = INTERFACE_CONTRACT_POLICY[interfaceType];
  return Array.isArray(allowed) && allowed.length > 0;
}

/**
 * Retorna a lista de tipos de contrato permitidos para um tipo de interface.
 */
export function allowedContractTypesForInterface(interfaceType: InterfaceType): ContractTypeValue[] {
  return INTERFACE_CONTRACT_POLICY[interfaceType] ?? [];
}

/**
 * Indica se um tipo de interface requer contrato por política.
 * Por padrão, qualquer interface que suporte contratos é elegível mas não obrigatória
 * a nível de política — a obrigatoriedade é definida pelo campo requiresContract na entidade.
 */
export function requiresContractForInterface(interfaceType: InterfaceType): boolean {
  return supportsContractsForInterface(interfaceType);
}

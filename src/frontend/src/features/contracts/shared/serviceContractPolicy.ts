/**
 * Política de contratos por tipo de serviço — espelho do ServiceContractPolicy do backend.
 * Define quais ContractTypes são permitidos para cada ServiceType do catálogo.
 * Usado para validação no frontend antes de enviar a criação de draft ao backend.
 */
import type { ServiceType } from '../../../types';
import type { ContractTypeValue } from './constants';

/**
 * Mapeamento oficial: ServiceType → ContractType(s) permitidos.
 * Serviços com lista vazia não expõem contratos de interface pública.
 */
export const SERVICE_CONTRACT_POLICY: Record<ServiceType, ContractTypeValue[]> = {
  RestApi:               ['RestApi'],
  SoapService:           ['Soap'],
  KafkaProducer:         ['Event', 'SharedSchema'],
  KafkaConsumer:         ['Event', 'SharedSchema'],
  GraphqlApi:            ['RestApi'],
  GrpcService:           [],  // ContractType.Grpc not yet exposed in frontend
  ZosConnectApi:         ['RestApi'],
  CicsTransaction:       ['CicsCommarea'],
  CobolProgram:          ['Copybook'],
  MqQueueManager:        ['MqMessage'],
  IntegrationComponent:  ['RestApi', 'Soap', 'Event'],
  Gateway:               ['RestApi'],
  ThirdParty:            ['RestApi', 'Soap', 'Event'],
  LegacySystem:          ['FixedLayout', 'Copybook', 'MqMessage'],
  SharedPlatformService: ['SharedSchema'],
  Framework:             ['SharedSchema'],
  // Sem contrato de interface pública
  BackgroundService:     [],
  ScheduledProcess:      [],
  BatchJob:              [],
  MainframeSystem:       [],
  ImsTransaction:        [],
};

/**
 * Indica se um tipo de serviço suporta contratos de interface pública.
 */
export function supportsContracts(serviceType: ServiceType): boolean {
  const allowed = SERVICE_CONTRACT_POLICY[serviceType];
  return Array.isArray(allowed) && allowed.length > 0;
}

/**
 * Retorna a lista de tipos de contrato permitidos para um tipo de serviço.
 * Retorna lista vazia se o serviço não suporta contratos.
 */
export function allowedContractTypes(serviceType: ServiceType): ContractTypeValue[] {
  return SERVICE_CONTRACT_POLICY[serviceType] ?? [];
}

/**
 * Verifica se um tipo de contrato é permitido para um tipo de serviço.
 */
export function isContractTypeAllowed(
  serviceType: ServiceType,
  contractType: ContractTypeValue,
): boolean {
  return allowedContractTypes(serviceType).includes(contractType);
}

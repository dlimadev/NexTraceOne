import { z } from 'zod';

/** Valores do passo de Identidade & Ownership (campos aceites por registerService). */
export interface ServiceIdentityValues {
  name: string;
  domain: string;
  teamName: string;
  description: string;
  serviceType: string;
  criticality: string;
  exposureType: string;
  technicalOwner: string;
  businessOwner: string;
  documentationUrl: string;
  repositoryUrl: string;
}

export const EMPTY_IDENTITY: ServiceIdentityValues = {
  name: '', domain: '', teamName: '', description: '',
  serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
  technicalOwner: '', businessOwner: '', documentationUrl: '', repositoryUrl: '',
};

const nonEmpty = z.string().trim().min(1);

export const serviceIdentitySchema = z.object({
  name: nonEmpty,
  domain: nonEmpty,
  teamName: nonEmpty,
  serviceType: nonEmpty,
});

/** Valores do passo de Interface (subconjunto de createServiceInterface). */
export interface ServiceInterfaceValues {
  name: string;
  interfaceType: string;
  description: string;
  exposureScope: string;
  basePath: string;
  topicName: string;
  wsdlNamespace: string;
  grpcServiceName: string;
  scheduleCron: string;
  documentationUrl: string;
  requiresContract: boolean;
}

export const EMPTY_INTERFACE: ServiceInterfaceValues = {
  name: '', interfaceType: 'RestApi', description: '', exposureScope: 'Internal',
  basePath: '', topicName: '', wsdlNamespace: '', grpcServiceName: '',
  scheduleCron: '', documentationUrl: '', requiresContract: false,
};

export const serviceInterfaceSchema = z.object({
  name: nonEmpty,
  interfaceType: nonEmpty,
});

/** Resultado estrutural de safeParse — evita depender do tipo interno do Zod. */
type SafeParseLike = {
  success: boolean;
  error?: { issues: ReadonlyArray<{ path: ReadonlyArray<PropertyKey>; message: string }> };
};

/** Converte issues do Zod num mapa campo→primeira-mensagem. */
function issuesToMap<T>(result: SafeParseLike): Partial<Record<keyof T, string>> {
  if (result.success || !result.error) return {};
  const map: Partial<Record<keyof T, string>> = {};
  for (const issue of result.error.issues) {
    const key = issue.path[0] as keyof T;
    if (key && !map[key]) map[key] = issue.message;
  }
  return map;
}

export function validateIdentity(
  values: ServiceIdentityValues,
): Partial<Record<keyof ServiceIdentityValues, string>> {
  return issuesToMap<ServiceIdentityValues>(serviceIdentitySchema.safeParse(values));
}

export function validateInterface(
  values: ServiceInterfaceValues,
): Partial<Record<keyof ServiceInterfaceValues, string>> {
  return issuesToMap<ServiceInterfaceValues>(serviceInterfaceSchema.safeParse(values));
}

/** Opções de select — labelKey é uma chave i18n resolvida pelo componente. */
export interface SelectOptionKey { value: string; labelKey: string; }

export const SERVICE_TYPE_OPTIONS: SelectOptionKey[] = [
  { value: 'RestApi', labelKey: 'catalog.badges.type.RestApi' },
  { value: 'GraphqlApi', labelKey: 'catalog.badges.type.GraphqlApi' },
  { value: 'GrpcService', labelKey: 'catalog.badges.type.GrpcService' },
  { value: 'SoapService', labelKey: 'catalog.badges.type.SoapService' },
  { value: 'KafkaProducer', labelKey: 'catalog.badges.type.KafkaProducer' },
  { value: 'KafkaConsumer', labelKey: 'catalog.badges.type.KafkaConsumer' },
  { value: 'BackgroundService', labelKey: 'catalog.badges.type.BackgroundService' },
  { value: 'IntegrationComponent', labelKey: 'catalog.badges.type.IntegrationComponent' },
  { value: 'ThirdParty', labelKey: 'catalog.badges.type.ThirdParty' },
];

export const CRITICALITY_OPTIONS: SelectOptionKey[] = [
  { value: 'Low', labelKey: 'catalog.badges.criticality.Low' },
  { value: 'Medium', labelKey: 'catalog.badges.criticality.Medium' },
  { value: 'High', labelKey: 'catalog.badges.criticality.High' },
  { value: 'Critical', labelKey: 'catalog.badges.criticality.Critical' },
];

export const EXPOSURE_OPTIONS: SelectOptionKey[] = [
  { value: 'Internal', labelKey: 'catalog.badges.exposure.Internal' },
  { value: 'Partner', labelKey: 'catalog.badges.exposure.Partner' },
  { value: 'External', labelKey: 'catalog.badges.exposure.External' },
];

export const INTERFACE_TYPE_OPTIONS: SelectOptionKey[] = [
  { value: 'RestApi', labelKey: 'serviceInterfaces.typeRestApi' },
  { value: 'GraphqlApi', labelKey: 'serviceInterfaces.typeGraphqlApi' },
  { value: 'GrpcService', labelKey: 'serviceInterfaces.typeGrpcService' },
  { value: 'SoapService', labelKey: 'serviceInterfaces.typeSoapService' },
  { value: 'KafkaProducer', labelKey: 'serviceInterfaces.typeKafkaProducer' },
  { value: 'KafkaConsumer', labelKey: 'serviceInterfaces.typeKafkaConsumer' },
];

export const INTERFACE_EXPOSURE_OPTIONS: SelectOptionKey[] = EXPOSURE_OPTIONS;

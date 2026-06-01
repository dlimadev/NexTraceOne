import { useTranslation } from 'react-i18next';
import { ContractBuilderLayout } from '../studio/ContractBuilderLayout';
import { AsyncApiChannelsPreview } from '../studio/components/previews/AsyncApiChannelsPreview';

const ASYNCAPI_TEMPLATE = `asyncapi: 3.0.0
info:
  title: My Event-Driven API
  version: 1.0.0
channels:
  userRegistered:
    address: user.registered
    bindings:
      kafka: {}
    messages:
      userRegisteredMessage:
        payload:
          type: object
          properties:
            userId:
              type: string
            email:
              type: string
  orderCreated:
    address: order.created
    bindings:
      kafka: {}
    messages:
      orderCreatedMessage:
        payload:
          type: object
          properties:
            orderId:
              type: string
            total:
              type: number
operations:
  onUserRegistered:
    action: receive
    channel:
      $ref: '#/channels/userRegistered'
  onOrderCreated:
    action: receive
    channel:
      $ref: '#/channels/orderCreated'
`;

export function AsyncApiBuilderPage() {
  const { t } = useTranslation();

  return (
    <ContractBuilderLayout
      contractName={t('asyncApiBuilder.title')}
      protocol="AsyncAPI 3.x"
      language="yaml"
      initialContent={ASYNCAPI_TEMPLATE}
      renderPreview={(content) => <AsyncApiChannelsPreview content={content} />}
    />
  );
}

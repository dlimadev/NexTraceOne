import { useTranslation } from 'react-i18next';
import { ContractBuilderLayout } from '../studio/ContractBuilderLayout';
import { RestOperationsPreview } from '../studio/components/previews/RestOperationsPreview';

const REST_TEMPLATE = `openapi: 3.1.0
info:
  title: My REST API
  version: 1.0.0
  description: API description
servers:
  - url: https://api.example.com/v1
paths:
  /resources:
    get:
      summary: List resources
      operationId: listResources
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Resource'
    post:
      summary: Create resource
      operationId: createResource
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/ResourceInput'
      responses:
        '201':
          description: Created
components:
  schemas:
    Resource:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
    ResourceInput:
      type: object
      required: [name]
      properties:
        name:
          type: string
`;

export function RestOpenApiBuilderPage() {
  const { t } = useTranslation();

  return (
    <ContractBuilderLayout
      contractName={t('restBuilder.title')}
      protocol="OpenAPI 3.1"
      language="yaml"
      initialContent={REST_TEMPLATE}
      renderPreview={(content) => <RestOperationsPreview content={content} />}
    />
  );
}

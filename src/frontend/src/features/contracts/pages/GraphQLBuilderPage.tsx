import { useTranslation } from 'react-i18next';
import { ContractBuilderLayout } from '../studio/ContractBuilderLayout';
import { GraphQlTypesPreview } from '../studio/components/previews/GraphQlTypesPreview';

const GRAPHQL_TEMPLATE = `type Query {
  user(id: ID!): User
  users(page: Int, pageSize: Int): UserConnection!
}

type Mutation {
  createUser(input: CreateUserInput!): User!
  updateUser(id: ID!, input: UpdateUserInput!): User!
  deleteUser(id: ID!): Boolean!
}

type User {
  id: ID!
  name: String!
  email: String!
  createdAt: String!
}

type UserConnection {
  items: [User!]!
  totalCount: Int!
}

input CreateUserInput {
  name: String!
  email: String!
}

input UpdateUserInput {
  name: String
  email: String
}
`;

export function GraphQLBuilderPage() {
  const { t } = useTranslation();

  return (
    <ContractBuilderLayout
      contractName={t('graphqlBuilder.title')}
      protocol="GraphQL SDL"
      language="graphql"
      initialContent={GRAPHQL_TEMPLATE}
      renderPreview={(content) => <GraphQlTypesPreview content={content} />}
    />
  );
}

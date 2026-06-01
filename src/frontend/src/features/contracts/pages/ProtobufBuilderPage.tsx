import { useTranslation } from 'react-i18next';
import { ContractBuilderLayout } from '../studio/ContractBuilderLayout';
import { ProtobufServicesPreview } from '../studio/components/previews/ProtobufServicesPreview';

const PROTO_TEMPLATE = `syntax = "proto3";

package myservice.v1;

option go_package = "github.com/example/myservice/v1";

// UserService manages user accounts.
service UserService {
  rpc GetUser (GetUserRequest) returns (User);
  rpc ListUsers (ListUsersRequest) returns (ListUsersResponse);
  rpc CreateUser (CreateUserRequest) returns (User);
  rpc DeleteUser (DeleteUserRequest) returns (DeleteUserResponse);
}

message User {
  string id = 1;
  string name = 2;
  string email = 3;
  string created_at = 4;
}

message GetUserRequest {
  string id = 1;
}

message ListUsersRequest {
  int32 page = 1;
  int32 page_size = 2;
}

message ListUsersResponse {
  repeated User users = 1;
  int32 total_count = 2;
}

message CreateUserRequest {
  string name = 1;
  string email = 2;
}

message DeleteUserRequest {
  string id = 1;
}

message DeleteUserResponse {
  bool success = 1;
}
`;

export function ProtobufBuilderPage() {
  const { t } = useTranslation();

  return (
    <ContractBuilderLayout
      contractName={t('protobufBuilder.title')}
      protocol=".proto / gRPC"
      language="proto"
      initialContent={PROTO_TEMPLATE}
      renderPreview={(content) => <ProtobufServicesPreview content={content} />}
    />
  );
}

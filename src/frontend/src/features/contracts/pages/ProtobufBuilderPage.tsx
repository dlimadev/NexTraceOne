import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FileCode2, AlertTriangle, CheckCircle2 } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';

const DEFAULT_PROTO = `syntax = "proto3";

package com.example.service.v1;

option go_package = "github.com/example/service/v1";
option java_multiple_files = true;

service UserService {
  rpc GetUser(GetUserRequest) returns (GetUserResponse);
  rpc ListUsers(ListUsersRequest) returns (ListUsersResponse);
  rpc CreateUser(CreateUserRequest) returns (CreateUserResponse);
  rpc DeleteUser(DeleteUserRequest) returns (DeleteUserResponse);
}

message GetUserRequest {
  string user_id = 1;
}

message GetUserResponse {
  User user = 1;
}

message ListUsersRequest {
  int32 page_size = 1;
  string page_token = 2;
}

message ListUsersResponse {
  repeated User users = 1;
  string next_page_token = 2;
}

message CreateUserRequest {
  string name = 1;
  string email = 2;
}

message CreateUserResponse {
  User user = 1;
}

message DeleteUserRequest {
  string user_id = 1;
}

message DeleteUserResponse {
  bool success = 1;
}

message User {
  string id = 1;
  string name = 2;
  string email = 3;
  int64 created_at = 4;
}
`;

export function ProtobufBuilderPage() {
  const { t } = useTranslation();
  const [proto, setProto] = useState(DEFAULT_PROTO);
  const [activeTab, setActiveTab] = useState<'editor' | 'services' | 'messages'>('editor');

  const serviceCount = (proto.match(/^service\s+\w+/gm) ?? []).length;
  const messageCount = (proto.match(/^message\s+\w+/gm) ?? []).length;
  const rpcCount = (proto.match(/^\s+rpc\s+\w+/gm) ?? []).length;

  const services = (proto.match(/^service\s+\w+/gm) ?? []).map((s) => s.replace('service ', ''));
  const messages = (proto.match(/^message\s+\w+/gm) ?? []).map((m) => m.replace('message ', ''));

  return (
    <PageContainer>
      <PageHeader
        title={t('protobufBuilder.title')}
        subtitle={t('protobufBuilder.subtitle')}
        actions={
          <div className="flex gap-2">
            <Button size="sm" variant="ghost">{t('protobufBuilder.checkCompat')}</Button>
            <Button size="sm">{t('protobufBuilder.publish')}</Button>
          </div>
        }
      />
      <PageSection>
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-4">
          {/* Stats sidebar */}
          <div className="lg:col-span-1 space-y-3">
            <Card>
              <CardBody className="p-4">
                <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-3">
                  {t('protobufBuilder.protoSummary')}
                </h3>
                <div className="space-y-2 text-xs">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('protobufBuilder.services')}</span>
                    <span className="font-bold">{serviceCount}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('protobufBuilder.rpcs')}</span>
                    <span className="font-bold">{rpcCount}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('protobufBuilder.messages')}</span>
                    <span className="font-bold">{messageCount}</span>
                  </div>
                </div>
              </CardBody>
            </Card>

            <Card>
              <CardBody className="p-4">
                <div className="flex items-center gap-2 mb-2">
                  <CheckCircle2 size={12} className="text-success" />
                  <h3 className="text-xs font-semibold">{t('protobufBuilder.compatibility')}</h3>
                </div>
                <Badge variant="success" className="text-xs">{t('protobufBuilder.backwardCompat')}</Badge>
              </CardBody>
            </Card>
          </div>

          {/* Main editor */}
          <div className="lg:col-span-3">
            <div className="flex gap-2 mb-3">
              {(['editor', 'services', 'messages'] as const).map((tab) => (
                <button
                  key={tab}
                  onClick={() => setActiveTab(tab)}
                  className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                    activeTab === tab ? 'bg-accent text-accent-foreground' : 'bg-muted text-muted-foreground hover:bg-muted/80'
                  }`}
                >
                  <FileCode2 size={10} />
                  {t(`protobufBuilder.tab.${tab}`)}
                </button>
              ))}
            </div>

            {activeTab === 'editor' && (
              <Card>
                <CardBody className="p-0">
                  <textarea
                    value={proto}
                    onChange={(e) => setProto(e.target.value)}
                    className="w-full h-[480px] p-4 text-xs font-mono bg-background rounded-lg resize-none border-0 focus:outline-none focus:ring-1 focus:ring-accent"
                    spellCheck={false}
                  />
                </CardBody>
              </Card>
            )}

            {activeTab === 'services' && (
              <Card>
                <CardBody className="p-4">
                  <div className="space-y-2">
                    {services.map((svc) => (
                      <div key={svc} className="flex items-center gap-2 p-2 rounded border">
                        <FileCode2 size={14} className="text-primary" />
                        <span className="text-sm font-mono font-medium">{svc}</span>
                        <Badge variant="secondary" className="text-xs ml-auto">
                          {(proto.match(new RegExp(`(?<=service ${svc}[^{]*{)[^}]*rpc`, 'gs')) ?? []).length} RPCs
                        </Badge>
                      </div>
                    ))}
                  </div>
                </CardBody>
              </Card>
            )}

            {activeTab === 'messages' && (
              <Card>
                <CardBody className="p-4">
                  <div className="grid grid-cols-2 gap-2">
                    {messages.map((msg) => (
                      <div key={msg} className="flex items-center gap-2 p-2 rounded border">
                        <AlertTriangle size={12} className="text-muted-foreground" />
                        <span className="text-xs font-mono">{msg}</span>
                      </div>
                    ))}
                  </div>
                </CardBody>
              </Card>
            )}
          </div>
        </div>

        <div className="mt-4 p-3 rounded-lg border border-primary/30 bg-primary/5 text-xs text-muted-foreground">
          {t('protobufBuilder.protoBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}

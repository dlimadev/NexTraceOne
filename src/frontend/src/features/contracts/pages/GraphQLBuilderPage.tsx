import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Hash, AlertTriangle, CheckCircle2 } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';

const DEFAULT_SDL = `type Query {
  user(id: ID!): User
  users(limit: Int = 10, offset: Int = 0): [User!]!
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

input CreateUserInput {
  name: String!
  email: String!
}

input UpdateUserInput {
  name: String
  email: String
}
`;

interface BreakingChange {
  type: string;
  description: string;
  severity: 'breaking' | 'dangerous' | 'safe';
}

export function GraphQLBuilderPage() {
  const { t } = useTranslation();
  const [sdl, setSdl] = useState(DEFAULT_SDL);
  const [previousSdl] = useState(DEFAULT_SDL);
  const [activeTab, setActiveTab] = useState<'editor' | 'diff' | 'explorer'>('editor');

  const breakingChanges: BreakingChange[] = [];

  const typeCount = (sdl.match(/^type\s+\w+/gm) ?? []).length;
  const inputCount = (sdl.match(/^input\s+\w+/gm) ?? []).length;
  const queryFields = (sdl.match(/^  \w+[^:]*:/gm) ?? []).length;

  return (
    <PageContainer>
      <PageHeader
        title={t('graphqlBuilder.title')}
        subtitle={t('graphqlBuilder.subtitle')}
        actions={
          <div className="flex gap-2">
            <Button size="sm" variant="ghost">{t('graphqlBuilder.validate')}</Button>
            <Button size="sm">{t('graphqlBuilder.publish')}</Button>
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
                  {t('graphqlBuilder.schemaSummary')}
                </h3>
                <div className="space-y-2 text-xs">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('graphqlBuilder.types')}</span>
                    <span className="font-bold">{typeCount}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('graphqlBuilder.inputs')}</span>
                    <span className="font-bold">{inputCount}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('graphqlBuilder.fields')}</span>
                    <span className="font-bold">{queryFields}</span>
                  </div>
                </div>
              </CardBody>
            </Card>

            <Card>
              <CardBody className="p-4">
                <div className="flex items-center gap-2 mb-2">
                  {breakingChanges.length === 0 ? (
                    <CheckCircle2 size={12} className="text-success" />
                  ) : (
                    <AlertTriangle size={12} className="text-destructive" />
                  )}
                  <h3 className="text-xs font-semibold">{t('graphqlBuilder.breakingChanges')}</h3>
                  <Badge variant={breakingChanges.length === 0 ? 'success' : 'destructive'}>{breakingChanges.length}</Badge>
                </div>
                {breakingChanges.length === 0 ? (
                  <p className="text-xs text-success">{t('graphqlBuilder.noBreakingChanges')}</p>
                ) : (
                  <div className="space-y-1">
                    {breakingChanges.map((bc, i) => (
                      <div key={i} className="text-xs text-destructive">{bc.description}</div>
                    ))}
                  </div>
                )}
              </CardBody>
            </Card>
          </div>

          {/* Main editor */}
          <div className="lg:col-span-3">
            <div className="flex gap-2 mb-3">
              {(['editor', 'diff', 'explorer'] as const).map((tab) => (
                <button
                  key={tab}
                  onClick={() => setActiveTab(tab)}
                  className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                    activeTab === tab ? 'bg-accent text-accent-foreground' : 'bg-muted text-muted-foreground hover:bg-muted/80'
                  }`}
                >
                  <Hash size={10} />
                  {t(`graphqlBuilder.tab.${tab}`)}
                </button>
              ))}
            </div>

            {activeTab === 'editor' && (
              <Card>
                <CardBody className="p-0">
                  <textarea
                    value={sdl}
                    onChange={(e) => setSdl(e.target.value)}
                    className="w-full h-[480px] p-4 text-xs font-mono bg-background rounded-lg resize-none border-0 focus:outline-none focus:ring-1 focus:ring-accent"
                    spellCheck={false}
                  />
                </CardBody>
              </Card>
            )}

            {activeTab === 'diff' && (
              <Card>
                <CardBody className="p-4">
                  <p className="text-xs text-muted-foreground text-center py-8">{t('graphqlBuilder.noDiff')}</p>
                </CardBody>
              </Card>
            )}

            {activeTab === 'explorer' && (
              <Card>
                <CardBody className="p-4">
                  <div className="space-y-1">
                    {(sdl.match(/^type\s+\w+[^{]*/gm) ?? []).map((line, i) => (
                      <div key={i} className="flex items-center gap-2 p-1.5 rounded hover:bg-muted/30">
                        <Hash size={12} className="text-info" />
                        <span className="text-xs font-mono">{line.trim()}</span>
                      </div>
                    ))}
                  </div>
                </CardBody>
              </Card>
            )}
          </div>
        </div>

        <div className="mt-4 p-3 rounded-lg border border-info/30 bg-info/5 text-xs text-muted-foreground">
          {t('graphqlBuilder.sdlBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}

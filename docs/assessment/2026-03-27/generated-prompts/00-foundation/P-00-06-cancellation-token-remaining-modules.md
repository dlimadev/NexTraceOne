# P-00-06 — Adicionar CancellationToken aos módulos restantes

## Modo de operação

Refactor

## Objetivo

Adicionar `CancellationToken` a todos os métodos assíncronos nos módulos restantes: Configuration (~19),
ChangeGovernance (~11), Governance (~9), Integrations (~9), ProductAnalytics (~8), AuditCompliance (~4)
e Knowledge (~2). Total de ~62 métodos. Este prompt fecha a série de CancellationToken, garantindo
cobertura total de 237 métodos assíncronos em toda a codebase do NexTraceOne.

## Problema atual

Após a aplicação dos prompts P-00-01 a P-00-05, restam 62 métodos `async Task` sem `CancellationToken`
distribuídos por 7 módulos menores. A distribuição por módulo é:

| Módulo              | Métodos afetados |
|---------------------|------------------|
| Configuration       | 19               |
| ChangeGovernance    | 11               |
| Governance          | 9                |
| Integrations        | 9                |
| ProductAnalytics    | 8                |
| AuditCompliance     | 4                |
| Knowledge           | 2                |

Embora individualmente cada módulo tenha poucos métodos, a correção conjunta é necessária
para atingir zero violações e permitir enforcement futuro via analyzer.

## Escopo permitido

- `src/modules/configuration/`
- `src/modules/changegovernance/`
- `src/modules/governance/`
- `src/modules/integrations/`
- `src/modules/productanalytics/`
- `src/modules/auditcompliance/`
- `src/modules/knowledge/`
- Application/**/*.cs e Infrastructure/**/*.cs de cada módulo

## Escopo proibido

- Módulos já tratados em P-00-01 a P-00-05 (identityaccess, catalog, notifications, aiknowledge, operationalintelligence)
- Ficheiros de migração existentes
- Configuração do host
- Lógica de negócio

## Ficheiros principais candidatos a alteração

### Configuration (~19 métodos)
- Repositórios e serviços de configuração da plataforma

### ChangeGovernance (~11 métodos)
- Handlers de change intelligence, promotion gates e workflow
- Repositórios de releases, blast radius e evidence packs

### Governance (~9 métodos)
- Repositórios de governance packs, waivers, teams e domains
- Serviços de compliance e risk

### Integrations (~9 métodos)
- Repositórios de connectors, ingestion sources e ingestion executions
- Serviços de integração com sistemas externos

### ProductAnalytics (~8 métodos)
- Repositórios de analytics events
- Serviços de tracking de produto

### AuditCompliance (~4 métodos)
- Repositórios de audit trail e compliance records
- Serviços de registo de evidências

### Knowledge (~2 métodos)
- Repositórios de documentos e notas operacionais

## Responsabilidades permitidas

- Adicionar `CancellationToken cancellationToken = default` a cada método async em todos os 7 módulos
- Propagar token para EF Core, HttpClient e qualquer operação I/O
- Atualizar interfaces e abstrações correspondentes
- Garantir coerência cross-module em interfaces partilhadas

## Responsabilidades proibidas

- Alterar lógica de negócio em qualquer módulo
- Refatorar estrutura ou nomes
- Adicionar funcionalidades novas

## Critérios de aceite

1. Zero métodos `async Task` sem `CancellationToken` em toda a codebase
2. Cada módulo compila individualmente sem erros
3. Solução completa compila (`dotnet build NexTraceOne.sln`)
4. Testes existentes compilam e passam
5. Pesquisa global confirma cobertura total

## Validações obrigatórias

- `dotnet build NexTraceOne.sln` — sem erros
- Para cada módulo: `grep -r "async Task" src/modules/<module>/ | grep -v CancellationToken` retorna zero
- Validação global: `grep -r "async Task" src/modules/ --include="*.cs" | grep -v CancellationToken | grep -v "\.g\.cs"` retorna zero (excluindo ficheiros gerados)

## Riscos e cuidados

- Interfaces cross-module (ex: IChangeIntelligenceModule, IGovernanceModule) podem precisar de atualização coordenada
- Módulos Integrations e Knowledge são novos (extraídos recentemente) — verificar que DbContexts não são afetados
- Configuration tem muitos métodos — pode haver serviços consumidos por múltiplos módulos

## Dependências

- Idealmente executar após P-00-01 a P-00-05 para evitar conflitos em interfaces partilhadas
- Pode ser executado em paralelo parcial se as interfaces partilhadas já estiverem atualizadas

## Próximos prompts sugeridos

- P-00-07 (Migração do módulo Knowledge)
- P-01-01 (Início da fase de correções críticas)

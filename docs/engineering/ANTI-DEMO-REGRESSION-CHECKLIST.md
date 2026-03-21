# Anti-Demo Regression Checklist

**Uso:** Executar antes de cada merge para produção e como parte de revisão de PR.  
**Script automático:** `scripts/quality/check-no-demo-artifacts.sh`  
**Política:** `docs/engineering/PHASE-0-PRODUCT-FREEZE-POLICY.md`

---

## Como Usar

1. Execute o script automatizado: `bash scripts/quality/check-no-demo-artifacts.sh`
2. Reveja os itens abaixo manualmente para garantias adicionais
3. Marque cada item como ✅ antes de aprovar o PR

---

## BLOCO A — Backend: Padrões Proibidos em Handlers

### A-01 — IsSimulated em handlers core
- [ ] Não existe nenhum `IsSimulated = true` **novo** em handler de feature core (fora da lista de dívida catalogada)
- [ ] Se `IsSimulated = true` existe, o frontend correspondente exibe `<DemoBanner />`
- [ ] Nenhum `GenerateSimulated*` ou `GenerateDemo*` método foi adicionado a handler de produção

### A-02 — Handlers vazios expostos
- [ ] Nenhum handler novo tem `// TODO: Implementar` como único conteúdo sem retornar erro explícito
- [ ] Nenhum endpoint novo registado aponta para handler sem implementação
- [ ] Se handler está incompleto, retorna `Result.Failure` com código `NotImplemented` ou `PreviewOnly`

### A-03 — Hardcodes operacionais
- [ ] Nenhum valor de domínio hardcoded em handler: environment names, team names, role names, auth modes
- [ ] Nenhum campo de DTO preenchido com string literal onde dado real deveria existir
- [ ] Nenhum `"Production"`, `"Staging"`, `"platform-squad"` hardcoded em handler de produção

### A-04 — Dados simulados em módulos core
- [ ] Módulo de Reliability não tem novos dados simulados
- [ ] Módulo de FinOps não tem novos dados simulados
- [ ] Módulo de AI Governance não tem novos handlers vazios
- [ ] Módulo de Automation Audit não tem novos `GenerateSimulated*`

---

## BLOCO B — Frontend: Padrões Proibidos em Páginas

### B-01 — Mock data local em páginas operacionais
- [ ] Não existe `const mockServices`, `const mockJobs`, `const mockQueues`, `const mockEvents` em páginas novas
- [ ] Não existe `const mockPersonas`, `const mockMilestones`, `const mockJourneys` em páginas novas
- [ ] Não existe `const mockDetails` ou `const mockData` em páginas operacionais fora de testes
- [ ] Arrays locais de dados demo não foram adicionados a nenhuma página fora de `__tests__/`

### B-02 — Devtools e debug
- [ ] `ReactQueryDevtools` não foi adicionado sem guard `import.meta.env.DEV`
- [ ] Nenhum painel de debug, console.log produtivo ou devtool foi adicionado sem guard de ambiente
- [ ] Nenhum `.env` com segredos reais foi commitado

### B-03 — Banners e sinalização
- [ ] Página nova que usa backend real não tem `DemoBanner` desnecessário
- [ ] Página que usa handler com `IsSimulated = true` tem `DemoBanner` correspondente
- [ ] Nenhum texto "Demo Data", "Sample Data", "Preview Data" hardcoded aparece em página core

### B-04 — Estados de página
- [ ] Páginas novas têm estado de loading implementado
- [ ] Páginas novas têm estado de erro implementado
- [ ] Páginas novas têm estado vazio implementado quando a lista pode ser vazia

### B-05 — i18n
- [ ] Nenhum texto em inglês ou português hardcoded visível ao utilizador sem `t()` wrapper
- [ ] Novas chaves i18n adicionadas a todos os arquivos de locale relevantes (en.json, pt-BR.json, pt-PT.json, es.json)

---

## BLOCO C — Segurança e Configuração

### C-01 — Credenciais e secrets
- [ ] Nenhuma credencial hardcoded (senhas, API keys, JWT secrets) fora de `appsettings.Development.json`
- [ ] `appsettings.json` não contém `Password=postgres` ou equivalente
- [ ] Nenhum secret foi commitado em qualquer arquivo rastreado por git

### C-02 — Configuração de produção
- [ ] `IntegrityCheck` está `true` em `appsettings.json` (não override de dev)
- [ ] `NEXTRACE_AUTO_MIGRATE=true` não está configurado em ambiente de produção
- [ ] Configurações novas têm valores seguros por default (fail-secure)

### C-03 — Autorização
- [ ] Nenhum endpoint novo permite acesso anónimo sem intenção explícita documentada
- [ ] Endpoints novos têm política de autorização ou atributo `[Authorize]` adequado

---

## BLOCO D — Infraestrutura e Banco de Dados

### D-01 — Migrations
- [ ] Migration nova criada com `dotnet ef migrations add` (não editada manualmente)
- [ ] Migration nova testada localmente
- [ ] Migration nova não destrói dados existentes sem aviso explícito

### D-02 — Containerização
- [ ] Se Dockerfile foi alterado, build da imagem testado localmente
- [ ] Nenhuma dependência nova foi adicionada sem atualizar o Dockerfile

---

## BLOCO E — Qualidade e Testes

### E-01 — Testes
- [ ] Testes existentes passam sem modificação (nenhum teste foi removido ou alterado para "passar")
- [ ] Testes de honestidade funcional (`SimulatedDataHonestyTests`, `GovernanceSimulatedDataTests`) foram actualizados se handler foi implementado como real
- [ ] Nenhum teste consolida comportamento fake como comportamento esperado correcto

### E-02 — Build
- [ ] `dotnet build` executa sem erros
- [ ] `tsc --noEmit` executa sem erros de TypeScript
- [ ] `npm run build` executa sem erros

---

## BLOCO F — Guardrail Automático

### F-01 — Script de verificação
- [ ] `bash scripts/quality/check-no-demo-artifacts.sh` executado sem erros críticos
- [ ] Se o script reportou achados, foram revisados e são falsos positivos justificados OU foram corrigidos

---

## Resultado

**PR aprovado para merge apenas quando todos os blocos aplicáveis estão marcados como ✅**

Para itens da dívida existente catalogada em `docs/audits/PHASE-0-DEMO-DEBT-INVENTORY.md`, o reviewer deve confirmar que **nenhum novo item foi adicionado** — apenas os itens já catalogados são tolerados temporariamente até serem resolvidos na fase adequada.

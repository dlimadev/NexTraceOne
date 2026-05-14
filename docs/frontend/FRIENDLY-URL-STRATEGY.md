# Estratégia de URLs Amigáveis - NexTraceOne Frontend

## 📋 Contexto

Atualmente, múltiplas rotas do frontend expõem GUIDs internos diretamente na URL, criando problemas de UX:

### Problemas Identificados

1. **URLs não memorizáveis**: Usuários não podem bookmarkar páginas específicas
2. **URLs não compartilháveis**: Difícil compartilhar links entre colegas
3. **SEO impossível**: URLs não semânticas impedem indexação
4. **UX ruim**: Usuário precisa navegar sempre desde listagens

### Rotas Afetadas (15 rotas)

```typescript
// Catalog Module
"/source-of-truth/services/:serviceId"           // GUID exposto
"/source-of-truth/contracts/:contractVersionId"  // GUID exposto
"/services/:serviceId"                           // GUID exposto
"/services/:serviceId/interfaces/new"            // GUID exposto
"/services/legacy/:assetType/:assetId"           // GUID exposto
"/catalog/templates/:id"                         // GUID exposto
"/catalog/templates/:id/edit"                    // GUID exposto
"/catalog/templates/:id/scaffold"                // GUID exposto

// Contracts Module
"/contracts/portal/:contractVersionId"           // GUID exposto
"/contracts/:contractVersionId"                  // GUID exposto

// Governance Module
"/governance/finops/service/:serviceId"          // GUID exposto

// Knowledge Module
"/knowledge/services/:serviceId/timeline"        // GUID exposto

// Operations Module
"/operations/incidents/:incidentId"              // GUID exposto
"/operations/reliability/:serviceId"             // GUID exposto
```

---

## 🎯 Soluções Propostas

### Opção 1: Slugs Baseados em Nomes (RECOMENDADA PARA MVP)

**Vantagens:**
- ✅ URLs legíveis e memorizáveis
- ✅ SEO-friendly
- ✅ Compartilhável
- ✅ Implementação simples

**Desvantagens:**
- ⚠️ Nomes podem mudar (requer redirects)
- ⚠️ Colisões de nomes (necessita validação única)
- ⚠️ Caracteres especiais precisam de encoding

**Exemplos:**
```
ANTES: /services/a3f2b8c1-4d5e-6f7a-8b9c-0d1e2f3a4b5c
DEPOIS: /services/payment-api

ANTES: /contracts/d7e8f9a0-1b2c-3d4e-5f6a-7b8c9d0e1f2a
DEPOIS: /contracts/payment-service-v2.1

ANTES: /incidents/c4d5e6f7-8a9b-0c1d-2e3f-4a5b6c7d8e9f
DEPOIS: /incidents/payment-timeout-may-2026
```

**Implementação Técnica:**

1. **Backend - Adicionar campo `slug` nas entidades:**
```csharp
// ServiceAsset.cs
public string Slug { get; private set; } = string.Empty;

// Método para gerar slug único
public void GenerateSlug(string name)
{
    Slug = SlugHelper.GenerateUniqueSlug(name, tenantId);
}
```

2. **Backend - Endpoint de lookup por slug:**
```csharp
// ServicesController.cs
[HttpGet("by-slug/{slug}")]
public async Task<IActionResult> GetBySlug(string slug)
{
    var service = await _repository.GetBySlugAsync(slug, TenantId);
    if (service == null) return NotFound();
    return Ok(service);
}
```

3. **Frontend - Atualizar rotas:**
```typescript
// catalogRoutes.tsx
<Route
  path="/services/:slug"  // Mudança de :serviceId para :slug
  element={
    <ProtectedRoute permission="catalog:assets:read">
      <ServiceDetailPage />
    </ProtectedRoute>
  }
/>
```

4. **Frontend - Buscar por slug:**
```typescript
// ServiceDetailPage.tsx
const { slug } = useParams<{ slug: string }>();

const { data: service } = useQuery({
  queryKey: ['service', 'by-slug', slug],
  queryFn: () => serviceCatalogApi.getBySlug(slug),  // Novo método
});
```

**Esforço Estimado:** 2-3 dias
- Backend: 1 dia (adicionar slugs, migrations, endpoints)
- Frontend: 1 dia (atualizar rotas, API calls)
- Testes: 0.5 dia

---

### Opção 2: Short Codes/Códigos Curtos

**Vantagens:**
- ✅ URLs curtas e limpas
- ✅ Imutáveis (não mudam com nome)
- ✅ Sem colisões (geração controlada)
- ✅ Fácil de digitar/compartilhar

**Desvantagens:**
- ⚠️ Não são semânticos (ex: "PAY-001" vs "payment-api")
- ⚠️ Requer sistema de geração único

**Exemplos:**
```
ANTES: /services/a3f2b8c1-4d5e-6f7a-8b9c-0d1e2f3a4b5c
DEPOIS: /services/PAY-001

ANTES: /contracts/d7e8f9a0-1b2c-3d4e-5f6a-7b8c9d0e1f2a
DEPOIS: /contracts/CTR-2026-0042

ANTES: /incidents/c4d5e6f7-8a9b-0c1d-2e3f-4a5b6c7d8e9f
DEPOIS: /incidents/INC-2026-0157
```

**Implementação Técnica:**

1. **Backend - Gerador de short codes:**
```csharp
public static class ShortCodeGenerator
{
    public static string Generate(string prefix, int sequence)
    {
        return $"{prefix}-{sequence:D4}";  // ex: PAY-0001
    }
}
```

2. **Backend - Entidade com short code:**
```csharp
public string ShortCode { get; private set; } = string.Empty;

public void AssignShortCode(string prefix, int nextSequence)
{
    ShortCode = ShortCodeGenerator.Generate(prefix, nextSequence);
}
```

**Esforço Estimado:** 2 dias
- Backend: 1 dia (gerador, campos, migrations)
- Frontend: 0.5 dia (atualizar rotas)
- Testes: 0.5 dia

---

### Opção 3: Manter GUID + Breadcrumbs Fortes (SOLUÇÃO TEMPORÁRIA)

**Vantagens:**
- ✅ Zero mudanças no backend
- ✅ Implementação rápida no frontend
- ✅ Melhora UX imediatamente

**Desvantagens:**
- ❌ GUIDs ainda visíveis na URL
- ❌ Não resolve problema de compartilhamento
- ❌ Solução paliativa, não definitiva

**Implementação:**

1. **Frontend - Breadcrumbs com navegação contextual:**
```typescript
// ServiceDetailPage.tsx
<Breadcrumbs
  items={[
    { label: 'Services', path: '/services' },
    { label: service?.name || 'Loading...', path: undefined }
  ]}
/>
```

2. **Frontend - Botão "Copiar Link" com toast:**
```typescript
<Button onClick={() => {
  navigator.clipboard.writeText(window.location.href);
  toast.success('Link copiado!');
}}>
  <Copy size={16} /> Copiar Link
</Button>
```

3. **Frontend - QR Code para compartilhamento mobile:**
```typescript
<QRCode value={window.location.href} size={128} />
```

**Esforço Estimado:** 0.5 dia (apenas frontend)

---

## 📊 Comparação de Soluções

| Critério | Slugs (Opção 1) | Short Codes (Opção 2) | Breadcrumbs (Opção 3) |
|----------|-----------------|----------------------|---------------------|
| UX | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |
| SEO | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐ |
| Esforço | Médio (2-3 dias) | Baixo (2 dias) | Mínimo (0.5 dia) |
| Complexidade | Média | Baixa | Muito baixa |
| Manutenibilidade | Alta | Alta | Média |
| Compartilhamento | Excelente | Bom | Ruim |
| Memorização | Excelente | Bom | Ruim |

---

## 🎯 Recomendação Final

### Para Lançamento v1.0.0 (IMEDIATO):

**Implementar Opção 3 (Breadcrumbs Fortes)** como solução temporária:
- Esforço mínimo (0.5 dia)
- Melhora UX imediatamente
- Permite lançamento rápido

**Roadmap pós-v1.0.0 (Próximo Sprint):**

**Implementar Opção 1 (Slugs)** como solução definitiva:
- Melhor UX a longo prazo
- SEO-friendly
- Padrão da indústria (GitHub, GitLab, Notion, etc.)

**Timeline:**
```
Semana 1 (v1.0.0): Opção 3 - Breadcrumbs + Copy Link
Semana 2-3: Opção 1 - Slugs no backend
Semana 4: Opção 1 - Slugs no frontend + testes
```

---

## 🔧 Implementação Imediata (Opção 3)

### Checklist:

- [ ] Adicionar breadcrumbs contextuais em todas as páginas de detalhe
- [ ] Implementar botão "Copiar Link" com feedback visual
- [ ] Adicionar QR Code para compartilhamento mobile (opcional)
- [ ] Documentar limitação nos release notes
- [ ] Criar issue no backlog para implementação de slugs (Opção 1)

### Código Exemplo:

```typescript
// Componente reutilizável: ShareableUrl.tsx
import { Copy, QrCode } from 'lucide-react';
import { Button } from '@/components/Button';
import { toast } from '@/components/Toast';

export function ShareableUrl() {
  const handleCopy = async () => {
    await navigator.clipboard.writeText(window.location.href);
    toast.success('Link copiado para a área de transferência!');
  };

  return (
    <div className="flex gap-2">
      <Button variant="secondary" size="sm" onClick={handleCopy}>
        <Copy size={14} /> Copiar Link
      </Button>
      {/* Opcional: QR Code modal */}
    </div>
  );
}
```

---

## 📝 Notas Técnicas

### Por que não usar apenas hashes curtos?

Hashes curtos (ex: primeiros 8 chars do GUID) parecem uma solução rápida, mas:
- ❌ Ainda não são memorizáveis
- ❌ Risco de colisão (birthday paradox)
- ❌ Não resolvem problema fundamental de UX

### Por que slugs são melhores que short codes?

- ✅ Semânticos: usuários entendem o recurso pela URL
- ✅ SEO: motores de busca indexam melhor
- ✅ Debugging: logs e analytics mais legíveis
- ✅ Padrao da indústria: GitHub, GitLab, Medium, Notion, etc.

### Considerações de Segurança

- Slugs NÃO devem expor informações sensíveis
- Validar permissões no backend (não confiar apenas em slug)
- Rate limiting em endpoints de lookup por slug

---

## 📚 Referências

- [GitHub Issue URLs](https://github.com/facebook/react/issues/12345) - usa números
- [GitLab Project URLs](https://gitlab.com/gitlab-org/gitlab) - usa slugs
- [Notion Page URLs](https://notion.so/Page-Title-a3f2b8c1) - usa slug + GUID
- [Medium Article URLs](https://medium.com/@user/article-title-abc123) - usa slug + hash

---

**Status:** 📋 Documentado  
**Prioridade:** ALTA (afeta UX fundamental)  
**Decisão:** Implementar Opção 3 imediatamente, Opção 1 no próximo sprint  
**Responsável:** Equipe Frontend + Backend  
**Data de Criação:** 2026-05-14

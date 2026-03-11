# Regras Estritas — NexTraceOne

## Idioma

- Código, logs, nomes de classe/método/variável/enum/DTO/endpoint: INGLÊS
- Comentários XML (`<summary>`, `<param>`, `<returns>`): PORTUGUÊS
- Comentários inline explicando lógica de negócio: PORTUGUÊS

## Proibições Absolutas

- NUNCA usar `DateTime.Now` ou `DateTimeOffset.UtcNow` diretamente — usar `IDateTimeProvider`
- NUNCA acessar DbContext de outro módulo
- NUNCA publicar Integration Events sem Outbox Pattern
- NUNCA usar exceções para controle de fluxo — usar `Result<T>`
- NUNCA usar `string` para IDs de entidade — usar StronglyTypedIds
- NUNCA criar constructor público em Aggregate Roots — usar factory methods
- NUNCA colocar lógica de negócio em endpoints
- NUNCA referenciar projetos de outro módulo diretamente

## Obrigações

- SEMPRE `sealed` em classes que não serão herdadas
- SEMPRE `CancellationToken` em toda operação assíncrona
- SEMPRE documentação XML em português em toda classe e método público
- SEMPRE comentários inline explicando o PORQUÊ, não o quê
- SEMPRE Guard clauses no início de métodos (fail fast)
- SEMPRE setters privados em propriedades de Aggregate Roots
- SEMPRE testes unitários junto com a implementação

## Ordem de Implementação

Dentro de qualquer módulo, seguir SEMPRE esta ordem:
1. Domain (Entities, Value Objects, Events, Errors)
2. Application (Features: Command + Handler + Validator)
3. Infrastructure (DbContext, Configurations, Repositories)
4. API (Endpoints)
5. Tests

Nunca avançar para a próxima camada sem completar a anterior.

# Análise de Vulnerabilidades — WrenchBox API

**Data:** 30/06/2026  
**Escopo:** API REST (.NET 8), camadas Domain, Application, Infrastructure e configuração de deploy  
**Metodologia:** Revisão estática de código e configuração (sem testes de penetração)

---

## Resumo executivo

O WrenchBox adota boas práticas para um MVP de demonstração: autenticação JWT, senhas com BCrypt, validação com FluentValidation, queries parametrizadas via EF Core e tratamento centralizado de erros. **Não está pronto para produção** sem mitigar riscos críticos relacionados a segredos expostos, credenciais padrão, transporte sem TLS e superfície de ataque ampliada (Swagger sempre ativo, token de rastreamento sem ciclo de vida).

| Severidade | Quantidade |
|------------|------------|
| Crítica    | 2          |
| Alta       | 7          |
| Média      | 6          |
| Baixa      | 7          |
| Informativa| 4          |

**Postura geral:** adequada para desenvolvimento e workshops; requer hardening antes de exposição pública.

---

## Controles de segurança existentes

| Área | Implementação |
|------|---------------|
| Autenticação | JWT Bearer com validação de issuer, audience, lifetime e chave (`Program.cs`) |
| Autorização | Endpoints administrativos protegidos com `[Authorize(Roles = "Admin")]` |
| Senhas | Hash BCrypt em `AuthServices.cs`; mensagem genérica no login |
| Validação | Pipeline MediatR + FluentValidation; value objects para CPF/CNPJ e placa |
| Banco de dados | EF Core (SQL parametrizado); índices únicos e constraints de tamanho |
| Erros | Middleware retorna RFC 7807; mensagem genérica em erros 500 |
| Rastreamento | DTO público omite PII do cliente (documento, e-mail, telefone) |
| Testes | Integração verifica 401 em endpoints admin sem token |

---

## Vulnerabilidades identificadas

### Críticas

#### 1. Segredos hardcoded no repositório

**Arquivos:** `src/WrenchBox.Api/appsettings.json`, `appsettings.Development.json`, `docker-compose.yml`

Chave JWT e senha do PostgreSQL estão em texto plano no código versionado. Qualquer pessoa com acesso ao repositório pode forjar tokens admin ou acessar o banco.

**Recomendação:** mover segredos para variáveis de ambiente ou gerenciador de secrets; rotacionar chaves; nunca commitar credenciais reais.

#### 2. Administrador padrão criado automaticamente

**Arquivo:** `src/WrenchBox.Infrastructure/Persistence/DatabaseSeeder.cs`

Na primeira execução, cria `admin@wrenchbox.local` / `Admin@123`. Em ambiente exposto à internet, isso permite acesso imediato com credenciais conhecidas.

**Recomendação:** desabilitar seed de admin fora de Development; exigir troca de senha no primeiro login.

---

### Altas

| # | Achado | Detalhe |
|---|--------|---------|
| 3 | Swagger em produção (mitigado) | Swagger só sobe em Development ou com `Swagger:Enabled=true` (demo K8s) |
| 4 | Docker em ambiente Development | `docker-compose.yml` — `ASPNETCORE_ENVIRONMENT: Development` desativa defaults de produção |
| 5 | Sem HTTPS/TLS | API roda apenas em HTTP (porta 8080); JWT e credenciais trafegam em cleartext |
| 6 | Token de rastreamento no e-mail | Links de aprovação/recusa carregam o token na query string (necessário para o clique no MailHog) |
| 7 | Aprovação anônima de orçamento | Header `X-Tracking-Token` permite aprovar orçamento e baixar estoque sem autenticação adicional; token não expira |
| 8 | Sem rate limiting | Login e endpoints de rastreamento vulneráveis a brute force e abuso |
| 9 | JWT simétrico sem revogação | Comprometimento da chave = forjar qualquer token; sem blacklist |

---

### Médias

| # | Achado | Detalhe |
|---|--------|---------|
| 10 | `AllowedHosts: "*"` | Desabilita validação de host header — risco em proxies reversos |
| 11 | Validação incompleta | Faltam validators para update de peças/serviços, ajuste de estoque, transições de OS e paginação |
| 12 | `pageSize` ilimitado | Cliente pode solicitar páginas enormes — risco de DoS por consumo de memória/DB |
| 13 | Race condition na aprovação | `GetByTrackingTokenForUpdateAsync` não usa lock de linha nem transação explícita |
| 14 | PII completa na API admin | `CustomerDto` retorna CPF/CNPJ, e-mail e telefone — preocupação LGPD |
| 15 | Expiração JWT inconsistente | Resposta do login usa 1h fixo; token usa `ExpiryMinutes` da config |

---

### Baixas

- Ausência de security headers (`X-Content-Type-Options`, CSP, etc.)
- Sem política CORS definida (necessária antes de SPA no browser)
- Mensagens de `DomainException` expostas ao cliente (information disclosure)
- Sem lockout de conta ou MFA
- Sem trilha de auditoria para mutações administrativas
- Papel único (`Admin`) — sem RBAC granular
- Coluna `PasswordHash` como `text` sem limite explícito no EF

---

### Informativas

- Sem endpoint de registro de usuários (reduz superfície de ataque)
- `ExecuteSqlRaw` no seeder usa SQL estático (não é input do usuário)
- Credenciais de dev documentadas no `README.md` — risco se copiadas para runbooks de produção
- `.env` está no `.gitignore`, mas segredos permanecem em `appsettings.json`

---

## Análise por camada

### API (`WrenchBox.Api`)

Pontos fortes: pipeline JWT, autorização por role, middleware de exceções.  
Gaps: sem HTTPS, rate limit, headers de segurança; Swagger exposto; tracking autenticado apenas por header.

### Application (`WrenchBox.Application`)

Pontos fortes: FluentValidation, exceções tipadas, validadores de domínio.  
Gaps: cobertura parcial de validators; paginação sem limites.

### Infrastructure (`WrenchBox.Infrastructure`)

Pontos fortes: BCrypt, migrations, constraints únicas.  
Gaps: segredos em config; token logado; locking insuficiente em operações críticas de estoque.

### Domain (`WrenchBox.Domain`)

Pontos fortes: encapsulamento, regras no domínio, validação de documento/placa.  
Gaps: PII armazenada em texto; tokens de rastreamento permanentes; sem criptografia em repouso.

---

## Recomendações prioritárias

1. **Remover segredos do repositório** e usar secrets por ambiente.
2. **Desabilitar Swagger** fora de Development; forçar `Production` no Docker de deploy.
3. **Terminar TLS** (proxy reverso ou Kestrel HTTPS) e habilitar HSTS.
4. **Eliminar admin padrão** em ambientes não-dev.
5. **Parar de logar tokens** de rastreamento; adicionar expiração e uso único após aprovação.
6. **Implementar rate limiting** em login e tracking.
7. **Completar validators** e limitar `pageSize` (ex.: máximo 100).
8. **Usar transações com row locking** na aprovação de orçamento e baixa de estoque.
9. **Mascarar CPF/CNPJ** nas respostas quando o valor completo não for necessário.

---

## Stack relevante para segurança

| Componente | Versão/tecnologia |
|------------|-------------------|
| Runtime | .NET 8 / ASP.NET Core |
| Auth | JWT Bearer 8.0.11, HMAC-SHA256 |
| Senhas | BCrypt.Net-Next 4.2.0 |
| ORM | EF Core 8.0.11 + PostgreSQL 16 |
| Validação | FluentValidation 12.1.1 |
| Documentação | Swashbuckle 6.6.2 |

---

## Limitações desta análise

Esta revisão cobre apenas análise estática de código e configuração. Não inclui testes de penetração, varredura de dependências (SCA), análise dinâmica nem avaliação de infraestrutura de rede. Recomenda-se complementar com scan automatizado (ex.: `dotnet list package --vulnerable`) antes de deploy em produção.

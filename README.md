# ExternalDataCollector (.NET 8) — Worker (RPA) + Web API + Docker Compose

## Visão geral
Este repositório implementa um ecossistema de captura automática de dados externos e exposição via endpoints REST:

1) **Worker Service (RPA)**
- Executa em background.
- A cada intervalo configurável, coleta cotações de moedas do feed público do ECB:
  `https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml`
- Aplica resiliência (retry/backoff) para falhas transitórias de rede e trata mudanças simples no layout do XML com fallback de parsing.
- Persiste resultados em **SQLite**.

2) **Web API (.NET 8 Minimal API)**
- Expõe os dados coletados via endpoints REST.
- Não há regra de negócio nos endpoints: eles chamam UseCases (Application).

Ambos rodam em containers isolados e compartilham o SQLite por um **volume Docker**.

---

## Como rodar (Docker Compose)
Pré-requisitos:
- Docker e Docker Compose

Subir tudo:
```bash
docker compose up --build
```

A API ficará disponível em:
- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`

---

## Endpoints
### GET /api/rates/latest
Query params:
- `quote` (opcional): filtra por moeda (ex: USD)
- `take` (opcional): quantidade (máx 500)

Exemplo:
`GET http://localhost:8080/api/rates/latest?quote=USD&take=10`

### GET /api/rates
Query params:
- `date` (obrigatório): `yyyy-MM-dd`
- `quote` (opcional)
- `take` (opcional)

Exemplo:
`GET http://localhost:8080/api/rates?date=2026-03-05&quote=BRL`

---

## Decisões arquiteturais
- Separação em camadas:
  - **Domain**: entidades e invariantes.
  - **Application**: contratos (abstractions) e casos de uso (use cases).
  - **Infrastructure**: EF Core (SQLite), repositório e implementação do scraper.
  - **API/Worker**: entrypoints; apenas composição/DI + handlers.
- **SOLID / DI**:
  - Scraper e Repositório via interfaces (`IRateScraper`, `IExchangeRateRepository`).
  - UseCases isolam comportamento (`UpsertRates`, `GetLatestRates`).
- Persistência:
  - SQLite compartilhado via volume Docker.
  - `EnsureCreated` para simplificar a execução do teste técnico (sem migrations).
- Resiliência:
  - `HttpClient` com **Polly**: retry + exponential backoff + jitter.
  - Parsing com estratégia padrão + fallback para reduzir impacto de mudanças leves no layout.

---

## O que eu melhoraria com mais tempo
- Migrations EF Core + pipeline de CI (GitHub Actions) rodando build/test.
- Upsert otimizado (bulk / `INSERT ON CONFLICT`) ao invés de loop por item.
- Observabilidade: métricas (Prometheus), tracing (OpenTelemetry), logs estruturados.
- Estratégia mais avançada de detecção de mudanças do layout:
  - contrato de schema (XSD), validação, alerts.
- Cache na API e paginação mais rica.
- Testes:
  - Unit tests dos UseCases
  - Integration tests para API e repositório (SQLite in-memory)

---

## Sugestão de commits (histórico claro)
1. `chore: initial solution structure`
2. `feat: domain + application use cases`
3. `feat: infrastructure sqlite + repository`
4. `feat: worker rpa with retry and scraper`
5. `feat: api endpoints + swagger`
6. `chore: dockerfiles + compose + docs`


---

## Rodando sem Docker (opcional)
Pré-requisitos:
- .NET SDK 8+

Build:
```bash
dotnet build ExternalDataCollector.sln
```

Executar API:
```bash
dotnet run --project src/ExternalDataCollector.Api
```

Executar Worker:
```bash
dotnet run --project src/ExternalDataCollector.Worker
```

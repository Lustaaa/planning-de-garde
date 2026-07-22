# Documentation technique — PlanningDeGarde

Cette documentation est **auto-générée à partir du code** par [DocFX](https://dotnet.github.io/docfx/) :

- **Référence API (code)** — extraite des commentaires XML `///` de tous les projets `src/`
  (Domain, Application, adaptateurs de droite InMemory / Mongo / Smtp / Securite, SignalR, Api, Web,
  Infrastructure). Voir l'onglet **Référence API (code)**.
- **OpenAPI de l'Api HTTP** — le contrat REST est exposé à l'exécution par l'hôte `PlanningDeGarde.Api`
  (endpoint `/openapi/v1.json`) et exploré via l'UI **Scalar** (`/scalar`). Voir la section
  « OpenAPI » de `docs/documentation-technique.md` pour l'intégrer au site DocFX.

## Architecture (rappel)

Application Clean / hexagonale, DDD + CQRS. Le front Blazor WASM n'appelle jamais le domaine en
direct : tout passe par l'API. **Écriture** = canal requête/réponse ; **diffusion** = SignalR
lecture seule. Voir `CLAUDE.md` et `docs/specs/` pour la source de vérité produit & architecture.

## Générer / servir cette doc

```bash
dotnet tool restore
dotnet docfx docfx/docfx.json --serve
```

Détails et intégration OpenAPI : `docs/documentation-technique.md`.

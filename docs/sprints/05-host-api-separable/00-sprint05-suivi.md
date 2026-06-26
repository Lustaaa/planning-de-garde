# Suivi — Sprint 05 · host-api-separable

> **Cadrage scaffolding.** Décision PO : **nouveau projet API détaché + Scalar**. On crée
> `src/PlanningDeGarde.Api` (SDK Web, `public partial class ApiProgram`) qui référence
> **Application + Infrastructure** mais **PAS** `PlanningDeGarde.Web` (preuve qu'il démarre
> seul). Y sont déplacés/portés : le canal d'écriture `MapperCanalEcriture`, l'**OpenAPI
> natif .NET** (`AddOpenApi`/`MapOpenApi`), l'**UI d'exploration interactive Scalar**
> (`Scalar.AspNetCore`) et le **CORS** autorisant l'origine du front. Acceptation des
> scénarios **backend** = **test d'intégration de bout en bout** via
> `WebApplicationFactory<ApiProgram>` (nouveau projet `tests/PlanningDeGarde.Api.Tests`,
> pkg `Microsoft.AspNetCore.Mvc.Testing`) : `POST` commande → handler **inchangé** → **store
> réel** singleton → projection **réelle** `GrilleAgendaQuery`. **Aucune doublure sur le
> chemin observé** (anti « vert qui ment » / early-green).
>
> **Niveau = intégration, pas unit.** Les règles métier (handler refuse lieu absent / exige
> un responsable, projection colore/positionne) sont **déjà vertes** au niveau
> Application/Domain et **ré-observées de bout en bout** au sprint 04 sur l'hôte Web. Ce
> sprint **ne ré-ouvre aucune règle de gestion** : il **détache l'hôte d'API** et **migre le
> front en WASM**. Les drivers réels sont donc l'**existence et le câblage du nouvel hôte
> API** (démarrable seul, CORS, exploration) ; les comportements métier ré-assertés en bout
> de chaîne sont des **caractérisations** (filet de non-régression du câblage détaché).
>
> **Non-référence du front = test d'architecture.** La preuve que l'hôte API démarre seul est
> un **test d'architecture** qui inspecte les `ProjectReference` (ou les assemblies chargées)
> de `PlanningDeGarde.Api` : `PlanningDeGarde.Web` **ne doit pas** y figurer. C'est le driver
> structurel du Sc.1.
>
> **Axe backend vs IHM (cf. routage).** Scénarios **backend → `tdd-auto`** (frontière HTTP de
> l'API détachée, observable de bout en bout) : **Sc.1, Sc.3, Sc.4, Sc.5**. Scénarios **🖥️ IHM
> / runtime → `ihm-builder`** (le comportement vit dans le navigateur : front WASM réel, HTTP
> **distant** vers l'API, render mode, message d'échec à l'écran) : **Sc.2, Sc.6**. Leur
> acceptation est un **test de NIVEAU RUNTIME sur l'app réellement câblée** (front WASM + API
> distante, DI réelle), **JAMAIS** un bUnit composant à doublures (un bUnit « ment au vert »
> sur un render mode manquant / un échec réseau réel / une URL d'API mal configurée).
>
> **Invariants NON-codants** (garde-fous structure, jamais pilotés en Gherkin) : convention
> code-behind systématique ; séparation canal écriture (requête/réponse) vs diffusion
> (SignalR lecture seule) en tant que **câblage** ; localisation d'hôte du hub SignalR après
> WASM (point de câblage). L'UI d'exploration Scalar est un **confort d'outillage** : Sc.3 et
> Sc.4 la pilotent par leur **servabilité HTTP** (la page/route répond et liste les endpoints),
> pas par une règle métier.
>
> **Hors périmètre.** **PWA** (cache + file d'écritures rejouée hors-ligne) **reportée** : ce
> sprint se borne à l'**échec clair** (Sc.6 : message + saisie non appliquée), **sans file ni
> rejeu**. Commande `définir-transfert` : exposée par le canal mais **sans observable** dans
> `GrilleAgendaQuery` → non scénarisée. La notification temps réel déclenchée par une écriture
> aboutie se vérifie par **Spy** sur `INotificateurPlanning`, jamais par le canal de diffusion.

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Le back démarre seul : l'API détachée enregistre une affectation sans le front](01-back-demarre-seul-affectation.md) | `@nominal` | ✅ GREEN | 3/3 | ✅ GREEN |
| 2 | [Le front WASM consomme l'API distante : un slot posé apparaît dans sa case](02-front-wasm-slot-via-api-distante.md) | `@nominal 🖥️ IHM` | ✅ GREEN | 2/2 | ✅ GREEN |
| 3 | [L'UI d'exploration interactive liste les endpoints du canal d'écriture](03-ui-exploration-liste-endpoints.md) | `@nominal` | ✅ GREEN | 2/2 | ✅ GREEN |
| 4 | [L'hôte d'API démarre headless et sert description + exploration](04-hote-headless-description-exploration.md) | `@limite` | ✅ GREEN | 2/2 | ✅ GREEN |
| 5 | [Le front sur une origine distincte est autorisé par le CORS de l'API](05-cors-origine-front-autorisee.md) | `@limite` | ✅ GREEN | 2/2 | ✅ GREEN |
| 6 | [API distante injoignable : la saisie est refusée et n'est pas appliquée](06-api-injoignable-saisie-refusee.md) | `@erreur 🖥️ IHM` | ✅ GREEN | 2/2 | ✅ GREEN |

**Total** : 6 scénarios — **4 backend** (`tdd-auto`, acceptation **intégration**
`WebApplicationFactory<ApiProgram>`) · **2 IHM/runtime** (`ihm-builder`, acceptation
**E2E/runtime** sur front WASM + API distante). **9 tests d'intégration** backend ; le détail
RED→GREEN des scénarios IHM (`.razor` WASM, config URL d'API, message d'échec) est piloté par
`ihm-builder`.

> **Scaffolding requis (à créer par `tdd-auto` / `ihm-builder`, hors périmètre de l'analyse)** :
> - **Nouveau projet** `src/PlanningDeGarde.Api` (SDK Web, `public partial class ApiProgram`)
>   référençant Application + Infrastructure, **jamais** `PlanningDeGarde.Web` ; porte
>   `MapperCanalEcriture`, OpenAPI, **Scalar** (`Scalar.AspNetCore`) et **CORS**.
> - **Nouveau projet de test** `tests/PlanningDeGarde.Api.Tests` (`Microsoft.AspNetCore.Mvc.Testing`,
>   `WebApplicationFactory<ApiProgram>`, environnement « Testing » → store vierge, comme l'hôte Web).
> - **Migration front WASM** : `PlanningDeGarde.Web` (ou nouveau projet `.Client`) passe Blazor
>   Server → WebAssembly ; `HttpClient.BaseAddress` = **URL d'API configurable** (config, plus
>   `nav.BaseUri`) ; hub SignalR consommé côté navigateur (point de câblage). Piloté par `ihm-builder`.
> - Déplacement de `MapperCanalEcriture` (et de l'OpenAPI) de `PlanningDeGarde.Web/Program.cs`
>   vers l'hôte API ; le canal cesse d'être servi par l'hôte front.

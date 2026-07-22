# Documentation technique auto-générée (DocFX)

La **documentation technique du code** est générée à la demande (et reproductible en CI) à partir
de deux sources déjà présentes dans le dépôt :

1. **Commentaires XML `///`** de tout le code `src/` → référence API (types, membres, `<summary>`,
   `<param>`, `<returns>`, `<see cref>`).
2. **OpenAPI** de l'hôte `PlanningDeGarde.Api` → contrat REST (voir section dédiée plus bas).

Outil retenu : **[DocFX](https://dotnet.github.io/docfx/)** (standard .NET pour la doc issue des XML
docs). Installé en **tool local** (`.config/dotnet-tools.json`, version épinglée) → génération
identique sur toute machine / en CI, sans installation globale.

## Ce qui est activé côté build

`src/Directory.Build.props` porte `<GenerateDocumentationFile>true</GenerateDocumentationFile>` pour
**tous les projets `src/`** (et eux seuls — les projets `tests/` ne remontent pas jusqu'à ce fichier).
Chaque assembly émet donc son `.xml` de doc à côté du `.dll`. `CS1591` (« membre public sans `///` »)
est masqué via `<NoWarn>$(NoWarn);CS1591</NoWarn>` pour garder une sortie de build propre — le
comportement d'exécution est strictement inchangé, seule l'émission de la doc XML est ajoutée.

## Générer la doc

```bash
# 1. Restaurer le tool DocFX épinglé (une fois par clone / en CI)
dotnet tool restore

# 2. Générer le site statique (métadonnées depuis les XML docs + build HTML)
dotnet docfx docfx/docfx.json

# 3. (option) générer ET servir sur http://localhost:8080
dotnet docfx docfx/docfx.json --serve
```

Sortie : `docfx/_site/` (HTML statique) + `docfx/api/` (métadonnées `.yml` intermédiaires). Ces deux
dossiers sont **git-ignorés** : on ne versionne que la CONFIG (`docfx/docfx.json`, `docfx/toc.yml`,
`docfx/index.md`, `.config/dotnet-tools.json`, `src/Directory.Build.props`).

## Périmètre de la référence API

Couverte : `Domain`, `Application`, adaptateurs de droite (`InMemory`, `Mongo`, `Smtp`, `Securite`),
`SignalR`, `Api`, `Infrastructure`.

**Exclu : `PlanningDeGarde.Web`** (Blazor WASM). DocFX extrait ses métadonnées via Roslyn **sans
exécuter le générateur de source Razor** ; les partials `.razor.cs` ne trouvent alors pas leur base
`ComponentBase` (erreurs `CS0115`/`CS0234` à l'extraction). Le projet est donc écarté de l'extraction
(`exclude` dans `docfx/docfx.json`). Ses commentaires `///` restent émis dans son `.xml` (via
`Directory.Build.props`) et pourront être intégrés plus tard si DocFX gère le Razor SG, ou via un
document conceptuel dédié.

## Intégration de l'OpenAPI de l'Api

Le contrat REST est exposé **à l'exécution** par l'hôte `PlanningDeGarde.Api` :

- document OpenAPI : `GET /openapi/v1.json` (via `Microsoft.AspNetCore.OpenApi`) ;
- UI interactive : `GET /scalar` (Scalar).

DocFX sait publier un swagger/OpenAPI comme page du site, mais il lui faut un **fichier `.json`
statique** ; or ici le document est produit dynamiquement (pas de fichier au build). Pour l'agréger au
site DocFX :

1. Produire le fichier au build en ajoutant le package `Microsoft.Extensions.ApiDescription.Server`
   au projet `Api` (émet `PlanningDeGarde.Api.json` dans `obj/`), **ou** capturer le document une fois
   l'API lancée :

   ```bash
   dotnet run --project src/PlanningDeGarde.Api &
   curl -s http://localhost:5xxx/openapi/v1.json > docfx/openapi/PlanningDeGarde.Api.json
   ```

2. Référencer ce `.json` dans `docfx/docfx.json` (section `build.content`) avec une entrée de `toc.yml`
   pointant dessus — DocFX rend alors la référence REST à côté de la référence code.

Tant que cette étape n'est pas câblée, l'exploration du contrat REST se fait via **Scalar** (`/scalar`)
sur l'hôte Api lancé, qui reste la voie de référence pour le canal HTTP.

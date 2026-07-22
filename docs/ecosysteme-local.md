# Écosystème local — planning-de-garde

Cette page liste **tout ce qui démarre** quand on lance l'application en local, avec les URLs/ports
et le rôle de chaque brique. Objectif : savoir, d'un coup d'œil, ce qui tourne et où — sans aide.

Les ports ci-dessous sont ceux réellement publiés par `docker-compose.yml` (racine du dépôt).

## Services

| Service | URL / port | Rôle |
|---|---|---|
| **Front (Blazor WASM)** | http://localhost:5292 | L'app de planning. Front WASM qui s'exécute dans le navigateur et consomme l'API comme une **API distante**. |
| **API détachée (Kestrel)** | http://localhost:5180 | Hôte d'API : canal **écriture/lecture** (requête/réponse) + **hub SignalR** (diffusion temps réel, lecture seule). |
| ↳ Explorateur Scalar | http://localhost:5180/scalar/v1 | UI interactive d'exploration du contrat REST. |
| ↳ Document OpenAPI | http://localhost:5180/openapi/v1.json | Contrat OpenAPI brut (JSON) généré à l'exécution. |
| **Mongo** | localhost:27017 | Store de la **config foyer durable** (acteurs : noms, couleurs…) — **seule donnée persistée** (slots/périodes/transferts restent InMemory). |
| **Mongo Express** | http://localhost:8081 | IHM web d'inspection des collections Mongo. |
| **smtp4dev (SMTP)** | localhost:2525 | Serveur SMTP de dev : capte les **vrais mails** (reset mot de passe) émis par l'adaptateur `EnvoiMailSmtp`. |
| ↳ smtp4dev (UI) | http://localhost:5081 | IHM web pour relire les mails captés. |
| **Doc technique (DocFX)** | http://localhost:8089 | Référence API générée depuis les commentaires `///` du code (voir `docs/documentation-technique.md`). |

## Comment lancer

### Tout l'écosystème (Docker)

```bash
docker compose up            # build (one-shot) puis mongo, api, web, mongo-express, smtp4dev, docfx
```

Le service `build` restaure et compile **une fois** (Api puis Web, séquentiel), puis `api`/`web`
démarrent en `--no-build` sur ses artefacts ; `docfx` génère la doc depuis ces mêmes binaires.

### Mongo seul (pour un run hôte)

```bash
docker compose up -d mongo   # ne démarre que le store, pour lancer Api/Web sur la machine via le skill `run`
```

### Alternative : skill `run` (hôtes Windows)

`.claude/skills/run/scripts/run.ps1` démarre `Api` (détaché) puis le front `Web` **directement sur la
machine Windows** (pas de conteneur pour l'app). Les ports hôte sont **identiques** à ceux du compose
(5180 / 5292), donc `Api:BaseUrl` et l'origine CORS restent alignés.

> **Prérequis** : Docker ne doit **pas déjà tenir** les ports 5180 / 5292 (sinon collision). En pratique,
> soit on tourne « tout Docker » (`docker compose up`), soit on tourne « Mongo Docker + Api/Web hôte »
> (`docker compose up -d mongo` + skill `run`), pas les deux à la fois pour l'app.

### Arrêter

```bash
docker compose down          # arrête et supprime les conteneurs
docker compose down -v       # + vide les volumes (mongo-data, caches build/nuget, sorties docfx)
```

## Piège du build servi périmé

Les conteneurs `api` et `web` tournent en **`--no-build`** sur les artefacts produits par le service
`build` (volume `build-artifacts`). Un simple `up` **ne recompile pas** : après un changement de code,
ils continueraient à servir l'**ancien** binaire.

Pour servir du **code frais** :

```bash
docker compose up build --force-recreate          # 1. recompile les artefacts
docker compose up -d --force-recreate api web      # 2. redémarre api/web sur les nouveaux binaires
```

La doc `docfx` s'aligne de la même façon : elle lit les DLL+XML du volume `build-artifacts`, donc un
`build --force-recreate` suivi d'un `up -d --force-recreate docfx` régénère la référence à jour.

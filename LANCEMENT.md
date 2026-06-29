# Lancer planning-de-garde

Deux façons de démarrer l'app, **hors pipeline de sprint**. Mêmes ports dans les deux cas, donc
la même URL pour l'utilisateur.

| Service | URL | Rôle |
|---|---|---|
| Front Blazor WASM | http://localhost:5292 | l'app, dans le navigateur |
| Hôte d'API | http://localhost:5180 | canal écriture + lecture + hub SignalR |
| Exploration API (Scalar) | http://localhost:5180/scalar/v1 | UI interactive de l'API |
| Mongo | mongodb://localhost:27017 | config foyer durable (acteurs) |
| mongo-express | http://localhost:8081 | IHM d'inspection des collections Mongo |

---

## 1. En local (machine de dev Windows)

Le plus rapide pour développer. Démarre l'API détachée puis le front WASM (hot reload possible).

```powershell
# via le skill Claude Code
/run

# ou directement le script
pwsh .claude/skills/run/scripts/run.ps1          # build séquentiel + lancement
pwsh .claude/skills/run/scripts/run.ps1 -Watch   # front en hot reload
pwsh .claude/skills/run/scripts/run.ps1 -NoBuild  # saute le build (sortie déjà compilée)
```

Mongo (config foyer **durable**) est démarré automatiquement par le script via Docker s'il est
disponible ; sinon repli **InMemory volatile** + avertissement (l'app reste lançable). Pour Mongo
seul sans le reste :

```powershell
docker compose up -d mongo
```

`Ctrl+C` arrête le front ; l'API d'arrière-plan est coupée à la sortie du script.

---

## 2. En conteneurs (tout dockerisé)

Aucune installation .NET requise sur l'hôte — seulement Docker. Source montée dans des images
`dotnet/sdk` (pas de Dockerfile à maintenir).

```bash
docker compose up            # build (une fois) puis api + web ; logs au premier plan
docker compose up -d         # en arrière-plan
docker compose down          # arrêt (ajouter -v pour vider mongo + caches build/nuget)
```

Séquence : le service **`build`** restaure et compile Api **puis** Web séquentiellement (anti-race
CS2012), puis **`api`** et **`web`** démarrent en `--no-build`. Le premier `up` est lent (restore
NuGet) ; les suivants réutilisent les caches (`nuget-cache`, `build-artifacts`).

Particularités :
- **Isolation binaire** : `--artifacts-path /artifacts` (volume) → les `bin/obj` Linux du conteneur
  ne polluent jamais ceux de l'hôte Windows, et inversement. Source montée en **lecture seule**.
- **NuGet** : les conteneurs utilisent `nuget.docker.config` (nuget.org seul), pas `nuget.config`
  (qui pointe une source offline Windows inexistante en Linux).
- **Mongo** : l'API y accède via le réseau compose (`mongodb://mongo:27017`), config foyer durable.
- **CORS / BaseUrl** : ports host identiques au run local → `Api:BaseUrl` (figé dans
  `wwwroot/appsettings.json`) et l'origine CORS (`Front__Origine=http://localhost:5292`) restent
  alignés sans reconfiguration.

### Dépannage

- **Front KO mais API up** : le dev server WASM doit écouter `0.0.0.0` — vérifier
  `ASPNETCORE_URLS=http://+:5292` dans le service `web` (déjà câblé).
- **Rebuild après modif code** : `docker compose up --build` ne suffit pas (pas d'image) — relancer
  fait re-tourner le service `build`. Pour forcer : `docker compose run --rm build`.
- **Repartir propre** : `docker compose down -v` (efface mongo + caches), puis `docker compose up`.

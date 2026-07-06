# planning-de-garde — exécution en conteneurs

Référence Docker complète. Stack orchestrée par `docker-compose.yml` à la racine. **Hors pipeline
de sprint** (outillage de lancement, pas de la méthode SCRUM).

Pour le lancement local sans Docker (skill `run`), voir [LANCEMENT.md](LANCEMENT.md).

---

## Vue d'ensemble

5 services, images officielles, **source montée** (pas de Dockerfile — choix assumé : itération
rapide, moins reproductible qu'une image figée).

| Service | Image | Port host | Rôle |
|---|---|---|---|
| `web` | `dotnet/sdk:10.0` | **5292** | front Blazor WASM (dev server) — l'app dans le navigateur |
| `api` | `dotnet/sdk:10.0` | **5180** | hôte d'API détaché : écriture + lecture + hub SignalR |
| `mongo` | `mongo:7` | **27017** | store durable du domaine (acteurs, comptes, rôles, slots, périodes, transferts, cycle) |
| `mongo-express` | `mongo-express:1.0.2` | **8081** | IHM web d'inspection des collections Mongo |
| `build` | `dotnet/sdk:10.0` | — | étape one-shot : restore + build Api puis Web (séquentiel) |

URLs une fois démarré :
- App : **http://localhost:5292**
- API + exploration Scalar : **http://localhost:5180** · **http://localhost:5180/scalar/v1**
- Inspection Mongo : **http://localhost:8081** (sans login)

---

## Démarrer

```bash
docker compose up            # build (1×) puis api + web + mongo + mongo-express ; logs au 1er plan
docker compose up -d         # en arrière-plan
docker compose logs -f api web    # suivre les logs applicatifs
docker compose ps            # état des services
docker compose down          # arrêt
docker compose down -v       # arrêt + purge volumes (mongo + caches build/nuget)
```

Pré-pull des images (optionnel, sinon `up` les tire) :

```bash
docker pull mcr.microsoft.com/dotnet/sdk:10.0 #Attention, pas d'ip v6 avec les docker microsoft
docker pull mongo:7
docker pull mongo-express:1.0.2
```

Le **premier `up` est lent** (restore NuGet complet). Les suivants réutilisent les caches
(`nuget-cache`, `build-artifacts`).

---

## Séquence de démarrage

```
mongo ─┐
       ├─► build (restore+build Api PUIS Web, séquentiel) ─► api ─► (Kestrel :5180)
mongo ─┘                                                  └─► web ─► (dev server :5292)
mongo ─► mongo-express (:8081)
```

- `build` se termine (`service_completed_successfully`) AVANT que `api`/`web` ne démarrent, qui
  tournent alors en `--no-build`.
- Build **séquentiel** Api→Web : évite la race CS2012 (« used by another process ») quand deux
  hôtes recompilent en parallèle `Domain.dll`/`Application.dll` partagés.

---

## Câblage & configuration

### Réseau
- L'API joint Mongo via le réseau compose : `Foyer__Mongo__ConnectionString=mongodb://mongo:27017`.
- Le front s'exécute dans le navigateur de **l'hôte** : il joint l'API via `localhost:5180` (port
  publié), PAS via le réseau interne.

### Alignement zéro-dérive
Les ports host sont **identiques** au run local → aucune reconfiguration :
- `Api:BaseUrl` (`http://localhost:5180/`, figé dans `wwwroot/appsettings.json`) reste valide.
- Origine CORS `Front__Origine=http://localhost:5292` reste valide.

### Variables clés (service `api`)
| Variable | Valeur | Effet |
|---|---|---|
| `ASPNETCORE_URLS` | `http://+:5180` | bind `0.0.0.0` (sinon localhost interne au conteneur, injoignable) |
| `Foyer__Persistance` | `Mongo` | config foyer **durable** (sinon InMemory volatile) |
| `Foyer__Mongo__ConnectionString` | `mongodb://mongo:27017` | accès au store |
| `Front__Origine` | `http://localhost:5292` | origine autorisée par le CORS |

### Isolation des binaires
Tout passe par `--artifacts-path /artifacts` (volume nommé `build-artifacts`) → les `bin/obj`
**Linux** du conteneur NE polluent PAS ceux de l'hôte **Windows**, et réciproquement. La source est
montée en **lecture seule** : aucune écriture conteneur ne fuit dans l'arbre de travail.

### NuGet
Les conteneurs restaurent via `nuget.docker.config` (**nuget.org seul**), pas `nuget.config` (qui
référence une source offline Windows `C:\Program Files (x86)\…` inexistante en Linux). Cache paquets
partagé dans le volume `nuget-cache`.

---

## Volumes

| Volume | Contenu | Purge |
|---|---|---|
| `mongo-data` | données Mongo (config foyer durable) | `down -v` |
| `build-artifacts` | bin/obj des conteneurs (isolés de l'hôte) | `down -v` |
| `nuget-cache` | cache paquets NuGet | `down -v` |

---

## Inspecter les données (mongo-express)

http://localhost:8081 → base **`planning_de_garde`** → collections du domaine (acteurs, comptes,
rôles, **slots / périodes / transferts / cycle de fond**). Auth basique désactivée
(`ME_CONFIG_BASICAUTH=false`, usage local).

> **Persistance généralisée depuis s15** : la borne anti-cliquet (règle 30) a été **levée**.
> Tout le domaine durable est désormais en Mongo (plus seulement la config foyer). En mode Mongo,
> **aucun seed** au 1er lancement (app vide, durable ensuite) ; le seed InMemory est conservé pour
> les tests. Voir `docs/BACKLOG-Done.md` (palier 14, sprint 15).

---

## Après une modif de code

Pas d'image figée → relancer fait re-tourner `build` :

```bash
docker compose up                 # build incrémental réutilise le cache
docker compose run --rm build     # forcer un rebuild seul
```

---

## Dépannage

| Symptôme | Cause / fix |
|---|---|
| `pull` échoue en `EOF` (mcr / Docker Hub) | egress registre du daemon. Désactiver IPv6 (Docker Engine `daemon.json` → `"ipv6": false`, **Apply & Restart**) ou sur la carte réseau Windows (`ncpa.cpl`), puis `wsl --shutdown` + relance Docker. |
| `NuGet.Config is not valid XML … '--'` | commentaire XML contenant `--` (interdit). Vérifier `nuget.docker.config`. |
| Front KO mais API up | dev server WASM doit écouter `0.0.0.0` → `ASPNETCORE_URLS=http://+:5292` (déjà câblé). |
| Build lent à chaque `up` | normal au 1er run ; ensuite caches `nuget-cache`/`build-artifacts`. Ne pas faire `down -v` inutilement. |
| Repartir 100 % propre | `docker compose down -v` puis `docker compose up`. |
| Conflit de port (5180/5292/8081/27017) | un process local occupe le port → l'arrêter, ou changer le mapping `ports:` dans `docker-compose.yml`. |

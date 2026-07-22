# MANDATORY TODO — Mise en production (« Go Live »)

> Feuille de route **obligatoire** pour passer de l'état actuel (runtime de développement)
> à une application réellement utilisable par un foyer, en ligne, en sécurité.
> Établie depuis l'état du dépôt à `main = 22f2b6e` (après refonte technique PR #71).

## But profond de l'application

Coordonner **à l'avance** la garde d'enfants d'un foyer :
- **qui** a les enfants **quand**, les **transferts** (passages de relais), **où** (activités / lieux) ;
- synchro **temps réel** (SignalR), **cloche** de notifications, **échanges avec accord**,
  gestion des **imprévus** (malade / retard), **multi-enfants**.

C'est un outil de coordination familiale **quotidien**, à faible nombre d'utilisateurs mais à
**forte confiance** : données sensibles, contrôle d'accès (admin / parent / invité), usage mobile.

## Forces déjà acquises

- Architecture Clean / hexagonale + CQRS propre, **917 tests verts** (dont durabilité Mongo réelle
  et E2E Playwright), temps réel SignalR, spec vivante, doc technique DocFX générée.

## L'écart avec « live » (constats sur le code)

- ❌ **Pas de `Dockerfile` de production** : les conteneurs font `dotnet run` sur la **source montée**
  (dev servers). Aucune image runtime figée.
- ❌ **URLs codées en dur sur `localhost`** : `wwwroot/appsettings.json` → `Api:BaseUrl = http://localhost:5180/` ;
  API → `Front:Origine = http://localhost:5292`, `AllowedHosts = "*"`. Aucune notion de domaine.
- ❌ **Pas de HTTPS / TLS**.
- ❌ **Mongo sans authentification**, volume local, **aucune sauvegarde**.
- ❌ **Mail de reset = smtp4dev** (catcher de dev) : n'envoie **rien de réel**.
- ❌ **Google OAuth = placeholder non câblé** (`FournisseurOAuthGoogleNonCable`, dette P0 connue).
- ❌ **Foyer de démo en dur** au seed (`Foyer.cs` : « Léa », `parent-a`/`parent-b`, école / domicile A/B / nounou)
  + compte démo seedé.
- ❌ **Secrets par défaut / en clair** (chaîne Mongo, SMTP, futur secret OAuth, clé de session).

---

## Les 5 chantiers OBLIGATOIRES

> Ordre de bataille : **1 → 2 → 3** débloquent un usage réel et sûr ; **4** rend le démarrage propre ;
> **5** ancre l'adoption au quotidien.

### 1. Packager pour la prod et héberger sur un domaine en HTTPS  *(bloqueur n°1)*

**Pourquoi** : personne ne peut utiliser l'app depuis son téléphone tant qu'elle vit sur `localhost`
en `dotnet run`.

- [ ] `Dockerfile` multi-stage pour l'**API** (build SDK → image runtime, plus de source montée).
- [ ] Build de **publication du front WASM** servi en **fichiers statiques** (via l'hôte API ou un hébergeur statique / CDN).
- [ ] **Reverse proxy** (Caddy ou Traefik) avec **TLS Let's Encrypt automatique**.
- [ ] **Nom de domaine** dédié.
- [ ] `Api:BaseUrl`, `Front:Origine` et `AllowedHosts` **pilotés par variables d'environnement** (fini le localhost en dur).
- [ ] Hébergement : petit **VPS** ou **PaaS**.

**Fait quand** : l'app est joignable en `https://<domaine>` depuis un téléphone hors réseau local, front + API + SignalR OK.

### 2. Sécuriser données + accès : Mongo authentifié, secrets, sauvegardes

**Pourquoi** : données familiales derrière un gating d'accès — une fuite ou une perte tue la confiance.

- [ ] Mongo **managé (Atlas)** ou authentifié par **credential fort** (fin du Mongo sans auth).
- [ ] **Tous les secrets** (Mongo, SMTP, OAuth, clé de signature de session) sortis en **env / secret store** (rien en clair dans le repo).
- [ ] `Foyer__Persistance=Mongo` en prod → slots / périodes / transferts **et** config foyer tous **durables**.
- [ ] **Sauvegardes automatiques** (backup Atlas ou `mongodump` planifié) **+ restauration testée**.
- [ ] Restreindre `AllowedHosts` / CORS au **domaine réel** uniquement.

**Fait quand** : Mongo exige une auth, aucun secret n'est en clair, un backup a été restauré avec succès en test.

### 3. Rendre l'authentification réellement utilisable

**Pourquoi** : aujourd'hui un parent qui perd son accès **ne peut pas le récupérer** (reset non réel),
et le bouton Google mène à un placeholder.

- [ ] Brancher un **fournisseur d'email transactionnel réel** (relais SMTP / SendGrid / Mailgun / Brevo) → le mail de reset **arrive vraiment**.
- [ ] **Finir l'adaptateur Google OAuth** (client id / secret + token endpoint — la logique de rapprochement est déjà testée) **OU masquer les boutons OAuth** en attendant (pas d'impasse UX).
- [ ] Confirmer la **persistance de session** + cookies / tokens **HTTPS-only**.

**Fait quand** : un reset de mot de passe est reçu sur une vraie boîte mail et permet de se reconnecter ; aucun bouton d'auth ne mène à un cul-de-sac.

### 4. Onboarding d'un vrai foyer depuis un état vide (retirer le seed démo)

**Pourquoi** : le but c'est *ton* foyer, pas « Léa ».

- [ ] Vérifier que le **premier lancement Mongo vide** donne un foyer **propre** (test `PremierLancementMongoVideTests` en appui).
- [ ] **Désactiver le seed démo** en prod (`Foyer.cs`, compte démo `Demo:SeedCompteDemo`).
- [ ] Parcours guidé de création : **parents, enfants, rôles, activités**.
- [ ] Configurer le **cycle de fond par enfant** (rappel s53 : un enfant sans cycle propre s'affiche **NEUTRE** — à configurer pour **chaque** enfant).

**Fait quand** : on peut créer son foyer réel depuis zéro, sans aucune donnée fictive résiduelle.

### 5. Mobile-first : PWA installable + notifications push pour la cloche

**Pourquoi** : la boucle quotidienne (« qui récupère ce soir », imprévu, proposer / accepter un échange)
vit sur le téléphone et **dépend d'être notifié**.

- [ ] **Manifest + service worker** (ajout à l'écran d'accueil, coquille offline).
- [ ] Adosser la cloche à du **Web Push (VAPID)** → recevoir imprévus / échanges **application fermée**
  (aujourd'hui SignalR ne notifie qu'app ouverte).
- [ ] Vérifier le rendu mobile de bout en bout (responsive iOS déjà couvert par des tests).

**Fait quand** : l'app s'installe sur l'écran d'accueil et un parent reçoit une notification d'échange / imprévu app fermée.

---

## Annexe — valeurs à dé-câbler du `localhost` (référence rapide)

| Fichier | Clé | Valeur actuelle | À rendre |
|---|---|---|---|
| `src/PlanningDeGarde.Web/wwwroot/appsettings.json` | `Api:BaseUrl` | `http://localhost:5180/` | env / domaine |
| `src/PlanningDeGarde.Api/appsettings.json` | `Front:Origine` | `http://localhost:5292` | env / domaine |
| `src/PlanningDeGarde.Api/appsettings.json` | `AllowedHosts` | `*` | domaine réel |
| `docker-compose.yml` | Mongo / SMTP | local, sans auth | managé + secrets |
| `src/PlanningDeGarde.Application/Foyer/Seed/Foyer.cs` | seed démo | « Léa », parent-a/b… | désactivé en prod |

## Références

- Écosystème local & lancement : `docs/ecosysteme-local.md`
- Suivi de la refonte technique + carte des bounded contexts + mapping des routes : `docs/briefs/technical-changes-plan.md`
- Dette OAuth réel : suivie au backlog produit (`docs/BACKLOG.md`)

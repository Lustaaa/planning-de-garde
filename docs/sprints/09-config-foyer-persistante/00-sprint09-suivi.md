# Suivi Sprint 09 — Config foyer persistante · ajout d'acteurs & survie au redémarrage

> **Cadrage scaffolding (décidé CP — adaptateur de droite durable Mongo derrière les 3 ports
> inchangés).** Le sujet (a) **ajoute** des acteurs au foyer (id stable neuf opaque) et (b)
> **persiste** la SEULE config foyer (référentiel acteurs : noms, couleurs, acteurs ajoutés)
> derrière un **adaptateur durable Mongo**, doublant/remplaçant le singleton
> `ConfigurationFoyerEnMemoire` (`ServiceCollectionExtensions.cs:28-31`). Le domaine et le CQRS
> de lecture (`GrilleAgendaQuery`) **ne bougent pas**.
> - **Commande `AjouterActeur` + handler (Application, NEUF)** — `{ nom, couleur? }` →
>   `AjouterActeurHandler` génère un **identifiant stable neuf opaque** (GUID ou séquence
>   « autre-N », forme laissée à `tdd-auto`), **jamais dérivé du libellé**, **unique** (jamais
>   un id existant) → persiste nom (+ couleur, sinon **repli neutre** par contrat
>   `IPaletteCouleurs`) via le port d'écriture → diffusion temps réel sur succès (Spy backend).
>   **Garde « nom non vide » réutilisée** (`EditerActeurHandler.cs:38`), **conditionnelle** (le
>   nominal ajout est déjà vert → un refus inconditionnel régresserait Sc.1).
> - **Accès de lecture d'énumération (NEUF)** — l'écran de config doit **énumérer les acteurs
>   depuis le store** (aujourd'hui `Foyer.ActeursEditables` est une **liste statique front**) :
>   sinon un acteur ajouté n'apparaît pas (Sc.1/Sc.6). Un accès de lecture d'énumération est à
>   exposer sur le port/store.
> - **Adaptateur durable Mongo (Infrastructure, NEUF)** réalisant les 3 ports
>   (`IReferentielResponsables` / `IPaletteCouleurs` / `IEditeurConfigurationFoyer` **inchangés**)
>   + écriture d'ajout + énumération, **singleton**. **Seed-au-démarrage durable** : seed depuis
>   `Foyer` **seulement si le store est vide** ; ensuite l'état persisté est relu **sans
>   re-seeder par-dessus les éditions** — c'est l'**inversion exacte de `Scenario10`** (re-seed
>   volatile), **principale surface de bug** (un re-seed à chaque démarrage ferait échouer le
>   pivot Sc.3).
> - **Read model / légende INCHANGÉS** — `GrilleAgendaQuery` résout `NomDe`/`CouleurDe` sur l'id
>   stable ; légende **dédoublonnée par id** (s07), **présents dans la fenêtre** (s07 Sc.3). La
>   case/légende d'un acteur **ajouté** « suivent » par **re-projection** sur son id neuf
>   (caractérisation), pas par un calcul neuf.
> - **Outillage Mongo via Docker (contrainte PO actée)** — Mongo doit **tourner en conteneur
>   Docker** (jamais embedded/in-process). Ajouter un **`docker-compose`** (service Mongo) ;
>   `run.ps1` démarre le conteneur avant l'API (ou documente le prérequis). Le **test
>   d'intégration du pivot (Sc.3)** exige un **Mongo RÉEL tournant** ; **skip propre** si Docker
>   indisponible plutôt qu'un faux vert. Garde-fou d'outillage, sans observable métier.
>
> **Routage backend (`tdd-auto`) vs IHM/runtime + intégration (`ihm-builder`) — axe explicite.**
> - **Drivers backend réels** (`tdd-auto`, frontière Application, sans Mongo réel — store/fake
>   en mémoire) : **Sc.1** (ajout : id neuf opaque résolvable + énumération), **Sc.8** (refus
>   nom vide / tout-espaces, aucun id généré, liste inchangée).
> - **Caractérisations backend** (filet anti-régression, ⚠️ early green **attendu**, **pas**
>   driver — composent du code déjà vert) : **Sc.4** (id neuf circule en légende — Sc.1 + s07
>   légende-par-id), **Sc.5** (sans couleur → neutre, **garanti par le contrat
>   `IPaletteCouleurs.CouleurDe`** sur clé absente — leçon s03), **Sc.6** (sans période → pas de
>   fantôme — s07 Sc.3 légende-présents + s08 Sc.6), **Sc.7** (deux libellés identiques → ids
>   distincts — Sc.1 id opaque + s07 légende dédoublonnée par id).
> - **Pivot durabilité = INTÉGRATION sur Mongo RÉEL (Docker)** (`ihm-builder` + intégration) :
>   **Sc.3** — survie au redémarrage prouvée sur **store Mongo réel** via la **grille réellement
>   câblée** (case + légende nommée après redémarrage). **Jamais** une doublure (anti
>   vert-qui-ment, R4). La logique **seed-once** est testée à l'intégration contre Mongo réel.
> - **Drivers IHM/runtime** (`ihm-builder`, app réellement câblée — DI réelle, front WASM + API
>   distante + SignalR) : **Sc.1** (Carla **apparaît dans la liste** de l'écran config, via
>   énumération depuis le store durable), **Sc.2** (case + légende suivent **sans rechargement**
>   — caractérisation, réutilise s08 Sc.1), **Sc.8** (**message clair** à l'écran), **Sc.9**
>   (**service injoignable** : échec clair, saisie conservée à resoumettre, aucun acteur
>   enregistré — réutilise l'échec transport s08 Sc.9).
>
> **Note IHM hors périmètre backend.** Aucun `.razor` ni câblage SignalR/Mongo réel dans les
> « Fichiers à créer » des scénarios **backend purs** ; la diffusion temps réel se vérifie en
> backend par un **Spy** sur `INotificateurPlanning`. Rendu / interactivité / persistance réelle
> relèvent d'`ihm-builder` + intégration.

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Ajouter la nounou : identifiant stable neuf](01-ajouter-acteur-identifiant-stable-neuf.md) | `@nominal` 🖥️ IHM · backend `tdd-auto` + runtime `ihm-builder` | ✅ GREEN backend (frontière Application) · ✅ GREEN runtime `ihm-builder` (Carla apparaît dans la liste config, énumérée depuis le store via API distante réelle) | 3/3 + runtime | ✅ GREEN (backend + runtime) |
| 2 | [Renommer un acteur déjà semé met à jour la grille](02-renommer-acteur-met-a-jour-grille.md) | `@nominal` 🖥️ IHM · caractérisation (s08) + runtime `ihm-builder` | ⏳ Pending (runtime : case + légende suivent sans rechargement) | 0/0 | ⏳ Pending |
| 3 | [L'ajout et l'édition survivent au redémarrage](03-ajout-edition-survivent-redemarrage.md) | `@nominal` 🖥️ pivot durabilité · **intégration Mongo réel (Docker)** + runtime `ihm-builder` | ⏳ Pending (**intégration/E2E sur Mongo réel** : seed-once, survie au redémarrage) | 0/0 | ⏳ Pending |
| 4 | [Un acteur ajouté apparaît en légende une fois une période affectée](04-acteur-ajoute-apparait-en-legende.md) | `@nominal` · caractérisation `tdd-auto` + runtime `ihm-builder` | ⏳ Pending (runtime : entrée légende « Carla » rose sur id neuf) | 0/1 | ⏳ Pending |
| 5 | [Un acteur ajouté sans couleur retombe sur la teinte neutre](05-acteur-sans-couleur-teinte-neutre.md) | `@limite` · caractérisation `tdd-auto` (contrat palette) + runtime `ihm-builder` | ⏳ Pending (runtime : case + légende « Papy Jo » gris) | 0/1 | ⏳ Pending |
| 6 | [Un acteur ajouté sans période ne crée pas d'entrée fantôme](06-acteur-sans-periode-pas-de-fantome.md) | `@limite` · caractérisation `tdd-auto` (s07/s08) + runtime `ihm-builder` | ⏳ Pending (runtime : présent en liste config, absent légende/case) | 0/1 | ⏳ Pending |
| 7 | [Deux acteurs de même libellé reçoivent deux identifiants distincts](07-deux-libelles-identiques-ids-distincts.md) | `@limite` · caractérisation `tdd-auto` (id opaque + dédoublonnage s07) | ⏳ Pending | 0/1 | ⏳ Pending |
| 8 | [Ajouter un acteur sans nom est refusé](08-ajouter-sans-nom-refuse.md) | `@erreur` 🖥️ IHM · backend `tdd-auto` + runtime `ihm-builder` | ⏳ Pending (runtime : message clair, liste inchangée) | 0/3 | ⏳ Pending |
| 9 | [Ajout impossible si le service de configuration est injoignable](09-service-injoignable-ajout-impossible.md) | `@erreur` 🖥️ IHM · driver runtime `ihm-builder` (backend néant) | ⏳ Pending (runtime : échec clair, saisie conservée, rien d'enregistré) | 0/0 | ⏳ Pending |

**Total** : 9 scénarios · **9 tests unitaires backend** (≈ 5 drivers réels : Sc.1×3 ajout/id/énumération,
Sc.8×2 garde ; ≈ 4 caractérisations early-green : Sc.4, Sc.5, Sc.6, Sc.7 + Sc.8×1 absence-diffusion).
**Pivot durabilité Sc.3 = intégration sur Mongo réel (Docker)**, hors compte unit (anti vert-qui-ment).
3 scénarios sans backend unit (Sc.2 = caractérisation s08/runtime ; Sc.3 = intégration ; Sc.9 = 100 %
runtime échec transport).

**Acceptation** : pivot **Sc.3** sur **Mongo réel tournant** (conteneur Docker) via la grille câblée
(case + légende nommée après redémarrage) — preuve la plus forte ; lecture via port sur instance
fraîche acceptable en complément. Runtime IHM Sc.1/2/8/9 sur l'app réellement câblée (front WASM + API
distante + SignalR). **Skip propre** si Docker indisponible plutôt qu'un faux vert.

**Statuts** : ⏳ Pending · 🔴 Red · ✅ Green.

**Légende routage** : `tdd-auto` = cycles unitaires backend (handler `AjouterActeur`, garde nom vide,
énumération, diffusion par Spy — store/fake en mémoire, **jamais Mongo réel**) ; `ihm-builder` =
acceptation runtime/E2E + **intégration Mongo réel** sur l'app câblée (écran config énumérant le store
durable, grille suivant l'ajout/édition, survie au redémarrage, messages d'échec). Un scénario `🖥️`
n'est **jamais** prouvé par bUnit seul (render mode, DI réelle, SignalR, transport HTTP, store durable).

**Borne anti-cliquet (règle 30)** : **seule** la config foyer passe durable. Slots / périodes /
transferts **restent InMemory** — ne pas tirer leur persistance en avant.

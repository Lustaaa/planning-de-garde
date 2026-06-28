# Suivi Sprint 13 — CRUD acteurs · tranche suppression (palier 8, `crud-acteurs-suppression`)

> **Cadrage scaffolding (décidé CP — D1→D6, `99-sprint13-retours.md`).** Le sujet **complète le
> cycle de vie des acteurs par le Delete**. La suppression est **autorisée** (pas de refus « si
> références ») et **neutralise par repli** les cases orphelines : la surcharge orpheline cesse de
> primer → la case retombe sur le **fond** (le cycle reprend) ou sur le **neutre** si l'index n'est
> ni mappé ni résolu, **sans nom fantôme** ; un acteur **mappé au cycle de fond** devient
> **non mappé → neutre**. Accusé **non bloquant « Acteur supprimé »**, **pas de réaffectation
> automatique** (règle 6). La config foyer étant **persistée Mongo** (palier 5), la suppression
> touche un **store réel** → **acceptation runtime obligatoire** (rempart anti vert-qui-ment).
>
> **Couches touchées (multi-couche, backend d'abord → IHM en fin).** L'incrément **n'ouvre aucune
> règle de résolution neuve** (priorité surcharge > fond > neutre déjà livrée au palier 6) ; le seul
> RED neuf est le **retrait d'acteur** et son **repli observable** :
> - **Application (NEUF)** — `SupprimerActeurCommand(string ActeurId)` → `SupprimerActeurHandler`
>   renvoyant un `Result`. **Idempotent** (id absent / déjà supprimé = no-op qui **réussit**, jamais
>   un refus — D3). Diffusion temps réel sur succès (Spy backend).
> - **Port (NEUF)** — `IEditeurConfigurationFoyer.Supprimer(string acteurId)` (miroir écriture, à
>   côté d'`Ajouter` / `Renommer` / `Recolorier`), retire l'entrée **nom** ET **couleur**.
>   L'identifiant stable opaque (`acteur-…`) est la clé — **jamais le libellé**.
> - **Résolution de la grille (`GrilleAgendaQuery`, Application — MODIFIÉE)** — une surcharge OU un
>   fond pointant un acteur **supprimé** doit **cesser de résoudre** : surcharge orpheline → retombe
>   sur le fond ; fond orphelin → non mappé → neutre, **sans nom fantôme** (aujourd'hui `NomDe`
>   retombe sur l'**id brut** = nom fantôme). Nécessite un **contrat d'existence d'acteur** (p.ex.
>   `IEnumerationActeursFoyer`, déjà réalisé par les stores) injecté dans la query — forme laissée à
>   `tdd-auto`. **→ remonter au CP si le contrat d'existence est ambigu.**
> - **Adapters droite (NEUF)** — `ConfigurationFoyerEnMemoire` (retrait des dictionnaires
>   `_noms` / `_couleurs`) **ET** `ConfigurationFoyerMongo` (retrait du store durable, write-through).
> - **Api (NEUF)** — endpoint canal `POST /api/canal/supprimer-acteur` (corps
>   `SupprimerActeurRequete(ActeurId)`), même convention succès/échec que les autres écritures ;
>   diffusion temps réel sur succès. **CQRS préservé** : write par le canal, read + diffusion SignalR
>   lecture seule à part, jamais confondus.
> - **Web (IHM, lot final `ihm-builder`)** — bouton supprimer + liste relue + accusé « Acteur
>   supprimé » à part + légende dédoublonnée + gating Invité (règle 9) + échec API injoignable
>   (règle 28) + temps réel SignalR.
>
> **Borne anti-cliquet (règle 30).** SEULE la config foyer est durable (Mongo) ; slots / périodes /
> transferts / **cycle de fond** restent **InMemory**. La suppression **exerce** une persistance
> déjà acquise (palier 5), sans tirer aucune persistance neuve en avant.
>
> **Routage backend (`tdd-auto`) vs IHM/runtime (`ihm-builder`) — axe explicite.**
> - **Drivers backend réels** (`tdd-auto`, frontière Application, store/fake mémoire) : **Sc.1**
>   (retrait du store relu), **Sc.2** (surcharge orpheline → fond), **Sc.4** (acteur mappé au fond
>   → index non mappé → neutre).
> - **Caractérisations backend** (filet anti-régression, ⚠️ early green **attendu**, **pas** driver)
>   : **Sc.3** (surcharge orpheline sur index non mappé → neutre — composé du driver **Sc.2** +
>   contrat `CycleDeFond` index non mappé → `null`, s10 Sc.4 déjà vert), **Sc.5** (idempotence —
>   `Dictionary.Remove` est naturellement no-op sur clé absente, `Result` toujours succès, garanti
>   par l'impl minimale de **Sc.1**).
> - **Acceptation Sc.1 = intégration sur Mongo RÉEL (Docker)** : l'acteur retiré disparaît du store
>   relu **et** après redémarrage (instance d'hôte fraîche, même base). **Skip propre** si Docker
>   indisponible, jamais un faux vert (R4).
> - **Lot IHM final 🖥️** (`ihm-builder`, app réellement câblée — DI réelle, front WASM + API
>   distante + SignalR + Mongo réel) : **Sc.6** (bouton + liste + accusé + légende), **Sc.7**
>   (gating Invité), **Sc.8** (API injoignable), **Sc.9** (temps réel). **Cascade early-green
>   (câblage IHM partagé)** : Sc.7/8/9 réutilisent le câblage posé par Sc.6 (bouton, issue accusé/
>   échec, gating mutualisé règle 9, transport règle 28) → **batchables** en lot de caractérisations.
>
> **Note IHM hors périmètre backend.** Aucun `.razor` ni câblage SignalR/Mongo réel dans les
> « Fichiers à créer » des scénarios **backend purs** (Sc.1→Sc.5) ; la diffusion temps réel s'y
> vérifie par un **Spy** sur `INotificateurPlanning`. Rendu / interactivité / persistance réelle =
> `ihm-builder`. Un scénario `🖥️` n'est **jamais** prouvé par bUnit seul (render mode, DI réelle,
> SignalR, transport HTTP, store durable).

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Supprimer un acteur le retire du store relu](archive/01-supprimer-acteur-retire-du-store.md) | `@nominal` `@driver` · backend `tdd-auto` + **intégration Mongo réel** | ✅ GREEN (intégration Mongo réel : Nounou disparaît du store relu et après redémarrage ; A & B restent ; skip propre si Docker absent) | 1/1 | ✅ GREEN |
| 2 | [Surcharge orpheline : la case retombe sur le fond](archive/02-surcharge-orpheline-retombe-fond.md) | `@limite` `@driver` · backend `tdd-auto` | ✅ GREEN (frontière Application : après suppression, la case retombe sur le responsable de fond) | 1/1 | ✅ GREEN |
| 3 | [Surcharge orpheline sur index non résolu : repli neutre sans nom fantôme](archive/03-surcharge-orpheline-retombe-neutre.md) | `@limite` `@caractérisation` · backend `tdd-auto` (⚠️ early green) | ✅ GREEN (caractérisation) (frontière Application : repli neutre, aucun nom — couvert par Sc.2 + contrat index non mappé) | 1/1 | ✅ GREEN |
| 4 | [Acteur mappé au cycle de fond : index non mappé → neutre](archive/04-acteur-mappe-fond-index-non-mappe.md) | `@limite` `@driver` · backend `tdd-auto` | ✅ GREEN (frontière Application : fond orphelin → index non mappé → neutre, sans nom fantôme) | 1/1 | ✅ GREEN |
| 5 | [Supprimer un acteur absent ou déjà supprimé : no-op qui réussit](archive/05-suppression-idempotente.md) | `@erreur` `@caractérisation` · backend `tdd-auto` (⚠️ early green) | ✅ GREEN (caractérisation) (frontière Application : DELETE idempotent — succès sans effet, aucune erreur) | 1/1 | ✅ GREEN |
| 6 | [Depuis l'écran de config : bouton supprimer → liste, légende, accusé](archive/06-ihm-bouton-supprimer-liste-legende.md) | `@nominal` 🖥️ IHM `@caractérisation` · runtime `ihm-builder` | ✅ GREEN (runtime : front WASM + API distante réelle + diffusion SignalR réelle — grand-père quitte la liste relue + légende dédoublonnée sans nom fantôme + accusé « Acteur supprimé » non bloquant ; RED bouton introuvable → GREEN) | 1/1 | ✅ GREEN |
| 7 | [Un Invité ne peut pas supprimer d'acteur](archive/07-ihm-invite-ne-supprime-pas.md) | `@erreur` 🖥️ IHM `@driver` · runtime `ihm-builder` (driver réel : aucun garde de rôle préexistant sur l'écran config) | ✅ GREEN (runtime : Invité → aucun bouton supprimer, aucune commande émissible, liste inchangée ; contrôle positif Parent ; gating règle 9 `@if EstParent` posé — RED bouton visible → GREEN) | 1/1 | ✅ GREEN |
| 8 | [API injoignable : suppression non appliquée](archive/08-ihm-api-injoignable.md) | `@erreur` 🖥️ IHM `@caractérisation` · runtime `ihm-builder` (early green câblage IHM partagé Sc.6) | ✅ GREEN (caractérisation, early-green confirmé : vert au 1er coup, aucun code de prod neuf — message d'échec clair, grand-père toujours listé, aucune fausse confirmation, store réel inchangé, aucune mise en file règle 28) | 1/1 | ✅ GREEN |
| 9 | [Temps réel : la suppression propage grille et légende sans rechargement](archive/09-ihm-temps-reel-propagation.md) | `@limite` 🖥️ IHM `@caractérisation` · runtime/intégration SignalR `ihm-builder` (early green câblage IHM partagé Sc.6) | ✅ GREEN (caractérisation, early-green confirmé : vert au 1er coup, aucun code de prod neuf — second écran voit la case du 16/06 retomber sur Parent A + légende dédoublonnée sans grand-père, sans rechargement ; convention anti-flake *TempsReel* appliquée, stable ≥3×) | 1/1 | ✅ GREEN |

**Total** : 9 scénarios · **5 tests unitaires backend** (3 drivers réels : Sc.1, Sc.2, Sc.4 ;
2 caractérisations early-green : Sc.3, Sc.5). **Acceptation Sc.1 = intégration Mongo réel (Docker)**,
hors compte unit (anti vert-qui-ment). **Lot IHM final** (Sc.6→Sc.9) = caractérisations runtime
groupables portées par `ihm-builder` (cascade de câblage partagé), hors compte unit backend.

**Acceptation runtime IHM : 4/4 — TOUS les scénarios IHM ✅** (Sc.6 ✅, Sc.7 ✅, Sc.8 ✅, Sc.9 ✅).
**Tranche suppression complète : 9/9 ✅.**
**Note Sc.7 : driver réel, pas l'early-green planifié** — l'écran config n'avait aucun garde de rôle
(le gating n'existait que sur la grille `PlanningPartage`) ; `@if EstParent` posé sur le bouton supprimer,
5 tests config rétrofités avec un `SessionPlanning` réel. **Sc.8/Sc.9 : early-green confirmés** (issue
d'échec transport + diffusion sur succès posées au Sc.6, aucun code de prod neuf). **Balayage Sc.9** : la
touche du composant partagé `ConfigurationFoyer` (boutons + gating) a exposé une course latente
`UnknownEventHandlerId` dans 7 tests `*TempsReel*` préexistants interagissant avec le `select` sans garde
d'énumération → garde déterministe `WaitForState` ajouté (standard des tests frères). **Suite complète :
196/196 verte, stable ≥3×** (Docker actif, sans `--no-build` ni filtre).

**Statuts** : ⏳ Pending · 🔴 Red · ✅ Green.

**Légende routage** : `tdd-auto` = cycles unitaires backend (handler `SupprimerActeur`, port
`Supprimer`, filtre d'existence dans `GrilleAgendaQuery`, idempotence, diffusion par Spy — store/fake
en mémoire, **jamais Mongo réel**) ; `ihm-builder` = acceptation runtime/E2E + **intégration Mongo
réel** sur l'app câblée (bouton supprimer, liste relue, accusé, légende dédoublonnée, gating Invité,
échec API, temps réel SignalR). Un scénario `🖥️` n'est **jamais** prouvé par bUnit seul.

**Borne anti-cliquet (règle 30)** : **seule** la config foyer est durable. Slots / périodes /
transferts / **cycle de fond** **restent InMemory** — ne pas tirer leur persistance en avant.

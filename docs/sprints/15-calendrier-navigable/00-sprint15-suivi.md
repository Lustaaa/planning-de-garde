# Suivi Sprint 15 — Calendrier navigable (palier 9, **absorbe le palier 14**, épics É4 + É7 · `calendrier-navigable`)

> **Cadrage scaffolding (analyse `/3` — `tdd-analyse`).** Le sprint cumule **deux blocs** et touche
> **plusieurs couches** — **PAS « Web only »** :
> - **Bloc A — Navigation du calendrier (read model + IHM).** Ouvre `GrilleAgendaQuery` du couple figé
>   « 35 j / 5 semaines depuis la semaine de référence » à un couple **(ancre, vue → span)** : **Semaine**
>   (7 j / 1 ligne), **4 semaines glissantes** (28 j / 4 lignes — **NOUVEAU défaut**), **Mois** (semaines ISO
>   entières recouvrant le mois calendaire). Endpoint de **lecture** `GET /api/grille/{annee}/{mois}/{jour}`
>   étendu d'un paramètre **vue**. Navigation (préc. / suiv. / **Aujourd'hui** / sélecteur de vue) et **état
>   de navigation (ancre + vue) en session/mémoire front** ; la grille reste **lecture seule** — la
>   navigation ne fait que **re-projeter**. Affectation par **plage de cases** réutilisant le canal
>   d'écriture `AffecterPeriode` existant (**aucun handler ni port neuf**).
> - **Bloc C — Persistance Mongo de TOUT le domaine (palier 14 absorbé, borne anti-cliquet LEVÉE).** 4
>   **adaptateurs de droite Mongo neufs** (`AdapterDroite.Mongo`) — slots, périodes, transferts, cycle de
>   fond — derrière les **ports existants** (impls InMemory **conservées** pour les tests). **DI
>   généralisée** : le flag `Foyer:Persistance` commute **tout** le domaine droite (Mongo runtime /
>   InMemory en test), au lieu de la seule config foyer (s09). **Démarrage sans seed runtime** : retrait de
>   `AmorcerDonneesDemo` **et** du seed-once des acteurs côté `ConfigurationFoyerMongo`.
>
> **Couches touchées** : **Application** (`GrilleAgendaQuery` vue/span) · **Api** (`CanalLecture` param vue ;
> `SeedDonneesDemo` retiré ; `Program` sans amorçage) · **AdapterDroite.Mongo** (4 adaptateurs) ·
> **Infrastructure** (DI commutée pour tout le domaine droite ; seed-once Mongo retiré) · **Web**
> (`PlanningPartage` navigation + plage + gating). Scope **multi-couche** assumé — aucun slug ne le
> contredit (pas de contrat de réponse de commande neuf : le bloc A réutilise l'outcome `AffecterPeriode`
> existant tel quel).
>
> **Asymétrie seed (clé, assumée).** *Runtime/Mongo* = **aucun seed, jamais** (store vierge → app vide →
> durable). *Tests/InMemory* = on **garde** le seed de base : la suite de non-régression reste **verte en
> InMemory seedé**. La durabilité se prouve sur **Mongo RÉEL** (Docker, façon s09 ; `MongoRequisFact` →
> skip propre si Docker absent, jamais un faux vert). L'état de **navigation** (ancre/vue) reste en
> session/mémoire front et **ne persiste pas**.
>
> **Bascule du défaut 5 → 4 semaines (re-pointage attendu, PAS une régression).** Le défaut de fenêtre
> passe de 35 j / 5 lignes à **28 j / 4 lignes**. Les tests **structurels existants** qui figent 5 semaines
> (`Scenario_GrilleStructure5Semaines`, `Scenario_SlotBorneHauteFenetre`, et tout test asserant 35 cases /
> dernière ligne au jour 34) **migrent** vers le défaut 4 semaines — re-pointage mécanique, dicté
> explicitement par Sc.3, à **ne pas confondre** avec une régression chez `tdd-auto`. → remonter au CP si
> la migration touche un observable inattendu.
>
> **Routage backend vs 🖥️ IHM.**
> - **Backend (frontière Application / intégration)** : **Sc.2, Sc.3** (read model vue/span sur
>   `GrilleAgendaQuery`, unit-testable sans Blazor — même registre que les `Scenario_Grille*` existants) ;
>   **Sc.8, Sc.9** (persistance Mongo, **acceptation intégration sur store RÉEL** via
>   `WebApplicationFactory<ApiProgram>`, façon `ConfigurationFoyerMongoDurabiliteTests`).
> - **🖥️ IHM → `ihm-builder`, acceptation RUNTIME** : **Sc.1** (naviguer ± semaine), **Sc.4**
>   (Aujourd'hui), **Sc.5** (affectation par plage), **Sc.6** (API injoignable en navigation), **Sc.7**
>   (gating Invité sur la plage). Symptôme PO = **fait d'usage runtime** (le bouton décale la fenêtre, la
>   sélection ouvre l'affectation, l'écran reste sur place en cas d'échec) : render mode interactif, DI
>   réelle, API distante. **bUnit seul ne prouve jamais** ces symptômes → app **réellement câblée**.
>
> **Re-résolution du fond = DÉJÀ ACQUISE (caractérisation, pas driver).** `GrilleAgendaQuery.CaseJourAu`
> résout déjà `ResponsableDeFond(date)` **par date** ; re-projeter à une ancre décalée re-résout donc le
> fond **mécaniquement**. La navigation par décalage d'ancre **ne demande aucun changement backend** (le
> seul backend neuf est la dimension **vue/span**). Tout « le fond re-résout Bruno » est donc une
> **caractérisation** (⚠️ early green), jamais un driver.
>
> **Cascade early-green (câblage IHM partagé) — contrôlée par exploration.** L'état de navigation
> (ancre + vue en session) posé par **Sc.1** rend **Sc.4** (Aujourd'hui = reset d'ancre) early-green sur sa
> plomberie ; le trigger de plage posé par **Sc.5** (gardé `Session.EstParent` — **gate vérifié présent**
> sur `PlanningPartage`, `OuvrirMenu`) rend **Sc.7** (gating Invité) early-green. Ces dépendants sont
> **batchables** en caractérisations chez `ihm-builder`, pas des early-green inattendus. **Sc.6** (préserver
> la fenêtre courante en cas d'échec de navigation) reste un **vrai driver** front (l'actuel `ChargerAsync`
> vide la grille sur échec — comportement à durcir pour la navigation).
>
> **Affectation par plage = AffecterPeriode existant (backend early-green).** Le handler `AffecterPeriode`
> couvre déjà un intervalle `[début, fin]` ; émettre une période sur 2 jours contigus n'ajoute **aucun
> handler ni port**. Le neuf de Sc.5 est **front** (sélection de plage + dialog pré-remplie sur l'intervalle).

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Naviguer d'une semaine vers le futur ou le passé](01-naviguer-semaine.md) | `@nominal` 🖥️ IHM · driver nav · runtime `ihm-builder` | ⏳ Pending | 0/2 | ⏳ Pending |
| 2 | [Basculer entre les vues prédéfinies](02-basculer-vues.md) | `@nominal` · backend (read model vue/span) | ✅ GREEN | 4/4 | ✅ GREEN |
| 3 | [Fenêtre par défaut à l'ouverture = 4 semaines glissantes](03-defaut-quatre-semaines.md) | `@limite` · backend (défaut 4 sem.) | ✅ GREEN | 3/3 | ✅ GREEN |
| 4 | [Retour à la semaine en cours après navigation](04-retour-aujourdhui.md) | `@limite` 🖥️ IHM · ⚠️ early green (câblage nav Sc.1) · runtime `ihm-builder` | ⏳ Pending | 0/1 | ⏳ Pending |
| 5 | [Affecter une période sur une plage de 2 cases contiguës](05-affecter-periode-plage.md) | `@nominal` 🖥️ IHM · driver front (write = `AffecterPeriode` early green) · runtime `ihm-builder` | ⏳ Pending | 0/2 | ⏳ Pending |
| 6 | [API distante injoignable pendant la navigation](06-api-injoignable-navigation.md) | `@erreur` 🖥️ IHM · driver front · runtime `ihm-builder` | ⏳ Pending | 0/1 | ⏳ Pending |
| 7 | [Sélection de plage indisponible en consultation seule](07-invite-plage-indisponible.md) | `@erreur` 🖥️ IHM · ⚠️ early green (gate `EstParent` + trigger Sc.5) · runtime `ihm-builder` | ⏳ Pending | 0/1 | ⏳ Pending |
| 8 | [Premier lancement sur store Mongo vierge : application vide](08-premier-lancement-mongo-vide.md) | `@limite` · backend **intégration Mongo réel** (`MongoRequisFact`) | ✅ GREEN | 1/1 | ✅ GREEN |
| 9 | [Chaque item du domaine survit au redémarrage (Mongo)](09-item-survit-redemarrage-mongo.md) | `@nominal` · backend **intégration Mongo réel** — boucle externe (4 adaptateurs) | ⏳ Pending | 0/4 | ⏳ Pending |

**Total** : 9 scénarios · **backend** : Sc.2 (4 unit `GrilleAgendaQuery`), Sc.3 (3 unit dont 1 driver + 2
caractérisations), Sc.8 (1 intégration Mongo réel), Sc.9 (4 intégration Mongo réel, 1 par item) · **🖥️
IHM → `ihm-builder`** : Sc.1, Sc.4, Sc.5, Sc.6, Sc.7 — **acceptation RUNTIME** (app réellement câblée),
inner-loop d'état de navigation/sélection listé à titre de boucle rapide, **jamais** une preuve
d'acceptation à lui seul.

**Statuts** : ⏳ Pending · 🔴 Red · ✅ Green.

**Légende routage** : `ihm-builder` = acceptation runtime / E2E sur l'app **réellement câblée** (front WASM
+ API distante + SignalR, DI réelle ; render mode interactif). Un scénario `🖥️` n'est **jamais** prouvé par
bUnit seul (render mode, DI réelle, transport HTTP). Sc.8 / Sc.9 = **intégration sur Mongo RÉEL** (Docker) —
`WebApplicationFactory<ApiProgram>` câblée `Foyer:Persistance=Mongo`, base isolée par exécution, `Skip`
propre si Docker absent (jamais un faux vert).

**Borne anti-cliquet (règle 30) — LEVÉE pour ce sprint (palier 14 absorbé).** La persistance n'est plus
bornée à la config foyer : **tout le domaine** (slots / périodes / transferts / cycle) passe durable Mongo.
La spec consolidée (`/5`) actera la levée ; jusque-là, le BACKLOG la séquençait en queue (palier 14),
désormais tirée dans ce sprint par révision PO hors process.

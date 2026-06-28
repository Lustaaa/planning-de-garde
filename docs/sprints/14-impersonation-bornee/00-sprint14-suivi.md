# Suivi Sprint 14 — Impersonation bornée · lecture seule (palier 8, tranche 2, épic É10 · `impersonation-bornee`)

> **Cadrage scaffolding (décidé CP — D1/D2/D3, `99-sprint14-retours.md`).** Le sujet ajoute une
> **impersonation bornée** : l'utilisateur principal (Parent configurateur) **incarne un acteur déjà
> déclaré** du foyer — **convenance d'administration, lecture seule**. La vue reflète le **rôle de
> l'acteur incarné** (bandeau « Vous incarnez X », menu d'écriture visible/masqué selon le type) ; le
> **retour à l'identité réelle** restaure l'état. **Pas d'écriture « au nom de »**, **zéro persistance
> neuve**, **borne dure** : ce n'est PAS l'authentification réelle du palier 16 (ni OAuth, ni comptes,
> ni sessions, ni prise en main, ni droits par rôle persistés).
>
> **Couche touchée — Web + extension du contrat de LECTURE du référentiel acteurs (PAS « Web only »).**
> Le scope est à **dominante front / session** (adaptateur de gauche `Web`), **MAIS** le pilotage de
> `EstParent` par le **type** de l'acteur incarné (règle 8) exige de **surfacer ce type**, qui n'existe
> aujourd'hui dans **aucun** contrat (référentiel id/nom/couleur seulement). **D3** tranche : le type est
> **surfacé via une EXTENSION READ-ONLY de l'énumération des acteurs** (`IEnumerationActeursFoyer` /
> projection de lecture `/api/foyer/acteurs` → DTO acteur), **type issu de la déclaration seed** du
> foyer ; les acteurs **ajoutés en session sont typés « Parent » par défaut** (aucune saisie de type).
> L'identité effective ne fait que **LIRE** ce type pour piloter `EstParent`. **Aucun port/handler
> d'écriture neuf, aucune persistance neuve, aucun recalcul métier neuf** — extension de read model, pas
> de write. (Scope déclaré ≠ « Web only » car la lecture du type traverse Application/Api/AdapterDroite.)
>
> **Cœur — extension de `SessionPlanning` (session, scoped circuit Blazor).** On distingue une
> **identité réelle** (fixe = le configurateur, type Parent) d'une **identité effective** (incarnée, ou
> **repli sur la réelle**), avec `Incarner(acteurId)` (lit le référentiel — refus silencieux si absent)
> et `RevenirIdentiteReelle()`. **`EstParent` dérive désormais de l'identité EFFECTIVE** : vrai si son
> type ∈ {Parent, Admin}, faux si Autre (règle 8). Le gating règle 9 **déjà câblé** sur la grille
> (`PlanningPartage` `@if Session.EstParent`, garde `OuvrirMenu`) et le gating du bouton supprimer config
> (s13) **lisent désormais l'identité effective sans changement** → **early-green** sur ces points ; le
> **neuf** est le **bandeau d'incarnation** + le **sélecteur d'incarnation** + le **durcissement** des
> écritures config non encore gatées (Sc.6).
>
> **CQRS — read vs write.** **READ seul** côté domaine (énumération + type, résolution de l'identité
> effective sur l'**identifiant stable** `acteur-…`, jamais le libellé — règles 5/19). **WRITE
> inchangé** : aucune commande/handler neuf ; les commandes (`PoserSlot`, `AffecterPeriode`,
> `DefinirTransfert`, `SupprimerActeur`) restent émises sous l'**identité réelle** (auteur inchangé) —
> l'impersonation **ne touche pas** le canal requête/réponse (Sc.4, early-green par construction).
>
> **Routage — TOUS les scénarios sont `🖥️ IHM` → `ihm-builder`, acceptation RUNTIME.** Le symptôme PO de
> chaque scénario est un **fait d'usage runtime** (bandeau affiché, menu clic-case visible/masqué, gating
> effectif d'un écran, retour auto sur diffusion temps réel) : render mode interactif, DI réelle,
> référentiel réel, SignalR réel. **bUnit seul ne prouve jamais** un tel symptôme (render mode, DI,
> transport, store) → acceptation sur l'app **réellement câblée** (front WASM + API distante + SignalR +
> Mongo réel). Les **tables de tests** des scénarios listent l'**inner-loop `SessionPlanning`** (POCO de
> session, logique pure, drivable en boucle rapide par `ihm-builder`) — l'**acceptation reste runtime**.
> Sc.5 et Sc.6 touchent diffusion temps réel / câblage d'écran → **acceptation runtime / G3** (D2),
> **pas** de filet automatisé flaky.
>
> **Cascade early-green (câblage IHM partagé).** Une fois Sc.1 posé (identité effective + `EstParent`
> dérivé + sélecteur + bandeau), plusieurs points tombent vert **par construction** : le **gating de la
> grille** (déjà `@if EstParent`) respecte l'incarnation (early-green côté menu masqué), le **bouton
> supprimer config** (déjà gaté s13) suit l'effective (early-green partiel Sc.6), et **Sc.4** (écriture
> sous identité réelle) est early-green car le canal d'écriture ne lit jamais l'effective. À **batcher**
> comme caractérisations, pas à traiter en early-green inattendu (round-trip CP évitable).
>
> **Borne anti-cliquet (règle 30).** État d'incarnation **mémoire / session uniquement**, **zéro
> persistance neuve** ; rien ne subsiste après redémarrage. Aucune persistance neuve tirée en avant.
>
> **Note IHM.** Un scénario `🖥️` n'est **jamais** prouvé par bUnit seul. Les `.razor` / le câblage
> SignalR figurent dans les « Fichiers à créer » car ces scénarios sont **routés vers `ihm-builder`**.

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Incarner un acteur déclaré : bandeau + vue selon son rôle](01-incarner-reflete-role.md) | `@nominal` 🖥️ IHM · driver · runtime `ihm-builder` | ✅ GREEN | 4/4 | ✅ GREEN |
| 2 | [Revenir à l'identité réelle : bandeau retiré, état restauré](02-revenir-identite-reelle.md) | `@nominal` 🖥️ IHM · driver · runtime `ihm-builder` | ⏳ Pending | 0/1 | ⏳ Pending |
| 3 | [Incarner un identifiant inconnu : refus, identité réelle conservée](03-incarner-acteur-inconnu-refus.md) | `@erreur` 🖥️ IHM · driver · runtime `ihm-builder` | ⏳ Pending | 0/1 | ⏳ Pending |
| 4 | [Pas d'écriture « au nom de » : l'écriture aboutit sous l'identité réelle](04-ecriture-sous-identite-reelle.md) | `@limite` 🖥️ IHM · caractérisation (⚠️ early green) · runtime `ihm-builder` | ⏳ Pending | 0/1 | ⏳ Pending |
| 5 | [Concurrence : l'acteur incarné est supprimé → retour auto à l'identité réelle](05-concurrence-suppression-retour-auto.md) | `@limite` 🖥️ IHM · driver · runtime / G3 (diffusion temps réel) `ihm-builder` | ⏳ Pending | 0/1 | ⏳ Pending |
| 6 | [Durcissement gating config : un « Autre » incarné masque toutes les écritures](06-durcissement-gating-config.md) | `@erreur` 🖥️ IHM · driver · runtime / G3 · **CUTTABLE** `ihm-builder` | ⏳ Pending | 0/1 | ⏳ Pending |

**Total** : 6 scénarios · **9 tests inner-loop `SessionPlanning`** (drivers : Sc.1 #2/#3, Sc.2, Sc.3,
Sc.5, Sc.6 ; caractérisations / early-green : Sc.1 #1 état initial + #4 Admin, Sc.4 écriture). **Tous
🖥️ IHM → `ihm-builder`** : l'**acceptation est RUNTIME** (app câblée réelle), hors compte d'un éventuel
filet bUnit. Sc.5 / Sc.6 = **acceptation runtime / G3** (diffusion temps réel / câblage d'écran, D2).
**Sc.6 CUTTABLE** (≤ ~2h une fois l'identité effective posée — D1 ; sinon coupé et re-séquencé sans
toucher au cœur Sc.1→Sc.5).

**Statuts** : ⏳ Pending · 🔴 Red · ✅ Green.

**Légende routage** : `ihm-builder` = acceptation runtime / E2E sur l'app **réellement câblée** (front
WASM + API distante + SignalR + Mongo réel, DI réelle, référentiel réel + type seed surfacé read-only) ;
l'**inner-loop `SessionPlanning`** (POCO de session, logique pure) est une boucle rapide pilotée par
`ihm-builder`, **jamais** une preuve d'acceptation à elle seule. Un scénario `🖥️` n'est **jamais**
prouvé par bUnit seul (render mode, DI réelle, SignalR, transport HTTP, store durable).

**Borne anti-cliquet (règle 30)** : l'état d'incarnation est **session / mémoire uniquement**, **zéro
persistance neuve** ; rien ne subsiste après redémarrage. La config foyer reste durable (Mongo, palier
5) ; aucune persistance neuve n'est tirée en avant.

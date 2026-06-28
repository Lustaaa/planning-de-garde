# Suivi — Sprint 12 · transfert en contexte (3ᵉ dialog, referme l'épic É12)

> **Cadrage scaffolding.** Reliquat du palier 7 « écriture en contexte » (spec v12,
> `docs/12-specification.md`) — **déplacement de la saisie du transfert là où on lit**, pas
> une mécanique métier neuve. **Couche unique touchée = Web (Blazor WASM).** `Domain` /
> `Application` / adaptateurs **inchangés** : **aucun handler neuf**. On réutilise la
> commande/handler **`DefinirTransfert`**, le **canal HTTP** (`POST /api/canal/definir-transfert`)
> et la **diffusion SignalR lecture seule** déjà livrés (s01→s05). CQRS préservé : **write**
> par canal requête/réponse, **read + diffusion** par query + SignalR — jamais confondus,
> **jamais d'écriture par la grille** (règle 14).
>
> **Contrôle de cohérence du scope (Web only, vérifié — pas de contradiction de slug).**
> Le « Web only » est **confirmé compatible avec chaque slug** : l'accusé **« Transfert
> défini »** est un **feedback transitoire** déclenché sur le **simple succès HTTP** du canal,
> il ne surface **aucun read model** ni DTO de réponse neuf (≠ s11 Sc.7 où le flag
> `Chevauchement` exigeait un contrat de réponse) ; le message de **refus** réutilise le
> **motif déjà renvoyé** par le canal (`DefinirTransfertCanalApiTests`, vert s01) ; la
> **relecture du store** au Sc.1 vit dans le **test d'acceptation runtime** (lecture directe
> du store réel via `DefinirTransfertCanalApiTests` étendu), **sans rendu grille ni endpoint
> de lecture neuf**. **Aucun slug n'exige de contrat de réponse d'API neuf** → le scope reste
> strictement **Web**.
>
> **Extraction en 3ᵉ dialog.** Le formulaire de `DefinirTransfert.razor` (dépose / récupère /
> lieu / date / heure) devient un **composant dialog (modal) réutilisable**
> (`DefinirTransfertDialog.razor`), déclenché depuis le planning. Les sélecteurs
> dépose/récupère bindent l'**identifiant stable** (`Parent A` / `Parent B`), **jamais** le
> libellé (cohérent avec la résolution couleur, règle 19). Les composants `PoserSlotDialog` /
> `AffecterPeriodeDialog` existent déjà (s11) : modèle à transposer.
>
> **Menu clic-case = 3ᵉ entrée.** Le menu d'actions ouvert au clic sur une case passe de 2 à
> **3 entrées** (Poser un slot / Affecter une période / **Définir un transfert**). Le
> **gating reste mutualisé** sur le **déclencheur unique** (`Session.EstParent`, règle 9) :
> ajouter une entrée ne change pas le point d'application du droit (acquis s11).
>
> **Ancrage date de contexte (règle 17).** La date de la **case cliquée prime** sur le défaut
> `IDateTimeProvider` « aujourd'hui ». La page actuelle pré-remplit `Horloge.Aujourdhui` en
> `OnInitialized` ; en contexte, la dialog **reçoit la date de la case en paramètre**. Le repli
> horloge **n'est pas supprimé** du port (garde-fou règle 17) ; il devient code mort tant que
> toute saisie passe par une case.
>
> **Rétroaction par issue** (grille **lecture seule**, règle 14) : **succès** → la dialog
> **se ferme** + accusé **« Transfert défini »** affiché **à part, non bloquant** (mécanisme du
> Sc.7 s11 réutilisé) ; **refus domaine OU API injoignable** (règle 28) → **un seul
> observable** : la dialog **reste ouverte**, message d'erreur **dans la dialog**, **saisie
> conservée** à resoumettre, grille **inchangée**.
>
> **Retrait du dernier écran dédié (referme É12).** Les routes/page/lien
> `/planning/definir-transfert` sont **retirés**, **uniquement APRÈS** que l'acceptation
> runtime de la dialog (Sc.1) prouve la **couverture intégrale** de l'écran supprimé (borne du
> Risque P1). À la livraison, **plus aucun écran de saisie dédié ne subsiste** → l'épic
> « écriture en contexte » est **refermé**.
>
> **Bornes anti-cliquet.** Le transfert **reste InMemory** (règle 30) ; grille **lecture
> seule** (règle 14, **annuler** n'émet aucune commande) ; **pas** de rendu du transfert en
> case ni d'amorce du **panneau cloche** (palier 14 / É9, **hors scope**). Ni auth ni
> impersonation tirées (paliers 9/16 intacts).
>
> **Axe backend vs IHM (routage) — les 6 scénarios sont 🖥️ IHM → `ihm-builder`.** Le
> comportement et le défaut **vivent dans le `.razor`** (3ᵉ entrée de menu au clic-case,
> ouverture/fermeture de dialog, pré-remplissage par la date de la case, accusé à part au
> succès, message d'erreur **dans** la dialog, gating Invité du déclencheur, absence de
> route/page/lien dédiés). **Aucun** scénario backend (aucun handler/règle neuve). Chaque
> scénario est piloté par un **test d'acceptation de NIVEAU RUNTIME** sur l'**app réellement
> câblée** (front WASM réel + API distante + store réel + SignalR), façon Sc.2 du s05
> (`FrontWasmApiDistanteTests` / `ApiDistanteFactory`) : **rempart anti vert-qui-ment** —
> prouver qu'un transfert **réellement enregistré** via la dialog est **relu depuis le store**
> (`DefinirTransfertCanalApiTests` étendu). Les **bUnit composant** listés par scénario sont
> des **drivers de détail** complémentaires (RED→GREEN sur le `.razor`, pilotés par
> `ihm-builder`) — **jamais** une preuve d'acceptation à eux seuls : bUnit force
> l'interactivité et ne prouve **jamais** un défaut runtime.
>
> **App = WASM standalone : pas de render mode à poser.** `App.razor` est WebAssembly
> standalone (s05) : tout est interactif côté navigateur, `@onclick`/`@bind` vivants par
> construction. Le neuf de ce sprint est le **câblage de la 3ᵉ entrée → dialog transfert**,
> l'**ancrage de la date de contexte**, l'**accusé succès à part**, l'**issue échec dans la
> dialog**, le **gating Invité** et le **retrait du dernier écran dédié**.
>
> **Cascade d'early-greens IHM (câblage partagé — anticipée).** Le **câblage unique** posé par
> le **driver Sc.1** (3ᵉ entrée de menu, ouverture/fermeture de dialog, issues
> succès/échec/accusé à part, paramètre de date de contexte, gating mutualisé `EstParent`)
> rend **Sc.2** (pré-remplissage date), **Sc.3** (échec, règle 28), **Sc.4** (gating Invité,
> règle 9) et **Sc.6** (annulation sans écriture, règle 14) **early-green par construction** :
> ce sont des **caractérisations** transposées de patterns déjà verts s11 (Sc.3/4/5/6 s11) et
> de l'invariant « transfert incomplet » (vert s01). Ils sont **batchables** en lot chez
> `ihm-builder` (filets anti-régression), **pas** des drivers. Seuls **Sc.1** (3ᵉ dialog +
> accusé succès) et **Sc.5** (retrait du dernier écran dédié) portent un **vrai cycle
> RED→GREEN**.
>
> **Caractérisations hors numérotation (filets déjà verts, non re-drivés).** Invariant domaine
> « transfert incomplet » (récupération/heure manquante → `TimeSpan.Zero`), vert s01
> (`Scenario11_DefinirTransfert`, `Scenario12_TransfertIncomplet`), couvert indirectement par
> les `Examples` du Sc.3 ; gating du menu lui-même (acquis s11) ; convergence temps réel sous
> dialog ouverte (acquis s10) ; édition concurrente du même jour sous dialog ouverte (**P3
> différée**, derrière stabilisation flakes SignalR P2, **hors scope**).

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Définir un transfert depuis une case via le menu clic-case](01-definir-transfert-depuis-case.md) | `@nominal 🖥️ IHM` | ✅ GREEN (RED→GREEN) | 0/2 | ✅ GREEN |
| 2 | [La dialog se pré-remplit sur la date de la case cliquée](02-pre-remplir-date-de-la-case.md) | `@limite 🖥️ IHM` | ✅ GREEN (caractérisation early-green) | 1/1 | ✅ GREEN |
| 3 | [Échec : la dialog reste ouverte et conserve la saisie](03-echec-dialog-reste-ouverte.md) | `@erreur 🖥️ IHM` | ✅ GREEN (caractérisation early-green) | 2/2 | ✅ GREEN |
| 4 | [Un Invité ne peut pas ouvrir le menu depuis une case](04-invite-ne-peut-pas-ouvrir-menu.md) | `@erreur 🖥️ IHM` | ✅ GREEN (caractérisation early-green) | 1/1 | ✅ GREEN |
| 5 | [La page de saisie dédiée n'existe plus](05-page-dediee-n-existe-plus.md) | `@limite 🖥️ IHM` | ✅ GREEN (RED→GREEN) | runtime (3 assertions) | ✅ GREEN |
| 6 | [Annuler la dialog n'émet aucune écriture](06-annuler-dialog-sans-ecrire.md) | `@limite 🖥️ IHM 🏷️ caractérisation` | ⏳ Pending | 0/1 | ⏳ Pending |

**Total** : 6 scénarios — **6 IHM/runtime** (`ihm-builder`, acceptation **E2E/runtime** sur
front WASM réel + API distante + store réel + SignalR), **0 backend** (aucun handler ni règle
neuve, reliquat palier 7 = déplacement de la saisie en contexte). **9 bUnit composant** au
total (drivers de détail, optionnels, pilotés par `ihm-builder` ; jamais preuve d'acceptation
seuls). **Drivers réels** = Sc.1 (3ᵉ dialog + accusé succès) et Sc.5 (retrait du dernier écran
dédié) ; **Sc.2 / Sc.3 / Sc.4 / Sc.6** prédits **early-green** (câblage IHM partagé +
invariants déjà verts s01/s11) → **batchables** en lot de caractérisations.

**Acceptation runtime IHM** : **5/6** (Sc.1 ✅ RED→GREEN — 3ᵉ dialog transfert + accusé « Transfert
défini » à part ; **Sc.2 / Sc.3 / Sc.4 ✅ caractérisations early-green confirmées en lot** — ancrage
date de contexte, issue d'échec règle 28 (2 Facts : refus domaine / API injoignable), gating Invité
règle 9 ; **Sc.5 ✅ RED→GREEN — retrait du dernier écran de saisie dédié (referme É12)** : page/route
`@page "/planning/definir-transfert"` + lien-barre `PlanningPartage` + entrée `NavMenu` retirés, seul
chemin restant = la dialog clic-case ; preuve runtime sur app câblée (absence de lien barre + NavMenu,
aucun `RouteAttribute` `/planning/definir-transfert` dans l'assembly Web) ; tests caducs des écrans
supprimés retirés (`DefinirTransfertTests`, `FrontWasmTransfertDateAujourdhuiTests`) ; tous prouvés au
**runtime** sur front WASM réel + API distante + store réel, témoins réels assertés (pas de faux-vert) ;
suite complète **181/181** verte, Docker actif). Reste : Sc.6 (caractérisation annulation).

> **Scaffolding requis (à créer par `ihm-builder`, hors périmètre de l'analyse)** :
> - **`DefinirTransfertDialog.razor` (+ code-behind)** — composant dialog (modal) réutilisable
>   extrait de `DefinirTransfert.razor`, prenant la **date de contexte** en paramètre et
>   exposant les issues succès/échec (callbacks `OnValide` / `OnAnnule`). Calqué sur
>   `PoserSlotDialog.razor` / `AffecterPeriodeDialog.razor` (s11).
> - **`PlanningPartage.razor` / `.razor.cs`** : **3ᵉ entrée** « Définir un transfert » au menu
>   clic-case, ouvrant la dialog pré-remplie sur la date de la case ; gestion d'état
>   d'ouverture ; **accusé « Transfert défini » à part, non bloquant** au succès (mécanisme
>   du bandeau s11 réutilisé) ; **gating Invité** du déclencheur (rendu conditionnel
>   réutilisant `SessionPlanning`).
> - **Suppression de la route/page/lien dédiés** `/planning/definir-transfert`
>   (`DefinirTransfert.razor` + code-behind) et de son lien (barre du planning + NavMenu),
>   **uniquement APRÈS** acceptation runtime Sc.1 verte (borne Risque P1). Referme É12.
> - **Aucun changement** dans `Domain`, `Application`, le handler `DefinirTransfert`, le canal
>   HTTP, la diffusion SignalR ni la persistance (transfert InMemory, config foyer Mongo).
> - **Tests** : `Web.Tests` (bUnit) pour ouverture de la 3ᵉ entrée, pré-remplissage par date
>   de case, accusé succès, message d'erreur **dans** la dialog, gating Invité, absence de
>   route/page/lien ; **acceptation runtime** sur app réellement câblée (réutiliser
>   `ApiDistanteFactory` / `ClientCanalEcriture.Construire` du s05, `DefinirTransfertCanalApiTests`
>   étendu pour relire le store).

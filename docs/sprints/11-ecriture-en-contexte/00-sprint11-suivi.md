# Suivi — Sprint 11 · écriture-en-contexte (dialogs depuis le planning)

> **Cadrage scaffolding.** Palier 7 « écriture en contexte » — **déplacement de la saisie
> là où on lit**, pas une mécanique métier neuve. **Couche unique touchée = Web (Blazor
> WASM).** `Domain` / `Application` / adaptateurs **inchangés** : **aucun handler neuf**. On
> réutilise les commandes/handlers `PoserSlot` et `AffecterPeriode`, le **canal HTTP**
> (`POST /api/canal/poser-slot` / `POST /api/canal/affecter-periode`) et la **diffusion
> SignalR lecture seule** déjà livrés (s04→s10). CQRS préservé : **write** par canal
> requête/réponse, **read + diffusion** par `GrilleAgendaQuery` + SignalR — jamais
> confondus, **jamais d'écriture par la grille** (règle 14).
>
> **Extraction en dialogs.** Les formulaires de `PoserSlot.razor` / `AffecterPeriode.razor`
> deviennent des **composants dialog (modal) réutilisables**, **déclenchés depuis
> `PlanningPartage.razor`**. Le **clic sur une case** (`data-testid="jour-case"`) ouvre la
> dialog correspondante, **pré-remplie sur la date de la case**. Les routes
> `/planning/poser-slot` et `/planning/affecter-periode` sont **retirées**, ainsi que leurs
> **liens-barre**. Le lien **Définir un transfert** reste tant que le transfert est en
> tranche de secours (hors numérotation).
>
> **Ancrage case + date de contexte.** La case fournit la **date pré-remplie**, qui
> **prime** sur le défaut `IDateTimeProvider` « aujourd'hui » (**règle 17 composée**, non
> révisée : le défaut nu ne vaut que **hors-contexte**). Pré-remplissage par **une** case ≠
> **sélection de plage** → hors scope.
>
> **Rétroaction par issue (3 observables, décision CP — cf. `99-sprint11-retours.md`).**
> **succès** → la dialog **se ferme**, la grille relue (retour commande / diffusion) ;
> **refus domaine OU API injoignable** (règle 28) → **un seul observable** : la dialog
> **reste ouverte**, message d'erreur **dans la dialog**, **saisie conservée**, grille
> **inchangée** ; **chevauchement** (règle 16) → écriture **aboutie** : la dialog **se
> ferme**, le slot **réapparaît**, l'avertissement s'affiche **à part** (toast/bandeau),
> **non bloquant**.
>
> **Droit Invité (rendu conditionnel IHM neuf).** Le déclencheur d'écriture **migre** de
> l'écran dédié **vers la case** : le **gater** en consultation seule (règle 9) réutilise le
> **contexte rôle existant** (`SessionPlanning`, acquis s01) — **ni auth ni impersonation**
> tirées (paliers 8/15 intacts).
>
> **Borne anti-cliquet.** Aucune persistance tirée en avant : **slots / périodes restent
> InMemory** (la config foyer reste Mongo, inchangée).
>
> **Axe backend vs IHM (routage) — les 7 scénarios sont 🖥️ IHM → `ihm-builder`.** Le
> comportement et le défaut **vivent dans le `.razor`** (ouverture/fermeture de dialog au
> clic, pré-remplissage par la date de la case, message d'erreur **dans** la dialog, rendu
> conditionnel Invité du déclencheur). **Aucun** scénario backend (aucun handler/règle
> neuve). Chaque scénario est piloté par un **test d'acceptation de NIVEAU RUNTIME** sur
> l'**app réellement câblée** (front WASM réel + API distante + store réel + SignalR), comme
> le Sc.2 du sprint 05 (`FrontWasmApiDistanteTests` / `ApiDistanteFactory`) : **rempart
> anti vert-qui-ment** — prouver qu'une saisie **réellement enregistrée** réapparaît
> **positionnée, colorée et nommée** à la **date de la case**. Les **bUnit composant** listés
> par scénario sont des **drivers de détail** complémentaires (RED→GREEN sur le `.razor`,
> pilotés par `ihm-builder`) — **jamais** une preuve d'acceptation à eux seuls.
>
> **App = WASM standalone : pas de render mode à poser.** `App.razor` est WebAssembly
> standalone (s05) : tout est interactif côté navigateur, `@onclick`/`@bind` vivants par
> construction. Le défaut runtime de ce sprint n'est **pas** un render mode manquant mais le
> **câblage du clic-case → dialog**, l'**ancrage de la date de contexte**, l'**issue dans la
> dialog** et le **gating Invité du déclencheur**.
>
> **Tranche de secours (hors numérotation).** **3ᵉ dialog — Définir un transfert** depuis une
> case : livrée **si le scope ~2h tient**, sinon séquencée juste derrière (jamais reportée en
> bloc). **Édition concurrente du même jour** sous dialog ouverte : **hors scope**, candidat
> séquençable. **Caractérisations déjà vertes, non re-drivées** : convergence temps réel sous
> dialog ouverte (dernière écriture gagne, règle 11, acquis s10) ; validation domaine
> sous-jacente (durée nulle, lieu inexistant, responsable requis, vertes s01) — couvertes
> indirectement par le Sc.4.

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Poser un slot depuis une case ouvre la dialog et le slot réapparaît](01-poser-slot-depuis-case.md) | `@nominal 🖥️ IHM` | ✅ GREEN | 0/2 | ✅ GREEN |
| 2 | [Affecter une période depuis une case colore et nomme la case](02-affecter-periode-depuis-case.md) | `@nominal 🖥️ IHM` | ✅ GREEN | 0/2 | ✅ GREEN |
| 3 | [La dialog se pré-remplit sur la date de la case cliquée](03-pre-remplir-date-de-la-case.md) | `@limite 🖥️ IHM` | ✅ GREEN (caractérisation) | 0/2 | ✅ GREEN |
| 4 | [Échec clair : la dialog reste ouverte et conserve la saisie](04-echec-clair-dialog-reste-ouverte.md) | `@erreur 🖥️ IHM` | ✅ GREEN (caractérisation) | 2/3 | ✅ GREEN |
| 5 | [Annuler la dialog ne modifie pas le planning](05-annuler-dialog-sans-ecrire.md) | `@limite 🖥️ IHM` | ⏳ Pending | 0/2 | ⏳ Pending |
| 6 | [Un Invité ne peut pas ouvrir la dialog depuis une case](06-invite-ne-peut-pas-ouvrir-dialog.md) | `@erreur 🖥️ IHM` | ⏳ Pending | 0/2 | ⏳ Pending |
| 7 | [Slot chevauchant accepté avec avertissement non bloquant](07-chevauchement-accepte-averti.md) | `@limite 🖥️ IHM` | ⏳ Pending | 0/2 | ⏳ Pending |

**Avancement** : **4/7** scénarios au vert — Sc.1 (poser un slot) + Sc.2 (affecter une période)
livrés depuis une case ; Sc.3 (ancrage date, règle 17) et Sc.4 (échec clair, règle 28) actés
**caractérisations early-green** (design acquis aux fix Sc.1/Sc.2, filets anti-régression conservés —
dont le cas API injoignable sur transport réellement coupé). Décision CP appliquée : **un menu
d'actions au clic-case** (deux entrées « Poser un slot » / « Affecter une période »), mutualisant le
gating Invité (Sc.6).

**Acceptation runtime IHM** : **4/7** (Sc.1 ✅, Sc.2 ✅, Sc.3 ✅ caractérisation, Sc.4 ✅ caractérisation).

**Total** : 7 scénarios — **7 IHM/runtime** (`ihm-builder`, acceptation **E2E/runtime** sur
front WASM réel + API distante + store réel + SignalR), **0 backend** (aucun handler ni
règle neuve, palier 7 = déplacement de la saisie en contexte). Le Sc.7 est une
**caractérisation** (règle 16 « accepté + averti » déjà verte s01) dont seul l'**habillage
IHM** est neuf (fermeture + avertissement à part non bloquant). Le détail RED→GREEN
(`PlanningPartage.razor`, composants dialog, suppression des routes/liens) est piloté par
`ihm-builder` ; les `~15` bUnit composant listés par scénario sont des **drivers de détail**
complémentaires, **jamais** preuve d'acceptation à eux seuls.

> **Scaffolding requis (à créer par `ihm-builder`, hors périmètre de l'analyse)** :
> - **2 composants dialog (modal) réutilisables** extraits des formulaires existants —
>   p.ex. `PoserSlotDialog.razor` / `AffecterPeriodeDialog.razor` (+ code-behind), prenant
>   en **paramètre la date de contexte** (case cliquée) et exposant les issues
>   succès/échec/avertissement (callbacks `OnValide` / `OnAnnule`).
> - **`PlanningPartage.razor` / `.razor.cs`** : clic sur `data-testid="jour-case"` ouvre la
>   dialog correspondante pré-remplie sur la date de la case ; gestion d'état d'ouverture ;
>   **gating Invité du déclencheur** (rendu conditionnel réutilisant `SessionPlanning`) ;
>   bandeau/toast d'avertissement de chevauchement **à part** (non bloquant) ; suppression
>   des **liens-barre** « Poser un slot » / « Affecter une période ».
> - **Suppression des routes/pages dédiées** `/planning/poser-slot` et
>   `/planning/affecter-periode` (`PoserSlot.razor` / `AffecterPeriode.razor` + code-behind),
>   le lien/route **Définir un transfert** restant tant que le transfert est en tranche de
>   secours.
> - **Aucun changement** dans `Domain`, `Application`, les handlers, le canal HTTP, la
>   projection `GrilleAgendaQuery`, la diffusion SignalR ni la persistance (slots/périodes
>   InMemory, config foyer Mongo).
> - **Tests** : `Web.Tests` (bUnit) pour ouverture/fermeture, pré-remplissage par date de
>   case, message d'erreur **dans** la dialog, rendu conditionnel Invité ; **acceptation
>   runtime** sur app réellement câblée (réutiliser `ApiDistanteFactory` /
>   `ClientCanalEcriture.Construire` du s05).

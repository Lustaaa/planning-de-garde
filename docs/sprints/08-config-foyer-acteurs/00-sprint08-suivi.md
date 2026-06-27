# Suivi Sprint 08 — Config foyer · édition volatile des acteurs (noms + couleurs)

> **Cadrage scaffolding (décidé CP — store mutable singleton derrière les ports existants).**
> Le sujet rend **éditables** (volatile, en session) le **nom** et la **couleur** des acteurs
> déjà semés, sans toucher au domaine ni au read model. L'identifiant stable ne change
> **jamais** ; la grille (case + légende) relit la config sur l'id stable (règle 18), via le
> read model `GrilleAgendaQuery` **INCHANGÉ**.
> - **Store mutable singleton (Infrastructure)** — remplace les dictionnaires
>   `static readonly` de `Foyer` lus par `FoyerReferentielResponsables` (noms) /
>   `FoyerPaletteCouleurs` (couleurs) par un **store en mémoire seedé à l'init depuis
>   `Foyer`**, puis **éditable** (renommer / recolorier sur l'id stable). Réalise les ports
>   **lecture** `IReferentielResponsables.NomDe` / `IPaletteCouleurs.CouleurDe` (contrat
>   inchangé) et expose des opérations d'**écriture** (renommer / recolorier) derrière un
>   **port d'écriture Application** (forme laissée à `tdd-auto`). Testable sans framework.
>   **Volatile = re-seedé à la (re)construction** (redémarrage serveur → seed d'origine).
> - **Commande `EditerActeur` + handler (Application)** — `{ acteurId (stable, doit
>   exister), nom?, couleur? }`. Validation **légère** : id stable connu, **nom non vide**
>   (vide / tout-espaces refusé, ancienne valeur conservée). Mute le store via le port
>   d'écriture, puis **déclenche la diffusion temps réel** (`INotificateurPlanning`) sur
>   **édition aboutie uniquement** — jamais d'écriture par le canal de diffusion.
> - **Canal d'écriture HTTP (adaptateur de gauche, Api)** + **client front (Web)** —
>   l'écran de config émet la commande à distance (règle 27) ; aucune vue n'écrit le domaine
>   en direct. Échec transport (API injoignable) = surfacé à l'écran, édition non appliquée.
> - **Read model / légende INCHANGÉS** — `GrilleAgendaQuery` relit nom + couleur côte à
>   côte sur l'id stable ; légende **dédoublonnée par id**, **présents dans la fenêtre**
>   (s07). La case/légende « suivent » l'édition par **re-projection** (caractérisation des
>   s07 Sc.1/2/3), pas par un nouveau calcul.
>
> **Routage backend (`tdd-auto`) vs IHM/runtime (`ihm-builder`) — axe explicite.** Le symptôme
> PO de chaque scénario nominal est un **fait d'usage runtime** (« sans recharger, la case
> suit ») : l'**acceptation** est **runtime/E2E sur l'app réellement câblée** (front WASM +
> API distante + store réel + SignalR — rempart anti « vert qui ment », **jamais bUnit seul**
> pour un fait runtime), routée `ihm-builder`. Mais la **tranche store + commande/handler**
> porte des **cycles unitaires backend** menables par `tdd-auto`.
> - **Drivers backend réels** : Sc.1 (store `renommer` + handler + diffusion sur succès),
>   Sc.2 (store `recolorier` + handler), Sc.8 (refus nom vide / tout-espaces, ancien nom
>   conservé).
> - **Caractérisations backend** (filet anti-régression, ⚠️ early green **attendu**, **pas**
>   driver) : Sc.4 (collision couleur — légende dédoublonnée par id, s07 Sc.2), Sc.5 (hors-set
>   neutre conservé — nom/couleur indépendants, s07 Sc.5), Sc.6 (hors fenêtre — pas d'entrée
>   fantôme, légende-présents s07 Sc.3), Sc.7 (dernière-écriture-gagne — `renommer` écrase,
>   pas de version), Sc.10 (re-seed à la (re)construction — seed-at-init du store).
> - **Drivers IHM/runtime** (`ihm-builder`) : Sc.1/2 case+légende suivent **sans
>   rechargement** (SignalR), Sc.3 **troncature + survol** d'un nom **édité** long (réutilise
>   le composant livré s07 Sc.6), Sc.7 **convergence des deux grilles** par diffusion, Sc.8
>   **message clair** à l'écran, Sc.9 **API injoignable** (échec clair, édition non appliquée,
>   à resoumettre).
>
> **Note couleur (palier 2, settled).** Les libellés couleur des Gherkin sont illustratifs ;
> les tests **backend** injectent le set librement, l'acceptation **runtime** asserte le set
> réel (`parent-a → bleu`, `parent-b → orange`). L'édition de couleur ne fait que **muter**
> la valeur résolue ; la légende/case **surfacent** la couleur déjà résolue.
>
> **Note IHM hors périmètre backend.** Aucun `.razor` ni câblage SignalR réel dans les
> « Fichiers à créer » des scénarios **backend** ; la notification temps réel se vérifie en
> backend par un **Spy** sur `INotificateurPlanning`. Le rendu / l'interactivité / la
> convergence live relèvent d'`ihm-builder`.

| # | Scénario | Tag | Acceptation (runtime IHM) | Tests backend | Statut |
|---|----------|-----|---------------------------|---------------|--------|
| 1 | [Renommer un acteur : la case et la légende suivent](01-renommer-acteur-case-et-legende-suivent.md) | `@nominal` 🖥️ IHM · backend `tdd-auto` + runtime `ihm-builder` | ✅ GREEN (runtime) | 3/3 | ✅ GREEN — backend (3/3) + runtime IHM `@vert` |
| 2 | [Recolorier un acteur : la case et la légende changent de couleur](02-recolorier-acteur-case-et-legende.md) | `@nominal` 🖥️ IHM · backend `tdd-auto` + runtime `ihm-builder` | ✅ GREEN (runtime) | 3/3 | ✅ GREEN — backend (3/3) + runtime IHM `@vert` |
| 3 | [Renommer vers un nom long : troncature, survol, légende complète](03-nom-long-troncature-survol.md) | `@limite` 🖥️ IHM · driver runtime `ihm-builder` (backend néant) | ✅ GREEN (caractérisation runtime) | 0/0 | ✅ GREEN — runtime IHM `@vert` (caract., réutilise s07 Sc.6) |
| 4 | [Collision de couleur : distingués par le nom](04-collision-couleur-distingues-par-nom.md) | `@limite` 🖥️ IHM · caract. `tdd-auto` + runtime `ihm-builder` | ✅ GREEN (runtime) | 1/1 | ✅ GREEN — backend caract. (1/1) + runtime IHM `@vert` |
| 5 | [Éditer un acteur hors set : nom suivi, teinte neutre conservée](05-acteur-hors-set-neutre-conservee.md) | `@limite` 🖥️ IHM · caract. `tdd-auto` + runtime `ihm-builder` | ✅ GREEN (runtime) | 1/1 | ✅ GREEN — backend caract. (1/1) + runtime IHM `@vert` |
| 6 | [Éditer un acteur hors fenêtre : pas d'entrée fantôme](06-acteur-hors-fenetre-pas-d-entree-fantome.md) | `@limite` 🖥️ IHM · caract. `tdd-auto` + runtime `ihm-builder` | ⏳ Pending | 0/1 | ⏳ Pending |
| 7 | [Deux écrans renomment : dernière écriture gagne, grilles convergent](07-deux-ecrans-derniere-ecriture-gagne.md) | `@limite` 🖥️ IHM · caract. `tdd-auto` + driver runtime `ihm-builder` | ⏳ Pending | 0/1 | ⏳ Pending |
| 8 | [Renommer avec un nom vide : édition refusée, ancien nom conservé](08-nom-vide-edition-refusee.md) | `@erreur` 🖥️ IHM · backend `tdd-auto` + runtime `ihm-builder` | ⏳ Pending | 0/3 | ⏳ Pending |
| 9 | [API distante injoignable : échec clair, édition non appliquée](09-api-injoignable-echec-clair.md) | `@erreur` 🖥️ IHM · driver runtime `ihm-builder` (backend néant) | ⏳ Pending | 0/0 | ⏳ Pending |
| 10 | [Volatilité : après redémarrage, le seed d'origine réapparaît](10-volatilite-reseed-au-redemarrage.md) | `@limite` · caract. `tdd-auto` (store re-seed) | ⏳ Pending | 0/1 | ⏳ Pending |

**Total** : 10 scénarios · **14 tests unitaires backend** (≈ 7 drivers réels : Sc.1×3,
Sc.2×2, Sc.8×2 ; ≈ 7 caractérisations early-green : Sc.2×1, Sc.4, Sc.5, Sc.6, Sc.7, Sc.8×1,
Sc.10) · **8 acceptations runtime IHM** (Sc.1–9 ; Sc.10 = store-level). 2 scénarios sans
backend (Sc.3, Sc.9 — 100 % runtime IHM).

**Acceptation runtime IHM** : **3/8** (Sc.1 ✅ — `FrontWasmConfigRenommerActeurTempsReelTests` ;
Sc.2 ✅ — `FrontWasmConfigRecolorierActeurTempsReelTests` ; Sc.3 ✅ caractérisation —
`FrontWasmConfigNomLongEditeTempsReelTests` ; Sc.4 ✅ —
`FrontWasmConfigCollisionCouleurTempsReelTests` ; Sc.5 ✅ —
`FrontWasmConfigHorsSetNeutreTempsReelTests`).

**Statuts** : ⏳ Pending · 🔴 Red · ✅ Green.

**Légende routage** : `tdd-auto` = cycles unitaires backend (store mutable, commande/handler
`EditerActeur`, diffusion par Spy) ; `ihm-builder` = acceptation runtime/E2E sur l'app
réellement câblée (écran de config, case + légende qui suivent sans rechargement,
convergence SignalR, messages d'échec). Un scénario `🖥️ IHM` n'est **jamais** prouvé par
bUnit seul (render mode, DI réelle, SignalR, transport HTTP).

# Suivi Sprint 07 — Lisibilité & thème (nom + légende)

> **Cadrage scaffolding (décidé CP — Option 1, port nom miroir palette).**
> Le sujet enrichit le **read model** de la grille (CQRS, lecture seule, règle 12 —
> domaine inchangé) du **nom** du responsable et d'une **légende** dérivée, par-dessus
> la couleur déjà résolue au palier 2.
> - **Port Application `IReferentielResponsables`** (nouveau) — `NomDe(identifiantStable)
>   → nom humain`, **miroir exact** d'`IPaletteCouleurs.CouleurDe`. Implémentation
>   Infrastructure `FoyerReferentielResponsables` (lit un nouveau référentiel `Foyer`),
>   double `FakeReferentielResponsables` côté tests. Aucune persistance construite
>   (référentiel semé, résolu en lecture — même dette que `IPaletteCouleurs`, palier 5).
> - **Read model enrichi** — `JourCase` gagne le **nom** (et l'identifiant stable) du
>   responsable d'une case couverte ; `GrilleAgenda` gagne une **`Légende`**
>   (`EntreeLegende { identifiantStable, nom, couleur }`) **dérivée** des responsables
>   présents dans la fenêtre, **dédoublonnée** par identifiant stable, **vide** si aucune
>   période. La projection résout **nom + couleur** côte à côte sur l'identifiant stable
>   (règle 17 : jamais sur le libellé — l'anti-pattern libellé-comme-identité corrigé au
>   s06 Sc.8 n'est PAS rejoué).
> - **Référentiel `Foyer` réel** — adopte les noms des scénarios (`parent-a` → « Alice »,
>   `parent-b` → « Bruno ») et **déclare l'acteur hors-set « grand-père »** (identifiant
>   stable valide, **absent du set couleur** → repli neutre, **nom conservé**).
>
> **Routage backend vs IHM (axe explicite).** Tous les scénarios sont **🖥️ IHM** au niveau
> **acceptation** (rendu sur grille **réellement câblée** : front WASM + API distante +
> palette/référentiel réels — rempart anti « vert qui ment », jamais bUnit à doublure pour
> un fait runtime). Mais la **tranche read-model enrichi** porte des **cycles unitaires
> backend** menables par **`tdd-auto`** sur `GrilleAgendaQuery` (référentiel + palette
> injectés). Le rendu (nom dans la case, composant Légende, masquage, troncature) et le
> **suivi temps réel** relèvent d'**`ihm-builder`** (runtime).
> - **Drivers backend réels** : Sc.1 (nom + légende-présence), Sc.2 (légende **dédoublonnée**).
> - **Caractérisations backend** (filet anti-régression, ⚠️ early green attendu, **pas**
>   driver) : Sc.3 (légende vide), Sc.5 (gris assumé), Sc.6 (nom complet porté).
> - **Drivers IHM/runtime** (ihm-builder) : Sc.3 **masquage** du bloc légende, Sc.4 **ajout
>   vivant** temps réel sans rechargement, Sc.6 **troncature + survol**.
>
> **Note couleur (palier 2, settled).** Les libellés couleur des `Given`/`Then` Gherkin
> sont **illustratifs** (ex. « Bruno (Parent B, vert) » alors que la palette réelle mappe
> `parent-b → orange`). Les tests **backend** injectent la palette librement (couleurs du
> Gherkin) ; l'acceptation **runtime** asserte la **palette réelle** (`parent-a → bleu`,
> `parent-b → orange`). La légende ne fait que **surfacer** la couleur déjà résolue.
>
> **Thème métier (règle 20)** — ergonomie de surface (CSS), aucun observable testable :
> **hors scénario**, validation visuelle au gate (cf. analyse technique du sprint).

| # | Scénario | Tag | Acceptation (runtime IHM) | Tests backend | Statut |
|---|----------|-----|---------------------------|---------------|--------|
| 1 | [Période affectée : nom + entrée de légende](01-periode-affectee-nom-et-legende.md) | `@nominal` 🖥️ IHM · backend `tdd-auto` + runtime `ihm-builder` | ✅ Green (runtime) | 2/2 ✅ backend | ✅ GREEN |
| 2 | [Plusieurs responsables : légende dédoublonnée](02-legende-dedoublonnee.md) | `@nominal` 🖥️ IHM · backend `tdd-auto` + runtime `ihm-builder` | ✅ Green (runtime ; caract., early-green attendu) | 1/1 ✅ backend (caract. ; driver #1 retiré PO/G4) | ✅ GREEN |
| 3 | [Fenêtre sans affectation : légende masquée](03-fenetre-vide-legende-masquee.md) | `@limite` 🖥️ IHM · driver masquage `ihm-builder` (+ caract. `tdd-auto`) | ✅ Green (runtime ; driver masquage RED→GREEN) | 1/1 ✅ backend (caract.) | ✅ GREEN |
| 4 | [Ajout vivant par diffusion temps réel](04-ajout-vivant-temps-reel.md) | `@limite` 🖥️ IHM · driver runtime `ihm-builder` (backend néant) | ⏳ Pending | 0/0 | ⏳ Pending |
| 5 | [Acteur hors set : gris assumé, nom conservé](05-acteur-hors-set-gris-assume.md) | `@limite` 🖥️ IHM · caract. `tdd-auto` + runtime `ihm-builder` | ⏳ Pending (ihm-builder) | 1/1 ✅ backend (caract.) | 🔴 RED (accept. IHM) |
| 6 | [Nom long : lisibilité de la case préservée](06-nom-long-lisible.md) | `@limite` 🖥️ IHM · driver troncature `ihm-builder` (+ caract. `tdd-auto`) | ⏳ Pending (ihm-builder) | 1/1 ✅ backend (caract.) | 🔴 RED (driver troncature IHM) |

**Total** : 6 scénarios · 7 tests unitaires backend (3 drivers réels Sc.1×2 + Sc.2×1 ;
4 caractérisations early-green Sc.2×1, Sc.3, Sc.5, Sc.6) · 6 acceptations runtime IHM.

**Acceptation runtime IHM** : **3/6 ✅** (Sc.1, Sc.2, Sc.3).

**Statuts** : ⏳ Pending · 🔴 Red · ✅ Green.

**Légende routage** : `tdd-auto` = cycles unitaires backend sur la projection
(`GrilleAgendaQuery`, référentiel + palette doublés) ; `ihm-builder` = acceptation
runtime/E2E sur l'app réellement câblée (rendu nom, composant Légende, masquage,
troncature, suivi SignalR). Un scénario `🖥️ IHM` n'est **jamais** prouvé par bUnit seul.

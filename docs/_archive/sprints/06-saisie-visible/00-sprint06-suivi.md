# Suivi — Sprint 06 · saisie-visible

> **Cadrage scaffolding.** Palier 2 « saisie visible » (règles 15/16/17). Les deux défauts
> vivent dans les **adaptateurs de gauche** (vues WASM + seed de l'API), **jamais** dans le
> domaine ni dans la lecture CQRS. La projection `GrilleAgendaQuery`
> (`CouleurResponsableAu → IPaletteCouleurs.CouleurDe`) est **correcte et inchangée** ; le set
> `CouleursParActeur` (`parent-a→bleu`, `parent-b→orange`, `nounou→vert`, neutre `gris`) ne
> bouge pas.
>
> **(A) Date par défaut = aujourd'hui.** Scaffolding à créer : une abstraction injectable
> **`IDateTimeProvider`** (Application) exposant `Today`, et son implémentation système
> (Infrastructure). Les trois `.razor.cs` (`PoserSlot`, `AffecterPeriode`, `DefinirTransfert`
> — cette dernière en **dette template** à passer en code-behind) pré-remplissent leurs dates
> depuis ce port, **jamais** `DateTime.Today` en dur ; les dates figées `new(2025,…)` sont
> supprimées. Le double de test **fixe** la date (`2026-06-26`), symétrie avec
> `Projeter(dateReference)` côté lecture.
>
> **(B) Couleur par identifiant stable.** Le défaut est à la **source** : `Foyer.Responsables`
> (Web + Infra) et le seed (`SeedDonneesDemo`) exposent les **libellés** « Parent A » / « Parent
> B » ; les sélecteurs bindent `value="@r"` → le canal reçoit `ResponsableId = "Parent A"`, clé
> absente de `CouleursParActeur` → repli gris. **Correction : la source fournit l'identifiant
> stable** (`parent-a` / `parent-b`) — sélecteur **affichant le libellé mais bindant l'id**, et
> seed semant l'id — pour rendre le set atteignable. `DefinirTransfert` corrige aussi ses
> sélecteurs dépose/récupère vers l'id stable **par cohérence, sans observable couleur** (aucun
> transfert projeté à ce palier).
>
> **Axe backend vs IHM (routage).** Les deux défauts sont des **faits d'usage runtime** (« la
> saisie n'apparaît pas / la case retombe au gris »), localisés dans les `.razor` (date portée
> par le formulaire, libellé bindé par le sélecteur) et le seed. Scénarios **🖥️ IHM / runtime →
> `ihm-builder`** : **Sc.1, Sc.2, Sc.3, Sc.6, Sc.8**. Leur acceptation est un **test de NIVEAU
> RUNTIME sur l'app réellement câblée** (front WASM réel + API distante, DI réelle, comme le Sc.2
> du sprint 05) : la saisie émise **sans toucher aux dates** doit réapparaître **à la date du
> jour injectée** dans la grille projetée, et l'affectation doit colorer la case **à la couleur
> du parent**. **JAMAIS** un bUnit composant à doublures comme preuve d'acceptation : un bUnit
> force l'interactivité et stub le transport — il **mentirait au vert** sur un `IDateTimeProvider`
> non injecté, un sélecteur bindant encore le libellé, ou un seed semant le mauvais identifiant.
>
> **Scénarios backend = caractérisation (déjà vert).** **Sc.4, Sc.5, Sc.7** sont **backend** →
> `tdd-auto`, mais la règle qu'ils exercent (fenêtre **35 jours** depuis le lundi, **exclusion
> hors fenêtre**, **repli gris** d'un id absent du set) est **déjà verte** au niveau
> `GrilleAgendaQuery` (sprint 03 : `Scenario_GrilleStructure5Semaines`,
> `Scenario_SlotHorsFenetreExclu`, `Scenario_CouleurResponsableCaseJour`). Ce sprint **ne
> rouvre aucune règle de projection** : ces scénarios sont des **caractérisations** /
> **diagnostics** du défaut d'adaptateur (Sc.5 illustre que la date figée 2025 tombe **hors
> fenêtre**, donc invisible — c'est la justification métier de (A) ; Sc.7 documente le **gris
> assumé** distinct du **gris-bug** du Sc.8). Aucun nouveau driver backend.
>
> **Garde-fous.** *Vert qui ment* : les scénarios IHM exigent des slots/périodes **réellement
> enregistrés** via le canal et **relus par la grille** (runtime WASM + API distante), pas une
> grille statique. *Gris assumé ≠ gris-bug* (Sc.7 vs Sc.8) : le repli neutre d'un acteur
> **légitimement hors set** (règle 17) est conforme ; distinct du gris provoqué par un **libellé
> fourni à la place de l'identifiant**. *Déterminisme* : `IDateTimeProvider` est **doublé** en
> test (date fixée), `Projeter(dateReference)` injecté côté lecture — **jamais** `DateTime.Today`
> / `DateTime.Now` en dur.
>
> **App = WASM standalone : pas de render mode à poser.** `App.razor` est WebAssembly standalone
> (sprint 05) : tout est interactif côté navigateur, `@onclick`/`@bind` vivants par construction.
> Le défaut runtime de ce sprint **n'est pas** un render mode manquant mais une **valeur de
> formulaire figée** (A) et une **source de libellé** (B).

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Slot posé sans toucher aux dates réapparaît à aujourd'hui](archive/01-slot-pose-reapparait-aujourdhui.md) | `@nominal 🖥️ IHM` | ✅ Passing | 1/1 | ✅ Vert |
| 2 | [Période affectée sans toucher aux dates tombe dans la fenêtre](archive/02-periode-affectee-visible-aujourdhui.md) | `@nominal 🖥️ IHM` | ✅ Passing | 1/1 | ✅ Vert |
| 3 | [Transfert défini sans toucher à la date prend aujourd'hui](archive/03-transfert-defini-date-aujourdhui.md) | `@nominal 🖥️ IHM` | ✅ Passing | 1/1 | ✅ Vert |
| 4 | [Saisie à la borne haute de la fenêtre reste visible](archive/04-borne-haute-fenetre-visible.md) | `@limite` | ✅ GREEN | 2/2 | ✅ GREEN |
| 5 | [Date figée hors fenêtre fait disparaître la saisie](archive/05-date-figee-hors-fenetre-disparait.md) | `@erreur` | ✅ GREEN | 1/1 | ✅ GREEN |
| 6 | [Période affectée à un parent se colore à sa couleur](archive/06-periode-parent-coloree.md) | `@nominal 🖥️ IHM` | ✅ Passing | 1/1 | ✅ Vert |
| 7 | [Acteur hors set retombe sur le neutre (gris assumé)](archive/07-acteur-hors-set-gris-neutre.md) | `@limite` | ✅ GREEN | 1/1 | ✅ GREEN |
| 8 | [Libellé fourni à la place de l'identifiant fait retomber sur gris](archive/08-libelle-au-lieu-identifiant-gris.md) | `@erreur 🖥️ IHM` | ✅ Passing | 1/1 | ✅ Vert |

**Avancement** : **8/8** scénarios au vert ✅ — sprint 06 complet (Sc.1 ✅, Sc.2 ✅, Sc.3 ✅, Sc.4 ✅, Sc.5 ✅, Sc.6 ✅, Sc.7 ✅, Sc.8 ✅).

**Total** : 8 scénarios — **5 IHM/runtime** (`ihm-builder`, acceptation **E2E/runtime** sur
front WASM réel + API distante : Sc.1, Sc.2, Sc.3, Sc.6, Sc.8) · **3 backend** (`tdd-auto`,
acceptation **projection / intégration** : Sc.4, Sc.5, Sc.7). Les **4 tests backend** sont des
**caractérisations** d'invariants de `GrilleAgendaQuery` **déjà verts** (sprint 03) — filet de
non-régression et diagnostic du défaut d'adaptateur, **aucun nouveau driver de règle**. Le
détail RED→GREEN des scénarios IHM (`.razor`/`.razor.cs`, `IDateTimeProvider`, sélecteurs
bindant l'id, seed) est piloté par `ihm-builder`.

> **Scaffolding requis (à créer par `ihm-builder` / `tdd-auto`, hors périmètre de l'analyse)** :
> - **Port `IDateTimeProvider`** (`src/PlanningDeGarde.Application/IDateTimeProvider.cs`, expose
>   `DateTime Today` / `DateOnly Aujourdhui`) + **implémentation système** en Infrastructure +
>   **enregistrement DI** côté front WASM. Double de test fixant la date (`2026-06-26`).
> - **Trois `.razor.cs` câblés** sur ce port (`PoserSlot`, `AffecterPeriode`, `DefinirTransfert`
>   passé en **code-behind** — la dette template de `DefinirTransfert` est levée) : suppression
>   des dates figées `new(2025,…)`, pré-remplissage depuis `Today`.
> - **Source → identifiant stable** : `Foyer.Responsables` (Web + Infra) et les sélecteurs
>   `AffecterPeriode` / `DefinirTransfert` exposent une **paire (id stable, libellé)** — option
>   affichant le libellé, `value` = id stable (`parent-a` / `parent-b`) ; `SeedDonneesDemo`
>   sème l'**id stable**, pas le libellé.
> - **Aucun changement** dans `GrilleAgendaQuery`, `IPaletteCouleurs`, `FoyerPaletteCouleurs`,
>   `CouleursParActeur` ni dans le domaine.

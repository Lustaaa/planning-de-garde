# Scénario 6 — Période affectée à un parent se colore à sa couleur

`@nominal` `@vert` · 🖥️ **scénario IHM** — **Routé vers `ihm-builder`**

[← Retour au suivi](00-sprint06-suivi.md)

> **Axe : IHM / runtime.** Le défaut vit à la **source** : le sélecteur `AffecterPeriode` bind
> `value="@r"` où `r` est le **libellé** « Parent A » (`Foyer.Responsables`), et le seed sème ce
> libellé. Le canal reçoit donc `ResponsableId = "Parent A"`, **clé absente** de
> `CouleursParActeur` → repli **gris** au lieu de la couleur du parent. Le symptôme PO est un
> **fait d'usage runtime** (« j'affecte une période à Parent A, la case devrait être bleue »). La
> correction = **la source fournit l'identifiant stable** (`parent-a`/`parent-b`) : sélecteur
> **affichant le libellé mais bindant l'id**, et **seed semant l'id**. **JAMAIS** un bUnit à
> doublures comme preuve : un bUnit stub le transport et ne verrait pas que le canal réel reçoit
> encore le libellé.
>
> **Niveau d'acceptation : E2E / runtime** sur l'app réellement câblée (front WASM réel émettant
> vers l'API distante avec sa **source réelle de responsables**, palette réelle, projection réelle
> `GrilleAgendaQuery`). RED→GREEN piloté par `ihm-builder`.

## Acceptation (BDD)

`Should_Colorer_en_bleu_les_cases_de_Parent_A_et_en_orange_celles_de_Parent_B_When_le_front_WASM_affecte_des_periodes_via_l_API_distante_avec_l_identifiant_stable_du_responsable` — ✅ Passing
(`tests/PlanningDeGarde.Web.Tests/FrontWasmPeriodeParentColoreeTests.cs`)

**Test de NIVEAU RUNTIME** sur l'app réellement câblée (front WASM réel + API distante, palette
réelle `parent-a→bleu` / `parent-b→orange`) :
- **Given** le set de couleurs associe **parent-a au bleu** et **parent-b à l'orange** ; le
  sélecteur d'affectation du front **fournit l'identifiant stable** du responsable (libellé
  affiché, id bindé) ; une période est affectée au responsable « Parent A » **du 24 au 27/06/2026**
  et une autre à « Parent B » **du 28 au 30/06/2026**, émises via le canal vers l'API distante ;
- **When** la grille est projetée à la semaine du lundi 22/06/2026 ;
- **Then** les **cases du 24 au 27/06/2026 sont bleues** ; **et** les **cases du 28 au 30/06/2026
  sont orange** — les affectations ayant réellement transité par le canal (avec l'id stable)
  jusqu'au store relu par la projection + palette réelles.

> Discriminance du rouge : si la source bind encore le libellé « Parent A », le canal reçoit
> « Parent A » → repli gris → cases non bleues → rouge. Un bUnit à doublures (transport stubé) ne
> verrait jamais que le canal reçoit le mauvais identifiant.

## Tests

> Détail RED→GREEN piloté par `ihm-builder` (source `Foyer.Responsables` exposant la paire (id,
> libellé), sélecteur `AffecterPeriode` bindant l'id, `SeedDonneesDemo` semant l'id stable).
> Boucle externe = acceptation runtime ci-dessus. **Aucune table de tests unitaires backend** :
> la résolution `IPaletteCouleurs.CouleurDe("parent-a") = "bleu"` et la coloration des cases sont
> **déjà vertes** (`Scenario_CouleurResponsableCaseJour`, `AffecterPeriodeCanalApiTests`) ; ce
> scénario prouve que la **source fournit enfin l'identifiant stable atteignable** par le set.

## Fichiers à créer / modifier

- **`src/PlanningDeGarde.Web/Foyer.cs`** et **`src/PlanningDeGarde.Infrastructure/Foyer.cs`** —
  `Responsables` expose une **paire (id stable, libellé)** (`parent-a`/« Parent A »,
  `parent-b`/« Parent B »).
- **`src/PlanningDeGarde.Web/Components/Pages/AffecterPeriode.razor`** — option `value` = **id
  stable**, texte = libellé.
- **`src/PlanningDeGarde.Api/SeedDonneesDemo.cs`** — sème l'**identifiant stable** (`parent-a` /
  `parent-b`), pas le libellé.

## Design notes

- **Anti « vert qui ment »** : app réellement câblée (source réelle, palette réelle, projection
  réelle) ; échoue comme l'utilisateur la voit si le sélecteur/seed bind encore le libellé. Pas
  de bUnit à doublures comme preuve d'acceptation.
- **Projection inchangée** : `GrilleAgendaQuery.CouleurResponsableAu → IPaletteCouleurs.CouleurDe`
  et `CouleursParActeur` ne bougent pas — le set devient simplement **atteignable**.
- **Cohérence sélecteurs** : les bUnit composant existants (`AffecterPeriodeTests`) assertent
  aujourd'hui le **libellé** comme `value`/corps — ils devront refléter l'**id stable** après
  correction (mise à jour pilotée par `ihm-builder`, pas un nouveau driver métier).
- **Distinct du Sc.8** : ici la source est **corrigée** (id stable) → couleur ; le Sc.8 documente
  le **gris-bug** quand le libellé est (encore) fourni.

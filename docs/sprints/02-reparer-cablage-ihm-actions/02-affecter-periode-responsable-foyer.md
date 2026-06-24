# Scénario 2 — Affecter une période avec un responsable du foyer `@nominal`

> **État acceptation : ✅ GREEN** — les deux drivers (peuplement + enregistrement)
> sont passés sans rouge (⚠️ early green) : le composant `AffecterPeriode.razor`
> était déjà entièrement câblé. Tests conservés comme filet de non-régression sur le
> peuplement du sélecteur et l'enregistrement effectif des valeurs métier.

[← Retour au suivi](00-suivi.md)

> **Acceptation (BDD)** —
> `Should_Afficher_la_periode_Parent_A_responsable_du_14_07_au_21_07_dans_la_responsabilite_du_planning_When_un_parent_choisit_un_responsable_du_foyer_et_valide`
> Composant bUnit : rendre `AffecterPeriode` sur un `InMemoryPeriodeRepository`
> partagé, sélectionner « Parent A » dans le sélecteur peuplé depuis
> `Foyer.Responsables`, saisir 14-07 → 21-07, soumettre, constater l'absence de
> `[data-testid=motif-echec]` ; la période (Parent A / 14-07 → 21-07) est
> **enregistrée dans le dépôt** (donc affichable dans la section Responsabilité du
> planning rendu sur le même dépôt).

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Proposer_les_responsables_du_foyer_Parent_A_et_Parent_B_When_un_parent_ouvre_la_dialog_d_affectation` | nil → constante (peuplement du sélecteur) | Driver : un sélecteur de responsable vide / non peuplé depuis `Foyer.Responsables` ne présente aucune option choisissable ; force le `@foreach (Foyer.Responsables)` | ⚠️ EARLY GREEN |
| 2 | `Should_Enregistrer_la_periode_Parent_A_responsable_du_14_07_au_21_07_When_un_parent_choisit_Parent_A_et_valide_du_14_07_au_21_07` | inconnu → constante (lecture du dépôt) | Driver : un câblage qui ne transmet pas le `ResponsableId`/bornes sélectionnés ou n'appelle pas `AffecterPeriodeHandler.Handle` laisse le dépôt vide ; force le binding + l'enregistrement | ⚠️ EARLY GREEN |

## Fichiers à créer / modifier

- `tests/PlanningDeGarde.Web.Tests/AffecterPeriodeTests.cs` — **nouveau fichier**
  (aucun test `AffecterPeriode` n'existe). Câbler les ports `IPeriodeRepository` →
  `InMemoryPeriodeRepository`, `IResponsableRepository` → `FoyerResponsableRepository`,
  `SessionPlanning`, et `AffecterPeriodeHandler`. Assertion #2 sur
  `InMemoryPeriodeRepository.AllSnapshots()` (valeurs métier exactes).

## Design notes

- **Scénario le plus susceptible de produire un vrai rouge** : aucun test bUnit ne
  couvre encore le câblage d'`AffecterPeriode`. Le test #1 (peuplement) et le test #2
  (enregistrement) sont des drivers réels.
- Le composant `AffecterPeriode.razor` est déjà câblé (sélecteur peuplé depuis
  `Foyer.Responsables`, binding `_form.ResponsableId`, appel
  `AffecterPeriodeHandler.Handle`) → les tests peuvent passer dès le 1er run
  (caractérisation du câblage existant) ; si l'un est rouge, c'est un vrai défaut de
  câblage à corriger (production hors périmètre de cet agent — signaler à `tdd-auto`).
- `AffecterPeriodeHandler` n'a **pas** de port notificateur (pas de Spy à brancher).
- Pas de test d'erreur « responsable requis » (déjà vert sprint 1, scénario 8 archivé).

# Scénario 4 — Modifier une période depuis le bouton Modifier `@nominal`

> **État acceptation : ✅ GREEN** — driver #1 (pré-remplissage du formulaire inline)
> passé en ⚠️ early green inattendu : le bloc d'édition inline (`OuvrirEditionPeriode`)
> était déjà câblé. Confirmé vrai filet par mutation (neutraliser le pré-remplissage du
> responsable fait virer le test rouge). Test #2 RETIRÉ sur décision PO : doublon strict
> du test existant `Modifier_une_periode_sur_un_etat_a_jour_remplace_le_responsable`
> (non compté). Compte effectif : 1/1.

[← Retour au suivi](00-suivi.md)

> **Acceptation (BDD)** —
> `Should_Afficher_la_periode_Parent_B_responsable_du_14_07_au_21_07_When_un_parent_ouvre_le_formulaire_inline_pre_rempli_choisit_Parent_B_et_enregistre`
> Composant bUnit : rendre `PlanningPartage` avec une période « Parent A du 14-07 au
> 21-07 » dans un `InMemoryPeriodeRepository` partagé, cliquer « Modifier » (ouverture
> du formulaire inline pré-rempli), choisir « Parent B », « Enregistrer », constater
> l'absence de `[data-testid=motif-edition-periode]` ; la période enregistrée devient
> « Parent B / 14-07 → 21-07 ».

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Pre_remplir_le_formulaire_inline_avec_le_responsable_et_les_bornes_actuels_When_un_parent_clique_Modifier_sur_une_periode` | nil → constante (ouverture + valeurs initiales) | Driver : un `@onclick="Modifier"` non câblé n'ouvre pas le formulaire inline / ne le pré-remplit pas avec la base observée (responsable et bornes courants) ; force l'ouverture inline + le pré-remplissage (jeton optimiste) | ⚠️ EARLY GREEN |
| ~~2~~ | ~~`Should_Remplacer_le_responsable_par_Parent_B_…`~~ | — | ❌ RETIRÉ (décision PO) : doublon strict du test existant `Modifier_une_periode_sur_un_etat_a_jour_remplace_le_responsable` — non ajouté, non compté | ❌ RETIRÉ |

## Fichiers à créer / modifier

- `tests/PlanningDeGarde.Web.Tests/PlanningPartageTests.cs` — ajouter le test driver #1
  (pré-remplissage du formulaire inline à l'ouverture : asserter que le `select`
  inline affiche le responsable courant et les bornes courantes). Le test #2 existe
  déjà (caractérisation) — ne pas dupliquer.

## Design notes

- Le bloc d'édition inline (`PlanningPartage.razor` / `.razor.cs`) est déjà câblé :
  `@onclick="OuvrirEditionPeriode"` pré-remplit `_editResponsableId`/`_editDebut`/
  `_editFin` depuis la base observée ; « Enregistrer » appelle
  `ModifierPeriodeHandler.Handle(ModifierPeriodeCommand(base observée, modification))`.
  Les tests sont des caractérisations de non-régression du câblage existant.
- Doubler **uniquement** les ports : `IPeriodeRepository` → `InMemoryPeriodeRepository`
  (seedé via `PeriodeDeGarde.Affecter`), plus les read models / handlers requis par le
  rendu de `PlanningPartage` (`JourneeEnfantQuery`, `ResponsabiliteQuery`,
  `DeplacerSlotHandler`, `ModifierPeriodeHandler`), `SessionPlanning` (Parent).
- **Pas** de réécriture du rejet d'écriture périmée (déjà vert sprint 1, scénario 10 ;
  test existant `Modifier_depuis_un_etat_perime_…`). La base observée sert de jeton
  optimiste mais ce scénario ne teste que le chemin nominal (état à jour).
- Aucun port notificateur à brancher pour ce scénario (la modification de période
  passe par `ModifierPeriodeHandler`, sans Spy temps réel).

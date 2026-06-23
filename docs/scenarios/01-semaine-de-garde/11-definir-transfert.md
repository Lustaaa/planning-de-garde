# Scénario 11 — Définir le transfert de bascule entre deux parents `@nominal`

> Suivi : [suivi.md](suivi.md) · Source : `docs/scenarios/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_afficher_le_transfert_dans_le_planning_partage_et_faire_basculer_la_responsabilite_au_point_de_transfert_When_un_Parent_definit_un_transfert_complet_entre_deux_periodes_contigues` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_confirmer_la_definition_du_transfert_When_un_Parent_definit_un_transfert_complet | nil → constant (2) | Baseline du nouvel agrégat transfert : succès toujours-réussir d'une définition complète | ⏳ Pending |
| 2 | Should_exposer_le_deposant_le_recuperant_le_lieu_l_heure_et_la_date_du_transfert_When_le_transfert_a_ete_defini | constant → scalar (3) | Contredit le succès vide : le transfert reflète dépose/récupère/lieu/heure/date fournis (snapshot) | ⏳ Pending |
| 3 | Should_afficher_le_transfert_dans_le_planning_partage_du_foyer_When_le_transfert_a_ete_defini | constant → scalar (3) | Contredit l'agrégat isolé : le transfert persisté apparaît dans le planning | ⏳ Pending |
| 4 | Should_faire_basculer_la_responsabilite_du_deposant_au_recuperant_au_point_de_transfert_When_le_transfert_borne_deux_periodes_contigues | conditional (4) | Contredit une responsabilité statique : au point de transfert la responsabilité passe de A à B (bascule observable) | ⏳ Pending |

**Fichiers à créer** : `src/PlanningDeGarde.Domain/Transfert.cs`, `src/PlanningDeGarde.Application/DefinirTransfertHandler.cs`, `src/PlanningDeGarde.Application/ITransfertRepository.cs`, `tests/PlanningDeGarde.Tests/Scenario11_DefinirTransfert.cs`, `tests/PlanningDeGarde.Tests/Fakes/FakeTransfertRepository.cs`, `tests/PlanningDeGarde.Tests/Builders/TransfertBuilder.cs`
**Design notes** :
- `Transfert { déposeParId, récupèreParId, lieuId, heure, date }` = point de bascule A↔B ; invariant : dépose + récupère + lieu + heure tous renseignés.
- Bascule de responsabilité (#4) observable au point de transfert : le transfert borne deux périodes contiguës (la cohérence trou/chevauchement entre périodes reste hors socle — cf. questions ouvertes).

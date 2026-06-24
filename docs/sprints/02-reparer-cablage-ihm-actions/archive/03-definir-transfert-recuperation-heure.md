# Scénario 3 — Définir un transfert avec récupération et heure transmises `@nominal`

> **État acceptation : ✅ GREEN** — les 2 drivers (absence d'échec + enregistrement)
> passent sans rouge comportemental (⚠️ early green inattendu, conservés sur décision
> PO comme filets de non-régression). Le câblage de `DefinirTransfert.razor` était déjà
> complet (sélecteurs peuplés, conversion `TimeOnly` 08:30 → `TimeSpan`, binding des 4
> valeurs). Le seul rouge rencontré était un artefact d'infrastructure bUnit (`FindAll`
> capturé une fois → handlers `onchange` invalidés par le re-render des `InputSelect`),
> corrigé en re-issuant `FindAll` avant chaque `Change` — pas un défaut de production.

[← Retour au suivi](00-sprint02-suivi.md)

> **Acceptation (BDD)** —
> `Should_Afficher_le_transfert_depose_Parent_A_recupere_Parent_B_ecole_08h_le_21_07_dans_les_transferts_du_planning_When_un_parent_renseigne_recuperation_lieu_et_heure_et_valide`
> Composant bUnit : rendre `DefinirTransfert` sur un `InMemoryTransfertRepository`
> partagé, choisir dépose « Parent A », récupère « Parent B », lieu « école »,
> 21-07 à 08:30, soumettre, constater l'absence de `[data-testid=motif-echec]` ;
> le transfert est **enregistré dans le dépôt** (donc affichable dans la section
> Transferts du planning rendu sur le même dépôt).

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Ne_pas_afficher_de_message_d_echec_When_un_parent_renseigne_la_recuperation_le_lieu_et_l_heure_et_valide` | nil → constante (absence d'effet d'échec) | Driver : le sprint 1 ne couvre que le **transfert incomplet** (`@erreur`) ; un câblage qui ne transmet pas la récupération **et** l'heure retombe sur le motif « Transfert incomplet » → ce test exige que la branche succès soit atteinte (heure ≠ `TimeSpan.Zero`, récupération renseignée) | ⚠️ EARLY GREEN |
| 2 | `Should_Enregistrer_le_transfert_depose_Parent_A_recupere_Parent_B_ecole_a_08h30_le_21_07_When_un_parent_renseigne_recuperation_lieu_et_heure_et_valide` | inconnu → constante (lecture du dépôt) | Driver : un câblage qui perd l'heure (`input type=time` → `TimeOnly` → `TimeSpan`) ou un des Id enregistre un transfert incohérent / vide ; force la conversion d'heure + le binding des 4 valeurs | ⚠️ EARLY GREEN |

## Fichiers à créer / modifier

- `tests/PlanningDeGarde.Web.Tests/DefinirTransfertTests.cs` — ajouter les deux tests
  nominaux (le fichier existe mais ne couvre que le transfert incomplet). Renseigner
  les sélecteurs (dépose/récupère/lieu) + l'`input type=time` (08:30). Assertion #2
  sur `InMemoryTransfertRepository.AllSnapshots()` (`Heure == 08:30`, Ids et lieu
  exacts).

## Design notes

- Point d'attention métier (analyse technique) : `input type=time` produit un
  `TimeOnly` 08:30 → converti en `TimeSpan` avant la commande ; le test #2 doit
  asserter que l'heure **08:30 est conservée** (régression typique du câblage).
- Doubler **uniquement** `ITransfertRepository` → `InMemoryTransfertRepository` +
  `SessionPlanning` + `DefinirTransfertHandler`. `DefinirTransfertHandler` n'a pas de
  port notificateur.
- Pas de test d'erreur « transfert incomplet » supplémentaire (déjà vert sprint 1,
  scénario 12 archivé ; le test existant `Un_transfert_incomplet_…` le couvre).
- Le composant `DefinirTransfert.razor` est déjà câblé → tests = caractérisation du
  câblage existant ; un rouge signalerait un vrai défaut de transmission.

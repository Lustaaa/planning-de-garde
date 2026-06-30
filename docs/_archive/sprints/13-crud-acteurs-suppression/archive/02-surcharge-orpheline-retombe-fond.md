# Sc.2 — Surcharge orpheline : la case retombe sur le fond (le cycle reprend) `@limite` `@driver`

← [Retour au suivi](00-sprint13-suivi.md)

> **Backend `tdd-auto`** (frontière Application — projection `GrilleAgendaQuery`). Vrai RED neuf :
> une surcharge (période saisie) pointant un acteur **supprimé** doit **cesser de primer** et laisser
> la case retomber sur le **responsable de fond**.

## Acceptation (BDD)

`Acceptation_Should_Faire_retomber_la_case_sur_le_responsable_de_fond_When_l_acteur_d_une_periode_saisie_est_supprime`
— à la frontière Application : foyer (Parent A, Nounou), cycle de fond N=2 mappant index 0 et 1 sur
Parent A, période saisie attribuant le mardi 16/06/2026 à Nounou (surcharge). Après suppression de
Nounou, la projection `GrilleAgendaQuery.Projeter` de la case du 16/06 affiche **Parent A** et **sa
couleur de fond** (la surcharge orpheline ne prime plus).

**✅ GREEN** — acceptation (store réel + handlers ajout/suppression + query) et driver verts ; suite complète 186/186 (Docker actif). Filtre d'existence sur la surcharge via `IEnumerationActeursFoyer` injecté en option (null → pas de filtrage), appliqué **avant** le `?? fond`.

## Tests unitaires (ordonnés)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Faire_retomber_la_case_sur_le_responsable_de_fond_avec_sa_couleur_When_l_acteur_d_une_surcharge_ponctuelle_est_supprime` | (constant → conditionnel) ajout d'une condition d'existence | **Driver** : aujourd'hui `CaseJourAu` prend `periode?.ResponsableId ?? fond` **sans vérifier l'existence** de l'acteur — après suppression, la case afficherait l'**id brut** de Nounou (nom fantôme) au lieu du fond. Force un **filtre d'existence sur la surcharge** : une surcharge orpheline est ignorée **avant** le repli `?? fond`, de sorte que la case retombe sur Parent A. | ✅ GREEN |

## Fichiers à créer

- Modification de `src/PlanningDeGarde.Application/Classes/GrilleAgendaQuery.cs` (filtre d'existence sur la surcharge dans `CaseJourAu`)
- `tests/PlanningDeGarde.Tests/Scenario2_SurchargeOrphelineRetombeFond.cs`
- Fake de lecture d'existence d'acteurs pour la query (cf. design notes)

## Design notes

- **Contrat d'existence (point d'attention).** La query doit distinguer un acteur **existant** d'un
  **id orphelin**. Le contrat naturel est `IEnumerationActeursFoyer.EnumererActeurs()` (déjà réalisé
  par `ConfigurationFoyerEnMemoire` / `ConfigurationFoyerMongo`), injecté dans `GrilleAgendaQuery` ;
  un contrat d'existence dédié (`ActeurExiste(id)`) est une alternative. **Forme laissée à `tdd-auto`.
  → remonter au CP si le choix du contrat d'existence est ambigu.**
- **Ordre du filtre** : il doit s'appliquer à la surcharge **avant** le `?? fond`. Filtrer le
  `responsableId` combiné serait un **faux raccourci** (une surcharge orpheline retomberait sur le
  neutre au lieu du fond) — ce piège est verrouillé par ce test.
- `FakeConfigurationFoyer` / les fakes de la query devront réaliser l'énumération d'acteurs pour que
  le test pose un acteur « supprimé » (absent de l'énumération) tout en gardant la période qui le
  référence.
- **Caractérisation déjà verte préservée** : `Scenario2_SurchargePrimeSurFond` (s10) — une surcharge
  vers un acteur **existant** continue de primer (ne pas régresser).

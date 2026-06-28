# Sc.1 — Supprimer un acteur autorisé le retire de la configuration persistée `@nominal` `@driver`

← [Retour au suivi](00-sprint13-suivi.md)

> **Backend `tdd-auto`** (frontière Application, store/fake mémoire) **+ acceptation = intégration
> sur Mongo RÉEL (Docker)**. Premier RED neuf du sprint : faire **exister** le retrait d'acteur
> (commande + handler + port `Supprimer` + retrait dans les adaptateurs droite).

## Acceptation (BDD)

`Acceptation_Should_Ne_plus_lister_Nounou_dans_le_store_relu_et_apres_redemarrage_tout_en_conservant_Parent_A_et_Parent_B_When_un_parent_supprime_Nounou_par_son_identifiant_stable`
— **intégration sur Mongo réel** (`MongoRequisFact`, skip propre si Docker absent, base isolée par
`Guid`) : un parent supprime « Nounou » via `POST /api/canal/supprimer-acteur` → succès ; une
**instance d'hôte fraîche** (= redémarrage) câblée sur la **même base Mongo** n'énumère plus Nounou,
tandis que « Parent A » et « Parent B » sont toujours présents. Preuve la plus forte (anti
vert-qui-ment R4) ; une lecture via port sur instance fraîche est acceptable en complément.

**✅ GREEN** — `SupprimerActeurMongoIntegrationTests` vert sur Mongo réel (Docker actif) ; suite complète 184/184.

## Tests unitaires (ordonnés)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Retirer_l_acteur_du_referentiel_relu_tout_en_conservant_les_autres_acteurs_When_un_parent_supprime_un_acteur_autorise_par_son_identifiant_stable` | nil → invocation de port neuf (statement) | **Driver** : aucune commande/handler/port `Supprimer` n'existe — le store ne sait qu'`Ajouter` / `Renommer` / `Recolorier`. Force la création de `SupprimerActeurCommand` + `SupprimerActeurHandler` + `IEditeurConfigurationFoyer.Supprimer(acteurId)`, qui retire l'entrée **nom ET couleur** du store, de sorte que la configuration relue n'énumère plus Nounou (et ne le résout plus) alors que Parent A / Parent B restent. | ✅ GREEN |

## Fichiers à créer

- `src/PlanningDeGarde.Application/Classes/SupprimerActeurHandler.cs` (commande + résultat `Result` + handler ; diffusion temps réel sur succès via `INotificateurPlanning`)
- Méthode `Supprimer(string acteurId)` ajoutée à `src/PlanningDeGarde.Application/Interfaces/IEditeurConfigurationFoyer.cs`
- Réalisations : `ConfigurationFoyerEnMemoire.Supprimer` (retrait `_noms` + `_couleurs`) et `ConfigurationFoyerMongo.Supprimer` (retrait write-through du document)
- `tests/PlanningDeGarde.Tests/Scenario1_SupprimerActeur.cs` (driver, frontière Application avec `FakeConfigurationFoyer` doté du retrait)
- `tests/PlanningDeGarde.Api.Tests/SupprimerActeurMongoIntegrationTests.cs` (acceptation Mongo réel + endpoint canal)

## Design notes

- **Retrait des deux surfaces** : `Supprimer` retire **nom ET couleur** (miroir d'`Ajouter`). Après
  retrait, `NomDe(id)` retombe sur l'**id brut** et `CouleurDe(id)` sur le neutre par contrat — c'est
  précisément ce repli qui devient un **nom fantôme** si une surcharge/un fond pointe encore l'acteur
  (traité aux Sc.2/3/4). Ici on assure seulement que l'acteur **n'est plus énuméré**.
- **Idempotence** déléguée à `Dictionary.Remove` (no-op sur clé absente) côté InMemory et à un retrait
  Mongo tolérant à l'absence — exercée explicitement au **Sc.5** (caractérisation).
- **CQRS** : l'écriture passe par le canal requête/réponse ; la diffusion SignalR est lecture seule.
  Le handler déclenche `INotificateurPlanning` sur succès (vérifié par **Spy** en backend, pas par
  SignalR réel — celui-ci relève du Sc.9 / `ihm-builder`).
- **`FakeConfigurationFoyer`** doit recevoir la réalisation de `Supprimer` (retrait des deux
  dictionnaires) pour servir le driver à la frontière Application.
- L'**identifiant stable opaque** (`acteur-…`) est la clé de suppression — **jamais le libellé**.

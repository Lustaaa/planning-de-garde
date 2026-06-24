# Scénario 1 — Poser un slot avec un lieu du foyer `@nominal`

> **État acceptation : ✅ GREEN** — driver #2 passé sans rouge (⚠️ early green) :
> la production était déjà câblée (cf. cadrage `00-suivi.md`), le test sert de filet
> de non-régression sur l'enregistrement effectif des valeurs métier.

[← Retour au suivi](00-suivi.md)

> **Acceptation (BDD)** —
> `Should_Afficher_le_slot_de_Lea_a_l_ecole_le_15_07_de_08h30_a_16h30_dans_la_localisation_du_planning_When_un_parent_choisit_un_lieu_du_foyer_et_valide`
> Composant bUnit : rendre `PoserSlot` sur un `InMemorySlotRepository` partagé,
> sélectionner « école » dans le sélecteur peuplé depuis `Foyer.Lieux`, soumettre,
 constater l'absence de `[data-testid=motif-echec]` ; le slot
> (Léa / école / 15-07 08:30 → 16:30) est **enregistré dans le dépôt** (donc
> affichable dans la section Localisation du planning rendu sur le même dépôt).

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Ne_pas_afficher_de_message_d_echec_et_notifier_le_planning_When_un_parent_pose_un_slot_a_un_lieu_du_foyer` | nil → constante (présence/absence d'effet) | ⚠️ probablement early green — couvert par le test existant `Un_parent_pose_un_slot_valide_…` (caractérisation : use case appelé + Spy notifié, pas driver) | ✅ GREEN (caractérisation) |
| 2 | `Should_Enregistrer_le_slot_de_Lea_a_l_ecole_le_15_07_de_08h30_a_16h30_When_un_parent_choisit_le_lieu_ecole_du_sapeur_de_lieux_et_valide` | inconnu → constante (lecture du dépôt) | Driver : un câblage qui n'enregistre pas le slot avec les valeurs concrètes sélectionnées (lieu « école », bornes 08:30→16:30) laisse le dépôt vide / incohérent ; force le binding `LieuId`/`Debut`/`Fin` + l'appel `Enregistrer` du handler | ⚠️ EARLY GREEN |

## Fichiers à créer / modifier

- `tests/PlanningDeGarde.Web.Tests/PoserSlotTests.cs` — ajouter le test driver #2
  (assertion sur `InMemorySlotRepository.AllSnapshots()`, valeurs métier exactes).
  Le test #1 existe déjà (caractérisation) — ne pas dupliquer s'il couvre déjà
  l'absence de motif + Spy notifié.

## Design notes

- **Aucune production attendue** côté domaine/application : handlers et composant
  `PoserSlot.razor` déjà câblés (sélecteur peuplé depuis `Foyer.Lieux`, binding
  `@bind-Value="_form.LieuId"`, appel `PoserSlotHandler.Handle`). Les tests sont des
  **caractérisations de non-régression** du câblage.
- Doubler **uniquement** les ports : `ISlotRepository` → `InMemorySlotRepository`,
  `ILieuRepository` → `FoyerLieuRepository`, `INotificateurPlanning` →
  `FakeNotificateurPlanning` (Spy). La notification temps réel se vérifie par le Spy
  (`Notifications == 1`), jamais par un hub SignalR vivant.
- Pas de test d'erreur « lieu inexistant » (déjà vert sprint 1, scénario 4 archivé).
- L'IHM (composant Blazor) est hors périmètre de production de cet agent ; ici on ne
  fait qu'ajouter un test de caractérisation du câblage existant.

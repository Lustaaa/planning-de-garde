# Suivi TDD — Réparer le câblage IHM des actions d'écriture

> **Cadrage scaffolding** — Solution .NET 10 existante (`PlanningDeGarde.sln`).
> Projet de tests cible : `tests/PlanningDeGarde.Web.Tests` (bUnit 1.40, xUnit) — déjà
> présent. **Aucun nouveau projet à créer.** Référentiel `Foyer` (static :
> `Lieux` = école / domicile A / domicile B / nounou ; `Responsables` = Parent A /
> Parent B) et ports `ILieuRepository` / `IResponsableRepository` (Fakes :
> `FoyerLieuRepository` / `FoyerResponsableRepository`) déjà câblés.
> Port temps réel doublé par `FakeNotificateurPlanning` (Spy `Notifications`)
> déjà présent dans `Web.Tests`. **Aucun composant Blazor ni câblage SignalR n'est
> produit par cet agent** : le câblage est testé en composant bUnit ; la
> notification temps réel se vérifie par le Spy sur le port.
>
> **Type de test dominant** : composant bUnit (acceptation = soumettre la dialog →
> l'état métier est enregistré dans le dépôt partagé + absence de
> `[data-testid=motif-echec]`). **Aucun test d'erreur sur les gardes métier**
> (lieu inexistant / responsable requis / transfert incomplet / écriture périmée) :
> déjà verts au sprint 1 → réécrits ici, ce seraient des *early green*
> (caractérisation, pas driver).
>
> **État de départ constaté (branche `ia-fix`)** : la production (composants +
> handlers + DI) est déjà câblée et plusieurs tests existent déjà. Conséquence : les
> tests *« le use case est appelé »* sont des **early green** ; le **driver** restant
> de chaque scénario est l'**assertion d'acceptation** — l'entité créée est
> réellement **enregistrée dans le dépôt partagé** (et donc affichable dans la
> section correspondante du planning). Voir doublons signalés ci-dessous.

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Poser un slot avec un lieu du foyer](01-poser-slot-lieu-foyer.md) | `@nominal` | ✅ GREEN | 2/2 | ✅ GREEN |
| 2 | [Affecter une période avec un responsable du foyer](02-affecter-periode-responsable-foyer.md) | `@nominal` | ✅ GREEN | 2/2 | ✅ GREEN |
| 3 | [Définir un transfert avec récupération et heure](03-definir-transfert-recuperation-heure.md) | `@nominal` | ✅ GREEN | 2/2 | ✅ GREEN |
| 4 | [Modifier une période depuis le bouton Modifier](04-modifier-periode-bouton.md) | `@nominal` | ⏳ Pending | 0/2 | ⏳ Pending |

## Doublons / early green anticipés

- **Sc.1** — `PoserSlotTests.Un_parent_pose_un_slot_valide_le_use_case_est_appele_et_notifie`
  existe déjà (assertion = use case appelé + Spy notifié). Le test #1 ci-dessous est
  une **caractérisation** de ce comportement (probable early green). Le **driver** est
  le test #2 (slot concret enregistré dans le dépôt, valeurs métier exactes).
- **Sc.2** — **aucun** test `AffecterPeriode` n'existe → c'est le scénario avec le
  plus de chances de produire un vrai rouge (test de peuplement du sélecteur de
  responsable + enregistrement). Création probable d'un `AffecterPeriodeTests.cs`.
- **Sc.3** — seul `DefinirTransfertTests.Un_transfert_incomplet_…` (`@erreur` sprint 1)
  existe : le nominal n'est pas couvert → les deux tests sont des drivers réels.
- **Sc.4** — `PlanningPartageTests.Modifier_une_periode_sur_un_etat_a_jour_remplace_le_responsable`
  existe déjà → test #1 = **caractérisation** (early green probable). Le test #2
  (pré-remplissage du formulaire inline = câblage `@onclick` d'ouverture) est le
  driver restant.

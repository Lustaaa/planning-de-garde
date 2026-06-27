# Scenario 4 — Échec clair : la dialog reste ouverte et conserve la saisie `@erreur 🖥️ IHM`

[← Retour au suivi](00-sprint11-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime**. Scenario **Outline**
> à 2 `Examples` (refus du domaine, API injoignable) qui partagent **un seul observable**
> (règle 28, décision CP) : la dialog **reste ouverte**, message **dans** la dialog, **saisie
> conservée**, grille **inchangée**. Couvre **indirectement** la validation domaine
> sous-jacente (lieu inexistant, vert s01).

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_Laisser_la_dialog_ouverte_avec_message_et_saisie_conservee_et_grille_inchangee_When_la_commande_de_pose_n_aboutit_pas** — ✅ GREEN (`FrontWasmEchecDialogResteOuverteTests`, 2 Facts : API injoignable + refus domaine)

> **Caractérisation early-green (décision CP)** : l'acceptation passe **sans fix** — le traitement
> d'échec complet (dialog ouverte, message dans la dialog, saisie conservée, grille inchangée) a atterri
> dès le **fix du Sc.1** dans `PoserSlotDialog`. Tests runtime **non-vacuous** : le cas **API injoignable**
> est prouvé sur **transport réellement coupé** via la grille (`ClientVersAvecEcritureInjoignable`, lève
> `HttpRequestException`, lecture initiale préservée), pas une doublure 4xx ; l'absence d'écriture est
> vérifiée sur le **store réel**. Si la dialog se fermait à tort, ou mutait la grille, ou perdait la
> saisie, ou écrivait au store, les tests échoueraient → filets conservés. Pas de cycle RED→GREEN factice.

Sur l'app **réellement câblée**, un Parent ouvre la dialog depuis la case du **vendredi
19/06/2026** (sans slot), saisit une pose, valide, **et la commande n'aboutit pas** :

- **refus du domaine** — saisie « Atlantide » 08:00→09:00 → message « Lieu inconnu : saisie
  non appliquée » ;
- **API injoignable** (règle 28) — saisie « École » 08:00→09:00, transport coupé → message
  « Service indisponible : à resoumettre ».

**Observable runtime (identique pour les deux causes)** : la dialog « Poser un slot » **reste
ouverte**, le **message** s'affiche **dans la dialog**, la **saisie est conservée** (à
resoumettre), la **grille reste inchangée** et **aucun slot** n'apparaît dans la case du
vendredi 19/06/2026 (vérifié sur le store distant : aucune écriture aboutie). Le cas **API
injoignable** doit échouer sur **câblage réel** (transport coupé), pas sur une doublure qui
mentirait au vert.

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Garder_la_dialog_ouverte_avec_le_message_dans_la_dialog_When_la_commande_est_refusee_par_le_domaine | (conditional) issue échec ≠ succès | Une issue qui ferme toujours la dialog (copie du succès) trahit l'échec | ⏳ Pending |
| 2 | Should_Conserver_la_saisie_a_resoumettre_When_la_commande_n_aboutit_pas | (conditional) état préservé | Un reset du formulaire à la soumission perdrait la saisie → contradiction | ⏳ Pending |
| 3 | Should_Laisser_la_grille_inchangee_When_la_commande_n_aboutit_pas | (conditional) pas d'effet de bord grille | Une mutation locale optimiste de la grille violerait « grille inchangée » (règle 14) | ⏳ Pending |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`PoserSlotDialog.razor` / `.razor.cs`** — sur issue échec (refus **ou** transport KO) :
  garder l'état d'ouverture, afficher le message **dans** la dialog (`data-testid` dédié),
  **ne pas** réinitialiser le formulaire, **ne pas** relire/muter la grille.
- **`PlanningPartage.razor.cs`** — ne ferme la dialog et ne relit la grille **que** sur succès.
- **`Web.Tests`** — bUnit (refus domaine via canal stubé) + acceptation runtime **API
  injoignable** (transport coupé sur câblage réel).

## Design notes

- **Décision CP (cf. `99-sprint11-retours.md`)** : refus domaine et API injoignable = **même
  observable** (règle 28), donc **un seul Outline**, pas deux scénarios.
- **Validation domaine sous-jacente** (durée nulle, lieu inexistant, responsable requis) =
  **caractérisation déjà verte s01**, exercée par l'`Example` « refus du domaine » — **non
  re-drivée** ici.
- **Anti vert-qui-ment** : le cas API injoignable est la raison d'être de l'acceptation
  runtime — un bUnit à transport stubé pourrait simuler un faux échec ; la preuve doit venir
  d'un **transport réellement coupé** et d'un **store distant resté vide**.

# Scenario 3 — Échec : la dialog reste ouverte et conserve la saisie `@erreur 🖥️ IHM`

[← Retour au suivi](00-sprint12-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime**.
> **⚠️ Probablement early green (câblage IHM partagé + invariant déjà vert).** Le pattern
> « échec → un seul observable : dialog ouverte, message **dans** la dialog, saisie conservée,
> grille inchangée » (règle 28) est **déjà vert s11** (Sc.4 s11) ; l'invariant domaine
> « transfert incomplet » (récupération/heure manquante) est **vert s01**
> (`Scenario12_TransfertIncomplet`) et le **motif de refus** est déjà renvoyé par le canal
> (`DefinirTransfertCanalApiTests`). Dès que le Sc.1 câble l'**issue échec** de la dialog
> transfert, les deux `Examples` sont acquis **par construction**. Caractérisation (filet),
> **pas un driver** — **batchable**.

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_Laisser_la_dialog_ouverte_avec_le_message_d_echec_et_la_saisie_conservee_et_la_grille_inchangee_When_la_definition_du_transfert_n_aboutit_pas** — ✅ GREEN (caractérisation early-green confirmée — `FrontWasmDefinirTransfertEchecTests`, 2 Facts : refus du domaine / API injoignable ; runtime sur app réellement câblée. Échec → un seul observable : dialog ouverte, motif **dans** la dialog, saisie conservée, store vide. **Témoins réels** : refus = motif propagé par le canal contenant « récupération » (message domaine réel `Transfert incomplet : la récupération et l'heure sont requises.`) ; injoignable = transport réellement coupé au handler + `MessagesEcriture.ServiceInjoignable`. Cf. note libellé ci-dessous.)

Sur l'app **réellement câblée**, un Parent ouvre la dialog depuis la case du vendredi
19/06/2026, saisit puis valide.

- **Refus du domaine** (« Parent A » dépositaire, **sans récupérateur**, lieu « École ») :
  **Observable runtime** — la dialog **reste ouverte**, le message **« Transfert incomplet : la
  récupération est requise »** s'affiche **dans la dialog**, la saisie est **conservée** à
  resoumettre, la grille reste **inchangée**.
- **API injoignable** (« Parent A » → « Parent B » lieu « École » à 08:30, transport réellement
  coupé) : **Observable runtime** — même issue unique : dialog ouverte, message **« Service
  indisponible : à resoumettre »** dans la dialog, saisie conservée, grille inchangée.

*Anti vert-qui-ment* : sur API injoignable, **couper réellement le transport** (pas une doublure
qui renvoie une erreur) ; vérifier qu'**aucune** écriture n'a transité (store vide).

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Garder_la_dialog_ouverte_avec_le_message_transfert_incomplet_et_la_saisie_conservee_When_le_domaine_refuse_le_transfert | (conditional) issue échec dans la dialog | ⚠️ probablement early green (câblage IHM partagé) — l'issue échec mutualisée du s11 (Sc.4) gère déjà dialog ouverte + message dedans + saisie conservée | ✅ couvert au runtime (Fact refus du domaine) |
| 2 | Should_Garder_la_dialog_ouverte_avec_le_message_service_indisponible_When_l_API_est_injoignable | (conditional) même issue, cause transport | ⚠️ probablement early green (câblage IHM partagé) — règle 28 : refus domaine et API injoignable convergent vers le même observable, déjà vert s11 | ✅ couvert au runtime (Fact API injoignable) |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`DefinirTransfertDialog.razor` / `.razor.cs`** — l'issue échec **n'empêche pas** la
  réouverture : message d'erreur **dans** la dialog (`data-testid` dédié), saisie conservée,
  pas de fermeture, **aucun accusé succès**.
- **`Web.Tests`** — bUnit (canal renvoyant le motif de refus / transport coupé) + acceptation
  runtime sur transport réellement coupé.

## Design notes

- **⚠️ Couvert par l'acquis** : règle 28 (un seul observable d'échec) verte s11, invariant
  « transfert incomplet » vert s01, motif de refus déjà renvoyé par le canal → `ihm-builder`
  doit attendre les deux Facts **verts d'emblée** (caractérisation), pas un défaut.
- **Distinction d'issue** : échec = dialog **ouverte** + message **dedans** (≠ succès Sc.1 :
  dialog fermée + accusé **à part**). C'est le point de conception protégé.
- **Libellés porteurs de message** (« Transfert incomplet : la récupération est requise » /
  « Service indisponible : à resoumettre ») : le 2ᵉ réutilise `MessagesEcriture.ServiceInjoignable`
  (relocalisé s11) ; le 1ᵉʳ surface le motif domaine. → remonter au CP si le libellé exact doit
  différer de la convention s11.
- **Note libellé (caractérisation — divergence non bloquante, déjà anticipée)** : les libellés
  réellement surfacés à l'écran diffèrent du texte *illustratif* de la feature Gherkin —
  refus domaine = `Transfert incomplet : la récupération et l'heure sont requises.` (motif
  domaine réel, vert s01) ; injoignable = `Enregistrement impossible : le service est
  injoignable, réessayez.` (`MessagesEcriture.ServiceInjoignable`, convention s11 réutilisée).
  L'acceptation runtime caractérise les **témoins réels** (pas le texte illustratif) : refus →
  motif contenant « récupération » ; injoignable → constante exacte. **Pas un driver, pas un
  RED** : c'est la convention de message **déjà acquise** s01/s11. Le réalignement du texte
  Gherkin sur les libellés réels (ou inversement) est un détail de wording **déférable au CP**,
  hors périmètre de cette caractérisation.

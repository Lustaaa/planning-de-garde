# Sc.8 — Édition du cycle impossible si le service de configuration est injoignable

`@erreur` `🖥️ IHM`

↩ Retour : [00-sprint10-suivi.md](00-sprint10-suivi.md)

**Routage** : **driver 100 % runtime IHM** (`ihm-builder`) — **backend néant**. C'est un échec de
**transport** (API/service de configuration injoignable au moment de valider) : il n'a **aucun** observable
à la frontière Application (le handler n'est jamais atteint). Le symptôme PO vit dans le **runtime**
(message d'échec à l'écran, saisie conservée, rien enregistré) → patron **s09 Sc.9** (canal d'écriture
`catch (HttpRequestException)`). bUnit seul ne prouve **jamais** ce câblage (DI réelle, chemin HTTP).

> **Données** : un parent a saisi un cycle de 2 semaines (pair → `parent-a` bleu, impair → `parent-b`
> orange) dans l'écran de configuration ; il valide alors que le **service de configuration est
> injoignable**.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Sur l'app réellement câblée, un parent valide l'édition du cycle alors que le service de configuration
> est **injoignable** ; un **message d'échec clair** s'affiche ; la **saisie du cycle reste à l'écran**
> à resoumettre ; **aucun cycle n'est enregistré** (aucune mise en file ni rejeu — règle 28). Prouvé sur
> transport réellement coupé (canal d'écriture levant `HttpRequestException` sur le seul POST ciblé),
> jamais par une doublure qui simulerait un succès.

`Should_Afficher_un_message_d_echec_clair_et_conserver_la_saisie_du_cycle_a_resoumettre_sans_rien_enregistrer_When_le_service_de_configuration_est_injoignable_a_la_validation_du_cycle` — ✅ GREEN *(runtime, `ihm-builder` — RED `HttpRequestException` non rattrapée plante l'écran sans message → GREEN après `catch (HttpRequestException)` ; `FrontWasmConfigCycleServiceInjoignableTempsReelTests`)*

## Tests unitaires backend (boucle interne, `tdd-auto`)

> **Backend néant.** Aucun test unitaire backend : l'échec est purement **transport/runtime** (le
> handler `DefinirCycle` n'est jamais atteint). Tout est porté par l'acceptation runtime ci-dessus.

## Fichiers à créer / modifier (runtime uniquement — routé `ihm-builder`)

- **Canal d'écriture front (Web)** — `catch (HttpRequestException)` sur le POST `definir-cycle` →
  message d'échec clair, saisie du cycle conservée à l'écran, aucun cycle appliqué (patron s09 Sc.9 /
  s05 Sc.6, règle 28).
- **Harness runtime** — transport déterministe levant `HttpRequestException` sur le seul POST
  `definir-cycle` (réutilise le patron `GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable` du s09,
  robuste au proxy loopback Docker).

## Design notes

- **Échec clair, sans file ni rejeu** (règle 28) : message à l'écran, saisie **non appliquée** et
  **conservée** à resoumettre — le hors-ligne rejouable est un palier technique ultérieur.
- **Aucun cycle enregistré** : la commande non aboutie ne mute pas le store cycle — observable cardinal.
- **Niveau du test = niveau du symptôme** : un bug de transport runtime se prouve sur l'app câblée
  (transport réellement coupé), jamais par un test à doublure qui « mentirait au vert ».

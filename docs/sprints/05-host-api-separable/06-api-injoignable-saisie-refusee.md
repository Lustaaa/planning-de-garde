# Scénario 6 — API distante injoignable : la saisie est refusée et n'est pas appliquée

`@erreur` · 🖥️ **scénario IHM** — **Routé vers `ihm-builder`**

[← Retour au suivi](00-sprint05-suivi.md)

> **Axe : IHM / runtime.** Le comportement vit dans le **front WASM** : quand l'API distante est
> **arrêtée** (HTTP distant en échec), le front doit **afficher un message clair à l'écran**, **ne pas
> appliquer** la saisie (elle reste à resoumettre) et **n'écrire nulle part** (aucune écriture
> silencieuse, **aucune mise en file** — PWA hors périmètre). Le symptôme PO est un **fait d'usage
> runtime** (« le service est injoignable et l'écran me le dit, ma saisie n'est pas perdue ni
> appliquée »). **JAMAIS** planifié comme un bUnit composant à doublures : seul l'**échec réseau réel**
> sur l'app câblée reproduit le symptôme ; un bUnit qui stub un `HttpStatusCode` ne prouve pas un
> service réellement injoignable.
>
> **Niveau d'acceptation : E2E / runtime** sur l'**app réellement câblée** (front WASM + API distante
> **arrêtée**, DI réelle). Le détail RED→GREEN du `.razor` / du message / de la non-application est
> piloté par `ihm-builder`.

## Acceptation (BDD)

`Should_Afficher_le_message_de_service_injoignable_et_ne_rien_enregistrer_When_le_front_WASM_tente_une_pose_alors_que_l_API_distante_est_arretee`

**Test de NIVEAU RUNTIME** sur l'app réellement câblée, **API distante arrêtée** :
- **Given** le front s'exécute dans le navigateur (WASM), configuré pour émettre ses écritures vers
  `https://api.planning.local` ; l'hôte d'API à cette adresse est **arrêté, donc injoignable** ; le
  foyer connaît « école » ; aucun slot pour le mercredi 24/06/2026 ;
- **When** le front tente d'émettre une pose de slot (Léa, « école », 24/06/2026 08:30→16:30) ;
- **Then** le front **affiche** le message « Enregistrement impossible : le service est injoignable,
  réessayez. » ; **et** la saisie **n'est pas appliquée** et **reste à resoumettre** ; **et** aucun
  slot n'est enregistré pour le mercredi 24/06/2026 — **aucune écriture silencieuse ni mise en file**
  n'ayant eu lieu.

## Tests

> Détail RED→GREEN piloté par `ihm-builder` (gestion de l'échec de transport HTTP côté vue WASM,
> affichage du message exact, conservation de la saisie). L'acceptation runtime ci-dessus est la
> **boucle externe**. Pas de test unitaire backend : aucune règle de domaine n'est en jeu — le
> comportement est entièrement **front / runtime** (un échec réseau, pas un refus métier).

## Fichiers à créer / modifier

- Vue `PoserSlot` (front WASM) : capter l'**échec de transport** (API injoignable, distinct d'un
  refus métier 4xx) et afficher le **message exact** « Enregistrement impossible : le service est
  injoignable, réessayez. ».
- Câblage front WASM + URL d'API configurable (commun à Sc.2).

## Design notes

- **Distinguer injoignable vs refus métier** : un refus métier (4xx, motif propagé) existe déjà
  (sprint 04, `motif-echec`). Ici c'est un **échec de transport** (connexion refusée / timeout) qui
  doit produire un **message dédié** — ne pas confondre les deux chemins.
- **Anti « vert qui ment »** : prouver l'absence d'effet de bord requiert que **rien** ne soit
  enregistré côté store **et** qu'**aucune file** ne soit créée (PWA reportée). L'acceptation runtime
  doit vérifier l'app réellement câblée, API réellement arrêtée — un bUnit stubant la réponse ne prouve
  pas l'injoignabilité réelle.
- **Saisie conservée** : la saisie « reste à resoumettre » → le formulaire conserve ses valeurs après
  l'échec (pas de navigation, pas de reset).
- **Hors périmètre** : **aucune file ni rejeu** (PWA reportée à un sprint ultérieur) — le front se borne
  à l'échec clair.

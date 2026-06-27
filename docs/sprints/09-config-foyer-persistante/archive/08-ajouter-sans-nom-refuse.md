# Sc.8 — Ajouter un acteur sans nom est refusé

`@erreur` `🖥️ IHM`

↩ Retour : [00-sprint09-suivi.md](00-sprint09-suivi.md)

**Routage** : tranche **backend** (`tdd-auto`, 2 drivers de la garde « nom non vide » sur le
**handler d'ajout neuf** + 1 caractérisation absence-de-diffusion) **+** acceptation **runtime
IHM** (`ihm-builder` : message clair à l'écran, liste inchangée). Garde **CONDITIONNELLE** (pas
inconditionnelle) : le nominal ajout (Sc.1) est **déjà vert** → un refus inconditionnel
**régresserait** Sc.1. La garde ne refuse que le nom vide / tout-espaces, **sans générer d'id** ni
muter le store.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Un parent tente d'ajouter un acteur en laissant le **nom vide** ; l'ajout est **refusé** avec le
> message « le nom ne peut pas être vide » affiché à l'écran ; **aucun identifiant n'est généré** et
> la **liste des acteurs reste inchangée**. Sur l'app réellement câblée (réutilise la gestion du
> motif métier surfacé du s08 Sc.8).

`Should_Refuser_l_ajout_avec_le_message_le_nom_ne_peut_pas_etre_vide_et_laisser_la_liste_des_acteurs_inchangee_When_un_parent_valide_un_ajout_au_nom_vide` — ✅ GREEN (runtime, `FrontWasmConfigAjouterSansNomRefuseTempsReelTests`)

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Refuser_l_ajout_avec_un_motif_clair_sans_generer_d_identifiant_ni_modifier_la_liste_When_le_nom_demande_est_une_chaine_vide` | commande → refus conditionnel | **Driver** — le handler d'ajout issu de Sc.1 génère un id et persiste **tout** nom, y compris « " " » : un nom vide créerait un **acteur fantôme**. Force la garde « nom non vide » qui refuse (`Result.Echec` porteur du motif), **sans générer d'id** ni muter le store (liste inchangée). | ✅ GREEN |
| 2 | `Should_Refuser_l_ajout_et_laisser_la_liste_inchangee_When_le_nom_demande_ne_contient_que_des_espaces` | refus « vide » → refus sur nom utile | **Driver** — la garde minimale du #1 (« chaîne vide ») laisse **passer** un nom tout-espaces (« "   " » ≠ ""), qui créerait un acteur sans nom utile. Force la garde sur le nom **utile** (espaces ignorés), contredisant l'impl minimale du #1. Réutilise le motif `EditerActeurHandler.cs:38`. | ✅ GREEN |
| 3 | `Should_Ne_declencher_aucune_diffusion_temps_reel_When_un_ajout_est_refuse` | refus → absence d'effet de bord (Spy) | ⚠️ **probablement early green — couvert par #1/#2 (le refus retourne AVANT la notification) (caractérisation, pas driver)**. La diffusion est déclenchée **après** mutation réussie ; un refus retourne avant, par construction. Filet (Spy) verrouillant « pas de diffusion sur ajout refusé ». `tdd-auto` marquera ✅ GREEN (caractérisation). | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier (backend uniquement ici)

- **`AjouterActeurHandler` (Application, Sc.1)** — ajoute la **garde conditionnelle** « nom non
  vide » (vide / tout-espaces refusé) **avant** la génération d'id et la mutation ; renvoie
  `Result.Echec("le nom ne peut pas être vide")` (motif **métier**, réutilisé de
  `EditerActeurHandler.cs:38`). Aucun id généré, store inchangé, aucune diffusion.
- **Doublures tests** — `FakeConfigurationFoyer` (ajout + énumération pour vérifier la liste
  inchangée) ; `FakeNotificateurPlanning` (Spy, 0 notification).
- **Volet runtime IHM (routé `ihm-builder`)** — message clair surfacé à l'écran de config (réutilise
  la désérialisation du motif `Results.BadRequest(string)` du s08 Sc.8).

## Design notes

- **Garde conditionnelle, jamais inconditionnelle.** Le nominal ajout (Sc.1) est déjà vert ; refuser
  inconditionnellement régresserait Sc.1. La garde ne vise que le **nom fourni vide / tout-espaces**.
- **Aucun id généré sur refus** : la génération d'id et la persistance n'ont lieu **qu'après** la
  garde. C'est l'observable cardinal (« aucun identifiant n'est généré »).
- **Motif métier, jamais technique** : « le nom ne peut pas être vide » (pas d'exception/`null`/HTTP
  dans l'étiquette ni le message).
- **Pas de diffusion sur échec** (invariant d'effet de bord) : le canal de diffusion reste lecture
  seule ; vérifié backend par Spy.

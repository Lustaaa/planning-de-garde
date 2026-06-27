# Sc.7 — Définir un cycle de zéro semaine est refusé

`@erreur` `🖥️ IHM`

↩ Retour : [00-sprint10-suivi.md](00-sprint10-suivi.md)

**Routage** : tranche **backend** (`tdd-auto`, 1 driver de la garde **N ≥ 1** sur `DefinirCycleHandler`
+ 1 caractérisation « cycle précédent inchangé ») **+** acceptation **runtime IHM** (`ihm-builder` :
message clair à l'écran, cycle précédent conservé). Garde **CONDITIONNELLE** (pas inconditionnelle) : le
nominal « définir un cycle de 2 semaines » est **déjà vert** (exercé dès l'acceptation Sc.1) → un refus
inconditionnel **régresserait** ce nominal. La garde ne refuse que **N = 0**, sans écraser le cycle existant.

> **Données** : le foyer a un cycle de fond de 2 semaines déjà défini (pair → `parent-a`, impair →
> `parent-b`). Un parent tente d'enregistrer un cycle de **zéro semaine** (N = 0). Attendu : refus avec
> le message « le cycle doit compter au moins une semaine » ; le cycle de 2 semaines reste inchangé.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Sur l'app câblée, un parent tente d'enregistrer un cycle de zéro semaine ; l'édition est **refusée**
> avec le message « le cycle doit compter au moins une semaine » affiché à l'écran ; le cycle de 2
> semaines précédent **reste inchangé** (la grille continue d'afficher l'alternance A/B).

`Should_Refuser_l_edition_avec_le_message_le_cycle_doit_compter_au_moins_une_semaine_et_conserver_le_cycle_precedent_When_un_parent_tente_d_enregistrer_un_cycle_de_zero_semaine` — ⏳ Pending *(runtime, `ihm-builder`)*

> **Acceptation backend (frontière Application, `tdd-auto`)** — via `DefinirCycleHandler` sur un store
> portant déjà un cycle N=2 : `DefinirCycleCommand(0, …)` renvoie `Result.Echec("le cycle doit compter au
> moins une semaine")` et le store résout encore le cycle N=2 d'origine.
> `Acceptation_Should_Refuser_le_cycle_de_zero_semaine_avec_motif_clair_sans_ecraser_le_cycle_de_deux_semaines_When_un_parent_tente_d_enregistrer_zero_semaine` — ✅ GREEN

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Refuser_la_definition_du_cycle_avec_le_message_que_le_cycle_doit_compter_au_moins_une_semaine_When_un_parent_tente_d_enregistrer_un_cycle_de_zero_semaine` | commande → refus conditionnel | **Driver** — le `DefinirCycleHandler` (exercé en succès dès Sc.1) accepte **tout** N, y compris 0 (un cycle de 0 semaine est insensé : `ISOWeek mod 0` n'est pas défini). Force la garde **N ≥ 1** qui refuse (`Result.Echec` porteur du motif **métier**). **Conditionnelle** : le nominal N=2 (Sc.1) reste accepté → la garde ne vise que N = 0. | ✅ GREEN |
| 2 | `Should_Laisser_le_cycle_precedent_inchange_When_la_definition_d_un_cycle_de_zero_semaine_est_refusee` | refus → absence d'effet de bord | ⚠️ **probablement early green — couvert par #1 (le refus retourne AVANT toute écriture) (caractérisation, pas driver)**. La garde refuse avant de muter le store → le cycle N=2 d'origine est intact, la grille résout encore l'alternance A/B. Filet verrouillant « aucun effet de bord sur refus ». `tdd-auto` marquera ✅ GREEN (caractérisation). | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier (backend uniquement ici)

- **`DefinirCycleHandler` (Application, Sc.1)** — ajoute la **garde conditionnelle N ≥ 1** **avant**
  l'écriture ; renvoie `Result.Echec("le cycle doit compter au moins une semaine")` (motif **métier**,
  jamais technique). Aucune écriture, cycle précédent conservé, aucune diffusion.
- **Doublures tests** — store/`Fake` du port cycle portant un cycle N=2 (pour vérifier l'inchangé) ;
  `FakeNotificateurPlanning` (Spy, 0 notification sur refus).
- **Volet runtime IHM (routé `ihm-builder`)** — message clair surfacé à l'écran de config (réutilise la
  désérialisation du motif `Results.BadRequest(string)`, patron s08/s09).

## Design notes

- **Garde conditionnelle, jamais inconditionnelle.** Le nominal N=2 (Sc.1) est déjà vert ; refuser
  inconditionnellement régresserait Sc.1. La garde ne vise que **N = 0** (et tout N < 1).
- **Invariant porté par le handler d'écriture, pas la projection** (cadrage CP) : la résolution suppose
  N ≥ 1 ; c'est l'écriture qui protège l'invariant. La projection n'a jamais à gérer N = 0.
- **Motif métier, jamais technique** : « le cycle doit compter au moins une semaine » (pas d'exception /
  division par zéro / HTTP dans l'étiquette ni le message).
- **Pas de diffusion sur refus** (invariant d'effet de bord) ; vérifié backend par Spy.

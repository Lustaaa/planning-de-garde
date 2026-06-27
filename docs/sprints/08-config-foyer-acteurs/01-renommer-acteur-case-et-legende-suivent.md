# Sc.1 — Renommer un acteur : la case et la légende suivent dans la session

`@nominal` `🖥️ IHM`

↩ Retour : [00-sprint08-suivi.md](00-sprint08-suivi.md)

**Routage** : **scénario fondation** de l'édition. Tranche **backend** (`tdd-auto`, 3 drivers :
store `renommer`, handler `EditerActeur`, diffusion sur succès) **+** acceptation **runtime
IHM** (`ihm-builder` : la case + la légende suivent **sans rechargement**).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Le symptôme PO est un fait d'usage **runtime** : depuis l'écran de config, je renomme et
> j'enregistre ; **sans recharger**, la grille **réellement câblée** (front WASM + API
> distante réelle + store réel + diffusion SignalR) restitue le nouveau nom dans la case
> **et** la légende. **Pas** un test bUnit à doublure (qui ne prouve ni la DI réelle, ni le
> chemin HTTP d'écriture, ni la diffusion).

`Should_Afficher_Alicia_dans_la_case_du_14_07_2026_et_dans_l_entree_de_legende_sans_recharger_la_page_et_conserver_l_identifiant_parent_a_When_l_acteur_parent_a_est_renomme_de_Alice_en_Alicia_depuis_l_ecran_de_configuration`

- **Niveau** : E2E/runtime sur l'app câblée (pattern `ApiDistanteFactory` + écran de config
  réel + grille réelle), store **réel** seedé (`parent-a → « Alice »`), diffusion SignalR
  **existante** (palier 1). Anti « vert qui ment » : si l'écriture n'emprunte pas le canal
  HTTP réel ou si la grille ne re-projette pas, l'observable reste « Alice » → rouge.
- **Observable** : après enregistrement, sans rechargement, la case du 14/07/2026 porte
  « Alicia » et l'entrée de légende affiche « Alicia » (toujours en bleu) ; l'identifiant
  « parent-a » est inchangé.

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Resoudre_le_nouveau_nom_pour_l_identifiant_stable_When_un_acteur_deja_seme_est_renomme_dans_le_store_de_configuration` | constante (seed figé) → valeur mutable | **Driver** — aujourd'hui le nom est lu d'un dictionnaire `static readonly` (`Foyer.NomsParResponsable`) : **aucun moyen de renommer**, la valeur résolue est immuable. Force un store seedé **éditable** dont `NomDe(id)` reflète la dernière écriture du nom. | ✅ GREEN |
| 2 | `Should_Appliquer_le_nouveau_nom_et_confirmer_l_effet_When_la_commande_renomme_un_acteur_connu_avec_un_nom_non_vide` | valeur mutable → commande orchestrée | **Driver** — il n'existe **aucune** commande/handler `EditerActeur` : rien ne valide l'id ni n'écrit le store. Force l'orchestration (id connu → mute le store via le port d'écriture, renvoie la confirmation de l'effet). | ✅ GREEN |
| 3 | `Should_Declencher_la_diffusion_temps_reel_une_fois_When_un_renommage_aboutit` | commande → effet de bord observé (Spy) | **Driver** — sans câblage, l'édition aboutie ne notifie personne : les autres grilles ne suivraient pas. Force le déclenchement de `INotificateurPlanning` **sur succès** (vérifié par Spy), jamais une écriture par le canal de diffusion. | ✅ GREEN |

> **Caractérisations anticipées (non listées, ⚠️ early green attendu chez `tdd-auto`)** :
> *(a)* l'**identifiant stable inchangé** après renommage est garanti par construction (le
> store est clé par id, seule la valeur nom mute) — invariant cardinal, vérifié par
> l'acceptation runtime, pas un test backend dédié ; *(b)* la **case + légende suivent** est
> une re-projection de `GrilleAgendaQuery` **inchangé** (caractérisation des s07 Sc.1/2) —
> prouvée au runtime, pas de rouge backend à attendre.

## Fichiers à créer / modifier (backend uniquement ici)

- **Store mutable singleton (Infrastructure)** — nouveau type seedé depuis `Foyer`,
  réalisant `IReferentielResponsables` (lecture) + opérations d'écriture `renommer`.
  Remplace `FoyerReferentielResponsables` (lecture du dictionnaire statique) par la lecture
  du store. **Volatile** = re-seedé à la (re)construction.
- **Port d'écriture (Application)** — surface d'édition (`renommer` / à étendre `recolorier`
  au Sc.2) consommée par le handler ; doublure `Fake` côté tests. *(Forme exacte du port
  laissée à `tdd-auto`.)*
- **`EditerActeurCommand` + `EditerActeurHandler` (Application)** — `{ acteurId, nom?,
  couleur? }`, valide l'id connu + nom non vide, mute via le port d'écriture, déclenche
  `INotificateurPlanning` sur succès, renvoie `Result<…>` (convention de refus existante).
- **Doublures tests** — `Fake` du port d'écriture (store en mémoire) ; réutiliser
  `FakeNotificateurPlanning` (Spy déjà présent).
- *(Écran de config, câblage HTTP réel et suivi SignalR : hors backend — routé `ihm-builder`.)*

## Design notes

- **Réutiliser les ports de lecture inchangés.** `IReferentielResponsables.NomDe` /
  `IPaletteCouleurs.CouleurDe` gardent leur **contrat** : seule leur **réalisation** passe
  d'un dictionnaire figé à un store mutable. `GrilleAgendaQuery` n'est **pas** touché (CQRS,
  règle 12).
- **Id stable, jamais éditable** (invariant cardinal). La commande ne porte que nom/couleur ;
  l'id stable est la **clé**, pas une donnée. Aucune API d'édition d'id.
- **Diffusion = effet de l'écriture aboutie.** Le handler notifie après mutation réussie ;
  le canal de diffusion reste **lecture seule** (jamais d'écriture par lui). Vérifié backend
  par Spy ; le suivi live réel est prouvé au runtime (`ihm-builder`).
- **Dernière écriture gagne** (décision CP) : le store **écrase** la valeur, sans version ni
  rejet — préparé pour Sc.7.

# Sc.4 — Un acteur ajouté apparaît en légende une fois une période affectée

`@nominal`

↩ Retour : [00-sprint09-suivi.md](00-sprint09-suivi.md)

**Routage** : **caractérisation backend** (⚠️ early green attendu) **+** runtime IHM. Prouve que
l'**identifiant neuf circule** de bout en bout dans le read model. Composé : Sc.1 (l'ajout
enregistre id neuf → nom + couleur) **+** s07 (`GrilleAgendaQuery` résout nom/couleur sur l'id et
dédoublonne la légende **par id**). **Aucun calcul neuf** : un acteur ajouté porté par une période
apparaît mécaniquement en légende/case sur son id neuf.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Carla vient d'être ajoutée (rose), aucune période ; un parent lui affecte la garde de Léa du 8
> au 12 juin ; **sans rechargement**, la légende fait apparaître une entrée « Carla » rose
> **portée par l'identifiant stable neuf de Carla**, et les cases du 8–12 juin affichent « Carla »
> rose. Sur l'app **réellement câblée** (front WASM + API distante + store durable).

`Should_Afficher_une_entree_de_legende_Carla_en_rose_portee_par_l_identifiant_neuf_et_les_cases_du_8_au_12_juin_en_Carla_rose_When_une_periode_est_affectee_a_l_acteur_ajoute_Carla` — ⏳ Pending

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Faire_apparaitre_une_entree_de_legende_Carla_en_rose_sur_l_identifiant_neuf_et_nommer_les_cases_couvertes_When_une_periode_est_affectee_a_l_acteur_ajoute` | composition (read model inchangé) | ⚠️ **probablement early green — couvert par Sc.1 #1 (ajout enregistre id→nom+couleur) + s07 légende-par-id (caractérisation, pas driver)**. `GrilleAgendaQuery.LegendeDesPresents` résout déjà `NomDe`/`CouleurDe` sur **tout** id présent dans une période et dédoublonne par id ; l'id neuf de Carla y entre **mécaniquement** dès qu'une période le porte. `tdd-auto` marquera ✅ GREEN (caractérisation). | ✅ GREEN (caractérisation) |

> **Pourquoi une caractérisation et pas un driver** (méthodo : anticiper, pas fusionner a
> posteriori) — Le read model `GrilleAgendaQuery` est **inchangé** et résout par id stable. Aucun
> rouge n'est forcé : la valeur du test est un **filet anti-régression** prouvant que l'**id neuf**
> (issu de Sc.1) **circule** jusqu'à la légende/case, pas un geste d'implémentation neuf.

## Fichiers à créer / modifier

- **Backend** : néant de neuf (read model s07 inchangé ; ajout = Sc.1). Test de caractérisation
  composant `AjouterActeurHandler` (Sc.1) + `AffecterPeriodeHandler` (existant) + `GrilleAgendaQuery`.
- **Doublures tests** — `FakeConfigurationFoyer` (avec ajout, Sc.1) + `FakePeriodeRepository`.
- **Volet runtime IHM (routé `ihm-builder`)** — grille câblée affichant l'entrée de légende sur
  l'id neuf.

## Design notes

- **L'id neuf circule** : c'est l'observable cardinal du scénario. La preuve forte est le
  dédoublonnage **par id** (jamais le libellé) déjà garanti s07 (`.Distinct()` sur `ResponsableId`).
- **Couleur rose** vient de l'ajout (Sc.1 enregistre la couleur fournie) ; la légende/case
  **surfacent** la couleur déjà résolue, sans calcul neuf.

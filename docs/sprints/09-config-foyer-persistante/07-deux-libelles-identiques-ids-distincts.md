# Sc.7 — Deux acteurs de même libellé reçoivent deux identifiants distincts

`@limite`

↩ Retour : [00-sprint09-suivi.md](00-sprint09-suivi.md)

**Routage** : **caractérisation backend** (⚠️ early green attendu). Deux ajouts du même libellé
« Carla » reçoivent **deux identifiants distincts** : c'est la conséquence mécanique de l'**id
opaque généré** (Sc.1 #2, jamais dérivé du libellé) ; la légende les dédoublonne **par id** en deux
entrées (s07, `.Distinct()` sur `ResponsableId`) et ne les fusionne jamais sur leur libellé.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Une nounou « Carla » a déjà été ajoutée ; un parent ajoute une **seconde** acteur également
> nommée « Carla » ; le foyer compte **deux** « Carla » portées par **deux identifiants distincts**,
> la légende les présente en **deux entrées** (si toutes deux portées par une période) et ne les
> fusionne **jamais** sur le libellé. Sur l'app réellement câblée.

`Should_Compter_deux_Carla_portees_par_deux_identifiants_distincts_et_les_dedoublonner_en_deux_entrees_de_legende_sans_jamais_les_fusionner_sur_le_libelle_When_une_seconde_actrice_nommee_Carla_est_ajoutee` — ✅ GREEN (caractérisation backend ; runtime IHM routé `ihm-builder`)

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Attribuer_deux_identifiants_distincts_aux_deux_acteurs_de_meme_libelle_sans_jamais_les_fusionner_sur_le_libelle_When_une_seconde_actrice_du_meme_nom_est_ajoutee` | composition (id opaque + dédoublonnage par id) | ⚠️ **probablement early green — couvert par Sc.1 #2 (id opaque généré, jamais le libellé) + s07 légende dédoublonnée par id (caractérisation, pas driver)**. Dès que l'ajout génère un id **opaque** (GUID/séquence), deux ajouts du même libellé donnent **mécaniquement** deux ids distincts ; la légende dédoublonne par id (jamais le libellé). `tdd-auto` marquera ✅ GREEN (caractérisation). **Confirmé : 1 passé d'emblée, aucun code neuf.** | ✅ GREEN (caractérisation) |

> **Pourquoi une caractérisation et pas le driver de l'opacité** (méthodo : identifier le driver et
> le placer en premier) — Le **driver de l'identité opaque** est **Sc.1 #2** (« id ≠ libellé »), qui
> casse le raccourci `id = nom`. Une fois ce driver vert, « deux mêmes libellés → deux ids
> distincts » **suit mécaniquement** : ce scénario est le **filet anti-régression** qui documente le
> `@limite` collision-de-libellé, pas un geste d'implémentation neuf. Le dédoublonnage légende est
> déjà vert (s07).

## Fichiers à créer / modifier

- **Backend** : néant de neuf (id opaque = Sc.1 ; dédoublonnage légende = s07). Test de
  caractérisation : deux `AjouterActeurCommand("Carla", …)` → deux ids distincts ; énumération
  compte deux entrées ; légende dédoublonnée par id.
- **Doublures tests** — `FakeConfigurationFoyer` (ajout) + énumération.

## Design notes

- **Libellé ≠ identité** (invariant cardinal s06). Deux « Carla » ne sont **jamais** la même
  personne : l'id opaque les sépare. Toute fusion sur le libellé serait le défaut à proscrire.
- **Dédoublonnage par id, jamais le libellé** (règle 17/18, s07) : deux ids distincts → deux entrées
  de légende même nom identique.

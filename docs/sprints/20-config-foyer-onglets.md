# Sprint 20 — Config foyer en onglets + convergence du dernier sélecteur (`config-foyer-onglets`)

> **Avancement : 3/7 ⏳**

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | Le sélecteur d'édition de l'écran config lit le **store vivant unifié** (`IEnumerationActeursFoyer`), **plus** `Foyer.ActeursEditables` — un **seul chemin de lecture** du référentiel acteurs | @back | ✅ |
| 2 | L'écran config présente **trois onglets par thème** (Acteurs / Période de garde / Slot récurrent), onglet **Acteurs actif par défaut**, contenu existant réparti | 🖥️ @ihm | ✅ |
| 3 | Onglet **Acteurs** = CRUD acteurs existant (édition / ajout / suppression) **iso-fonctionnel**, grille relue immédiatement | 🖥️ @ihm | ✅ |
| 4 | Onglet **Période de garde** = section cycle de fond existante **iso-fonctionnelle** (définir / éditer le cycle) | 🖥️ @ihm | ⏳ |
| 5 | Onglet **Slot récurrent** **réservé** (placeholder « à venir »), **aucune** fonctionnalité neuve | 🖥️ @ihm | ⏳ |
| 6 | Sélecteur d'édition (onglet Acteurs) **cohérent** avec dialogs + grille (source unifiée) **et** temps réel préservé : ajout/renommage depuis un 2ᵉ écran s'y reflète sans rechargement (SignalR) | 🖥️ @ihm | ⏳ |
| 7 | **Gating** identité effective **préservé sur chaque onglet** (Invité ne peut écrire) — non-régression du durcissement config | 🖥️ @ihm | ⏳ |

---

> Sujet `/planning` = `config-foyer-onglets` (épic É2), goal tranché **G2 (PO)** sur le 3ᵉ candidat.
> **Origine** : retour produit s17 #6 (« refonte de l'écran config foyer en onglets par thème, proposition
> de structuration attendue ») **+** dette de cohérence signalée à la clôture s19 (le sélecteur d'édition
> config lit **encore** `Foyer.ActeursEditables` au lieu du store vivant unifié). Store durable (Mongo,
> s15) → **acceptation runtime obligatoire**. Suite courante = **281/281**.
>
> **Décisions CP (déterministes).** Périmètre = **@ihm dominant** (réagencement de surface) **+ un**
> scénario @back bornant la **convergence de la source de lecture**. **Aucun handler d'écriture neuf** :
> on réutilise le CRUD acteurs (s08/s09/s13) et la définition/édition du cycle de fond (s10). Le sujet est
> **iso-fonctionnel** : on **réorganise** l'existant en onglets et on **unifie** le dernier chemin de
> lecture ; **aucune règle métier neuve**, **aucune persistance neuve**.
>
> **Structuration des onglets (proposition CP, tranchée pour ce sprint).** Trois onglets par thème :
> **Acteurs** (CRUD acteurs existant), **Période de garde** (section cycle de fond existante),
> **Slot récurrent** (**réservé** — le slot récurrent n'existe pas encore ; placeholder « à venir »
> **sans** fonctionnalité neuve, pour tenir la structure proposée par le PO sans violer l'iso-fonctionnel).
> Onglet **Acteurs actif par défaut**. Le passage d'un onglet à l'autre **ne perd pas** l'état et **ne
> casse aucune** écriture existante.
>
> **Convergence (dette s19).** Le sélecteur d'édition de l'écran config doit lire **exclusivement** le
> **store vivant unifié** `IEnumerationActeursFoyer` (id stable), **même source** que les sélecteurs des
> dialogs, la grille et la légende (convergés au s19). Après ce sprint, il existe **un seul chemin de
> lecture** du référentiel acteurs ; `Foyer.ActeursEditables` n'est **plus** la source du sélecteur config.
> Observable : cohérence stricte config ↔ dialogs ↔ grille, y compris sous propagation SignalR d'un 2ᵉ écran.
>
> **Hors scope** : modèle de rôles (retour s17 #3/#4/#5), transfert bicolore diagonale (s17 #7), toute
> **nouvelle** fonctionnalité de slot récurrent (l'onglet est un placeholder), refonte visuelle profonde
> du thème. Pas de nouvel écran hors les onglets ; pas de nouvelle persistance ni de handler d'écriture.

---

## Scénarios

### @back — convergence de la source de lecture (frontière Application/ports)

```gherkin
@back @vert
Scénario 1 — Un seul chemin de lecture du référentiel acteurs (store vivant unifié)
  Étant donné un foyer dont le store d'acteurs déclarés (id stables) est alimenté via le CRUD acteurs
  Quand l'écran de configuration énumère les acteurs éditables de son sélecteur d'édition
  Alors la liste provient exclusivement du store vivant unifié (IEnumerationActeursFoyer, id stable)
    Et elle n'est plus lue depuis Foyer.ActeursEditables (l'ancien chemin n'est plus la source)
    Et elle est strictement identique à la source lue par les sélecteurs des dialogs et la grille
    Et la suite complète reste verte (281/281, Docker actif, sans filtre ni --no-build)
```

### @ihm — écran config en onglets (RED → GREEN runtime)

```gherkin
@ihm @vert
Scénario 2 — Trois onglets par thème, Acteurs actif par défaut
  Étant donné que j'ouvre l'écran de configuration du foyer
  Alors je vois trois onglets : « Acteurs », « Période de garde », « Slot récurrent »
    Et l'onglet « Acteurs » est actif par défaut
    Et le contenu existant de l'écran est réparti dans ces onglets (rien de perdu, rien de dupliqué)
```

```gherkin
@ihm @vert
Scénario 3 — Onglet Acteurs : CRUD acteurs iso-fonctionnel
  Étant donné l'onglet « Acteurs » actif
  Quand je renomme, recolorie, ajoute ou supprime un acteur comme avant la refonte
  Alors l'écriture aboutit exactement comme au sprint précédent (aucun handler neuf)
    Et la grille et la légende sont relues immédiatement
    Et aucun comportement d'édition/ajout/suppression n'a régressé
```

```gherkin
@ihm @pending
Scénario 4 — Onglet Période de garde : cycle de fond iso-fonctionnel
  Étant donné l'onglet « Période de garde » actif
  Quand je définis ou j'édite le cycle de fond (mapping index → acteur, alternance par parité ISO)
  Alors la définition/édition aboutit exactement comme au sprint précédent (réutilise DefinirCycle)
    Et la grille résout le responsable de fond comme avant (surcharge > fond > neutre)
    Et aucun comportement du cycle de fond n'a régressé
```

```gherkin
@ihm @pending
Scénario 5 — Onglet Slot récurrent réservé (placeholder, aucune fonctionnalité neuve)
  Étant donné l'onglet « Slot récurrent »
  Quand je l'ouvre
  Alors il affiche un placeholder « à venir » (structure tenue sans fonctionnalité neuve)
    Et aucune écriture ni persistance n'est déclenchée depuis cet onglet
```

```gherkin
@ihm @pending
Scénario 6 — Sélecteur config cohérent (source unifiée) + temps réel préservé
  Étant donné deux écrans ouverts sur le même foyer (store partagé) et l'onglet « Acteurs » actif
  Alors le sélecteur d'édition config propose exactement les mêmes acteurs déclarés que les dialogs et la grille (id stable)
  Quand j'ajoute ou renomme un acteur depuis le second écran
  Alors le sélecteur d'édition config du premier écran reflète le changement sans rechargement (SignalR)
    Et il reste cohérent avec la grille, la légende et les sélecteurs des dialogs (aucune divergence de source)
```

```gherkin
@ihm @pending
Scénario 7 — Gating identité effective préservé sur chaque onglet
  Étant donné une identité effective « Invité » (non Parent/Admin)
  Quand j'ouvre chacun des onglets de l'écran de configuration
  Alors les actions d'écriture (éditer/ajouter/supprimer un acteur, définir/éditer le cycle) sont gatées sur chaque onglet
    Et le durcissement du gating config acquis au s14 n'a pas régressé
```

---

# Retours produit (PO)

<!-- Rempli après le gate G3 (clôture). Un item par retour. -->

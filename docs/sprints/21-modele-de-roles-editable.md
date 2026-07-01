# Sprint 21 — Modèle de rôles éditable (`modele-de-roles-editable`)

> **Avancement : 6/11 ⏳**

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | **Créer un rôle** dans le référentiel (id stable opaque, libellé, persisté config foyer Mongo borné) | @back | ✅ |
| 2 | **Renommer un rôle** du référentiel (même id stable, libellé mis à jour, persisté) | @back | ✅ |
| 3 | **Rejet** création/renommage : libellé vide **ou** doublon de libellé (référentiel inchangé) | @back | ✅ |
| 4 | **Affecter un rôle à un acteur** : la valeur provient **exclusivement** du référentiel ; rôle hors référentiel **rejeté** (jamais de rôle en dur) | @back | ✅ |
| 5 | **Acteur sans rôle = neutre assumé** (aucun rôle fantôme, aucune erreur ; l'attribut rôle est optionnel) | @back | ✅ |
| 6 | **Supprimer un rôle référencé** → les acteurs porteurs retombent « sans rôle » (repli neutre, pas de rôle fantôme), **idempotence** (absent/déjà supprimé = no-op qui réussit) | @back | ✅ |
| 7 | Onglet **Acteurs** : **gérer le référentiel de rôles** (créer / renommer / supprimer : Nounou, Grand-parent…) depuis l'écran config | 🖥️ @ihm | ⏳ |
| 8 | Onglet **Acteurs** : **affecter un rôle à un acteur** via un **sélecteur borné au référentiel** ; acteur sans rôle affiché **neutre** | 🖥️ @ihm | ⏳ |
| 9 | **Gating identité effective** : « Invité » ne peut **ni** gérer le référentiel **ni** affecter un rôle (non-régression durcissement s14, par onglet s20) | 🖥️ @ihm | ⏳ |
| 10 | **Temps réel SignalR** : création / suppression d'un rôle propagée à un **2ᵉ écran** sans rechargement (sélecteur de rôle relu) | 🖥️ @ihm | ⏳ |
| 11 | **Non-régression grille/légende** : la résolution de responsabilité (surcharge > fond > neutre) est **inchangée** ; le rôle **n'intervient pas** dans la case ni la légende | 🖥️ @ihm | ⏳ |

---

> Sujet `/planning` = `modele-de-roles-editable` (épic É2), goal tranché **G2 (PO)** sur le 1er candidat.
> **Origine** : retours produit s17 **#3** (rôle affectable à un acteur — Nounou / Grand-parent) **+ #4**
> (les parents créent/gèrent les rôles — référentiel éditable, non figé) **+ #5** (seuls les rôles définis
> sont utilisés dans l'app — pas de rôle en dur). Store durable (Mongo config foyer, s09/s15) →
> **acceptation runtime obligatoire**. Suite courante = **288/288**.
>
> **Décisions CP (déterministes).**
> - **Référentiel de rôles = nouveau petit agrégat de config foyer**, miroir du CRUD acteurs
>   (s08/s09/s13). Port de lecture `IEnumerationRoles` + port d'écriture `IEditeurReferentielRoles`
>   (créer / renommer / supprimer), **deux adaptateurs de droite** : InMemory (tests) **+** Mongo
>   (runtime), **bornés à la config foyer** — réutilise le socle Mongo config déjà acquis, **ne tire
>   aucune persistance neuve hors config foyer** (borne anti-cliquet règle 30 respectée). Un rôle =
>   **id stable opaque** + libellé éditable.
> - **Rôle porté par l'acteur = attribut optionnel** (id de rôle **nullable**, `null` = « sans rôle »
>   = neutre assumé). L'affectation **réutilise le chemin d'écriture de la config acteur** (édition
>   d'acteur, s08) **augmenté** d'un id de rôle — **aucun nouveau modèle de concurrence**, **aucun
>   nouvel agrégat acteur**. **Borne dure** : affecter un id de rôle **absent du référentiel** = rejet
>   (le champ est fermé sur le référentiel, jamais un libellé en dur).
> - **Aucune règle de résolution grille/légende touchée.** Le rôle est une **caractéristique
>   d'organisation/affichage de l'acteur**, **pas** une responsabilité de garde : `GrilleAgendaQuery`
>   et la résolution surcharge > fond > neutre restent **inchangées** (Sc.11 le borne en
>   non-régression). Aucune teinte, aucun nom de case, aucune légende ne dépend du rôle ce sprint.
> - **Suppression d'un rôle référencé** = miroir du repli acteur orphelin (s13/s19) : les acteurs qui
>   le portaient retombent « sans rôle » (neutre), **pas de rôle fantôme**, **idempotence** (absent /
>   déjà supprimé = no-op qui réussit). Pas de blocage sur rôle référencé (repli, pas rejet).
> - **Placement IHM** : tout dans l'**onglet Acteurs** de l'écran config (structure s20), **gating
>   identité effective par onglet** préservé (s14/s20), écran **abonné au hub SignalR lecture** (s20)
>   pour la ré-énumération temps réel du référentiel de rôles.
>
> **Hors scope** : afficher le rôle **dans la grille/légende** (le rôle ne pilote pas la responsabilité
> ce sprint) ; **droits/actions dérivés du rôle** (Admin/Parent/Autre — reste É2/É10, palier 5/13) ;
> transfert bicolore diagonale (s17 #7) ; rétrofit flake TempsReel (dette P1, +2) ; toute persistance
> hors config foyer. Pas de nouvel écran hors l'onglet Acteurs ; pas de handler de résolution neuf.

---

## Scénarios

### @back — référentiel de rôles & affectation (frontière Application/ports)

```gherkin
@back @vert
Scénario 1 — Créer un rôle dans le référentiel
  Étant donné un foyer configuré (store de config durable) sans rôle « Nounou »
  Quand le parent configurateur crée un rôle de libellé « Nounou »
  Alors le référentiel contient un rôle « Nounou » doté d'un identifiant stable opaque neuf
    Et ce rôle est persisté avec la config foyer (survit au redémarrage, store Mongo réel)
    Et l'énumération des rôles (IEnumerationRoles) le retourne exactement une fois
```

```gherkin
@back @vert
Scénario 2 — Renommer un rôle du référentiel
  Étant donné un référentiel contenant un rôle « Nounou » (id stable connu)
  Quand le parent configurateur renomme ce rôle en « Assistante maternelle »
  Alors le rôle conserve le même identifiant stable
    Et son libellé est « Assistante maternelle » (persisté, relu après redémarrage)
    Et aucun doublon n'est créé (toujours un seul rôle pour cet id)
```

```gherkin
@back @vert
Scénario 3 — Rejet : libellé vide ou doublon
  Étant donné un référentiel contenant déjà un rôle « Grand-parent »
  Quand le parent tente de créer un rôle de libellé vide
    Ou tente de créer un second rôle de libellé « Grand-parent »
  Alors la commande échoue avec un motif clair (libellé requis / libellé déjà défini)
    Et le référentiel reste inchangé (aucun rôle vide ni doublon persisté)
```

```gherkin
@back @vert
Scénario 4 — Affecter un rôle (du référentiel) à un acteur — champ borné
  Étant donné un acteur déclaré (id stable) et un référentiel contenant le rôle « Nounou »
  Quand on affecte à cet acteur le rôle « Nounou » (par son id de rôle)
  Alors l'acteur porte l'id de rôle « Nounou » (persisté avec la config acteur)
  Mais quand on tente d'affecter un id de rôle absent du référentiel
  Alors l'affectation est rejetée (valeur hors référentiel, jamais de rôle en dur)
    Et l'acteur conserve son rôle précédent (aucune écriture d'un rôle inconnu)
```

```gherkin
@back @vert
Scénario 5 — Acteur sans rôle = neutre assumé
  Étant donné un acteur déclaré auquel aucun rôle n'a été affecté
  Quand on énumère cet acteur et son rôle
  Alors son rôle est « sans rôle » (attribut optionnel non renseigné, valeur neutre)
    Et aucune erreur n'est levée, aucun rôle fantôme n'est inventé
    Et retirer le rôle d'un acteur qui en portait un le ramène à « sans rôle » (neutre)
```

```gherkin
@back @vert
Scénario 6 — Supprimer un rôle référencé : repli neutre + idempotence
  Étant donné un référentiel contenant « Nounou » et un acteur portant ce rôle
  Quand le parent supprime le rôle « Nounou » du référentiel
  Alors le rôle disparaît du référentiel (persisté, relu après redémarrage)
    Et l'acteur qui le portait retombe « sans rôle » (repli neutre, aucun rôle fantôme)
    Et supprimer un rôle déjà absent est un no-op qui réussit (idempotence)
    Et la suite complète reste verte (288/288, Docker actif, sans filtre ni --no-build)
```

### @ihm — gestion des rôles dans l'onglet Acteurs (RED → GREEN runtime)

```gherkin
@ihm @pending
Scénario 7 — Onglet Acteurs : gérer le référentiel de rôles
  Étant donné l'onglet « Acteurs » de l'écran de configuration, actif
  Quand je crée un rôle « Nounou », le renomme, puis crée un rôle « Grand-parent »
  Alors les rôles créés apparaissent dans la liste des rôles du foyer
    Et je peux supprimer un rôle ; il disparaît de la liste
    Et les écritures aboutissent sur le store réel (persistées, survivent au redémarrage)
```

```gherkin
@ihm @pending
Scénario 8 — Onglet Acteurs : affecter un rôle via un sélecteur borné au référentiel
  Étant donné un acteur déclaré et un référentiel contenant « Nounou » et « Grand-parent »
  Quand j'ouvre le sélecteur de rôle de cet acteur
  Alors il propose exactement les rôles du référentiel (plus une option « sans rôle »), aucun libellé en dur
  Quand je lui affecte « Nounou »
  Alors l'acteur porte « Nounou » (persisté), et un acteur sans rôle s'affiche « sans rôle » (neutre)
```

```gherkin
@ihm @pending
Scénario 9 — Gating identité effective : Invité ne gère ni le référentiel ni les affectations
  Étant donné une identité effective « Invité » (non Parent/Admin) sur l'onglet Acteurs
  Quand j'ouvre l'écran de configuration
  Alors les actions de gestion des rôles (créer/renommer/supprimer) sont gatées
    Et l'affectation d'un rôle à un acteur est gatée
    Et le durcissement du gating config (s14) et le gating par onglet (s20) n'ont pas régressé
```

```gherkin
@ihm @pending
Scénario 10 — Temps réel : le référentiel de rôles converge sur un 2ᵉ écran
  Étant donné deux écrans de configuration ouverts sur le même foyer (store partagé)
  Quand je crée puis supprime un rôle depuis le second écran
  Alors la liste des rôles et les sélecteurs de rôle du premier écran reflètent le changement sans rechargement (SignalR)
    Et un acteur portant un rôle supprimé retombe « sans rôle » sur les deux écrans, sans rôle fantôme
```

```gherkin
@ihm @pending
Scénario 11 — Non-régression : grille et légende inchangées par le rôle
  Étant donné des acteurs porteurs de rôles et un cycle de fond + périodes existants
  Quand la grille et la légende sont projetées
  Alors la responsabilité résolue (surcharge > fond > neutre) est strictement inchangée
    Et ni la teinte, ni le nom de case, ni la légende ne dépendent du rôle
    Et aucun comportement de résolution acquis (paliers 6/8/9, s13/s19) n'a régressé
```

---

# Retours produit (PO)

<!-- Rempli après le gate G3 (clôture). Un item par retour. -->

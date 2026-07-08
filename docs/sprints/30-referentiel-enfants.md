# Sprint 30 — Référentiel d'enfants (hisser l'enfant en agrégat de 1er rang)

> **Goal (G2 tranché PO)** : hisser l'**enfant** en **agrégat de premier rang**, **miroir strict du
> hissage des lieux (s27)**. Aujourd'hui l'`EnfantId` (« Léa ») reste **implicite/masqué** : il est
> transmis via `Session.EnfantId` au canal d'écriture **sans jamais être choisi** (fantôme).
> Bloquant dès qu'un foyer a **≥2 enfants** (dette P1 actée au gate s29). Ce sprint rend l'enfant
> **explicite, énumérable, persisté et choisi** dans la dialog de pose.
>
> Volets : (1) **agrégat Enfant** (id stable opaque + prénom) + **port d'énumération** (droite),
> (2) **onglet « Enfants »** dans la Config du foyer (lister / ajouter / éditer), (3) **persistance
> Mongo durable + InMemory**, (4) **sélecteur d'enfant explicite** dans la dialog de pose (slot
> ponctuel ET récurrent), (5) **rétro-affectation** idempotente des slots existants du fantôme.

## Périmètre — DANS / HORS scope

- **DANS** : agrégat `Enfant` (identifiant stable neuf, jamais dérivé du libellé) + snapshot
  (prénom) ; **port d'énumération** de droite (miroir du référentiel de lieux s27) ; commandes/handlers
  **ajouter** et **éditer** (rejets **libellé vide** / **doublon** sans écriture, R5/R6) ; **persistance
  durable Mongo** + InMemory (parité asymétrie seed s15) ; **validation de pose** contre le référentiel
  (slot référençant un enfant inconnu → rejet, miroir lieu inconnu s29 S2) ; **migration rétro-affectation
  idempotente** des slots existants attachés au fantôme « Léa » vers l'enfant réel, **prouvée sur store
  réel** ; **onglet IHM « Enfants »** dans la Config du foyer ; **sélecteur d'enfant** dans les dialogs de
  pose (ponctuel + récurrent).
- **HORS scope (autres sprints / backlog)** : **récurrence multi-jours** (D2) ; **slot conditionné à la
  garde** (D1) ; **transfert auto-dérivé** (D3) — *D1 + D3 reportés ensemble au **sprint 31**, retranchés
  au /planning s31 (le PO les veut combinés là-bas)*. Également hors scope : **suppression** d'un enfant
  (Delete — non demandé ce sprint) ; familles recomposées / graphe de parents (R2/R3) ; sélecteur d'enfant
  dans les autres surfaces que la pose (périodes, transferts) ; multi-enfants dans le cycle de fond.

> **Décision SM — miroir strict du hissage lieux s27, l'enfant reste une DONNÉE de référentiel.**
> L'agrégat `Enfant` suit le patron déjà éprouvé de `FoyerLieu` (s27) : **identifiant stable opaque +
> libellé (prénom)**, énuméré par un **port de droite**, persisté derrière un adaptateur durable. Le
> hissage **ne touche pas** la résolution de responsabilité (surcharge > fond > neutre inchangée) : un
> enfant est un **axe de rattachement du slot**, pas un responsable.

> **Décision SM — borne « ≥1 enfant » (R1) non exercée ce sprint, mais tenue par construction.** La
> spec règle 1 impose **au moins un enfant** par foyer. Comme la **suppression est hors scope**, la
> borne n'est **testée par aucun scénario** ici ; elle est **garantie par la migration** : le fantôme
> « Léa » devient le **premier enfant réel** (le foyer part donc toujours avec ≥1 enfant). La borne
> défensive au Delete est reportée au sprint qui introduira la suppression d'enfant.

> **Décision SM — rétro-affectation = migration idempotente, pas une règle métier neuve.** Les slots
> existants portent un `EnfantId` fantôme (« Léa ») transmis par `Session`. La migration **réattache**
> ces slots à l'enfant réel correspondant, **une fois**, de façon **idempotente** (rejeu = no-op). Elle
> **n'invente aucune sémantique** de rattachement : elle solde une dette de données. Preuve **sur store
> Mongo réel** (pas par doublure).

> **Preuve = runtime réel** (Docker/Mongo actifs, suite complète sans filtre ni `--no-build`).
> Persistance et migration prouvées sur **store Mongo durable**. **Aucune** entorse de preuve par
> doublure ici → statuts `⏳`/`🔴`/`✅` **francs**, **pas de dette de câblage**.

## Avancement — 7/10 (back 8 · IHM 2)

| # | Scénario | Type | Statut |
|---|----------|------|:------:|
| S1 | Ajouter un enfant valide → succès + id stable neuf + snapshot (prénom) + diffusion | @back | ✅ |
| S2 | Rejet : prénom vide → échec **sans écriture** (miroir libellé vide, R5) | @back | ✅ |
| S3 | Rejet : prénom **doublon** d'un enfant existant → échec **sans écriture** (R6) | @back | ✅ |
| S4 | Éditer le prénom d'un enfant existant (clé = **id stable**) → succès, relu, diffusion | @back | ✅ |
| S5 | **Port d'énumération** : liste les enfants du foyer (id stable + prénom), dédoublonnée par id | @back | ✅ |
| S6 | Persistance durable Mongo : un enfant **survit au redémarrage** (parité asymétrie seed s15) | @back | ✅ |
| S7 | Pose d'un slot référençant un enfant **inconnu** du foyer → rejet **sans écriture** (miroir lieu inconnu s29) | @back | ✅ |
| S8 | **Migration rétro-affectation idempotente** : slots du fantôme « Léa » réattachés à l'enfant réel ; rejeu = no-op ; **prouvé store réel** | @back | ⏳ |
| S9 | IHM : onglet **« Enfants »** (Config foyer) — lister / ajouter / éditer, rejets visibles sans enregistrer (RED→GREEN) | 🖥️ IHM | ⏳ |
| S10 | IHM : **sélecteur d'enfant** explicite dans la dialog de pose (ponctuel + récurrent) — choix transmis, plus de fantôme — **gate G3** | 🖥️ IHM | ⏳ |

## Scénarios

### Volet back — Agrégat Enfant + port d'énumération (frontière Application)

```gherkin
@back @vert
Scénario: S1 — Ajouter un enfant valide
  Étant donné un foyer configuré
  Quand un Parent ajoute un enfant de prénom "Léa"
  Alors la commande réussit
  Et l'enfant est enregistré avec un identifiant stable neuf (jamais dérivé du prénom)
  Et son snapshot porte : prénom = "Léa"
  Et la diffusion temps réel de mise à jour est déclenchée

@back @vert
Scénario: S2 — Rejet d'un enfant au prénom vide
  Étant donné un foyer configuré
  Quand un Parent ajoute un enfant de prénom "" (vide ou blancs)
  Alors la commande échoue avec un motif clair (prénom requis)
  Et aucun enfant n'est enregistré
  Et aucune diffusion n'est déclenchée

@back @vert
Scénario: S3 — Rejet d'un enfant au prénom doublon
  Étant donné un foyer dont le référentiel d'enfants contient déjà "Léa"
  Quand un Parent ajoute un enfant de prénom "Léa"
  Alors la commande échoue avec un motif clair (prénom déjà existant)
  Et aucun second enfant "Léa" n'est enregistré
  Et le référentiel d'enfants est inchangé

@back @vert
Scénario: S4 — Éditer le prénom d'un enfant existant
  Étant donné un enfant enregistré d'identifiant stable connu, prénom "Léa"
  Quand un Parent édite son prénom en "Léana" (clé = identifiant stable)
  Alors la commande réussit
  Et l'enfant relu porte le prénom "Léana" avec le même identifiant stable
  Et la diffusion temps réel de mise à jour est déclenchée
  # Un prénom vide ou en doublon d'un autre enfant est rejeté (mêmes rejets que S2/S3), rien appliqué

@back @vert
Scénario: S5 — Le port d'énumération liste les enfants du foyer
  Étant donné un référentiel d'enfants contenant "Léa" et "Tom"
  Quand on énumère les enfants du foyer via le port de droite
  Alors la liste porte "Léa" et "Tom" avec leurs identifiants stables et prénoms
  Et la liste est dédoublonnée par identifiant (jamais par libellé)

@back @vert
Scénario: S6 — Un enfant persiste sur le store durable Mongo
  Étant donné le store Mongo durable actif
  Et un enfant "Léa" enregistré avec son identifiant stable
  Quand le référentiel d'enfants est relu après redémarrage (nouvelle instance de dépôt)
  Alors l'enfant "Léa" est toujours présent avec son identifiant stable et son snapshot intacts
  # Parité asymétrie seed s15 : en mode Mongo, AUCUN enfant seedé au 1er lancement

@back @vert
Scénario: S7 — Rejet d'une pose de slot référençant un enfant inconnu du foyer
  Étant donné un foyer dont le référentiel d'enfants NE contient PAS l'identifiant "enfant-x"
  Quand un Parent pose un slot en référençant l'enfant "enfant-x"
  Alors la commande échoue avec un motif clair (enfant inexistant)
  Et aucun slot n'est enregistré
  Et aucune diffusion n'est déclenchée
  # Miroir de la validation "lieu inconnu" du slot récurrent (s29 S2)

@back @pending
Scénario: S8 — Migration idempotente de rétro-affectation des slots du fantôme
  Étant donné le store Mongo durable actif
  Et des slots existants attachés au fantôme "Léa" (EnfantId transmis via Session, jamais choisi)
  Et un enfant réel "Léa" présent au référentiel d'enfants
  Quand la migration de rétro-affectation est exécutée
  Alors chaque slot du fantôme est réattaché à l'identifiant stable de l'enfant réel "Léa"
  Et la re-projection de la grille montre ces slots rattachés à l'enfant réel
  Quand la migration est rejouée sur le même store
  Alors elle réussit en no-op (idempotence), sans double rattachement ni erreur
```

### Volet IHM — Onglet Enfants + sélecteur de pose (menée RED→GREEN runtime, fin de sprint)

```gherkin
@ihm @pending
Scénario: S9 — Configurer les enfants depuis l'onglet "Enfants" de la Config du foyer
  Étant donné un Parent connecté sur la Configuration du foyer
  Quand il ouvre l'onglet "Enfants"
  Alors il voit la liste des enfants du foyer (prénoms)
  Quand il ajoute un enfant "Tom" valide et valide
  Alors "Tom" apparaît dans la liste
  Et un prénom vide ou en doublon laisse un message d'erreur sans rien enregistrer
  Quand il édite le prénom d'un enfant existant et valide
  Alors la liste reflète le nouveau prénom sans rechargement

@ihm @pending
Scénario: S10 — Sélectionner explicitement l'enfant dans la dialog de pose
  Étant donné un Parent connecté et un foyer avec les enfants "Léa" et "Tom"
  Quand il ouvre la dialog "Poser un slot" (slot ponctuel comme récurrent)
  Alors un sélecteur d'enfant est présent et l'enfant n'est plus implicite (fantôme retiré)
  Quand il choisit "Tom" et pose le slot
  Alors le slot enregistré porte l'identifiant stable de l'enfant "Tom" (transmis au canal d'écriture existant)
  Et non plus l'EnfantId de session
  # Validation visuelle au gate G3 : onglet Enfants rendu + sélecteur rendu, cohérence
```

# Retours produit (PO)

<!-- Rempli après le gate G3, consommé à la /cloture. -->

# Sprint 35 — Refonte Config foyer : Lieux → « Activités » liées à l'enfant

> **Goal G2 (tranché PO, délégué au SM)** : **4ᵉ incrément vertical** de l'épic **Refonte de la
> Configuration du foyer** (brief `docs/briefs/refonte-configuration-foyer.md`). Le PO repense le
> **« lieu »** comme une **« activité » liée à l'enfant** (« lieux n'est pas le bon terme »). Une
> tranche verticale en trois volets, **miroir direct du lien enfant↔parent s34** :
> - **@back — renommer le référentiel « Lieux » (s27) en « Activités ».** Refactor **sémantique
>   iso-comportement** de l'agrégat / ports / query / adaptateurs (InMemory seedé + Mongo durable) ;
>   la **validation de pose reste PRÉSERVÉE** (poser un slot sur une activité inconnue reste refusé
>   sans écriture, miroir « lieu inconnu » s29).
> - **@back — enrichir l'agrégat Activité d'un champ « adresse ».** **Miroir strict de l'adresse
>   acteur s33** : persistée Mongo durable, **vide accepté** (optionnel), **aucune écriture
>   partielle** des autres champs.
> - **@back — lien enfant↔activité N-M.** Commandes **lier / délier** idempotentes, **bornées au
>   référentiel** (enfant s30 ET activité), **rejets enfant/activité inconnu sans écriture
>   partielle**, id stables inchangés (enrichissement, pas recréation) — miroir du lien
>   enfant↔parent s34. Plusieurs enfants peuvent **partager la même activité** (N-M).
> - **@ihm — onglet « Activités » au patron tableau lecture seule + crayon → modal** (miroir
>   Acteurs s32 / Enfants s34) : colonnes **libellé + adresse + « Enfants liés »** en lecture, modal
>   avec **champ adresse** + **sélecteur des enfants**, **Échap = Annuler** (port
>   `IEcouteurEchapModal`, s33), **Parent-gated**, **convergence SignalR** lecture.
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (renommage iso-comportement → adresse →
> lien enfant↔activité + rejets + persistance Mongo) → puis @ihm (onglet Activités au patron + modal
> + sélecteur d'enfants + invariants).
>
> **HORS scope (périmètre resserré, décision SM)** :
> - **Liste de slots récurrents/non par activité** (le PO évoque une activité « avec une liste de
>   slots ») : **non traité ici** — extension du modèle de slots, incrément séparé.
> - **Lien adresse acteur-parent ↔ « lieu/domicile » de l'enfant en garde** (retour PO à chaud s33) :
>   la relation acteur↔lieu (domicile-parent comme lieu implicite) est **non traitée ici**.
> - **Révision de la validation de pose** : ce sprint la **préserve iso**, ne la repense pas.
> - **Familles recomposées R2/R3** (« exactement 2 parents », graphe enfant-racine) : autre incrément.

## Avancement — 3/6

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | Renommer le référentiel **« Lieux » → « Activités »** (refactor iso-comportement, validation de pose préservée) | back | ✅ |
| 2 | Champ **« adresse »** sur l'agrégat Activité (Mongo durable, vide accepté, sans écriture partielle) | back | ✅ |
| 3 | Lien **enfant↔activité N-M** : commandes lier/délier idempotentes + rejets sans écriture partielle | back | ✅ |
| 4 | **SWAP** onglet « Lieux » inline → onglet **« Activités »** tableau lecture + crayon → modal (+ renommage HTTP/DTO/record Web, lot atomique) | 🖥️ IHM | ⏳ |
| 5 | Modal activité : **champ adresse** + **sélecteur des enfants** à lier / délier | 🖥️ IHM | ⏳ |
| 6 | Invariants Activités — refus→modal ouverte + **Parent-gated** + convergence **SignalR** | 🖥️ IHM | ⏳ |

> **⚠️ POINT DE VIGILANCE — le renommage Lieux→Activités est un REFACTOR TRANSVERSE, TRANCHÉ EN DEUX
> SEAMS (décision SM).** Sc.1 est **@back-scoped** : renommage **Domaine + Application UNIQUEMENT**
> (agrégat / ports `IEnumerationLieux`,`IEditeurLieux` / query de config / handlers / commandes /
> **deux adaptateurs** InMemory seedé + Mongo durable) + la **validation de pose** (slot sur activité
> inconnue refusée) — **iso-comportement** (ajout/suppression, rejets vide/doublon, id stable +
> libellé, conservation de la référence d'un slot déjà posé **ne changent pas**, seul le **nom du
> concept** change). Le **nommage HTTP** (`/api/foyer/lieux`, `canal/*-lieu`), les **DTOs Api**, le
> **record Web `LieuFoyer`** et l'**onglet config « Lieux »** restent **INCHANGÉS en Sc.1** : leur
> renommage est **absorbé par le SWAP de Sc.4** (voir NB ci-dessous). L'adaptateur **Api MAPPE**
> « lieu » HTTP → « Activité » Application entre Sc.1 et Sc.4 — seam **temporaire, cohérent, vert**,
> **pas** une incohérence qui traîne. **GARDE lot atomique** : en Sc.1, le renommage `src/` back **et
> la migration des tests BACKEND** qui pilotaient « Lieu » = **deux faces d'un même refactor → MÊME
> commit** (rempart suite-complète-verte, aucun scénario s27/s29 ne régresse). *(récit :
> JOURNAL-METHODE s32.)*
>
> **Axe LOCALISATION distinct — non renommé ce sprint.** `SlotSnapshot.LieuId`,
> `PoserSlotCommand.LieuId`, la grille et le transfert portent le **« où »** de la garde : axe
> **distinct** du référentiel, **hors périmètre**, **pas renommé** (ni back ni HTTP/IHM) — préservé iso.

> **NB surface Sc.4 — SWAP CONFIRMÉ (découverte dev-team), PAS une surface neuve.** Une surface de
> config « Lieux » **EXISTE DÉJÀ** : onglet `onglet-lieux`/`panneau-lieux`, ajout+suppression
> **INLINE**, `liste-lieux`, routes `/api/foyer/lieux` + `canal/{ajouter,supprimer}-lieu`, record Web
> `LieuFoyer`, **tests Web dédiés**. Sc.4 est donc un **SWAP atomique** (lot-atomique-de-surface s32) :
> retrait de l'inline **ET** branchement tableau + crayon → modal **ET** renommage HTTP+DTO+record
> Web+testids/labels « Lieux »→« Activités », les **tests Web de l'inline MIGRANT vers la modal** —
> **UN seul commit**. On **n'anticipe PAS** ce renommage HTTP/IHM en Sc.1 (churn jetable des testids
> voués à être réécrits au swap). Le patron (tableau lecture + crayon → modal + Échap + Parent-gated
> + SignalR) reste **strictement** celui d'Acteurs s32 / Enfants s34.

> **@back réels attendus (Sc.1-3).** Sc.1 = **refactor** d'un référentiel **existant** (early-green
> massif attendu sur les comportements iso — c'est **voulu** : le renommage ne doit **rien** casser ;
> les tests s27/s29 renommés restent le **filet de non-régression**, à conserver). Sc.2 (adresse) et
> Sc.3 (lien enfant↔activité) sont des **modèles neufs** (champ + collection de liens + handlers +
> persistance Mongo étendue). Si une capacité s'avère **déjà couverte** au-delà de l'attendu, la
> dev-team **signale** au SM qui tranche (doublon supprimé / filet conservé), **sans** réinventer.

> **Définition « activité » + cadrage lien (SM).** Une **activité** = ce qui s'appelait un **lieu**
> (référentiel foyer, id stable + libellé, s27) **enrichi** d'une **adresse** (Sc.2). Le **lien
> enfant↔activité** est **N-M** (plusieurs enfants partagent une activité ; un enfant a plusieurs
> activités) — **distinct** du lien enfant↔parent s34 (borné 0..2). **Aucune borne de cardinalité**
> imposée sur le lien enfant↔activité ce sprint (0..N des deux côtés). Le lien est **optionnel** (0
> accepté). La **validation de pose** reste sur l'existence de l'activité (Sc.1), **pas** sur le lien
> enfant↔activité (non exigé à la pose ce sprint).

---

## Scénarios

### Sc.1 — Renommer le référentiel « Lieux » → « Activités » (iso-comportement, @back-scoped) @back @vert
```gherkin
Étant donné le référentiel de lieux éditable + persisté (s27) qui pilote la validation de pose,
  réalisé InMemory seedé + Mongo durable, exposé par la query de config au niveau Application
Quand le CONCEPT « Lieux » est renommé en « Activités » côté DOMAINE + APPLICATION UNIQUEMENT
  (agrégat / ports IEnumeration+IEditeur / query / handlers / commandes / deux adaptateurs)
Alors le comportement reste ISO : ajouter / supprimer une activité, rejets libellé VIDE ou DOUBLON,
  id stable + libellé — sémantique strictement inchangée (seul le NOM du concept change)
Et la VALIDATION DE POSE reste PRÉSERVÉE : poser un slot sur une activité INCONNUE est refusé SANS
  écriture (miroir « lieu inconnu » s29)
Et une activité déjà référencée par un slot posé CONSERVE sa référence (aucune réécriture rétroactive)
Et l'axe LOCALISATION du slot (LieuId sur SlotSnapshot / PoserSlotCommand / grille / transfert) est un
  axe DISTINCT (« où » a lieu la garde) et n'est PAS renommé ce sprint (hors périmètre du référentiel)
Et les tests BACKEND référençant « Lieu » migrent vers « Activité » dans le MÊME commit (lot atomique,
  aucune fenêtre rouge, iso-comportement prouvé : aucun scénario s27/s29 ne régresse)
Et le nommage HTTP (routes /api/foyer/lieux, canal/*-lieu), les DTOs Api, le record Web LieuFoyer et
  l'onglet config « Lieux » restent INCHANGÉS ici : leur renommage est ABSORBÉ par le SWAP de Sc.4
  (l'adaptateur Api MAPPE « lieu » HTTP → « Activité » Application — seam temporaire, cohérent, vert)
```

### Sc.2 — Champ « adresse » sur l'agrégat Activité @back @vert
```gherkin
Étant donné une activité du référentiel (id stable + libellé, Sc.1)
Quand elle est enrichie d'un champ « adresse » (miroir strict de l'adresse acteur s33)
Alors l'adresse est PERSISTÉE (Mongo durable) et relue par la query qui alimente la config
Et une adresse VIDE est acceptée (champ optionnel, comme l'adresse acteur s33)
Et éditer l'adresse ne touche AUCUN autre champ (aucune écriture partielle, id stable + libellé inchangés)
Et un refus (ex. libellé vidé en même temps) laisse le store INCHANGÉ (pas d'écriture partielle de l'adresse)
```

### Sc.3 — Lien enfant↔activité N-M : lier / délier + rejets @back @vert
```gherkin
Étant donné un enfant déclaré (agrégat s30) et une activité déclarée (référentiel renommé, Sc.1)
Quand la commande « lier un enfant à une activité » est émise (enfantId, activiteId)
Alors le lien enfant↔activité est porté et PERSISTÉ (Mongo durable), relu par la query de config
Et le lien est N-M : plusieurs enfants partagent une même activité ; un enfant porte plusieurs activités
Et les identifiants stables de l'enfant et de l'activité restent inchangés (enrichissement)
Quand la commande « délier un enfant d'une activité » est émise
Alors le lien est retiré et l'état PERSISTÉ ; délier un lien DÉJÀ absent est IDEMPOTENT (neutre, sans erreur)
Étant donné une commande « lier » désignant un enfant OU une activité INCONNU du référentiel
Alors le domaine REFUSE (motif restitué à l'appelant), sans écriture partielle (liens existants intacts)
```

### Sc.4 — SWAP onglet « Lieux » inline → onglet « Activités » tableau + crayon → modal @ihm @pending
```gherkin
Étant donné l'onglet de config « Lieux » EXISTANT (onglet-lieux/panneau-lieux, ajout+suppression INLINE,
  liste-lieux, routes /api/foyer/lieux + canal/*-lieu, record Web LieuFoyer, tests Web dédiés)
Quand cet onglet est BASCULÉ en onglet « Activités » — LOT ATOMIQUE, UN SEUL COMMIT : retrait de
  l'édition inline + branchement tableau lecture + crayon → modal, ET renommage HTTP
  (/api/foyer/lieux→activites, canal/*-lieu→*-activite) + DTOs Api + record Web LieuFoyer→ActiviteFoyer
  + testids/labels « Lieux »→« Activités », les tests Web de l'inline MIGRANT vers la modal (même commit)
Alors chaque activité apparaît sur UNE ligne de TABLEAU en LECTURE SEULE (libellé + adresse + colonne « Enfants liés »)
Et une colonne « Actions » porte un CRAYON par activité
Et un bouton « Ajouter une activité » est présent
Quand je clique le crayon d'une activité
Alors une MODAL pré-remplie s'ouvre (libellé, adresse, enfants liés courants) ; « Enregistrer » émet les
  commandes EXISTANTES (édition activité + adresse), la modal se ferme, le tableau est relu
Quand je clique « Ajouter une activité »
Alors la MÊME modal s'ouvre VIDE (mode création) → « Enregistrer » crée une activité (id stable neuf)
Et la fermeture Échap de la modal referme SANS mutation (port IEcouteurEchapModal, patron s33)
Et aucune surface d'édition inline « Lieux » ne subsiste (pas de code mort ni de coexistence durable)
```

### Sc.5 — Modal activité : champ adresse + sélecteur des enfants @ihm @pending
```gherkin
Étant donné la modal d'édition d'une activité ouverte (Parent)
Quand la modal est rendue
Alors le champ « adresse » est éditable (vide accepté)
Et un SÉLECTEUR des enfants (référentiel enfants s30) est proposé, les enfants DÉJÀ liés pré-affichés comme liés
Quand je lie un enfant puis « Enregistrer »
Alors la commande « lier » (Sc.3) est émise via le canal HTTP, la modal se ferme,
  la colonne « Enfants liés » en lecture reflète le nouvel enfant
Quand je délie un enfant puis « Enregistrer »
Alors la commande « délier » (Sc.3) est émise, l'enfant disparaît des enfants liés en lecture
Et HORS scope : liste de slots par activité, lien adresse acteur↔lieu (le sélecteur ne borne pas la cardinalité)
```

### Sc.6 — Invariants Activités : refus→modal ouverte + gating + SignalR @ihm @pending
```gherkin
Étant donné la modal activité (libellé + adresse + sélecteur d'enfants)
Quand une valeur est refusée par le domaine (libellé vide/doublon, enfant/activité inconnu) ou API injoignable
Alors la modal RESTE OUVERTE, le motif est affiché DEDANS, ma saisie (libellé + adresse + sélection) CONSERVÉE
Et le tableau reste INCHANGÉ (aucune écriture partielle)
Étant donné une identité EFFECTIVE non-Parent (Invité)
Quand j'ouvre l'onglet « Activités »
Alors le tableau reste en LECTURE SEULE, sans crayon ni « Ajouter », aucune modal atteignable
Étant donné deux écrans /configuration ouverts sur l'onglet « Activités »
Quand une activité est éditée (libellé / adresse / lien enfant) depuis le 1ᵉʳ écran
Alors le tableau du 2ᵉ écran CONVERGE (libellé + adresse + enfants liés) sans rechargement,
  sans écriture par la diffusion (canal SignalR lecture seule)
```

---

# Retours produit (PO)

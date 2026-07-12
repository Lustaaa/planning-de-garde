# Sprint 34 — Refonte Config foyer : Enfants au patron crayon→modal + lien enfant↔parent

> **Goal G2 (tranché PO)** : **3ᵉ incrément vertical** de l'épic **Refonte de la Configuration
> du foyer** (brief `docs/briefs/refonte-configuration-foyer.md`). Deux choses en une tranche
> verticale :
> - **@back neuf — lien enfant↔parent.** L'enfant est aujourd'hui un agrégat **nu**
>   `EnfantFoyer(Id, Prénom)` : **aucun lien parent**. On l'enrichit d'un lien vers **1..2
>   parents-acteurs** (« lier un enfant à 2 parents », retour PO **re-signalé au gate s33**),
>   avec commande/handler **lier / délier**, persistance **Mongo durable**, règle **« 2 parents
>   max »** et **rejets** (acteur inexistant / non-parent / déjà lié) **sans écriture partielle**.
> - **@ihm — onglet Enfants harmonisé** au patron **tableau lecture seule + crayon → modal**
>   (comme Acteurs s32 / Rôles-Cycle s33), avec **colonne « Parents liés » en lecture** et, dans
>   la modal, un **sélecteur des parents** à lier/délier (depuis le référentiel acteurs).
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (modèle enfant enrichi + commandes
> lier/délier + règles/rejets + persistance Mongo) → puis @ihm (swap de surface Enfants + modal +
> sélecteur parents + invariants).
>
> **HORS scope (périmètre resserré, décision SM)** :
> - **Familles recomposées (spec R2/R3)** — enfants de parents différents, graphe foyer complet,
>   « toujours exactement 2 parents » : **non traité ici**. Ce sprint pose le **lien 0..2 parents**
>   (borne haute), **pas** la contrainte d'exactement-2 ni le graphe recomposé.
> - **Renommage « Lieux » → « Activités »** + lien enfant↔activité + lien adresse acteur↔lieu :
>   autre incrément de l'épic, **non traité ici**.
> - **Vue d'accueil = graph enfant en racine** : présentation à venir, **non traitée ici** (ce
>   sprint = tableau + modal, pas de vue graphe).
> - **Suppression d'un enfant** (Delete + borne « ≥1 enfant » R1) : hors goal.

## Avancement — 5/6

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | Modèle enfant enrichi d'un lien parents + commande **lier** (persistance Mongo durable) | back | ✅ |
| 2 | Règles du lien : **« 2 parents max »** + rejets (inexistant / non-parent / déjà lié), sans écriture partielle | back | ✅ |
| 3 | Commande **délier** un enfant d'un parent (idempotente, relue par la query) | back | ✅ |
| 4 | Onglet Enfants au patron **tableau lecture + crayon → modal** + « Ajouter » (**swap de surface**, colonne « Parents liés ») | 🖥️ IHM | ✅ |
| 5 | Modal enfant : **sélecteur des parents** à lier / délier (depuis le référentiel acteurs) | 🖥️ IHM | ✅ |
| 6 | Invariants Enfants — refus→modal ouverte + **Parent-gated** + convergence **SignalR** | 🖥️ IHM | ⏳ |

> **⚠️ GARDE lot atomique de surface (Sc.4) — l'onglet Enfants porte AUJOURD'HUI une surface
> INLINE.** L'onglet Enfants (`ConfigurationFoyer.razor`) rend une **liste `<ul>` avec édition
> inline par ligne** (`data-testid="champ-editer-enfant"` + `bouton-editer-enfant`) et un
> **formulaire d'ajout inline** (`form-ajouter-enfant`, `champ-prenom-enfant`). Le passage au
> patron **tableau + crayon → modal** est donc un **swap de surface** : le **retrait de l'inline**
> et le **branchement de la modal** sont les **deux faces d'un même refactor** — mêmes commandes
> d'écriture (`ajouter-enfant` / `editer-enfant`), les tests d'acceptation qui pilotent l'inline
> **migrent vers la modal dans le MÊME commit** (juste l'étape « ouvrir la modal » ajoutée avant
> les mêmes champs). **Sc.4 = UN lot atomique = un seul commit** (chaque assertion restant
> individuelle), pour ne PAS ouvrir de **fenêtre rouge multi-scénarios** (rempart suite-complète-
> verte). **Interdit** : coexistence durable inline+modal (code mort + démolition contredisant le
> Gherkin). *(récit : JOURNAL-METHODE s32.)* Sc.5 (sélecteur parents) et Sc.6 (invariants) sont
> des **incréments propres** derrière, sans fenêtre rouge.

> **@back réels attendus (Sc.1-3).** Le lien enfant↔parent est un **modèle neuf** : l'agrégat
> `EnfantFoyer` gagne une collection de parents liés (0..2 `ActeurId`), un port de persistance
> **étendu** (Mongo durable) et des handlers **lier / délier**. La **validation d'existence &
> rôle** du parent réutilise le référentiel d'acteurs existant (énumération) — pas un référentiel
> neuf. Si une capacité s'avère **déjà couverte** (early-green improbable ici), la dev-team le
> **signale** au SM qui tranche (doublon supprimé / filet conservé), **sans** réinventer un handler.

> **Définition « parent » (cadrage SM).** Un **parent** = un acteur portant le **rôle Parent**
> (référentiel de rôles, s21/s33). Le rejet « non-parent » de Sc.2 s'appuie sur ce rôle. Borne
> haute **2 parents** = décision SM alignée sur le retour PO (« lier un enfant à 2 parents ») ;
> la contrainte **exactement 2** et les familles recomposées restent **hors scope** (spec R2/R3).

---

## Scénarios

### Sc.1 — Modèle enfant enrichi d'un lien parents + commande « lier » @back @vert
```gherkin
Étant donné un enfant déclaré dans le foyer (agrégat de 1er rang, id stable + prénom, s30)
Et un acteur du foyer portant le rôle « Parent » (référentiel acteurs/rôles existant)
Quand la commande « lier un enfant à un parent » est émise (enfantId, acteurId)
Alors le lien enfant→parent est porté par le modèle et PERSISTÉ (store réel, durable au rechargement)
Et la query qui alimente la config relit l'enfant avec la LISTE de ses parents liés
Et l'identifiant stable de l'enfant reste inchangé (enrichissement, pas recréation)
Et un enfant sans aucun parent lié reste valide (lien optionnel, 0 parent accepté)
```

### Sc.2 — Règles du lien : « 2 parents max » + rejets, sans écriture partielle @back @vert
```gherkin
Étant donné un enfant déjà lié à DEUX parents
Quand la commande « lier » émet un TROISIÈME parent
Alors le domaine REFUSE (borne « 2 parents max »), sans aucune écriture (les 2 liens existants intacts)
Étant donné une commande « lier » désignant un acteur qui N'EXISTE PAS dans le référentiel
Alors le domaine REFUSE (acteur inexistant), sans écriture
Étant donné une commande « lier » désignant un acteur existant mais qui N'EST PAS Parent
Alors le domaine REFUSE (non-parent), sans écriture
Étant donné une commande « lier » désignant un parent DÉJÀ lié à cet enfant
Alors l'opération est REFUSÉE ou NEUTRE (pas de doublon de lien), sans écriture partielle
Et dans tous les cas de refus, le motif est restitué à l'appelant et le store reste INCHANGÉ
```

### Sc.3 — Commande « délier » un enfant d'un parent @back @vert
```gherkin
Étant donné un enfant lié à un parent
Quand la commande « délier un enfant d'un parent » est émise (enfantId, acteurId)
Alors le lien est retiré et l'état PERSISTÉ (store réel, durable), l'enfant relu sans ce parent
Et l'identifiant stable de l'enfant et ses autres liens éventuels restent inchangés
Et délier un parent DÉJÀ non lié est IDEMPOTENT (neutre, sans erreur, sans écriture partielle)
```

### Sc.4 — Onglet Enfants au patron tableau + crayon → modal (SWAP DE SURFACE) @ihm @vert
```gherkin
Étant donné l'onglet « Enfants » de /configuration, connecté en tant que Parent
Quand la page est rendue
Alors chaque enfant apparaît sur UNE ligne de TABLEAU en LECTURE SEULE (prénom + colonne « Parents liés »)
Et une colonne « Actions » porte un CRAYON par enfant
Et un bouton « Ajouter un enfant » est présent
Et la surface d'édition INLINE préexistante des enfants (liste <ul>, champ-editer-enfant,
  formulaire form-ajouter-enfant inline) n'est PLUS rendue (lot atomique de surface, MÊME commit)
Quand je clique le crayon d'un enfant
Alors une MODAL pré-remplie s'ouvre (prénom courant, parents liés courants) ; « Enregistrer »
  émet la commande d'édition enfant EXISTANTE, la modal se ferme, le tableau est relu
Quand je clique « Ajouter un enfant »
Alors la MÊME modal s'ouvre VIDE (mode création) → « Enregistrer » crée un enfant (id stable neuf)
Et la fermeture Échap de la modal (patron s33) referme SANS mutation
```

### Sc.5 — Modal enfant : sélecteur des parents à lier / délier @ihm @vert
```gherkin
Étant donné la modal d'édition d'un enfant ouverte (Parent)
Quand la modal est rendue
Alors un SÉLECTEUR des parents (acteurs portant le rôle Parent, depuis le référentiel acteurs)
  est proposé, les parents DÉJÀ liés à cet enfant pré-cochés / affichés comme liés
Quand je lie un parent puis « Enregistrer »
Alors la commande « lier » (Sc.1) est émise via le canal HTTP, la modal se ferme,
  le tableau en lecture reflète le nouveau parent dans la colonne « Parents liés »
Quand je délie un parent puis « Enregistrer »
Alors la commande « délier » (Sc.3) est émise, le parent disparaît des parents liés en lecture
Et HORS scope : familles recomposées / exactement-2-parents / vue graphe (le sélecteur borne à 2)
```

### Sc.6 — Invariants Enfants : refus→modal ouverte + gating + SignalR @ihm @pending
```gherkin
Étant donné la modal enfant (prénom + sélecteur de parents)
Quand une valeur est refusée par le domaine (prénom vide/doublon, 3ᵉ parent, non-parent) ou API injoignable
Alors la modal RESTE OUVERTE, le motif est affiché DEDANS, ma saisie (prénom + sélection parents) CONSERVÉE
Et le tableau reste INCHANGÉ (aucune écriture partielle)
Étant donné une identité EFFECTIVE non-Parent (Invité)
Quand j'ouvre l'onglet « Enfants »
Alors le tableau reste en LECTURE SEULE, sans crayon ni « Ajouter », aucune modal atteignable
Étant donné deux écrans /configuration ouverts sur l'onglet « Enfants »
Quand un enfant est édité (prénom / lien parent) depuis le 1ᵉʳ écran
Alors le tableau du 2ᵉ écran CONVERGE (prénom + parents liés) sans rechargement,
  sans écriture par la diffusion (canal SignalR lecture seule)
```

---

# Retours produit (PO)

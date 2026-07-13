# Sprint 37 — Distinguer père / mère sur le lien enfant↔parent

> **Goal G2 (tranché PO — délégation, goal 1 du SM)** : enrichir le **lien enfant↔parent**
> (posé s34, éligibilité role-flag s36) d'un **attribut « rôle-du-lien »** distinguant les deux
> parents liés — aujourd'hui distingués par le **seul NOM**. Une tranche verticale :
> - **@back neuf — attribut « rôle-du-lien ».** Chaque lien enfant→parent porte un **rôle-du-lien**
>   ∈ **{père, mère, parent-libre}**, enrichissement du modèle de lien, **persisté Mongo durable**,
>   id enfant **inchangé**. Rejets **sans écriture partielle** (deux « père » ou deux « mère » sur
>   le même enfant), **compat** des liens déjà persistés sans attribut (**défaut « parent-libre »**,
>   pas de migration destructive).
> - **@ihm — sélecteur père/mère/parent** par parent lié dans la modal Enfants (patron crayon→modal
>   s34) + **affichage du rôle-du-lien** dans la colonne « Parents liés » du tableau.
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (modèle du lien enrichi + rejets + compat
> + query expose le rôle-du-lien) → puis @ihm (sélecteur dans la modal + rendu colonne + invariants).
>
> **HORS scope explicite (périmètre resserré, décision SM)** :
> - **Familles recomposées R2/R3** — « toujours exactement 2 parents », enfants de parents
>   différents, graphe foyer recomposé : **non traité ici** (goal séparé). Ce sprint **n'impose PAS**
>   « exactement 2 » ; borne 0..2 (s34) et éligibilité role-flag (s36) **inchangées**.
> - **Vue d'accueil = graphe enfant en racine** : goal séparé, **non traité ici**.
> - **Contrainte de cohérence père↔mère au-delà du « pas deux mêmes rôles »** (ex. « exactement 1
>   père ET 1 mère », complétude du couple) : **non exigée** ce sprint. Le seul invariant posé est
>   « pas deux liens de même rôle-du-lien sur un enfant » ; « parent-libre » reste répétable.
> - **Saisie/édition du `TypeActeur`**, champ père/mère au niveau de l'acteur lui-même : hors goal
>   (le rôle-du-lien vit sur le **lien**, pas sur l'acteur).

## Avancement — 2/5

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | Lien enfant↔parent enrichi d'un **rôle-du-lien** {père/mère/parent-libre} + « lier » avec rôle (persistance Mongo durable, id enfant inchangé) | back | ✅ |
| 2 | Rejets **sans écriture partielle** : deux « père » (ou deux « mère ») refusés ; borne 0..2 (s34) + éligibilité role-flag (s36) inchangées ; lier/délier idempotents | back | ✅ |
| 3 | **Compat + query** : le rôle-du-lien est relu au rechargement ; lien déjà persisté sans attribut → **défaut « parent-libre »** (pas de crash, pas de migration destructive) | back | ⏳ |
| 4 | Modal Enfants : **sélecteur père/mère/parent** par parent lié (pré-réglé) ; refus→modal ouverte + motif + sélection conservée ; Échap=Annuler | 🖥️ IHM | ⏳ |
| 5 | Colonne « Parents liés » affiche le **rôle-du-lien** à côté du nom ; **Parent-gated** + convergence **SignalR** | 🖥️ IHM | ⏳ |

> **⚠️ Point de vigilance — compat des liens déjà persistés (Sc.3).** Le lien enfant↔parent (s34)
> est déjà **persisté Mongo** sans rôle-du-lien. L'ajout de l'attribut est **rétro-compatible** :
> un lien stocké **sans** l'attribut se relit à **« parent-libre »** (défaut neutre), **jamais** un
> crash de désérialisation, **jamais** de migration destructive du store. Le rôle-du-lien est un
> **enrichissement additif** du modèle de lien (id enfant + liens existants intacts).

> **⚠️ GARDE lot atomique (Sc.1) — si un test s34 fige la FORME du lien.** Le lien enfant↔parent est
> aujourd'hui une **collection d'`ActeurId`** (s34) ; le passer à une **collection de {ActeurId,
> rôle-du-lien}** change la forme du modèle. Si un ou plusieurs **tests s34 figent cette forme**
> (assertion sur la structure du lien / de la commande « lier » / de la query), la **migration de
> ces tests vers la forme enrichie** et le **changement de modèle** sont les **deux faces d'un même
> refactor** → **même commit** (Sc.1), chaque assertion restant individuelle, pour ne PAS ouvrir de
> **fenêtre rouge multi-scénarios** (rempart suite-complète-verte). **Défaut « parent-libre »** =
> comportement s34 strictement préservé (un lien sans rôle explicite ≡ ancien lien nu). *(récit :
> JOURNAL-METHODE s32.)*

> **@back réels attendus (Sc.1-3).** Le rôle-du-lien est un **attribut neuf** porté par le lien,
> pas un référentiel neuf : la commande « lier » (s34) gagne un paramètre **rôle-du-lien** (défaut
> « parent-libre » si absent), le port de persistance (Mongo durable) **étend** la forme du lien, la
> query de config **surfait** le rôle-du-lien par parent lié. Éligibilité « parent » (role-flag s36)
> et borne 0..2 (s34) **réutilisées telles quelles**, non retouchées. Si une capacité s'avère **déjà
> couverte** (early-green improbable), la dev-team le **signale** au SM qui tranche (doublon supprimé
> / filet conservé), **sans** réinventer un handler.

> **Cadrage « rôle-du-lien » (décision SM).** Valeurs = **{père, mère, parent-libre}**. Invariant
> **minimal** ce sprint : **pas deux liens de même rôle exclusif** (père/mère) sur un même enfant ;
> **« parent-libre » reste répétable** (compat + neutralité). Ce n'est **pas** la contrainte R2/R3
> « exactement 2 parents » (hors scope). Le rôle-du-lien est **présentation + distinction**, il
> **n'intervient PAS** dans la résolution grille/légende ni dans le gating (miroir de l'invariant
> rôle-caractéristique R10).

---

## Scénarios

### Sc.1 — Lien enfant↔parent enrichi d'un « rôle-du-lien » + « lier » avec rôle @back @vert
```gherkin
Étant donné un enfant déclaré dans le foyer lié à un parent-acteur (lien s34, éligibilité role-flag s36)
Quand la commande « lier un enfant à un parent » est émise avec un rôle-du-lien (enfantId, acteurId, rôle ∈ {père, mère, parent-libre})
Alors le lien enfant→parent porte le rôle-du-lien et l'état est PERSISTÉ (store réel, durable au rechargement)
Et la query qui alimente la config relit l'enfant avec, pour chaque parent lié, son rôle-du-lien
Et l'identifiant stable de l'enfant reste inchangé (enrichissement additif, pas recréation)
Et lier SANS préciser de rôle vaut « parent-libre » (défaut neutre, comportement s34 préservé)
Et modifier le rôle-du-lien d'un parent déjà lié met à jour le lien SANS le dupliquer (id enfant + autres liens intacts)
```

### Sc.2 — Rejets sans écriture partielle + bornes s34/s36 inchangées @back @vert
```gherkin
Étant donné un enfant déjà lié à un parent avec le rôle « père »
Quand la commande « lier » désigne un SECOND parent avec le rôle « père »
Alors le domaine REFUSE (pas deux « père » sur un enfant), sans aucune écriture (lien existant intact)
Et le même refus vaut pour deux « mère » sur un même enfant
Étant donné deux liens de rôle « parent-libre » proposés sur un même enfant
Alors ils sont ACCEPTÉS (parent-libre répétable), dans la limite de la borne 0..2 parents (s34, inchangée)
Étant donné une commande « lier » désignant un acteur NON éligible (pas de rôle marqué parent, role-flag s36)
Alors le domaine REFUSE (éligibilité s36 inchangée), sans écriture, quel que soit le rôle-du-lien demandé
Et dans tous les cas de refus, le motif est restitué à l'appelant et le store reste INCHANGÉ
Et lier (rôle inclus) et délier restent IDEMPOTENTS (ré-émission neutre, sans doublon, sans écriture partielle)
```

### Sc.3 — Compat des liens déjà persistés + relecture du rôle @back @pending
```gherkin
Étant donné un lien enfant↔parent DÉJÀ persisté par un sprint antérieur (s34), SANS attribut rôle-du-lien
Quand la query de config relit cet enfant après rechargement du store
Alors le parent lié est relu avec le rôle-du-lien « parent-libre » (défaut neutre), SANS crash de désérialisation
Et AUCUNE migration destructive n'est appliquée au store (l'ancien lien reste valide, enrichi à la lecture)
Étant donné un lien créé ce sprint avec un rôle explicite « mère »
Quand le store est rechargé
Alors le rôle-du-lien « mère » est relu tel quel (round-trip durable), l'id enfant inchangé
```

### Sc.4 — Modal Enfants : sélecteur père/mère/parent par parent lié @ihm @pending
```gherkin
Étant donné la modal d'édition d'un enfant ouverte (Parent), avec un ou deux parents liés (patron crayon→modal s34)
Quand la modal est rendue
Alors chaque parent lié porte un SÉLECTEUR rôle-du-lien (père / mère / parent), pré-réglé sur son rôle courant
Quand je change le rôle-du-lien d'un parent puis « Enregistrer »
Alors la commande « lier » (Sc.1, rôle inclus) est émise via le canal HTTP, la modal se ferme, le tableau est relu
Quand un rôle-du-lien est REFUSÉ par le domaine (deux « père », non éligible) ou l'API est injoignable
Alors la modal RESTE OUVERTE, le motif est affiché DEDANS, ma sélection (parents + rôles) est CONSERVÉE
Et le tableau reste INCHANGÉ (aucune écriture partielle)
Et la fermeture Échap (port IEcouteurEchapModal s33) referme SANS mutation (aucune commande émise)
```

### Sc.5 — Colonne « Parents liés » affiche le rôle-du-lien + gating + SignalR @ihm @pending
```gherkin
Étant donné l'onglet « Enfants » de /configuration, connecté en tant que Parent
Quand le tableau est rendu
Alors la colonne « Parents liés » affiche, pour chaque parent, son NOM ET son rôle-du-lien (père / mère / parent)
Étant donné une identité EFFECTIVE non-Parent (Invité)
Quand j'ouvre l'onglet « Enfants »
Alors le tableau reste en LECTURE SEULE (rôles-du-lien visibles), sans crayon ni « Ajouter », aucune modal atteignable
Étant donné deux écrans /configuration ouverts sur l'onglet « Enfants »
Quand le rôle-du-lien d'un parent est modifié depuis le 1ᵉʳ écran
Alors le tableau du 2ᵉ écran CONVERGE (nom + rôle-du-lien) sans rechargement,
  sans écriture par la diffusion (canal SignalR lecture seule)
```

---

# Retours produit (PO)

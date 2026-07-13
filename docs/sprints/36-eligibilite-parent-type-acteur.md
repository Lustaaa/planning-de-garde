# Sprint 36 — Éligibilité « parent » du lien enfant↔parent : `TypeActeur.Parent` (option A)

> **Goal G2 (tranché PO — option A retenue)** : **révision d'invariant** du lien enfant↔parent
> livré s34. Aujourd'hui un acteur n'est **liable comme parent** QUE s'il porte un **rôle du
> référentiel de libellé LITTÉRAL « Parent »** (`LierEnfantParentHandler` + IHM `ActeursParents()`),
> et l'éligibilité **IGNORE `TypeActeur.Parent`** (enum `Parent|Admin|Autre`). Conséquence
> contre-intuitive (retour PO gate s35) : un Papa / une Maman, **parents par nature**, ne sont
> liables qu'en leur créant un rôle nommé exactement « Parent ».
>
> **Option A retenue (reco SM confirmée PO)** : l'éligibilité « parent liable » = **`TypeActeur.Parent`**.
> On **réutilise la source de vérité existante** — celle qui porte DÉJÀ l'invariant **admin = Parent**
> (`AdministrationFoyer.DesignerAdmin`, s22, refuse un non-Parent AVANT mutation). **Une seule
> notion de « qui est parent par nature »**, plus de double définition. Les **rôles redeviennent
> des libellés d'affichage libres** (Papa, Maman) sans rôle nommé « Parent » à créer.
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (bascule de l'éligibilité dans le
> handler + filet d'invariant révisé + non-régression gating) → puis @ihm (sélecteur de la modal
> Enfants aligné + preuve runtime seed démo).
>
> **HORS scope (périmètre resserré, décision SM)** :
> - **Champ « père / mère » distinct** sur l'acteur ou le lien : **non traité ici**. La distinction
>   Papa/Maman s'affiche **par le NOM d'acteur** dans la colonne « Parents liés » (aucun champ de
>   modèle neuf). Si le PO veut un rôle père/mère explicite → **backlog**.
> - **Options B (flag « rôle parent » sur `RoleFoyer`) / C (unification)** : **écartées en G2**
>   (double source de vérité / refonte plus lourde, hors ~2h). Notées au backlog pour mémoire.
> - **Familles recomposées R2/R3** (« exactement 2 parents », graphe enfant-racine) : autre
>   incrément, **non traité ici** (le lien reste borné 0..2 comme s34).

## Avancement — 1/5

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | Éligibilité du lien basculée sur **`TypeActeur.Parent`** dans `LierEnfantParentHandler` (source unique, alignée admin=Parent s22) | back | ✅ |
| 2 | **Filet d'invariant révisé** : Parent-par-nature SANS rôle « Parent » = LIABLE ; `TypeActeur.Autre` (Mamie) = REFUSÉ même avec un rôle libellé « Parent », sans écriture partielle | back | ⏳ |
| 3 | Non-régression **gating impersonation R8/R9** (porté par `TypeActeur`, aucune dérive du droit d'écriture) | back | ⏳ |
| 4 | Sélecteur de la modal Enfants `ActeursParents()` énumère les acteurs **`TypeActeur.Parent`** (l'IHM suit exactement la règle back) | 🖥️ IHM | ⏳ |
| 5 | **Preuve runtime** : seed démo (Alice/Bruno = Parent) liable DIRECTEMENT, sans créer de rôle « Parent » ; Papa/Maman distingués par le NOM | 🖥️ IHM | ⏳ |

> **⚠️ GARDE — bascule d'invariant ⇒ lot atomique règle ↔ tests dépendants (Sc.1/Sc.2).** Le lien
> s34 fixait l'éligibilité sur le **libellé de rôle** ; **les tests d'acceptation s34** rendaient un
> acteur liable **en lui créant un rôle nommé « Parent »**. La règle change (éligibilité =
> `TypeActeur.Parent`) : ces montages de test **doivent basculer sur `TypeActeur.Parent` dans le
> MÊME commit** que le changement de handler — sinon la suite passe rouge sur des scénarios non
> ciblés (fenêtre rouge, rempart suite-complète-verte). Sc.1 (bascule) + migration des tests
> dépendants + Sc.2 (contrepoint Mamie) forment **un lot atomique** ; chaque assertion reste
> **individuelle**. **Interdit** : laisser coexister les deux critères d'éligibilité (libellé ET
> type) — c'est la double source que l'option A supprime. *(récit lot atomique : JOURNAL-METHODE s32.)*

> **⚠️ Early-green attendu possible (à signaler, pas à masquer).** Un test s34 « acteur non-parent
> refusé » peut rester **vert sans modification** si l'acteur non-parent y était AUSSI `TypeActeur ≠
> Parent`. Si la dev-team constate un scénario **déjà vert** après la bascule, elle le **signale au
> SM** (filet de non-régression à conserver vs doublon à retirer) — **sans** réinventer de handler.

> **Définition « parent liable » (cadrage SM, option A).** Un **parent liable** = un acteur dont
> **`TypeActeur == Parent`** (enum acteur). C'est **exactement** le prédicat déjà utilisé par
> `AdministrationFoyer.DesignerAdmin` (admin = Parent, s22). Le **libellé de rôle** (référentiel s21)
> **ne qualifie PLUS** l'éligibilité — il redevient un attribut d'affichage. Borne **0..2 parents**
> (s34) **inchangée**.

---

## Scénarios

### Sc.1 — Éligibilité du lien basculée sur `TypeActeur.Parent` @back @vert
```gherkin
Étant donné un enfant déclaré dans le foyer (agrégat de 1er rang, id stable + prénom, s30)
Et un acteur du foyer de type TypeActeur.Parent (Papa/Maman) NE portant AUCUN rôle nommé « Parent »
Quand la commande « lier un enfant à un parent » est émise (enfantId, acteurId)
Alors le lien enfant→parent est ACCEPTÉ et PERSISTÉ (store réel, durable au rechargement)
Et la query de config relit l'enfant avec ce parent dans sa liste de parents liés
Et l'éligibilité « parent liable » est résolue sur TypeActeur.Parent
  — la MÊME source de vérité que l'invariant admin=Parent (AdministrationFoyer.DesignerAdmin, s22)
Et le critère « rôle du référentiel de libellé littéral 'Parent' » n'intervient PLUS dans l'éligibilité
```

### Sc.2 — Filet d'invariant révisé : nature ≠ libellé de rôle @back @pending
```gherkin
Étant donné un acteur de type TypeActeur.Parent qui n'a AUCUN rôle affecté (RoleDe = null)
Quand la commande « lier » le désigne comme parent d'un enfant
Alors le lien est ACCEPTÉ (le rôle n'est plus requis pour l'éligibilité)
Étant donné un acteur de type TypeActeur.Autre (ex. Mamie) à qui on a affecté un rôle LIBELLÉ « Parent »
Quand la commande « lier » le désigne comme parent d'un enfant
Alors le domaine REFUSE (acteur non-parent = TypeActeur ≠ Parent), le motif est restitué à l'appelant
Et AUCUNE écriture partielle ne touche le store (liens existants de l'enfant intacts)
Et le libellé « Parent » porté par le rôle n'a AUCUN effet sur l'éligibilité
```

### Sc.3 — Non-régression du gating impersonation R8/R9 @back @pending
```gherkin
Étant donné le gating du droit d'écriture dérivé du type de l'identité EFFECTIVE (R8/R9, déjà sur TypeActeur)
Quand l'éligibilité du lien enfant↔parent bascule sur TypeActeur.Parent (Sc.1)
Alors le droit d'écriture / l'impersonation bornée lecture reste INCHANGÉ (aucune dérive)
Et une identité effective non-Parent ne gagne AUCUN droit d'écriture du fait de la bascule
Et la non-régression impersonation s14 (retour à l'identité réelle, repli sur suppression) est préservée
Et la suite COMPLÈTE reste verte (aucun scénario hors-cible cassé par la révision d'invariant)
```

### Sc.4 — Sélecteur de la modal Enfants aligné sur `TypeActeur.Parent` @ihm @pending
```gherkin
Étant donné la modal d'édition d'un enfant ouverte (onglet Enfants, connecté en tant que Parent)
Et le sélecteur des parents alimenté par ActeursParents() (ConfigurationFoyer.razor.cs)
Quand la modal est rendue
Alors le sélecteur énumère les acteurs de type TypeActeur.Parent (au lieu du match sur libellé de rôle)
Et un Papa/une Maman SANS rôle nommé « Parent » APPARAÎT dans le sélecteur (liable)
Et un acteur TypeActeur.Autre portant un rôle libellé « Parent » N'APPARAÎT PAS
Quand je lie un tel parent puis « Enregistrer »
Alors la commande « lier » (Sc.1) est émise via le canal HTTP et le tableau reflète le parent lié
Et l'IHM suit EXACTEMENT la règle back (aucun critère d'éligibilité divergent entre back et IHM)
```

### Sc.5 — Preuve runtime : seed démo liable directement + distinction par le nom @ihm @pending
```gherkin
Étant donné l'app lancée sur le seed démo (Alice et Bruno, tous deux TypeActeur.Parent)
Et un enfant déclaré dans le foyer
Quand j'ouvre la modal Enfants et le sélecteur des parents
Alors Alice ET Bruno sont proposés DIRECTEMENT comme parents liables
Et AUCUN rôle nommé « Parent » n'a eu besoin d'être créé au préalable
Quand je lie Alice puis Bruno à l'enfant
Alors la colonne « Parents liés » affiche « Alice » et « Bruno » (distinction Papa/Maman PAR LE NOM)
Et aucun champ père/mère distinct n'est requis (hors scope, backlog si demandé)
Et la borne 0..2 parents (s34) reste tenue (un 3ᵉ parent refusé, invariant inchangé)
```

---

# Retours produit (PO)

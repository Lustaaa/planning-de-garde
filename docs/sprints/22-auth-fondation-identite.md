# Sprint 22 — Auth · tranche 1 : fondation identité & compte↔acteur (`auth-fondation-identite`)

> **Avancement : 2/9 ⏳**

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | **Créer un compte utilisateur** (id stable opaque neuf, email, statut **inactif** par défaut) associé 1-1 à un acteur déclaré, persisté config foyer Mongo borné | @back | ✅ |
| 2 | **Rejet** création : email vide **ou** email en doublon (aucun compte écrit) | @back | ✅ |
| 3 | **Association bornée 1-1** : un acteur ne peut porter qu'un seul compte, un compte ne référence qu'un acteur **déclaré** (acteur inconnu → rejet sans écriture) | @back | ⏳ |
| 4 | **Invariant admin = parent** : désigner comme **admin du foyer** un acteur de type **Parent** réussit ; un acteur non-Parent est **rejeté sans écriture** (retour PO URGENT #3) | @back | ⏳ |
| 5 | **Deux parents admins** : quand les deux parents sont utilisateurs, les deux peuvent être admins (l'invariant borne le **type**, pas l'unicité) | @back | ⏳ |
| 6 | **Suppression concurrente de l'acteur associé** → le compte retombe **orphelin/désassocié** (repli propre, pas de compte fantôme référençant un acteur absent), idempotence | @back | ⏳ |
| 7 | Onglet **Acteurs** : **créer/associer un compte** à un acteur (email obligatoire) + voir le statut (inactif) ; échec API → le formulaire reste ouvert avec un motif clair | 🖥️ @ihm | ⏳ |
| 8 | Onglet **Acteurs** : **gating identité effective** — « Invité » ne peut ni créer un compte ni désigner l'admin (non-régression durcissement s14, gating par onglet s20) | 🖥️ @ihm | ⏳ |
| 9 | **Temps réel SignalR** : création d'un compte / désignation de l'admin propagée à un **2ᵉ écran** sans rechargement | 🖥️ @ihm | ⏳ |

---

> Sujet `/planning` = `auth-fondation-identite` (épic É10, amorce ; recoupe É2), goal tranché **G2 (PO)** sur le 1er candidat.
> **Origine** : retours produit s21 — **auth marquée URGENT** (page de connexion custom + OAuth Google/Apple/Microsoft ; acteur par défaut = utilisateur connecté ; admin obligatoirement parent). L'auth complète est un **palier lourd** (landing, OAuth 3 providers, sessions, droits) → **découpé en tranches**. Cette **tranche 1** pose la **fondation identité** (modèle de compte + association acteur + invariant admin=parent) **sans OAuth ni session HTTP**. Store durable (Mongo config foyer, s09/s15) → **acceptation runtime obligatoire**. Suite courante = **317/317**.
>
> **Décisions CP (déterministes).**
> - **CompteUtilisateur = nouveau petit agrégat de config foyer**, miroir du CRUD acteurs (s08/s09/s13) et du référentiel de rôles (s21). Port de lecture `IEnumerationComptes` + port d'écriture `IEditeurComptes` (créer / associer / désigner-admin / désassocier), **deux adaptateurs de droite** : InMemory (tests) **+** Mongo (runtime), **bornés à la config foyer** — réutilise le socle Mongo config déjà acquis, **ne tire aucune persistance neuve hors config foyer** (borne anti-cliquet règle 30 respectée). Un compte = **id stable opaque** + email + statut (`Inactif`/`Actif`, défaut **Inactif** — l'activation viendra avec la prise en main de compte, palier 13).
> - **Association 1-1 compte↔acteur.** Le compte **référence l'id stable d'un acteur déclaré** (borne : acteur inconnu → rejet, réutilise `IEnumerationActeursFoyer`). Un acteur porte **au plus un** compte. Aucun nouveau modèle de concurrence sur l'agrégat acteur : le compte est son **propre** agrégat, seul l'id de l'acteur est référencé (comme le référentiel de rôles s21 référence l'acteur sans le muter).
> - **Invariant admin = parent = invariant métier PUR** (Domain), tranchable **maintenant** sans auth : désigner l'admin du foyer exige un acteur de **type Parent** (`TypeActeur` déjà surfacé read-only depuis le seed, s14). Non-Parent → **rejet sans écriture** (retour PO URGENT #3). Les **deux** parents peuvent être admins (l'invariant borne le **type**, pas le cardinal) — Sc.5.
> - **Suppression concurrente de l'acteur associé** = miroir du repli acteur orphelin (s13/s19) : le compte retombe **désassocié** (pas de compte fantôme pointant un acteur absent), **idempotence** (désassocier deux fois = no-op qui réussit). Repli propre, pas rejet.
> - **Prépare « acteur par défaut = utilisateur connecté »** (retour URGENT #2) : la **relation identité↔acteur** posée ici rendra le couplage trivial en **tranche 2** (quand une session existera). **Aucune session ce sprint** : on ne branche pas encore de défaut de sélection sur un utilisateur « connecté » (il n'y a pas de connexion).
> - **Placement IHM** : dans l'**onglet Acteurs** de l'écran config (structure s20), **gating identité effective par onglet** préservé (s14/s20), écran **abonné au hub SignalR lecture** (s20) pour la ré-énumération temps réel des comptes.
> - **Rétrofit flake TempsReel (dette P1, +2)** : NON pris ce sprint (auth priorisée au G2). Le garde-fou triage durci (rétro s21 : re-run **en isolation x2-3** avant tout étiquetage « flake » ; **N/N rouge = régression, STOP**) s'applique tel quel — un rouge déterministe sur `*TempsReel*` est une **régression**, jamais un flake.
>
> **Hors scope (tranche 2+)** : **page de connexion custom**, **OAuth Google / Apple / Microsoft** (3 intégrations externes + secrets + callbacks, non testables en runtime local), **sessions HTTP réelles / logout** ; **acteur par défaut config = utilisateur connecté** (dépend d'une session) ; **activation / prise en main de compte** (palier 13) ; **droits par rôle** (É10, palier 13) ; toute **persistance hors config foyer** ; sprint de design (retour PO P1, séparé) ; cohérence config→planning (retour PO, séparé).

---

## Scénarios

### @back — compte utilisateur, association & invariant admin (frontière Application/ports)

```gherkin
@back @vert
Scénario 1 — Créer un compte utilisateur associé à un acteur déclaré
  Étant donné un foyer configuré (store de config durable) et un acteur déclaré (id stable) sans compte
  Quand le parent configurateur crée un compte d'email « alice@foyer.fr » pour cet acteur
  Alors le référentiel des comptes contient un compte doté d'un identifiant stable opaque neuf
    Et ce compte porte l'email « alice@foyer.fr » et le statut « inactif » par défaut
    Et il référence l'id stable de l'acteur (association 1-1)
    Et il est persisté avec la config foyer (survit au redémarrage, store Mongo réel)
    Et l'énumération des comptes (IEnumerationComptes) le retourne exactement une fois
```

```gherkin
@back @vert
Scénario 2 — Rejet : email vide ou email en doublon
  Étant donné un référentiel contenant déjà un compte d'email « alice@foyer.fr »
  Quand le parent tente de créer un compte d'email vide
    Ou tente de créer un second compte d'email « alice@foyer.fr »
  Alors la commande échoue avec un motif clair (email requis / email déjà utilisé)
    Et le référentiel reste inchangé (aucun compte vide ni doublon persisté)
```

```gherkin
@back @pending
Scénario 3 — Association bornée 1-1 (acteur déclaré, au plus un compte)
  Étant donné un acteur déclaré portant déjà un compte
  Quand on tente de créer un second compte pour ce même acteur
  Alors la commande échoue (un acteur ne porte qu'un seul compte), aucune écriture
  Et quand on tente de créer un compte pour un id d'acteur absent du foyer
  Alors la commande échoue (acteur inconnu), aucune écriture
    Et l'énumération des comptes reste inchangée
```

```gherkin
@back @pending
Scénario 4 — Invariant admin = parent (rejet sans écriture sinon)
  Étant donné un acteur de type Parent et un acteur de type Autre (nounou), tous deux déclarés
  Quand on désigne l'acteur Parent comme admin du foyer
  Alors la désignation réussit (l'admin du foyer est ce Parent, persisté)
  Mais quand on tente de désigner l'acteur Autre comme admin du foyer
  Alors la désignation est rejetée avec un motif clair (l'admin doit être un parent)
    Et l'admin du foyer reste inchangé (aucune écriture d'un admin non-Parent)
```

```gherkin
@back @pending
Scénario 5 — Deux parents peuvent être admins
  Étant donné deux acteurs de type Parent déclarés, tous deux utilisateurs
  Quand on désigne l'un puis l'autre comme admin du foyer
  Alors les deux désignations réussissent (l'invariant borne le type, pas l'unicité)
    Et le foyer reconnaît deux admins, tous deux de type Parent
    Et aucun acteur non-Parent ne peut rejoindre l'ensemble des admins (invariant tenu)
```

```gherkin
@back @pending
Scénario 6 — Suppression concurrente de l'acteur associé : désassociation propre + idempotence
  Étant donné un acteur déclaré portant un compte
  Quand l'acteur est supprimé du foyer (store réel)
  Alors le compte retombe désassocié (ne référence plus d'acteur), pas de compte fantôme pointant un acteur absent
    Et désassocier un compte déjà désassocié est un no-op qui réussit (idempotence)
    Et la suite complète reste verte (317/317, Docker actif, sans filtre ni --no-build)
```

### @ihm — création/association de compte dans l'onglet Acteurs (RED → GREEN runtime)

```gherkin
@ihm @pending
Scénario 7 — Onglet Acteurs : créer/associer un compte à un acteur (email obligatoire)
  Étant donné l'onglet « Acteurs » de l'écran de configuration, actif, avec un acteur déclaré sans compte
  Quand je saisis un email et crée le compte de cet acteur
  Alors le compte apparaît associé à l'acteur, avec son statut « inactif » affiché
    Et l'écriture aboutit sur le store réel (persistée, survit au redémarrage)
  Mais quand je soumets un email vide (ou déjà utilisé)
  Alors le formulaire reste ouvert avec un motif clair, sans compte créé
```

```gherkin
@ihm @pending
Scénario 8 — Gating identité effective : Invité ne crée pas de compte ni ne désigne l'admin
  Étant donné une identité effective « Invité » (non Parent/Admin) sur l'onglet Acteurs
  Quand j'ouvre l'écran de configuration
  Alors la création/association de compte est gatée
    Et la désignation de l'admin du foyer est gatée
    Et le durcissement du gating config (s14) et le gating par onglet (s20) n'ont pas régressé
```

```gherkin
@ihm @pending
Scénario 9 — Temps réel : compte et admin convergent sur un 2ᵉ écran
  Étant donné deux écrans de configuration ouverts sur le même foyer (store partagé)
  Quand je crée un compte puis désigne l'admin du foyer depuis le second écran
  Alors la liste des comptes et l'admin affiché du premier écran reflètent le changement sans rechargement (SignalR)
    Et un compte dont l'acteur est supprimé apparaît désassocié sur les deux écrans, sans compte fantôme
```

---

# Retours produit (PO)

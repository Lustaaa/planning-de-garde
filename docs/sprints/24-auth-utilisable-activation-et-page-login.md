# Sprint 24 — Auth utilisable de bout en bout : activation (Inactif→Actif) + page de connexion dédiée (`auth-utilisable-activation-et-page-login`)

> **Avancement : 11/11 ✅**

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | **Activation d'un compte Inactif** → le statut passe `Inactif→Actif` (Domain pur + Mongo, `IEditeurComptes` s22) ; le compte devient connectable | @back | ✅ |
| 2 | **Idempotence : activer un compte déjà Actif** → no-op qui **réussit** (aucune double mutation, statut reste Actif) | @back | ✅ |
| 3 | **Rejet : activer un compte inconnu** (id absent) → refus, motif clair, **aucune mutation** | @back | ✅ |
| 4 | **Boucle auth complète (E2E back)** : créer compte (naît Inactif, s22) → connexion **refusée** (Inactif, s23) → **activer** → connexion **réussit** (session ouverte, s23) | @back | ✅ |
| 5 | IHM **bouton « Activer » (onglet Acteurs, Parent-gated)** : un compte Inactif affiche « Activer » → clic → compte Actif + accusé non bloquant « Compte activé » ; un compte déjà Actif n'affiche plus l'action | 🖥️ @ihm | ✅ |
| 6 | IHM **gating Invité + échec API** activation : l'Invité ne voit pas l'action ; échec transport → message clair, statut inchangé à l'écran | 🖥️ @ihm | ✅ |
| 7 | IHM **temps réel SignalR** : l'activation d'un compte propage le nouveau statut à un 2ᵉ écran (onglet Acteurs) sans rechargement | 🖥️ @ihm | ✅ |
| 8 | IHM **page de connexion dédiée = landing par défaut** : app démarrée non connecté → **page login dédiée** (pas le planning) ; email valide (compte Actif) → connexion (`SeConnecterCommand` s23) → **redirection planning** | 🖥️ @ihm | ✅ |
| 9 | IHM **motif clair sur la page login** : email inconnu / compte Inactif → la page affiche un motif clair, reste sur la page login, aucune redirection | 🖥️ @ihm | ✅ |
| 10 | IHM **bandeau login inline retiré** : `PlanningPartage` n'expose plus le champ email/bouton « Se connecter » inline (**un seul chemin d'entrée** = la page dédiée) ; non-régression du reste du planning | 🖥️ @ihm | ✅ |
| 11 | IHM **menu utilisateur connecté** : une fois connecté, un menu affiche le nom/acteur + **accès config foyer** + **« Se déconnecter »** ; déconnexion (logout s23) → retour à la page login | 🖥️ @ihm | ✅ |

---

> Sujet `/planning` = `auth-utilisable-activation-et-page-login` (épic É10 ; recoupe É2/É5), goal tranché **G2 (PO)** = **combinaison des candidats 1 (P0 activation) + 3 (UX auth, page login dédiée)** en un seul sprint goal : *« l'auth devient réellement utilisable de bout en bout »*.
>
> **Origine.** Retours produit s23 + signal bloquant : les comptes naissent **Inactif** (fondation identité s22), la connexion locale + session serveur sont livrées (s23), **mais aucun chemin d'activation n'existe** → **aucune connexion ne réussit sur données réelles aujourd'hui** (auth fonctionnellement inerte en prod). En parallèle, le PO juge le **bandeau login inline** sur l'écran planning « sans aucun sens » et veut une **page de connexion dédiée = page par défaut**. Les deux forment une tranche cohérente : l'activation **débloque** la réussite de connexion, la page login dédiée **rend le parcours naturel**.
>
> **Séquence imposée : activation D'ABORD (@back Sc.1-4 puis IHM Sc.5-7), PUIS page login dédiée (@ihm Sc.8-11).** Sc.8 (redirection après connexion réussie) dépend d'un compte réellement activable — la boucle E2E (Sc.4) doit être verte avant.
>
> **HORS SCOPE explicite (à ne pas déborder).** OAuth externe (tranche 2b : Google/Apple/Microsoft) ; **création de compte en libre-service** ; **récupération de mot de passe par email** (adaptateur de droite mail — introduit un facteur mot de passe distinct de l'email-only s23, à arbitrer plus tard). La création de compte reste **par le parent dans l'onglet Acteurs** (s22). Le login reste **email-only** (s23) : la page dédiée ne fait qu'emballer `SeConnecterCommand`, elle n'ajoute pas de mot de passe.
>
> **Décisions CP (déterministes).**
> - **Activation = commande applicative** (canal requête/réponse, comme toute écriture) : `ActiverCompteCommand`/`ActiverCompteHandler` ciblent un compte **par son id stable opaque** (s22), font passer le statut `Inactif→Actif`. Réutilise le **port d'écriture `IEditeurComptes`** (s22, InMemory + Mongo config foyer) — **aucun nouvel agrégat, aucun store neuf**. La mutation de statut est portée par l'agrégat **`CompteUtilisateur`** (Domain pur : une méthode `Activer()` qui est un no-op si déjà Actif).
> - **Idempotence assumée** (Sc.2) : activer un compte déjà Actif **réussit** sans double mutation (miroir des suppressions idempotentes s16/s18). Compte **inconnu** (Sc.3) = **refus** motif clair, **aucune mutation** (Result échec, pas d'exception silencieuse).
> - **Chemin d'activation = bascule par l'admin/parent** depuis l'**onglet Acteurs**, **Parent-gated** (identité effective, non-régression gating s14/s20) — c'est la voie **testable en runtime local**, sans provider externe, sans lien email. Pas de « self-activation par lien » ce sprint (dépendrait d'un adaptateur mail hors scope).
> - **Page de connexion dédiée = route de landing par défaut.** Non connecté → l'app **atterrit sur la page login** (pas sur le planning). La page emballe `SeConnecterCommand` (s23) ; succès → **redirection vers le planning**. Le **bandeau login inline** de `PlanningPartage` (s23 Sc.7) est **retiré** : **un seul chemin d'entrée** (Sc.10). Ne pas casser le reste de `PlanningPartage` (sélecteur d'acteur pré-positionné s23 Sc.8, temps réel s20).
> - **Menu utilisateur** (Sc.11) : une fois connecté, un menu surface **nom/acteur** (résolu serveur, s23), **accès config foyer** (route existante), **déconnexion** (logout s23 = destruction de session → retour page login). Réutilise le logout s23, aucun nouveau canal.
> - **Aucune persistance neuve hors config foyer.** L'activation ne touche que le store **comptes** (déjà Mongo config foyer s22), en **écriture** de statut. La session reste un état d'hôte/requête (borne anti-cliquet règle 30). La page login et le menu user sont du **front** (routing + composants), pas de persistance.
> - **Non-régression obligatoire** : connexion/logout s23, refus Inactif s23 (Sc.3 s23 devient un état **transitoire** avant activation, pas une impasse), acteur-par-défaut = utilisateur connecté (s23 Sc.4/8), impersonation bornée s14, temps réel SignalR s20.
>
> **GARDE de cohérence** : aucun scénario de ce sprint ne couple date ↔ index/parité de cycle (sujet purement auth/identité) — garde sans objet ici.
>
> **Acceptation runtime (rempart anti vert-qui-ment).** La boucle Sc.4 (créer Inactif → refus → activer → connexion réussit) **et** Sc.8 (landing login → connexion → planning) doivent être prouvées sur **câblage réel + store Mongo réel** (Docker actif), pas par doublures. Suite complète verte attendue (base 367/367 + scénarios neufs).

---

## Scénarios

### @back — Activation (frontière Application, store Mongo réel)

```gherkin
@back @vert
Scénario 1 : Activation d'un compte Inactif
  Étant donné un CompteUtilisateur existant de statut "Inactif" (créé s22, id stable opaque)
  Quand j'exécute ActiverCompteCommand sur son id
  Alors la commande réussit
  Et le statut du compte devient "Actif"
  Et le changement est persisté (relu depuis le store, le statut reste "Actif")
  Et aucune autre caractéristique du compte n'est modifiée (email, ActeurId inchangés)
```

```gherkin
@back @vert
Scénario 2 : Idempotence — activer un compte déjà Actif
  Étant donné un CompteUtilisateur de statut "Actif"
  Quand j'exécute ActiverCompteCommand sur son id
  Alors la commande réussit (no-op)
  Et le statut reste "Actif"
  Et aucune double mutation n'est appliquée (relu depuis le store : Actif, une seule fois)
```

```gherkin
@back @vert
Scénario 3 : Rejet — activer un compte inconnu
  Étant donné qu'aucun compte ne porte l'id "id-absent"
  Quand j'exécute ActiverCompteCommand sur "id-absent"
  Alors la commande échoue avec un motif clair (compte introuvable)
  Et aucune mutation n'est appliquée au store (aucun compte créé, aucun statut changé)
```

```gherkin
@back @vert
Scénario 4 : Boucle auth complète de bout en bout (E2E back, Mongo réel)
  Étant donné un acteur déclaré et un CompteUtilisateur créé pour lui (naît "Inactif", s22)
  Quand je tente SeConnecterCommand par l'email du compte
  Alors la connexion est refusée (compte Inactif, motif clair, aucune session — non-régression s23 Sc.3)
  Quand j'exécute ActiverCompteCommand sur ce compte
  Et que je tente à nouveau SeConnecterCommand par le même email
  Alors la connexion réussit
  Et une session serveur est ouverte, identité effective = l'acteur lié au compte (s23 Sc.1)
```

### @ihm — Activation dans l'onglet Acteurs (RED→GREEN runtime)

```gherkin
@ihm @vert
Scénario 5 : Bouton « Activer » dans l'onglet Acteurs (Parent-gated)
  Étant donné un Parent connecté sur l'onglet Acteurs de la config foyer
  Et un compte utilisateur de statut "Inactif" listé
  Quand je clique « Activer » sur ce compte
  Alors le compte passe "Actif" (relu depuis le store)
  Et un accusé non bloquant « Compte activé » s'affiche
  Et l'action « Activer » disparaît pour ce compte (déjà Actif)
```

```gherkin
@ihm @vert
Scénario 6 : Gating Invité + échec API à l'activation
  Étant donné un utilisateur en identité effective "Invité"
  Alors l'action « Activer » n'est pas offerte (gating effectif, non-régression s14/s20)
  Étant donné un Parent connecté et un échec de transport lors de l'activation
  Quand je clique « Activer »
  Alors un message d'échec clair s'affiche
  Et le statut affiché reste inchangé (aucun faux positif)
```

```gherkin
@ihm @vert
Scénario 7 : Temps réel SignalR de l'activation
  Étant donné deux écrans ouverts sur l'onglet Acteurs (même foyer)
  Quand un compte est activé sur le premier écran
  Alors le second écran reflète le statut "Actif" sans rechargement (propagation SignalR lecture s20)
```

### @ihm — Page de connexion dédiée & menu utilisateur (RED→GREEN runtime)

```gherkin
@ihm @vert
Scénario 8 : Page de connexion dédiée = landing par défaut → connexion → planning
  Étant donné l'application ouverte non connecté
  Alors j'atterris sur la page de connexion dédiée (pas sur le planning)
  Quand je saisis l'email d'un compte Actif et valide « Se connecter »
  Alors la connexion réussit (SeConnecterCommand s23)
  Et je suis redirigé vers le planning
  Et le sélecteur d'acteur est pré-positionné sur l'acteur du compte connecté (non-régression s23 Sc.8)
```

```gherkin
@ihm @vert
Scénario 9 : Motif clair sur la page login (email inconnu / compte Inactif)
  Étant donné la page de connexion dédiée
  Quand je saisis un email inconnu (ou d'un compte Inactif) et valide
  Alors un motif clair s'affiche sur la page (email inconnu / compte non activé)
  Et je reste sur la page de connexion (aucune redirection, aucune session)
```

```gherkin
@ihm @vert
Scénario 10 : Retrait du bandeau login inline du planning (un seul chemin d'entrée)
  Étant donné un utilisateur connecté sur le planning
  Alors PlanningPartage n'expose plus de champ email ni de bouton « Se connecter » inline
  Et le reste du planning fonctionne (grille, légende, dialogs, temps réel — non-régression s19/s20)
```

```gherkin
@ihm @vert
Scénario 11 : Menu utilisateur connecté (config + déconnexion)
  Étant donné un utilisateur connecté
  Alors un menu utilisateur affiche son nom / acteur (résolu serveur s23)
  Et propose l'accès à la config foyer
  Et propose « Se déconnecter »
  Quand je clique « Se déconnecter »
  Alors la session est détruite (logout s23)
  Et je reviens à la page de connexion dédiée
```

---

# Retours produit (PO)

Tout fonctionne bien. Par contre :
- Il faudrait mettre un mot de passe 
- Il faudrait mettre en place l'authentification google, microsoft et apple
- Lors que je me connecte avec Mamie, j'ai le role parent
- Les page sont toute accessible meme sans être loggé

Pour plus tard :
- Il faudrait un envoi de mail pour activer son compte. Mais on verra pour faire une description exacte de ce que je veux.


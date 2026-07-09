# Sprint 31 — Login F5 (P0) + œil · Transfert dérivé (D3) · Slot conditionné (D1)

> **Goal G2 tranché (PO)** : livrer **les trois volets dans le même sprint**. Le PO **accepte
> le risque des 2 changements de cœur** de la résolution (alerte SM actée).
>
> **⚠️ CADRAGE SM — séquençage STRICT, imposé, non négociable** : dérisquer d'abord, ne
> **jamais croiser** les deux révisions de résolution.
> 1. **Volet 1 — Login F5 (P0) + œil** : **0 changement de cœur**. Fait d'abord pour dérisquer tôt.
> 2. **Volet 2 — D3 transfert dérivé** : **1er cœur** (dérivation depuis la succession de périodes).
>    Doit être **prouvé vert** avant d'ouvrir le volet 3.
> 3. **Volet 3 — D1 slot conditionné** : **2e cœur** (le slot lit la responsabilité). Attaqué
>    **seulement sur base D3 stable**.
>
> **Chaque invariant est borné séparément** (voir en tête de chaque volet). Interdiction de
> travailler D1 et D3 en parallèle : un cœur à la fois, chacun vert avant le suivant.

## Avancement — 10/14

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| **Volet 1 — Login F5 (P0) + œil · 0 cœur · dérisque tôt** ||||
| 1 | F5 sur `/planning` connecté → reste connecté (session restaurée) | 🖥️ IHM | ✅ |
| 2 | Contrat de restauration : jeton persisté au login, relu au démarrage client | back | ✅ |
| 3 | Logout purge le persisté → F5 après logout → `/connexion` (R30 + logout s23 tenus) | 🖥️ IHM | ✅ |
| 4 | Bouton œil afficher/masquer le mot de passe sur `/connexion` | 🖥️ IHM | ✅ |
| **Volet 2 — D3 transfert AUTO-dérivé · 1er cœur · prouvé vert AVANT volet 3** ||||
| 5 | Nominal : fin période A (J) + début période B (J+1), même enfant → transfert dérivé le jour de bascule | back | ✅ |
| 6 | Priorité **SAISI > DÉRIVÉ** : transfert saisi le même jour prime, pas de doublon | back | ✅ |
| 7 | Limite **NEUTRE** : fin de garde sans successeur → aucun transfert dérivé | back | ✅ |
| 8 | Limite **bord de fenêtre** : J+1 hors fenêtre chargée → pas de dérivation fantôme | back | ✅ |
| 9 | Erreur **orphelin (R6)** : cédant/recevant supprimé → retombée neutre côté orphelin | back | ✅ |
| 10 | Rendu : transfert dérivé → pastille bicolore comme le saisi (présentation s29) ; jour sans bascule unicolore | 🖥️ IHM | ✅ |
| **Volet 3 — D1 slot récurrent conditionné à la garde · 2e cœur · sur base D3 stable** ||||
| 11 | Nominal : slot « seulement les jours où l'enfant est chez moi » → occurrence projetée uniquement les jours où le poseur est responsable | back | ⏳ |
| 12 | Limite : jour où l'enfant n'est pas chez le poseur → occurrence masquée | back | ⏳ |
| 13 | Non-régression : slot **non conditionné** (défaut) → comportement s29 strictement inchangé | back | ⏳ |
| 14 | Toggle « seulement les jours où l'enfant est chez moi » dans la dialog « Poser un slot » | 🖥️ IHM | ⏳ |

**back : 9 · 🖥️ IHM : 5 · total : 14.**

---

## Volet 1 — Login F5 (P0) + bouton œil

> **Invariant borné (V1)** : persister/restaurer la session côté client **sans casser** la borne
> anti-cliquet **R30** (`SessionPlanning` en mémoire) ni le **logout s23** (le logout doit purger
> le persisté). **0 changement de cœur de résolution.** Retours non traités s29 (PO).

```gherkin
@ihm @vert
Scénario 1 — La session survit au rechargement (F5)
  Étant donné un utilisateur connecté sur "/planning"
  Quand il recharge la page (F5)
  Alors il reste sur "/planning" connecté
  Et il n'est PAS re-redirigé vers "/connexion"
  Et la borne anti-cliquet R30 reste tenue (aucune régression d'état de session)
```

```gherkin
@back @vert
Scénario 2 — Contrat de restauration de session (jeton persisté / relu)
  Étant donné une connexion réussie
  Alors un jeton de session est persisté côté client (cookie/stockage durable au rechargement)
  Quand le client redémarre et relit le jeton persisté valide
  Alors la session est restaurée sans repasser par le flux de connexion
  Et un jeton absent ou invalide n'ouvre AUCUNE session (pas de session fantôme)
```

```gherkin
@ihm @vert
Scénario 3 — Le logout purge le persisté (borne R30 + logout s23 tenus)
  Étant donné un utilisateur connecté avec session persistée
  Quand il se déconnecte (logout s23)
  Alors le jeton persisté est purgé
  Et un F5 après logout redirige vers "/connexion"
  Et aucune session n'est restaurée (le logout reste effectif au rechargement)
```

```gherkin
@ihm @vert
Scénario 4 — Bouton œil afficher/masquer le mot de passe
  Étant donné le champ mot de passe sur "/connexion" (masqué par défaut)
  Quand l'utilisateur active le bouton œil
  Alors la saisie du mot de passe devient visible en clair
  Quand il ré-active le bouton œil
  Alors la saisie redevient masquée
```

---

## Volet 2 — D3 : transfert AUTO-dérivé de la succession de périodes

> **Invariant borné (V2 — 1er cœur)** : dériver une bascule de responsabilité depuis la
> **succession de périodes** (fin A jour J + début B jour J+1, **même enfant** ⇒ transfert le
> jour de bascule). **Priorité SAISI > DÉRIVÉ.** Le rendu réutilise la **présentation bicolore
> s29** (aucune règle de rendu neuve). **Prouvé vert AVANT d'ouvrir le volet 3.**
> *Note cohérence date↔index : ces scénarios raisonnent sur la **succession de périodes saisies**,
> pas sur un index de cycle de fond — aucune date/parité ISO à ancrer ici.*

```gherkin
@back @vert
Scénario 5 — Nominal : transfert dérivé le jour de bascule
  Étant donné une période A responsable "Cédant" se terminant le jour J
  Et une période B responsable "Recevant", même enfant, débutant le jour J+1
  Quand la résolution s'exécute sur le jour de bascule
  Alors un transfert est dérivé automatiquement (Cédant → Recevant) ce jour-là
  Et aucun transfert n'a été saisi manuellement
```

```gherkin
@back @vert
Scénario 6 — Priorité SAISI > DÉRIVÉ (pas de doublon)
  Étant donné une succession de périodes qui dériverait un transfert le jour J
  Et un transfert SAISI existant le même jour J pour le même enfant
  Quand la résolution s'exécute sur le jour J
  Alors le transfert saisi prime et est seul retenu
  Et aucun transfert dérivé en doublon n'est produit
```

```gherkin
@back @vert
Scénario 7 — Limite NEUTRE : fin de garde sans successeur
  Étant donné une période A se terminant le jour J
  Et aucune période débutant le jour J+1 pour le même enfant
  Quand la résolution s'exécute sur le jour de bascule
  Alors aucun transfert n'est dérivé (retombée neutre)
```

```gherkin
@back @vert
Scénario 8 — Limite bord de fenêtre : J+1 non chargé
  Étant donné une période A se terminant le dernier jour de la fenêtre chargée
  Et le jour J+1 est hors de la fenêtre chargée
  Quand la résolution s'exécute sur le bord de la fenêtre
  Alors aucun transfert dérivé fantôme n'est produit (pas de dérivation sur données non chargées)
```

```gherkin
@back @vert
Scénario 9 — Erreur orphelin (R6) : acteur supprimé
  Étant donné une succession de périodes qui dériverait un transfert le jour J
  Et le cédant OU le recevant a été supprimé du référentiel (R6)
  Quand la résolution s'exécute sur le jour de bascule
  Alors le côté orphelin retombe sur le neutre (sans nom fantôme)
  Et aucune couleur/nom n'est résolu pour l'acteur supprimé
```

```gherkin
@ihm @vert
Scénario 10 — Rendu du transfert dérivé (présentation s29 réutilisée)
  Étant donné un jour portant un transfert dérivé (volet back)
  Quand la grille rend la pastille de date
  Alors la pastille est coupée par une diagonale bicolore (cédant → recevant), comme un transfert saisi
  Et la légende porte le motif "Transfert"
  Et un jour sans bascule reste unicolore, inchangé
```

---

## Volet 3 — D1 : slot récurrent conditionné à la garde

> **Invariant borné (V3 — 2e cœur)** : un slot récurrent peut être **conditionné** à la garde
> (« seulement les jours où l'enfant est chez moi ») → son occurrence n'est projetée que les jours
> où la **résolution surcharge>fond** désigne le **parent poseur** responsable. **Révision d'invariant
> assumée** : le slot (localisation orthogonale s29) **lit désormais la responsabilité**. Un slot
> **non conditionné** (défaut) garde le comportement s29 **strictement inchangé**. **Attaqué seulement
> sur base D3 (volet 2) stable.**

```gherkin
@back @pending
Scénario 11 — Nominal : occurrence projetée les jours de garde du poseur
  Étant donné un slot récurrent posé par le parent "Poseur", conditionné "seulement les jours où l'enfant est chez moi"
  Quand la grille projette les occurrences du slot sur la fenêtre
  Alors une occurrence est projetée uniquement les jours de récurrence OÙ la résolution désigne "Poseur" responsable
  Et le conditionnement lit la résolution (surcharge > fond), sans la modifier
```

```gherkin
@back @pending
Scénario 12 — Limite : jour où l'enfant n'est pas chez le poseur
  Étant donné le même slot conditionné du scénario 11
  Et un jour de récurrence OÙ la résolution désigne un AUTRE responsable que "Poseur"
  Quand la grille projette les occurrences
  Alors aucune occurrence du slot n'est projetée ce jour-là (occurrence masquée)
```

```gherkin
@back @pending
Scénario 13 — Non-régression : slot non conditionné (défaut s29 inchangé)
  Étant donné un slot récurrent NON conditionné (toggle inactif, comportement par défaut)
  Quand la grille projette les occurrences
  Alors le slot est projeté sur TOUS ses jours de récurrence (comportement s29 strictement inchangé)
  Et la résolution de responsabilité n'intervient PAS dans sa projection
```

```gherkin
@ihm @pending
Scénario 14 — Toggle de conditionnement dans la dialog de pose
  Étant donné la dialog "Poser un slot" (récurrent, s29)
  Quand l'utilisateur active le toggle "seulement les jours où l'enfant est chez moi"
  Et pose le slot
  Alors le slot est enregistré comme conditionné à la garde
  Et sa projection dans la grille respecte le conditionnement (volet back)
  Et laisser le toggle inactif pose un slot au comportement s29 par défaut
```

---

# Retours produit (PO)

*(À remplir après le gate G3.)*

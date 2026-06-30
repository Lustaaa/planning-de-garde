# Sprint 19 — Acteurs réels partout, retrait des « Parent A / Parent B » fictifs (`acteurs-reels-partout`)

> **Avancement : 1/7 ⏳**

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | La projection de grille résout les responsables **uniquement** depuis le store vivant des acteurs déclarés (id stable), jamais depuis un libellé en dur | @back | ✅ |
| 2 | Store d'acteurs **vide** → grille entièrement neutre + légende vide, **aucun** acteur fictif injecté | @back | ⏳ |
| 3 | Référence orpheline (id absent du store) → repli **surcharge > fond > neutre sans nom fantôme** | @back | ⏳ |
| 4 | Aucun libellé « Parent A / Parent B » exposé par le domaine ; runtime Mongo démarre vide, fixtures de test renommées (asymétrie seed s15 préservée) | @back | ⏳ |
| 5 | Sélecteurs des dialogs + légende + grille ne montrent que les **acteurs déclarés** (runtime) | 🖥️ @ihm | ⏳ |
| 6 | Store vide au 1er lancement → sélecteurs vides + message « aucun acteur, ajoutez-en », grille neutre, **zéro** fantôme | 🖥️ @ihm | ⏳ |
| 7 | Temps réel : l'ajout d'un acteur réel propage aux sélecteurs + grille + légende du second écran sans rechargement (SignalR), toujours sans fantôme | 🖥️ @ihm | ⏳ |

---

> Sujet `/planning` = `acteurs-reels-partout` (épics É1/É2), goal tranché **G2 (PO)** sur le 2ᵉ
> candidat. **Origine** : retour produit s17 #2 (« Parent A / Parent B » fictifs supprimés, acteurs
> réels partout). **Recoupe l'asymétrie seed s15** (InMemory seedé pour la non-régression / Mongo
> vide en runtime). Store du domaine **durable (Mongo, s15)** → **acceptation runtime obligatoire**.
>
> **Décisions CP (déterministes).** Périmètre = **backend d'abord, IHM en fin**. Le sujet est un
> **nettoyage de cohérence** : éliminer toute **référence en dur** aux libellés fictifs « Parent A »
> / « Parent B » dans le **domaine** ET dans l'**IHM** ; tous les sélecteurs, cases, légendes et
> formulaires consomment **exclusivement** les **acteurs déclarés** lus depuis le **store vivant**
> (`IEnumerationActeursFoyer` / `IReferentielResponsables` / `IPaletteCouleurs`), **clé = identifiant
> stable**, jamais un libellé. **Aucun handler d'écriture neuf** n'est attendu : on s'appuie sur le
> CRUD acteurs déjà livré (s08/s09/s13) et le filtre `Resolvable()` (s13).
>
> **Asymétrie seed (rappel décision PO hors process s15, à préserver).** En **runtime Mongo**,
> **aucun seed** : l'app démarre **vide** (pas de Parent A/B fantôme). En **InMemory** (tests), un
> seed est **conservé** pour la non-régression, mais ses acteurs de fixture sont **renommés** en
> libellés neutres de test (id stables) — **aucun** « Parent A/B » ne subsiste comme constante de
> domaine affichable. Sc.4 borne cette exigence ; ne pas casser la suite (258/258).
>
> **Repli (réutilise l'acquis).** Toute case dont le responsable n'est plus résolvable (id orphelin,
> store vide) retombe sur **surcharge > fond > neutre sans nom fantôme** (priorité palier 6 + filtre
> `Resolvable()` s13). **Aucune** réaffectation automatique, **aucun** libellé de secours fictif.
>
> **Hors scope** : modèle de rôles (retour s17 #3/#4/#5, candidat sprint suivant), refonte config en
> onglets (s17 #6), suppression de slot (s17 #1), transfert bicolore (s17 #7). Pas de nouvel écran ;
> pas de nouvelle persistance.

---

## Scénarios

### @back — résolution depuis le store vivant

```gherkin
@back @vert
Scénario 1 — La grille ne résout que des acteurs déclarés (id stable)
  Étant donné un foyer dont le store d'acteurs contient des acteurs déclarés (id stables)
    Et au moins une période affectée et un cycle de fond mappés sur ces id stables
  Quand on projette la grille agenda
  Alors chaque case résolue affiche le nom et la couleur lus depuis le store vivant des acteurs déclarés
    Et aucune case ne provient d'un libellé d'acteur codé en dur (« Parent A », « Parent B »)
    Et la légende ne liste que les acteurs déclarés effectivement référencés
```

```gherkin
@back @pending
Scénario 2 — Store d'acteurs vide → grille neutre, aucun fictif injecté
  Étant donné un foyer dont le store d'acteurs est vide
  Quand on projette la grille agenda
  Alors toutes les cases sont en repli neutre (aucun nom, couleur neutre)
    Et la légende est vide
    Et aucun acteur fictif (« Parent A / Parent B ») n'est injecté dans la projection
```

```gherkin
@back @pending
Scénario 3 — Référence orpheline → repli sans nom fantôme
  Étant donné une surcharge (période) référençant un identifiant stable absent du store d'acteurs
  Quand on projette la grille agenda
  Alors la case orpheline retombe sur surcharge > fond > neutre selon la priorité acquise (palier 6)
    Et aucun nom fantôme n'est affiché pour l'id orphelin (filtre Resolvable)
    Et la légende n'expose pas l'acteur orphelin
```

```gherkin
@back @pending
Scénario 4 — Aucun « Parent A / Parent B » dans le domaine ; runtime vide, fixtures renommées
  Étant donné le démarrage runtime sur store Mongo réel
  Alors l'application démarre sans aucun acteur seedé (app vide, durable ensuite)
    Et aucune constante de domaine n'expose un libellé fictif « Parent A » ou « Parent B »
  Étant donné la suite de tests InMemory (non-régression)
  Alors le seed de test est conservé mais ses acteurs portent des libellés neutres de test (id stables)
    Et la suite complète reste verte (258/258, Docker actif, sans filtre ni --no-build)
```

### @ihm — IHM ne montre que des acteurs réels (RED → GREEN runtime)

```gherkin
@ihm @pending
Scénario 5 — Sélecteurs, légende et grille ne montrent que les acteurs déclarés (runtime)
  Étant donné un foyer dont le store contient des acteurs déclarés réels
  Quand j'ouvre le planning et chaque dialog d'écriture (poser slot, affecter période, transfert, éditer)
  Alors chaque sélecteur de responsable ne propose que les acteurs déclarés (id stable)
    Et la grille et la légende n'affichent que ces acteurs
    Et nulle part n'apparaît « Parent A » ou « Parent B »
```

```gherkin
@ihm @pending
Scénario 6 — Store vide au 1er lancement → sélecteurs vides, grille neutre, zéro fantôme
  Étant donné un runtime Mongo dont le store d'acteurs est vide (1er lancement)
  Quand j'ouvre le planning et une dialog d'écriture
  Alors les sélecteurs de responsable sont vides et invitent à ajouter un acteur (« aucun acteur, ajoutez-en »)
    Et la grille est entièrement neutre et la légende vide
    Et aucune entrée fictive « Parent A / Parent B » n'apparaît
```

```gherkin
@ihm @pending
Scénario 7 — Temps réel : ajout d'un acteur réel propage sans fantôme
  Étant donné deux écrans ouverts sur le même foyer (store partagé)
  Quand j'ajoute un acteur réel depuis l'écran de config
  Alors le second écran voit le nouvel acteur dans les sélecteurs, la grille et la légende sans rechargement (SignalR)
    Et aucun acteur fictif n'apparaît avant ni après la propagation
```

---

# Retours produit (PO)

<!-- Rempli après le gate G3 (clôture). Un item par retour. -->

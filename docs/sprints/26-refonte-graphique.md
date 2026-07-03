# Sprint 26 — `refonte-graphique`

> **Sprint goal (imposé PO, pas de G2 de sélection)** : **refonte graphique complète « Cocon élevé »
> gouvernée par l'intuitivité**. Habillage **purement visuel** de **tout** le front Blazor WASM.
> **Zéro régression de comportement**, suite complète **161/161** verte.
>
> **Source de vérité du design** : [`docs/superpowers/specs/2026-07-03-refonte-graphique-design.md`](../superpowers/specs/2026-07-03-refonte-graphique-design.md)
> (validé PO). Principe directeur : *un parent comprend en 3 s qui a les enfants et comment agir, sans
> mode d'emploi* — l'esthétique sert l'intuitivité, jamais l'inverse.

## Cadre d'exécution — à respecter sur CHAQUE scénario

- **Refonte visuelle uniquement.** Les `data-testid`, les observables, les **couleurs de responsabilité**
  (parent bleu / parent orange / slots, **inline, priorité max**) et **tous les flux d'écriture**
  (canal requête/réponse, dialogs, diffusion SignalR) restent **INTACTS**. On réécrit l'habillage
  (couleurs de surface, typo, formes, espacement), jamais le comportement ni le balisage porteur de test.
- **Non-destructif** : CSS custom **par-dessus Bootstrap conservé** (grille + utilitaires gardés). Pas de
  big-bang, pas de suppression de Bootstrap.
- **Règles GtaX écartées pour tout le sujet** : **pas** de `var(--gold-*)`, **pas** de CONV-*, **pas** de
  gold.css. Tokens propres au projet : **`--pdg-*`** centralisés dans `wwwroot/app.css` (`:root` +
  `[data-theme=sombre]`).
- **Couleurs de responsabilité ≠ accent de marque.** L'accent est **sauge** (`--pdg-accent`), **hors bleu
  et hors orange**. Le bleu/orange restent **la donnée** « qui a les enfants », lisibles en clair **ET**
  en sombre (contraste du nom garanti via la couleur de texte, teinte porteuse inchangée).
- **Thème sombre** : switch dans le layout · défaut = préférence système (`prefers-color-scheme`) · choix
  explicite **persisté** (`localStorage`) et prioritaire · appliqué via `data-theme="clair|sombre"` sur
  `<html>` · **zéro flash** (script inline court avant le rendu Blazor).
- **Deux cibles à parité** : **navigateur PC** (agenda pleine largeur, riche) et **Safari iOS / WebKit**
  (tap ≥ 44px, `100vh`, `env(safe-area-inset-*)`, `position: sticky`, polices web self-hosted). Pas de
  feature CSS non supportée par le Safari iOS courant.
- **Guidance esthétique** : exécution menée avec le skill **`frontend-design`** (direction visuelle, typo
  Fraunces titres / Inter corps, éviter le rendu « Bootstrap par défaut » et le templated).
- **Gate visuel G3 obligatoire** : le PO valide **à l'œil chaque écran refondu en clair ET en sombre**.

> **Note preuve** : les scénarios sont `@ihm` → menés **RED→GREEN runtime** (app lancée via le skill `run`,
> observation visuelle réelle). Le vert « visuel » est constaté au runtime + gate G3 ; la **preuve
> comportementale** vient de Sc.14 (suite complète verte). On ne substitue jamais l'un à l'autre.

---

## Avancement — 6/14

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | Fondation : tokens `--pdg-*` + polices Fraunces/Inter self-hosted (offline) | 🖥️ IHM | ✅ |
| 2 | Fondation thème : défaut = préférence système, `data-theme` sur `<html>`, zéro flash | 🖥️ IHM | ✅ |
| 3 | Fondation switch clair/sombre : choix persisté `localStorage`, prime sur système | 🖥️ IHM | ✅ |
| 4 | Calendrier (cœur) : cases mini-cartes, responsable héroïque, « aujourd'hui » marqué | 🖥️ IHM | ✅ |
| 5 | Calendrier : menu clic-case restructuré (primaires / destructives) | 🖥️ IHM | ✅ |
| 6 | Calendrier : barre nav + sélecteurs (vue / incarnation / rôle) regroupés | 🖥️ IHM | ✅ |
| 7 | Connexion : page d'entrée soignée + boutons OAuth Google/Microsoft/Apple habillés | 🖥️ IHM | ⏳ |
| 8 | Accueil : point d'entrée / orientation refondu | 🖥️ IHM | ⏳ |
| 9 | Config foyer : acteurs + formulaires habillés, onglets si pertinent | 🖥️ IHM | ⏳ |
| 10 | Layout : nav + marque + menu utilisateur + bandeaux d'alerte adoucis | 🖥️ IHM | ⏳ |
| 11 | Dialogs (×6) : habillage cohérent + hiérarchie de boutons | 🖥️ IHM | ⏳ |
| 12 | Légende : découvrable, cohérente avec les couleurs de responsabilité (clair + sombre) | 🖥️ IHM | ⏳ |
| 13 | Transverse responsive : Safari iOS / WebKit (safe-areas, 100vh, sticky, polices web) | 🖥️ IHM | ⏳ |
| 14 | Non-régression : suite complète 161/161 verte, `data-testid`/observables/flux intacts | @back | ⏳ |

---

## Scénarios

### Sc.1 — Fondation : design tokens + polices self-hosted `@ihm @vert`

```gherkin
Scénario: Les tokens --pdg-* et les polices web sont chargés sans appel réseau externe
  Étant donné l'app Blazor WASM lancée dans un navigateur PC
  Et les polices Fraunces (titres) et Inter (corps) déposées sous wwwroot/fonts/
  Quand je charge n'importe quel écran
  Alors app.css expose les tokens --pdg-bg / --pdg-card / --pdg-accent / --pdg-ink /
    --pdg-muted / --pdg-border (et variantes accent) dans :root
  Et les titres s'affichent en Fraunces, le corps/UI en Inter
  Et aucune requête réseau vers un CDN de polices n'est émise (offline-friendly)
  Et aucun écran ne présente le rendu « Bootstrap par défaut » (surfaces, rayons, ombres tendres)
```

### Sc.2 — Fondation thème : défaut système + zéro flash `@ihm @vert`

```gherkin
Scénario: Le thème par défaut suit la préférence système, sans flash au chargement
  Étant donné un utilisateur sans choix de thème enregistré
  Et une préférence système "sombre" (prefers-color-scheme: dark)
  Quand j'ouvre l'app
  Alors <html> porte data-theme="sombre" dès le premier rendu (script inline avant Blazor)
  Et les tokens [data-theme=sombre] sont appliqués (fond #1C1A17, carte #26231F, accent #4FB89C…)
  Et aucun flash de thème clair n'apparaît avant le rendu

Scénario: Préférence système "clair" → thème clair par défaut
  Étant donné un utilisateur sans choix enregistré et une préférence système "clair"
  Quand j'ouvre l'app
  Alors <html> porte data-theme="clair" et les tokens :root (clair) s'appliquent sans flash
```

### Sc.3 — Fondation switch : choix persisté, prioritaire sur le système `@ihm @vert`

```gherkin
Scénario: Le switch clair/sombre persiste le choix et prime sur la préférence système
  Étant donné l'app affichée avec un switch de thème visible dans le layout
  Quand je bascule le switch sur "sombre"
  Alors data-theme="sombre" est appliqué immédiatement (transition sobre, pas de rechargement)
  Et le choix est écrit dans localStorage
  Quand je recharge la page (même avec une préférence système "clair")
  Alors le thème reste "sombre" (le choix explicite prime sur le système), sans flash
  Quand je bascule le switch sur "clair"
  Alors data-theme="clair" est appliqué et persisté
```

### Sc.4 — Calendrier (cœur) : lisibilité héroïque du responsable `@ihm @vert`

```gherkin
Scénario: Sur le calendrier, « qui a les enfants » est identifiable en < 3 s
  Étant donné le calendrier partagé (Pages/PlanningPartage.razor) avec des périodes affectées
  Quand j'observe une journée où un parent est responsable
  Alors la case est une mini-carte tokenisée (rayon, ombre tendre, espacement généreux)
  Et la pastille du responsable est l'information héroïque (couleur de responsabilité inline,
    parent bleu / parent orange, nom lisible)
  Et la case « aujourd'hui » est visuellement marquée
  Et les slots restent lisibles et distincts
  Et en thème sombre, le nom du responsable reste lisible sur sa teinte porteuse (teinte inchangée)
  Et les data-testid des cases / pastilles / slots sont inchangés (Sc.14 le prouve)
```

### Sc.5 — Calendrier : menu clic-case restructuré `@ihm @vert`

```gherkin
Scénario: Le menu clic-case sépare visuellement actions primaires et destructives
  Étant donné une case du calendrier ouvrant le menu contextuel à 6 entrées
  Quand j'ouvre le menu
  Alors les actions primaires (poser / affecter) sont visuellement repérables sans lire tout le menu
  Et les actions destructives (supprimer) sont regroupées et distinctes (hiérarchie claire)
  Et les mêmes 6 entrées déclenchent exactement les mêmes commandes qu'avant (aucun flux modifié)
  Et les libellés métier (« Incarner », « poser un slot »…) sont conservés (renommage hors périmètre)
```

### Sc.6 — Calendrier : barre nav + sélecteurs regroupés `@ihm @vert`

```gherkin
Scénario: La barre de navigation et les sélecteurs sont regroupés proprement
  Étant donné l'en-tête du calendrier avec navigation temporelle + sélecteurs (vue, incarnation, rôle)
  Quand j'observe l'en-tête sur navigateur PC
  Alors les contrôles sont regroupés et hiérarchisés (pas d'alignement Bootstrap brut)
  Et les sélecteurs restent fonctionnels et pilotent les mêmes états qu'avant
  Et sur écran étroit, l'en-tête se replie proprement sans casser la lecture de la grille
```

### Sc.7 — Connexion : page d'entrée + OAuth habillés `@ihm @pending`

```gherkin
Scénario: La page de connexion est soignée et les boutons OAuth sont habillés
  Étant donné la page /connexion (Pages/Connexion.razor)
  Quand je l'affiche
  Alors la mise en page est centrée, calme, tokenisée (marque sauge, typo Fraunces/Inter)
  Et les boutons OAuth Google / Microsoft / Apple sont habillés de façon cohérente et cliquables
  Et le clic OAuth déclenche exactement le même flux qu'avant (aucun comportement d'auth modifié)
  Et la page rend correctement en clair et en sombre
```

### Sc.8 — Accueil : orientation refondue `@ihm @pending`

```gherkin
Scénario: L'accueil oriente clairement l'utilisateur
  Étant donné la page d'accueil (Pages/Home.razor)
  Quand je l'affiche
  Alors le point d'entrée / l'orientation est habillé au système « Cocon élevé » (tokens, typo, formes)
  Et les liens / actions d'orientation mènent aux mêmes destinations qu'avant
  Et l'écran rend correctement en clair et en sombre
```

### Sc.9 — Config foyer : formulaires habillés `@ihm @pending`

```gherkin
Scénario: La configuration du foyer est habillée et lisible
  Étant donné la page de config foyer (Pages/ConfigurationFoyer.razor) — acteurs + formulaires
  Quand je l'affiche
  Alors les acteurs et formulaires sont habillés (cartes tokenisées, champs lisibles, espacement)
  Et un regroupement en onglets est appliqué si cela sert la clarté (sinon regroupement visuel simple)
  Et tous les flux CRUD acteurs / persistance config foyer restent inchangés
  Et l'écran rend correctement en clair et en sombre
```

### Sc.10 — Layout : nav, marque, menu utilisateur, bandeaux `@ihm @pending`

```gherkin
Scénario: Le layout porte la marque et adoucit les bandeaux, switch de thème inclus
  Étant donné MainLayout / NavMenu / MenuUtilisateur
  Quand j'observe la coquille applicative
  Alors la barre de nav et la marque sont tokenisées (accent sauge, typo Fraunces)
  Et le menu utilisateur est habillé et porte le switch de thème (Sc.3)
  Et les bandeaux d'alerte sont adoucis (couleurs tendres, pas de rouge/jaune Bootstrap brut)
  Et l'état de connexion et les actions de menu restent inchangés
  Et la coquille rend correctement en clair et en sombre
```

### Sc.11 — Dialogs (×6) : cohérence + hiérarchie de boutons `@ihm @pending`

```gherkin
Scénario: Les six dialogs partagent un habillage cohérent et une hiérarchie de boutons
  Étant donné les dialogs PoserSlot, AffecterPeriode, DefinirTransfert, EditerPeriode,
    SupprimerPeriode, SupprimerSlot (Components/*Dialog.razor)
  Quand j'ouvre chacun
  Alors ils partagent le même habillage (surface carte tokenisée, rayons, ombre tendre)
  Et l'action de confirmation primaire est visuellement dominante, l'annulation secondaire,
    la confirmation destructive distincte
  Et chaque dialog déclenche exactement la même commande / le même flux d'écriture qu'avant
  Et les couleurs de responsabilité affichées dans les dialogs restent inline et lisibles (clair + sombre)
```

### Sc.12 — Légende : découvrable et cohérente `@ihm @pending`

```gherkin
Scénario: La légende reste découvrable et cohérente avec les couleurs de responsabilité
  Étant donné le composant Légende (Components/Legende.razor)
  Quand j'affiche le calendrier
  Alors la légende est découvrable (repérable sans chercher)
  Et elle reflète fidèlement les couleurs de responsabilité (parent bleu / orange / slots) telles
    qu'affichées dans la grille
  Et elle reste correcte et lisible en clair ET en sombre
```

### Sc.13 — Transverse responsive : Safari iOS / WebKit `@ihm @pending`

```gherkin
Scénario: Le rendu est correct sur Safari iOS (WebKit) à parité avec le PC
  Étant donné l'app affichée sur Safari iOS (ou émulation WebKit fidèle)
  Quand je navigue sur le calendrier puis les autres écrans
  Alors la hauteur pleine page respecte 100vh sans zone coupée (fallback safe si besoin)
  Et les zones sûres sont respectées via env(safe-area-inset-*) (encoche / barre home)
  Et les en-têtes sticky restent collés correctement (pas de saut WebKit)
  Et les cibles tactiles font ≥ 44px
  Et les polices web self-hosted se chargent et s'affichent correctement
  Et la mise en page se replie proprement (pas de débordement horizontal)
```

### Sc.14 — Non-régression : suite complète + balisage intact `@back @pending`

```gherkin
Scénario: La refonte est purement visuelle — aucune régression comportementale
  Étant donné la refonte graphique appliquée à tout le front
  Quand j'exécute la suite COMPLÈTE (dotnet test, sans --no-build ni filtre, Docker actif)
  Alors la suite est verte 161/161 (référence de non-régression)
  Et aucun data-testid porteur de test n'a été retiré ou renommé
  Et aucun observable ni flux d'écriture (canal requête/réponse, dialogs, diffusion SignalR)
    n'a été modifié
  Et les couleurs de responsabilité inline (parent bleu / orange / slots) sont préservées
```

---

# Retours produit (PO)

<!-- rempli après le gate G3, consommé à la /cloture -->

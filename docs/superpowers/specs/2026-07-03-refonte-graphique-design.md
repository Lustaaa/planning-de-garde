# Refonte graphique complète — « Cocon élevé »

> Design doc issu du brainstorming du 2026-07-03. Sujet **hors règles GtaX** (carte blanche
> créative, décision PO). Périmètre = **tous les écrans** du front Blazor WASM. Bootstrap
> **conservé sous le capot** (habillage réécrit, grille utilitaire gardée).

## Principe directeur : l'intuitivité prime

Contrainte n°1 posée par le PO : **l'IHM doit être intuitive**. L'esthétique sert cet objectif,
jamais l'inverse. Chaque écran est jugé sur : *un parent comprend-il en 3 secondes qui a les
enfants, et comment agir, sans lire de mode d'emploi ?*

Conséquences concrètes :

- **Hiérarchie** : « qui est responsable maintenant » est l'information héroïque, toujours visible
  et immédiatement identifiable.
- **Action évidente** : l'action primaire (affecter / poser) doit être repérable sans ouvrir un
  menu opaque. Le menu clic-case à 6 entrées est visuellement structuré (primaires vs
  destructives) pour réduire l'hésitation.
- **Moins de charge mentale** : espacement, regroupement, feedback clair, états vides explicites.
- **Vocabulaire** : les libellés jargonnants (« Incarner », « Poser un slot », « Affecter une
  période ») sont **signalés comme candidats à clarification** mais **non renommés dans ce sujet** —
  un renommage touche la spec produit, hors périmètre purement graphique. Listés en « suites
  possibles ».

## Système visuel

**Palette** (accent **hors bleu / hors orange** — ces teintes restent la donnée « qui a les
enfants »). Deux thèmes : **clair** (défaut) et **sombre**, mêmes tokens, valeurs différentes.
Le sombre est un **slate froid net** (révision hors-sprint sur retour PO : le brun chaud initial,
jugé terne, a été écarté au profit d'un neutre profond où l'accent sauge-menthe ressort).

| Token | Clair | Sombre | Rôle |
|---|---|---|---|
| `--pdg-bg` | `#F7F1E8` | `#14161A` | Fond (slate profond, légèrement froid) |
| `--pdg-card` | `#FFFDF9` | `#1E222A` | Surface carte (surélevée) |
| `--pdg-accent` | `#2E6F5E` | `#5FC9AC` | Sauge (marque, actions) — sauge-menthe en sombre, ressort sur le slate |
| `--pdg-accent-dark` | `#245A4C` | `#47AC90` | Hover / focus |
| `--pdg-accent-soft` | `#E4F0EA` | `#1E2E29` | Badges, pastilles |
| `--pdg-ink` | `#2B2620` | `#E9ECF1` | Texte principal (quasi-blanc froid) |
| `--pdg-muted` | `#8C8273` | `#98A1AE` | Texte secondaire (gris-bleu) |
| `--pdg-border` | `#ECE1D2` | `#2C313B` | Bordures |

Les **couleurs de responsabilité** (parent bleu, parent orange, slots) restent **inline, priorité
maximale**, inchangées : elles sont de la donnée, pas du thème. En sombre, on garantit leur
contraste (lisibilité du nom sur la teinte) via la couleur de texte, sans changer la teinte porteuse.

**Bascule de thème** : un **petit switch** clair/sombre dans le layout (barre supérieure / menu
utilisateur). Comportement :
- Défaut = préférence système (`prefers-color-scheme`).
- Choix explicite de l'utilisateur **persisté** (`localStorage`), prime sur le système.
- Appliqué via un attribut sur `<html>` (`data-theme="clair|sombre"`) → tokens `:root` /
  `[data-theme=sombre]`. Aucun flash au chargement (script inline très court avant le rendu Blazor).

**Typographie**
- Titres / affichage : **Fraunces** (serif éditorial, chaleur + caractère).
- Corps / UI : **Inter**.
- Chargement des polices : self-hosted (`wwwroot/fonts/`), aucun CDN externe.

**Formes & profondeur**
- Rayons : 14–16px cartes, 10–12px contrôles.
- Ombres : multi-couches tendres (pas de bord dur Bootstrap).
- Espacement généreux, échelle cohérente.

**Cibles & responsive** : deux surfaces à **parité** —
- **Navigateur PC classique** (desktop) : grille agenda pleine largeur, riche.
- **Safari iOS** (mobile) : tap-friendly, cibles ≥ 44px, mise en page qui replie proprement.

Attention **WebKit / Safari iOS** : tester `100vh`/safe-areas (`env(safe-area-inset-*)`), le
comportement des `position: sticky`, les transitions, et le rendu des polices web. Pas de
dépendance à des features CSS non supportées par le Safari iOS courant.

## Périmètre — tous les écrans

| Écran / zone | Fichier | Traitement |
|---|---|---|
| Calendrier partagé | `Pages/PlanningPartage.razor` | **Cœur.** Cases = mini-cartes, pastille responsable héroïque, slots lisibles, « aujourd'hui » marqué. Menu clic-case restructuré (primaires / destructives). Barre nav + sélecteurs (vue, incarnation, rôle) regroupés proprement. |
| Connexion | `Pages/Connexion.razor` | Page d'entrée soignée, boutons OAuth Google/Microsoft/Apple habillés. |
| Accueil | `Pages/Home.razor` | Point d'entrée / orientation. |
| Config foyer | `Pages/ConfigurationFoyer.razor` | Acteurs, formulaires, en onglets si pertinent. |
| Layout | `Layout/MainLayout.razor`, `NavMenu.razor`, `MenuUtilisateur.razor` | Nav, marque, menu utilisateur, bandeaux d'alerte adoucis, **switch de thème clair/sombre**. |
| Dialogs (×6) | `Components/*Dialog.razor` | PoserSlot, AffecterPeriode, DefinirTransfert, EditerPeriode, SupprimerPeriode, SupprimerSlot — habillage cohérent, hiérarchie de boutons. |
| Légende | `Components/Legende.razor` | Toujours découvrable, cohérente avec les couleurs de responsabilité. |
| Feuille globale | `wwwroot/app.css` | Design tokens `--pdg-*` étendus, styles de base. |

## Approche technique

- **Non-destructif** : CSS custom par-dessus Bootstrap. On garde la grille/les utilitaires, on
  réécrit l'habillage (couleurs, typo, formes, espacement). Pas de big-bang, pas de suppression de
  Bootstrap.
- **Design tokens** : centraliser dans `app.css` (`:root`), consommés partout. Les styles inline
  spécifiques composant (ex. grille agenda dans `PlanningPartage.razor`) migrent vers des classes
  tokenisées quand ça sert la cohérence.
- **Polices self-hosted** : aucun appel réseau externe (offline-friendly, WASM).
- **Aucune régression comportementale** : la refonte est **purement visuelle**. Les `data-testid`,
  les observables, les couleurs de responsabilité, les flux d'écriture restent intacts. La suite de
  tests complète (161/161) reste verte.

## Exécution via le pipeline

Un **sprint UI/UX** dans la boucle SCRUM habituelle :

1. `/planning` — le scrum-master pose le goal « refonte graphique intuitive » + scénarios `@ihm`
   (un par écran / zone), tableau d'avancement.
2. `/sprint` — la dev-team implémente écran par écran, **guidée par le skill `frontend-design`**
   (direction esthétique, typo, éviter le templated). Gate visuel **G3** obligatoire (le PO valide
   à l'œil chaque écran refondu).
3. `/cloture` — retours au backlog, git → PR → merge.

Règles GtaX écartées pour tout le sujet (pas de `var(--gold-*)`, pas de CONV-*, pas de gold.css).

## Hors périmètre (suites possibles, non engagées)

- Renommage des libellés métier jargonnants (« Incarner », « slot », « affecter ») → touche la spec
  produit, sujet séparé.
- Animations / micro-interactions avancées (au-delà de transitions de hover sobres).
- Refonte de l'architecture d'information / des parcours (au-delà de la clarté visuelle).

## Critères de succès

- Aucun écran ne lit « Bootstrap par défaut ».
- « Qui a les enfants maintenant » identifiable en < 3 s sur chaque écran pertinent.
- Cohérence : un seul système de tokens, typo, formes sur tous les écrans.
- Couleurs de responsabilité toujours lisibles et distinctes de l'accent, en clair **et** en sombre.
- **Thème sombre** cohérent sur tous les écrans, switch persistant, aucun flash au chargement.
- Rendu correct sur **navigateur PC** et **Safari iOS** (safe-areas, sticky, polices).
- Suite complète verte, aucune régression comportementale.
- Gate visuel G3 validé par le PO sur chaque écran (clair + sombre).

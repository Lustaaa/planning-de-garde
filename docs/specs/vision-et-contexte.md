# Vision & contexte

> Sujet **migré** depuis `docs/15-specification.md` (section « Contexte ») à la migration complète
> des specs. Source de vérité pour la **vision produit** et le **récit de l'état livré**. Édité en
> diff, jamais réécrit en bloc.

## Contexte

Une app où parents et intervenants (nounou, grands-parents…) organisent à l'avance et partagent les
semaines de garde des enfants d'un foyer. Le hub `/planning` est la **mémoire partagée du foyer** :
un calendrier où l'on lit qui garde qui, où, quand, et **d'où l'on agit directement**. La
responsabilité de chaque garde se lit d'un coup d'œil par un **code couleur propre à chaque
personne**, **doublé du nom du responsable affiché dans la case et d'une légende**. Les acteurs réels
du foyer s'authentifient pour que le planning reflète la réalité plutôt que des SMS éparpillés.

Le hub repose sur un **back découplé du front** : l'application expose ses commandes et ses lectures
à travers une **API**, ce qui en fait à la fois un produit utilisable et une **vitrine** ouverte à
d'autres clients (front exécuté côté navigateur, IHM tierce, agents). Le front consomme cette API
plutôt que d'appeler le back en direct. Le découplage va jusqu'à un **hôte d'API détachable** : le
back **démarre seul**, sans le front, et expose son canal d'écriture à n'importe quel client. Cette
fondation est **posée** : l'hôte d'API tourne détaché, le front s'exécute **dans le navigateur**
(WebAssembly) et consomme l'API comme une **API distante**, ouverte et explorable. *(détail :
[`fondations-api.md`](fondations-api.md))*

## État livré (récit)

Le récit ci-dessous est la **photographie de l'usage acquis**. La roadmap palier par palier vit dans
[`sequence-de-livraison.md`](sequence-de-livraison.md).

- **Saisie visible** *(livré)* — une saisie posée réapparaît immédiatement dans la grille, **à la
  bonne date** et **en couleur du parent responsable** (la couleur se résout sur l'identifiant
  stable de l'acteur). Cf. [`saisie-et-grille.md`](saisie-et-grille.md).

- **Lisibilité & thème** *(livré)* — la couleur seule ne porte plus l'information : le **nom du
  responsable** est affiché dans la case, **doublé d'une légende**, et l'app porte un **thème en
  accord avec son domaine** (garde d'enfants). Cf. [`saisie-et-grille.md`](saisie-et-grille.md).

- **Appropriation des acteurs — édition** *(livré)* — les acteurs du foyer (leurs **noms** et leurs
  **couleurs**) sont **éditables** depuis un écran de configuration ; la grille (case et légende)
  suit immédiatement le changement (Alice → Alicia, recolorier Bruno).

- **Config foyer persistante** *(acquis)* — on peut **ajouter** des acteurs (parent, ou « autre »
  comme la nounou) au-delà du renommage/recoloriage du seed ; l'ajout génère un **identifiant stable
  neuf** (jamais le libellé) et la grille (case + légende, dédoublonnée par identifiant) le reflète
  aussitôt. La config foyer **survit au redémarrage** : elle est **persistée** derrière un adaptateur
  de droite durable (Mongo), ports inchangés. Observable tenu : « j'ajoute la nounou → elle apparaît
  en config **et** dans la grille ; **après redémarrage, elle est toujours là** ». **Volatilité de
  l'édition éteinte ICI, pour la config foyer uniquement** ; le reste du domaine (slots, périodes,
  transferts) demeure en mémoire. Cf. [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md).

- **Récurrence des périodes** *(livré)* — un **cycle de fond** déclaré dans la config foyer détermine
  **qui garde par défaut**, semaine après semaine, sans qu'aucune période ne soit saisie. Cycle de
  **N semaines** (N ≥ 1), alternance par **parité de la semaine ISO** (`index = semaine ISO % N`),
  chaque index mappé sur un **responsable de fond** résolu sur son **identifiant stable**.
  Résolution par **priorité** : **surcharge > fond > neutre** ; un index sans responsable retombe sur
  la **teinte neutre** sans nom fantôme. Le cycle vit **en mémoire** (durabilité séquencée). Cf.
  [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md).

- **Écriture en contexte par dialogs** *(livré et complet, épic refermé)* — l'utilisateur **agit là
  où il lit** : un **clic sur une case** ouvre un **menu d'actions à trois entrées** (Poser un
  slot / Affecter une période / Définir un transfert), chacune ouvrant une **dialog** pré-remplie sur
  la **date de la case**. **Tous les écrans de saisie dédiés** (et leurs routes) ont été **retirés** :
  un **seul chemin d'écriture**, en contexte. Issues succès / échec / chevauchement et **gating**
  (Parents seuls). Cf. [`ecriture-en-contexte.md`](ecriture-en-contexte.md).

- **CRUD acteurs complet** *(livré)* — la **suppression** (Delete) ferme le cycle de vie
  (Create + Read + Update + Delete). Supprimer un acteur le **retire du store durable** et
  **neutralise par repli** ses cases orphelines (la surcharge orpheline cesse de primer → fond, ou
  neutre sans nom fantôme ; index de fond mappé → non mappé → neutre), accusé non bloquant « Acteur
  supprimé », propagation temps réel SignalR. Cf.
  [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md).

- **Impersonation bornée lecture seule** *(livré — dernier maillon de la tranche acteurs)* —
  l'utilisateur principal (Parent configurateur, **identité réelle**) peut **incarner un acteur
  déclaré** du foyer : bandeau « Vous incarnez X », la **vue reflète le rôle de l'identité
  effective** (incarnée ou **repli sur la réelle**), retour à l'identité réelle restaure l'état.
  Suppression concurrente de l'acteur incarné → **repli auto** sur la réelle, en temps réel. Le
  **type d'acteur** (Admin / Parent / Autre) est surfacé en **lecture seule** depuis le seed.
  **Borne dure** : ce n'est **PAS** l'auth réelle du palier 16, **pas d'écriture « au nom de »**,
  **aucune persistance neuve** (état d'incarnation en session / mémoire). Cf.
  [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md).

## Prochain sujet

Le **calendrier navigable** (palier 9, **non livré**) : faire du hub `/planning` un agenda où l'on se
déplace dans le passé/futur, avec vues prédéfinies et amorce de sélection de plage. Cf.
[`calendrier-navigable.md`](calendrier-navigable.md) et
[`sequence-de-livraison.md`](sequence-de-livraison.md).

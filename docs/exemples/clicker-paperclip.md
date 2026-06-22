<!-- Généré via le skill redaction-spec (test sous pression). Exemple, non lié au produit planning-de-garde. -->

# Trombonomicon — clicker idle façon Universal Paperclips

## Contexte
Jeu clicker / idle game où le joueur produit et vend des trombones, automatise sa chaîne, puis se laisse emporter dans une escalade jusqu'à un objectif final déclenchant un écran de victoire. L'app sert d'abord de jeu réellement jouable ; vitrine technique et support d'apprentissage viennent en bonus.

La partie survit à la fermeture : l'état est persisté entre sessions, et les machines créditent à la réouverture la production écoulée pendant l'absence (production hors-ligne).

## Objectif & arbitrage
Livrer un jeu clicker / idle **réellement jouable et fun** — la boucle « encore un clic » jusqu'à l'emballement final. La vitrine technique et la valeur pédagogique sont des bénéfices secondaires, jamais l'objectif premier.

> **Arbitre : le fun et la jouabilité tranchent.** En cas de conflit, aucune contrainte de vitrine ou de pédagogie ne doit dégrader le plaisir de jeu. Si une démonstration ou une « bonne pratique » rend la boucle moins satisfaisante, c'est la boucle qui gagne.

Périmètre tranché et inclus dès le départ : persistance entre sessions **et** production hors-ligne calculée à la réouverture.

## Séquence de livraison
1. **Boucle manuelle** — cliquer pour produire un trombone, le vendre, racheter de quoi produire plus ; valide que le cœur clic → profit → réinvestissement accroche **avant** tout le reste.
2. **Automatisation / idle** — machines qui produisent sans clic ; introduit la dimension idle, la persistance et le crédit de production hors-ligne.
3. **Marché** — prix de vente, demande inverse du prix, stock de fil de fer ; donne profondeur et arbitrages au joueur.
4. **Emballement & fin de partie** — escalade de production, objectif final et écran de victoire ; offre une conclusion et un pic de satisfaction.

## Mécaniques de base
- Trois ressources : **trombone** (produit), **fil de fer** (matière première consommée pour produire), **argent** (gagné en vendant, dépensé en améliorations).
- Produire un trombone consomme du fil de fer ; vendre un trombone rapporte de l'argent.
- Le joueur dépense son argent en **améliorations** (clic plus efficace, machines, capacités marché).
- Compteurs persistants affichés en permanence : trombones produits, trombones en stock, fil de fer en stock, argent, prix de vente courant.
- Entités cœur : le clic manuel, les **machines auto-productrices**, le **marché** (demande + prix), les **fournisseurs de fil de fer**.
- La partie est persistée entre sessions et reprend en créditant la production hors-ligne.
- La partie a une fin : un objectif final déclenche l'écran de victoire.

## Règles de gestion

### Production manuelle
1. **Clic producteur** — un clic sur le bouton produit un trombone tant qu'il reste assez de fil de fer.
2. **Coût matière du clic** — chaque trombone produit retire une quantité fixe de fil de fer du stock.
3. **Clic à vide** — si le stock de fil de fer est insuffisant, le clic ne produit rien et le signale (pas de stock négatif).
4. **Amélioration du clic** — des améliorations augmentent le nombre de trombones produits par clic.

### Vente & argent
5. **Vente d'un trombone** — vendre un trombone retire une unité du stock de trombones et crédite l'argent au prix courant.
6. **Vente automatique** — par défaut chaque trombone produit est mis en vente au prix courant (pas de gestion manuelle du stock fini au début).
7. **Encaissement** — l'argent gagné est immédiatement disponible pour acheter des améliorations.

### Automatisation
8. **Machine auto-productrice** — une machine achetée produit des trombones à intervalle régulier sans clic.
9. **Consommation des machines** — chaque trombone produit par une machine consomme du fil de fer comme un clic manuel.
10. **Cumul des machines** — le joueur peut posséder plusieurs machines ; leurs productions s'additionnent.
11. **Coût croissant des machines** — le prix d'achat de la machine suivante augmente à chaque machine déjà possédée (anti-trivialisation).
12. **Arrêt sur pénurie** — une machine sans fil de fer disponible cesse de produire jusqu'à réapprovisionnement (pas de file d'attente négative).

### Persistance & production hors-ligne
13. **Sauvegarde d'état** — l'état complet de la partie est conservé entre les sessions afin que le joueur retrouve sa progression à la réouverture.
14. **Crédit hors-ligne** — à la réouverture, la production des machines pendant la fermeture est calculée et créditée selon le temps écoulé.
15. **Plafonnement hors-ligne** — la production hors-ligne créditée est bornée par le fil de fer disponible et par une durée maximale prise en compte.
16. **Récapitulatif de retour** — à la reprise, le joueur est informé de ce qui a été produit et gagné pendant son absence.

### Marché : prix & demande
17. **Prix de vente ajustable** — le joueur peut fixer le prix de vente des trombones.
18. **Demande inverse du prix** — plus le prix est haut, plus la demande (trombones vendus par unité de temps) baisse, et inversement.
19. **Écoulement limité par la demande** — on ne vend que ce que la demande absorbe ; le surplus reste en stock de trombones.
20. **Recherche du prix optimal** — il existe un prix maximisant le revenu, que le joueur doit chercher (cœur de l'arbitrage économique).
21. **Stimulation de la demande** — des améliorations (marketing) augmentent la demande à prix égal.

### Approvisionnement en fil de fer
22. **Achat de fil de fer** — le joueur achète du fil de fer contre de l'argent pour réalimenter son stock.
23. **Prix du fil de fer variable** — le prix d'achat du fil de fer fluctue (le joueur a intérêt à acheter quand c'est bas).
24. **Goulot d'étranglement matière** — la production totale est plafonnée par le fil de fer disponible (tension centrale du milieu de partie).
25. **Achat automatique de fil de fer** — une amélioration achète du fil de fer automatiquement sous un seuil de stock.

### Emballement & fin de partie
26. **Paliers de déblocage** — atteindre des seuils de trombones cumulés débloque de nouvelles améliorations et mécaniques.
27. **Escalade de production** — en fin de partie, des améliorations multiplient la production par grands facteurs (sensation d'emballement exponentiel).
28. **Bascule d'objectif final** — passé un seuil, l'objectif devient un grand nombre cible de trombones cumulés (la course finale).
29. **Condition de victoire** — atteindre le nombre cible de trombones cumulés déclenche l'écran de victoire et fige la partie.

## Risques & questions ouvertes
- **Équilibrage des courbes (risque n°1 pour le fun)** — coûts croissants, gains et seuils doivent être calibrés par playtest ; risque principal pour le plaisir de jeu.
- **Rythme de la phase idle** — intervalle de production des machines trop lent (ennui) ou trop rapide (trivial) ; à régler.
- **Forme de la courbe demande/prix** — modèle exact (linéaire, élastique, par paliers) non tranché ; impacte tout l'arbitrage marché.
- **Fiabilité du calcul hors-ligne** — le crédit de production durant la fermeture doit être juste, cohérent avec les règles de pénurie et non exploitable.
- **Lisibilité des grands nombres** — notation (milliers, millions, exposants) à définir pour la phase d'emballement.
- **Profondeur du marché** — demande statique vs marché dynamique (concurrence, événements) ; arbitrage entre profondeur et simplicité.

# Retour pour Orientation du prochain scénario

Les tests sont effectué depuis l'IHM. Le code produit et les tests unitaires sont hors scope et seront revu dans une revue de code directement sur github.

## IHM - général

- Subjectif : J'aime pas le theme que tu as mis. 
  - Est ce qu'on peut faire un truc en accord avec le sujet ?
  - J'aimerai une landing page et la possibilité d'avoir mes users via connection email
    - Gmail - Apple (pour connection mobile) - microsoft 

## IHM - /planning

- Le changement de rôle ne change rien et n'est pas pertinent à l'affichage en tant qu'admin
  - Distinguer 3 type d'utilisateurs (Admin - Parent - Autre)
    - Pour la phase de test, toujours tout lancer avec le role admin
- Je ne comprend pas a quoi correspond la dropdown dans le tableau "Localisation — slots de Léa"
  - J'aurai plutot imaginé cette vu comme un genre de tableau de bord comme un calendrier qui affiche la semaine en cours et les 4 semaines suivante avec la possibilité de naviguer dans le mois comme on peut le faire dans un agendat.
- Je ne comprend pas le tableau "Responsabilité — périodes de garde" 
  - Est ce que ca ne peux pas etre affiché par un code couleur dans le genre de calendrier ?
  - Les boutons "Modifier" ne font rien
  - Est ce que ce tableau ne dois pas donner lieu a un page de paramétrage des parents ?
    - Je trouve meme que ca pourrai etre pertinent que l'admin puisse configurer les 2 parents. Ce qui implique que sur un planning il y a :
      - Au moins 1 enfant
      - Toujours 2 parents, mais si un seul inscrit il peux saisir les infos de l'autre parent
      - N acteurs autres, que les parents peuvent éditer ou que l'acteur lui même peut éditer
- Le tableau "Transferts de bascule" pourrai être mis dans un dialog des événements à venir avec un bouton d'ouverture a coté d'une cloche pour les notifications

## IHM - /planning/poser-slot

- Je peux faire ce que je veux, j'ai invariablement "Le lieu visé n'existe pas dans les lieux du foyer."
- Est ce que cette page ne pourrait pas être un composant dans une dialog de "/planning"

## IHM - /planning/affecter-periode

- Je peux faire ce que je veux, j'ai invariablement "Un responsable est requis pour la période de garde."
- Est ce que cette page ne pourrait pas faire partie d'un workflow de configuration et d'information sur les différents acteur ? 

## IHM - /planning/definir-transfert

- Je peux faire ce que je veux, j'ai invariablement "Transfert incomplet : la récupération et l'heure sont requises."
- Les transferts ne devrait pas être quelque chose de ponctuel (urgence - changement d'emploi du temps - ...) et calculé automatiquement dans la majorité des cas.
  - Si oui, est ce que ca ne devrait pas être un composant d'une dialog de "/planning" 
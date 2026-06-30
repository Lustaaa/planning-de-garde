# Risques & questions ouvertes

> Sujet **migré** depuis `docs/15-specification.md` (section « Risques & questions ouvertes ») à la
> migration complète des specs. **Registre canonique** des risques, dettes, questions ouvertes et
> révisions de règle en attente. Édité en diff, jamais réécrit en bloc.

## Pilotage & priorisation

- **L'usage tient la main — ne pas enchaîner un sprint sans valeur produit.** Deux sprints structurels
  (fondation, hôte d'API) puis neuf sprints d'usage (saisie visible, lisibilité & thème, édition des
  acteurs, config foyer persistante, récurrence des périodes, écriture en contexte slot/période,
  écriture en contexte transfert, suppression d'acteur, **impersonation bornée lecture**) sont derrière
  nous. Les prochains sujets (calendrier navigable, cycle de fond riche, survol enrichi) restent des
  paliers **d'usage** ; les paliers techniques débloqués (persistance du reste du domaine, PWA,
  Docker) **et la dette de test** (rétrofit temps-réel SignalR) sont tentants mais **doivent rester
  derrière l'usage**. La seule persistance tirée devant a été **bornée à la config foyer** (observable
  direct) : ne pas en faire un cliquet.

- **Prochain sujet — calendrier navigable (`calendrier-navigable`, palier 9, épics É4 + É7).** Faire
  de `/planning` un **agenda navigable** : déplacement **passé/futur**, **vues prédéfinies** (semaine,
  mois, 4 semaines glissantes), **amorce de sélection de plage de cases** pour définir une période sur
  l'intervalle. Besoin **ancien** (retours s02 #3 navigation / s03), **rang +2** du backlog, tranché en
  **porte G2** par le PO. **Orientation de découpe (pas une règle, palier groupé)** : (a) plus petit
  incrément probable = **navigation seule** ; (b) **sélection de plage** = sujet plein **cuttable en
  tranche 2** si débordement ~2h ; (c) **périmètre exact tranché au make-gherkin**, non pré-arbitré
  (corollaire de découpe). **Aucune persistance neuve.** La grille actuelle est une vue posée non
  navigable : ne pas la sous-entendre acquise.

- **Pilotage au catalogue — retours produit VIDE au sprint 14.** Le sprint 14 (impersonation bornée
  lecture) a été livré et validé au gate G3 **sans aucun retour d'usage déposé** (chemin nominal, goal
  9/9 atteint, aucune anomalie, livraison verte 6/6 · 214/214) : la priorisation du prochain sujet
  (calendrier navigable) dérive du **backlog seul** (rang +2, G2 PO), sans signal d'usage frais. Point
  de vigilance : **confirmer le besoin réel au démarrage du sprint** ; ne pas enchaîner un sprint « à
  vide » sur du catalogue si un retour d'usage plus pressant émerge.

## Frontières & bornes

- **Impersonation bornée lecture livrée — frontière avec l'auth réelle (palier 16) et l'écriture « au
  nom de » (hors-cap).** L'impersonation bornée **lecture seule** est **livrée** (palier 8) : incarner
  un acteur déclaré (bandeau, vue selon le rôle effectif, retour identité réelle, retour auto sur
  suppression concurrente), **sans** écriture « au nom de », **sans** persistance neuve, **sans** auth
  réelle. La **borne dure tient** : ce n'est pas l'authentification du palier 16. L'**impersonation
  écriture « au nom de »** (agir réellement sous l'identité incarnée) est **hors-cap** : elle **franchit
  la borne dure du palier 8** (lecture seule) et **amorce l'auth réelle** (chemin d'écriture neuf,
  règle 30 anti-cliquet) — à ne tirer que sur **décision PO explicite de changer le cap** (candidat
  **G1**). À la livraison du palier 16, l'impersonation se transforme en accès réel par acteur.

- **Borne anti-cliquet à tracer sans déraper.** La persistance devant l'usage est une **exception
  bornée à la config foyer**. Risque de **cliquet** : que le reste du domaine (slots / périodes /
  transferts / **cycle de fond**) suive devant l'usage. Garder cette persistance **en queue** (paliers
  « config foyer durable — reste » puis « persistance réelle ») ; la borne est écrite noir sur blanc
  (règle 30, et `docs/BACKLOG.md`). L'écriture en contexte (transfert compris) l'a respectée ; la
  **suppression d'acteur** a exercé la persistance **déjà acquise** de la config foyer, sans l'étendre ;
  l'**impersonation bornée lecture** n'a tiré **aucune persistance neuve** (état session / mémoire). Le
  **calendrier navigable** ne doit en tirer aucune non plus.

- **Édition vs persistance — deux périmètres à ne pas confondre.** La config foyer est **durable**
  (référentiel des acteurs), mais le **reste du domaine reste volatile** : slots, périodes, transferts
  **et le cycle de fond** vivent encore en mémoire jusqu'à leur palier de persistance. Ne pas reprocher
  au reste de ne pas persister (c'est la découpe) ; ne pas tirer leur persistance en avant au prétexte
  que la config foyer est durable (c'est le cliquet).

## Dettes de test & temps réel

- **Rétrofit déterministe des tests temps-réel SignalR (rang +3 — dette de test).** Au palier 8, la
  touche des composants config / grille partagés a exposé une **course latente**
  (`UnknownEventHandlerId`) dans des tests `*TempsReel*` interagissant avec un `select` sans garde
  d'énumération → un **garde déterministe `WaitForState`** a été posé localement, mais le **rétrofit
  reste à généraliser** (et la **convergence SignalR multi-clients** — distincte de la course
  d'énumération — n'est pas couverte ; flake observé ~1/30 sous charge). C'est une **dette de test sans
  observable métier** (vigilance « faux sentiment de progrès ») : à porter en **retro-sprint**,
  **derrière l'usage** (Calendrier navigable). C'est un **prérequis de fait** de l'édition concurrente
  (ci-dessous) : driver celle-ci sur une fondation instable produirait des scénarios **flaky par
  construction**.

- **Édition concurrente du même jour sous dialog ouverte (rang +4 — différée).** Prouver le
  comportement quand **deux acteurs éditent le même jour** alors qu'une dialog est ouverte —
  **dernière-écriture-gagne** (règle 11, acquise) à démontrer **sous dialog en contexte**. La
  caractérisation actuelle ne couvre que « le rafraîchissement de fond n'interfère pas avec une dialog
  ouverte », pas l'édition concurrente du même jour. **Dépend du rétrofit SignalR (rang +3)** :
  différée jusqu'à stabilisation de la fondation temps-réel ; aucune règle neuve.

- **Diffusion déclenchée par l'écriture** — Garantir qu'une écriture aboutie déclenche bien
  l'actualisation temps réel des autres écrans, sans jamais écrire par le canal de diffusion ; validé
  en usage réel pour le cycle de fond (deux écrans convergent sans rechargement), **acquis pour les
  dialogs en contexte** (l'ouverture d'une dialog n'interfère pas avec le rafraîchissement de fond),
  **pour la suppression d'acteur** (un second écran voit la case orpheline retomber sur le fond et la
  légende se dédoublonner) **et pour le retour auto d'incarnation** (la suppression concurrente de
  l'acteur incarné replie l'identité effective sur la réelle, bandeau retiré, en temps réel). La
  stabilité sous exécution parallèle des tests temps-réel reste à durcir (rétrofit déterministe, rang
  +3).

## Cycle de fond riche (palier 10)

- **Cycle de fond riche (palier 10) — sujet plein, deux frontières à surveiller.** (1) L'enrichissement
  **rouvre** la décision actée « ancrage ISO sans ancre » : choisir explicitement un début/une phase
  est l'option « date d'ancrage » jadis écartée, à **ré-arbitrer au make-gherkin de ce palier**
  (révision de règle hors boucle) — la règle 11 n'est **pas** révisée d'ici là. (2) Plage début/fin
  **+** sur-cycles vacances **chevauchent la durabilité du cycle** (palier « config foyer durable —
  reste ») : n'enrichir que l'**observable** de cycle, ne PAS tirer Mongo pour le cycle par précaution
  (borne anti-cliquet). Le besoin est gardé **groupé** ; sa **découpe est impérative** au cadrage.
  Risque spec « coût de saisie du cycle » exactement ici.

- **Coût de saisie du cycle** — Saisir un cycle multi-semaines est lourd ; le cycle de fond livré
  (parité ISO, mapping par index) en pose le socle, et le palier « cycle de fond riche » devra le
  rendre **supportable** sans le complexifier inutilement. Le transfert dérivé automatiquement réduit
  ce coût pour le cas nominal.

## Acceptation runtime

- **Acceptation runtime obligatoire (rempart anti vert-qui-ment).** Les incréments d'usage sont
  prouvés sur l'**app réellement câblée** (front WASM + API distante + SignalR + store réel), pas par
  doublures : le cycle de fond a été accepté en affichant le **fond résolu** sans saisie de période ;
  l'écriture en contexte (slot, période, **transfert**) en prouvant qu'une saisie **réellement
  enregistrée** via une dialog réapparaît **positionnée, colorée et nommée** à la **date de la case** ;
  la **suppression d'acteur** sur le **store Mongo réel** (l'acteur retiré disparaît du store **et**
  après redémarrage, cases retombées sur fond/neutre, légende dédoublonnée, gating Invité, échec API
  laissant le store inchangé, propagation SignalR) ; l'**impersonation bornée lecture** sur l'**app
  câblée / G3** (bandeau affiché, menu clic-case visible/masqué selon le rôle effectif, gating complet
  de l'écran config, **retour auto** sur suppression concurrente par diffusion temps réel). Ce rempart
  reste la règle pour les prochains incréments : le **calendrier navigable** se valide sur l'app
  câblée ; un test de composant à doublures peut afficher une grille alors que le câblage réel échoue.

## Ergonomie de surface & non-bugs

- **Légende ≠ bug (non-bug, harmonisation de teinte).** Le ressenti « les couleurs de la légende ne
  sont pas celles des acteurs » a été **confronté au code courant** : la légende et la case-jour
  résolvent le **même token couleur sur le même singleton**. **Aucun défaut de résolution.** L'écart
  vient d'une **incohérence de teinte de présentation** : pastille de légende **saturée** vs fond de
  case **pâle** (choix de design : fond pâle = texte sombre lisible). C'est une **évolution de teinte**,
  **jamais** un fix ciblé. À regrouper avec l'ergonomie config (palette/picker de couleur) quand elle
  remontera, pas un sujet seul.

- **Évolutions de surface non priorisées seules** — **sélecteur de couleur (palette / picker)** dans
  l'écran de config (au lieu d'une saisie libre) ; **onglets** de config par acteur (faible conviction
  PO : « un seul foyer → tous les acteurs sur le même écran ») ; **harmonisation de teinte** légende ↔
  case (ci-dessus). Reconnues, séquencées derrière l'usage, sans règle ni palier dédié tant que l'usage
  ne les appelle pas.

- **Périmètre « résumé de la journée » (survol enrichi) non défini** — périodes ? slots ? responsable ?
  transferts ? Sujet potentiellement plus gros qu'il n'y paraît, proche du « qui récupère ce soir »
  (palier immédiat). À **cadrer au make-gherkin** quand le survol sera pris ; ne pas le sous-estimer
  comme « simple tooltip ». Le survol simple (nom complet) est **conforme et accepté** : rien de cassé,
  le résumé est un comportement **neuf**. Skippé ce cycle faute de demande PO.

## Révisions de règle en attente (hors boucle)

- **Question ouverte — workflow demande/accord (révision de règle 26)** — Le PO veut qu'une période ne
  puisse être réaffectée à l'autre parent qu'après une **demande explicite acceptée**. C'est une
  révision de la règle « modification directe », pas un correctif ; elle attend le palier « imprévu &
  échange » et ne génère aucune règle ni sujet tant qu'il n'est pas ouvert.

- **Question ouverte — interdiction/dédoublonnage de slot (révision de règle 16)** — Le PO veut
  **refuser ou dédoublonner** la pose répétée d'un même slot. C'est une révision du choix v1 « accepté
  avec avertissement », hors de la boucle courante.

- **Question ouverte — ancre/début explicite du cycle (révision de la décision « ancrage ISO sans
  ancre »)** — Le PO veut **choisir le début / la phase** du cycle (quelle semaine = index 0). C'est
  une révision de la décision actée d'ancrage ISO, rattachée au palier « cycle de fond riche » (palier
  10) et tranchée **à son make-gherkin**, pas avant.

## Risques produit & contraintes techniques

- **Adoption de l'autre parent (risque mortel)** — L'app n'a de valeur que si l'autre parent l'utilise
  aussi ; sinon le planning est faux. L'ouverture de l'accès (auth réelle, palier 16) la traite, mais
  elle vient tard : **à ne pas laisser glisser indéfiniment** derrière la technique. Aucun des paliers
  techniques en queue ni des prochains incréments d'usage ne lève ce risque ; l'impersonation bornée
  (livrée) est une **convenance admin**, pas une réponse à ce risque.

- **Contraintes du découplage front/API distant** — Émettre les commandes à travers une API
  **distante** introduit des contraintes (échanges inter-domaines, sérialisation des commandes,
  configuration de l'URL d'API, future authentification) absentes quand le front parlait au back en
  direct ; elles s'accentuent avec l'hôte détaché et le front WASM.

- **Hors-ligne rejouable — piste à trancher au palier PWA** — Au-delà de l'échec clair livré, une
  **file d'écritures côté client** (type *outbox*, IndexedDB) garantissant un rejeu « exactement une
  fois » est la piste minimale ; l'*event sourcing* n'est retenu que si offline / rejeu / audit le
  justifient. Décision **au moment d'ouvrir le palier**, pas un prérequis.

- **Idées consignées non prioritaires** — Indicateur de **présence de l'autre parent** (temps réel) ;
  **slot imbriqué** (un slot peut en contenir un autre) ; **parents liés via leurs enfants** (graphe
  foyer) ; **familles recomposées** (déjà règle 2) : besoins reconnus, séquencés derrière l'usage
  prioritaire, sans règle ni palier dédié tant que l'usage ne les appelle pas.

- **Différenciation** — Vs Cozi / FamilyWall / agenda partagé : le manque précis qu'on couvre mieux
  reste à nommer.

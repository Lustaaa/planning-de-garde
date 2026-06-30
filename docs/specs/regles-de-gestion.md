# Règles de gestion

> Sujet **migré** depuis `docs/15-specification.md` (section « Règles de gestion ») à la migration
> complète des specs. **Catalogue canonique** des règles numérotées R1→R30. Les règles du **cycle de
> fond / cycle de vie d'une période** (R11, R12, R14, R15 + R15bis) sont détaillées dans
> [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md) : seules elles ont leur texte là-bas
> (source unique), elles sont **référencées** ici pour garder le catalogue continu. Édité en diff.

## Foyer & acteurs

1. **Multi-enfants** — Un foyer compte au moins un enfant, et peut en compter plusieurs, chacun avec
   sa propre organisation de garde.

2. **Familles recomposées** — Un foyer peut mélanger des enfants de parents différents, tous visibles
   dans le même planning.

3. **Toujours deux parents** — Un foyer a toujours exactement deux parents ; tant qu'un seul est
   inscrit, il peut saisir les informations de l'autre.

4. **Acteurs « autres » ajoutables, éditables et supprimables** — Un foyer peut avoir N acteurs
   « autres » (nounou, grands-parents…). Ils sont **ajoutables, éditables et supprimables** (par les
   parents ou par l'acteur lui-même), au-delà du seul renommage/recoloriage du seed initial : ajouter
   un acteur le fait exister dans le foyer et dans la grille, **supprimer** un acteur l'en retire et
   **neutralise ses cases orphelines** par repli (cf. règle 6, **livrée** — palier 8 « CRUD acteurs —
   suppression »).

5. **Édition des acteurs (noms + couleurs)** — Les acteurs du foyer (leurs **noms** et leurs
   **couleurs**) sont **éditables** depuis un écran de configuration, et la **grille (case + légende)
   relit immédiatement** la configuration éditée : renommer ou recolorier un acteur se voit aussitôt,
   **partout où l'acteur apparaît** (case, légende, sélecteurs de saisie et mapping du cycle de fond
   doivent lire le même référentiel vivant). Cette édition s'est livrée d'abord **en mémoire, dans la
   session** ; sa **durabilité** (survie au redémarrage) est portée par la règle 6 et la règle 30. La
   survie au redémarrage **est acquise pour la config foyer** : la dette volatile de l'édition s'éteint
   là, ailleurs elle subsiste (cf. règle 30). *Tous les sélecteurs de l'écran de config — y compris le
   sélecteur « Acteur du foyer » — lisent désormais le **store vivant** des acteurs (et non plus une
   liste statique) : un acteur renommé, ajouté ou supprimé est aussitôt cohérent **partout** (case,
   légende, sélecteurs, mapping du cycle de fond). Le **type** d'acteur y est surfacé en **lecture
   seule** (depuis le seed), lu par l'identité effective (cf. règle 8), jamais saisi.*

6. **Ajout & suppression d'acteur, neutralisation par repli (cases ET incarnation), persistance
   bornée** — On peut **ajouter** un acteur au foyer (parent / autre / nounou) : l'ajout génère un
   **identifiant d'acteur stable neuf** (jamais dérivé du libellé d'affichage) et la grille (case +
   **légende dédoublonnée par identifiant**) le reflète immédiatement. La **configuration du foyer** —
   référentiel des acteurs : **noms, couleurs, acteurs ajoutés** — est **persistée** derrière les
   ports de droite par un **adaptateur durable** : elle **survit au redémarrage**. Cette persistance
   est **bornée à la config foyer** : c'est le **premier client** du store durable, tiré **devant
   l'usage** parce qu'il porte un observable direct. La **suppression d'un acteur** (Delete) est
   **livrée** (palier 8) et **autorisée** — pas de refus « si références existantes » (qui
   contredirait l'additivité et le repli neutre). Les **cases orphelines** de l'acteur retiré sont
   **neutralisées par repli** : sa **surcharge cesse de primer** et la case **retombe sur le fond** (le
   cycle reprend, cf. règles 12 et 15) ou sur le **neutre** si l'index n'est ni mappé ni résolu, **sans
   nom fantôme** (règles 15/19) ; si l'acteur supprimé était **mappé au cycle de fond**, son index
   devient **non mappé → neutre** (règles 11/19). **Extension du repli à l'incarnation (livrée —
   palier 8)** : si l'acteur supprimé est **incarné** (identité effective courante, cf. règle 8),
   l'identité effective **retombe automatiquement** sur l'**identité réelle** (bandeau retiré), en
   **temps réel** (diffusion SignalR) — même logique de neutralisation par repli, appliquée à
   l'incarnation. La suppression s'accompagne d'un **accusé non bloquant** (« Acteur supprimé »,
   registre avertissement-à-part, règles 16/28). La suppression est **idempotente** : un identifiant
   absent ou déjà supprimé est un **no-op qui réussit** (jamais un refus). Il n'y a **pas de
   réaffectation automatique** (ce serait une règle neuve, hors périmètre). La suppression a opéré sur
   la config foyer **déjà persistée Mongo** (palier 5) → **acceptation runtime tenue** (store réel :
   l'acteur retiré disparaît du store relu et après redémarrage). *Variantes refus/réaffectation =
   porte **G1 au make-gherkin uniquement** si un vrai trou émerge (ex. interdire la suppression du
   dernier responsable d'un enfant). Les transferts dérivés (règle 24) restent invisibles jusqu'au
   panneau cloche → aucun orphelin de transfert observable séparé ; un transfert ponctuel explicite
   (règle 25) suit la même neutralisation, à scénariser au make-gherkin (pas un pré-arbitrage de cette
   spec).* La persistance du **reste du domaine** reste **en queue** (règle 30, borne anti-cliquet) ;
   l'**état d'incarnation** ne persiste pas (session / mémoire).

7. **Responsabilité de fond en config, exception au calendrier** — La responsabilité récurrente de
   garde (le **cycle de fond** : qui garde par défaut) se déclare dans l'écran de configuration du
   foyer, en même temps que les acteurs ; elle est **éditable** (en mémoire d'abord, durable une fois
   son adaptateur posé — palier « config foyer durable — reste »). Le calendrier ne sert qu'aux
   **surcharges ponctuelles** d'une période donnée, qui **priment** sur le fond. Les dialogs
   d'affectation et de surcharge, comme le **sélecteur de responsable du cycle**, sont alimentés par
   les acteurs du foyer sur leur **identifiant stable**.

## Rôles & accès

8. **Trois types d'acteurs & impersonation bornée lecture (livrée)** — Trois types d'accès : Admin
   (gère tout, y compris la configuration du foyer), Parent (gère les créneaux, lieux, acteurs et
   invitations de son foyer), Autre (consultation et édition limitée à ses propres informations).
   L'affichage s'adapte au type, surfacé en **lecture seule** depuis le seed (cf. Mécaniques). Tant que
   les acteurs ne sont pas des **utilisateurs réels** (authentification au palier « ouverture de
   l'accès », palier 16), une **impersonation bornée lecture seule** est **livrée** (épic É10, **2ᵉ
   tranche du palier 8**, après la tranche suppression) : la **session** distingue une **identité
   réelle** (le configurateur, fixe, type Parent) d'une **identité effective** (l'acteur **incarné**,
   ou **repli sur la réelle**). `Incarner(acteurId)` lit le référentiel — **refus silencieux si
   l'identifiant est absent**, identité réelle conservée — et `RevenirIdentiteReelle()` restaure
   l'état ; un **bandeau « Vous incarnez X »** signale l'incarnation. Le **droit d'écriture
   `EstParent` dérive du type de l'identité EFFECTIVE** (vrai si Parent ou Admin, faux si Autre — cf.
   règle 9), résolu sur l'**identifiant stable** de l'acteur (jamais le libellé, règles 5/19).
   **Bornes dures tenues** : c'est une **convenance d'administration**, **PAS** l'authentification
   complète (ni OAuth, ni landing, ni comptes, ni sessions, ni prise en main par demande, ni droits
   par rôle persistés — tout cela reste au palier 16) ; il n'y a **pas d'écriture « au nom de »** (les
   commandes restent émises sous l'**identité réelle**, canal requête/réponse inchangé — cf. règle
   28) ; **aucune persistance neuve** (état d'incarnation **session / mémoire**, rien ne subsiste au
   redémarrage — règle 30). À la livraison du palier 16, l'impersonation se transforme en **accès réel
   par acteur**. *L'**impersonation écriture « au nom de »** (agir réellement sous l'identité incarnée)
   est **hors-cap** : elle franchit cette borne dure et amorce l'auth réelle — elle exige une
   **décision PO explicite** de changer le cap (cf. Risques).*

9. **Modification réservée aux parents et à l'admin, gating sur l'identité effective (durcissement
   config livré)** — Seul un Parent (ou l'Admin) peut créer, éditer ou supprimer un créneau, une
   période **ou le cycle de fond** ; un acteur « Autre » n'édite que ses propres informations. Depuis
   que l'écriture se fait **en contexte** (palier 7), le **point d'application** de ce droit est le
   **déclencheur unique** — le **menu ouvert au clic sur une case** — rendu **conditionnel sur le rôle
   de l'identité effective** (`@if EstParent`, dérivé de l'identité incarnée, cf. règle 8) : un
   « Autre » incarné (ou un Invité) ne voit pas le menu et n'ouvre aucune dialog, un Parent / Admin
   l'ouvre. Ce gating est **mutualisé** sur le déclencheur quelle que soit l'entrée (slot, période,
   **transfert**). **Durcissement de l'écran de config — livré (palier 8) :** **toutes** les écritures
   de l'écran `ConfigurationFoyer` — **édition d'acteur, ajout d'acteur, édition du cycle de fond,
   suppression d'acteur** — sont désormais **gatées sur l'identité effective** (`@if
   Session.EstParent`), et non plus le seul bouton supprimer : l'**angle mort** d'un Invité / « Autre »
   incarné voyant les écritures config (signalé Sc.7 au palier précédent) est **refermé**. Le gating
   **lit l'identité effective sans recalcul** et ne tire **ni authentification réelle ni écriture « au
   nom de »** (séquencées au palier 16).

## Planning & créneaux

10. **Responsable unique** — Un créneau a toujours un seul responsable (pas de « X ou Y »).

11. **Cycle de fond récurrent, éditable** — *Texte canonique :*
    [`periodes-et-cycle-de-fond.md` § R11](periodes-et-cycle-de-fond.md). Résumé : cycle de **N
    semaines** (N ≥ 1), `index = semaine ISO % N`, chaque index mappé sur un responsable de fond
    résolu sur l'**identifiant stable** ; éditable depuis la config foyer ; **zéro semaine refusé** ;
    ré-édition → grille à jour sans rechargement ; édition concurrente → **dernière écriture gagne** ;
    dialog ouverte n'interfère pas avec le rafraîchissement de fond. Ancre/début explicite, frontière
    de jour, plages, sur-cycles = palier « cycle de fond riche » (palier 10, rouvre l'ancrage ISO).

12. **Exception ponctuelle prime sur le fond** — *Texte canonique :*
    [`periodes-et-cycle-de-fond.md` § R12](periodes-et-cycle-de-fond.md). Résumé : une **période
    saisie prime** sur le fond (surcharge > fond > neutre) ; le cycle reprend ensuite. Une surcharge
    **orpheline** (acteur supprimé, R6) cesse de primer → fond ou neutre (R15).

13. **Lieux éditables** — La liste des lieux (domicile A/B, école, nounou, activité…) est librement
    modifiable et sert de référentiel aux sélecteurs de saisie.

14. **Grille en lecture seule, écriture en dialog contextuelle** — *Texte canonique :*
    [`periodes-et-cycle-de-fond.md` § R14](periodes-et-cycle-de-fond.md). Résumé : la grille consomme
    slots, périodes et **fond résolu** déjà enregistrés et les rend **sans jamais écrire** ; toute
    écriture passe par une **dialog ouverte depuis une case** (seul chemin, tous écrans dédiés
    retirés) ; **annuler** n'émet aucune commande. La **sélection de plage** pour affecter une période
    est une capacité du palier 9. Cf. [`ecriture-en-contexte.md`](ecriture-en-contexte.md).

15. **Suppression de période** — *Texte canonique (livrée s16, + R15bis édition s17) :*
    [`periodes-et-cycle-de-fond.md` § R15](periodes-et-cycle-de-fond.md). Résumé : un Parent / Admin
    supprime une période depuis une dialog contextuelle ; sous la période supprimée, le **fond
    reprend** (responsable de fond, ou neutre si index non mappé, sans nom fantôme). Le même repli
    s'applique à une période **orpheline** (acteur supprimé, R6).

16. **Pose répétée d'un même slot acceptée avec avertissement** — Un slot qui chevauche ou redouble un
    slot existant est **accepté**, accompagné d'un **avertissement** ; il n'est ni refusé ni
    dédoublonné. Côté écriture en contexte, l'issue est donc un **succès** : la dialog **se ferme**, le
    slot **réapparaît**, et l'**avertissement s'affiche à part** (bandeau/toast), **non bloquant**. Cet
    avertissement est **un acquis surfacé** (porté par l'**issue de la commande**, dans le contrat de
    réponse du canal poser-slot) — **jamais** recalculé depuis la grille relue, jamais une règle neuve.
    *L'**accusé « Acteur supprimé »** de la suppression d'acteur (règle 6) et l'**accusé « Transfert
    défini »** (règle 25) relèvent du même registre d'avertissement-à-part. (Une demande
    d'interdiction/dédoublonnage est en attente comme révision de règle — cf. Risques & questions
    ouvertes.)*

17. **Date de saisie par défaut = aujourd'hui, ancrage de contexte prioritaire** — Les formulaires de
    saisie (poser un slot, affecter ou surcharger une période, **définir un transfert**) pré-remplissent
    leur date sur la **date de référence « aujourd'hui »** **uniquement hors-contexte** ; **en
    contexte** (saisie ouverte depuis une case), la **date de la case cliquée prime** sur ce défaut.
    Dans les deux cas la date tombe **dans la fenêtre affichée** et la saisie **réapparaît
    immédiatement** dans la grille. Une date par défaut figée hors fenêtre est une non-conformité à
    corriger, pas un comportement attendu. *Garde-fou de mise en œuvre : tant que **toute** saisie
    passe par une case, le pré-remplissage est **exclusivement** la date de contexte et le **repli
    horloge devient du code mort** ; ce repli **ne doit pas être supprimé du port d'horloge** (la
    grille s'en sert pour « aujourd'hui »/fenêtre) et **doit être réintroduit dans la dialog** si un
    point d'entrée de saisie **hors-contexte** réapparaît à un futur palier.*

## Code couleur & lisibilité

18. **Responsabilité lisible : couleur par personne + nom + légende** — La responsabilité de garde se
    lit dans la grille par une couleur qui distingue chaque **personne** (Parent A ≠ Parent B), pas
    seulement son type de rôle ; cette couleur est **doublée du nom du responsable affiché dans la case
    et d'une légende**, car la teinte seule ne suffit pas à dire qui garde. La **légende** agrège les
    responsables présents dans la fenêtre **y compris les responsables de fond** issus du cycle,
    dédoublonnés par identifiant ; un acteur **supprimé** quitte aussitôt la légende (plus de pastille
    ni de nom fantôme). Un **nom trop long** est **tronqué** dans la case tout en restant lisible **en
    entier au survol** ; un acteur **hors du set de couleurs connu** reste **affiché et distingué**
    (teinte neutre assumée) **sans perdre son nom**.

19. **Couleur résolue sur un identifiant d'acteur stable** — La couleur d'un responsable se résout sur
    l'**identifiant stable** de l'acteur, jamais sur son libellé d'affichage. Les sélecteurs de saisie
    (affectation, surcharge, **transfert**) **et le mapping du cycle de fond** **fournissent ce même
    identifiant** que la palette ; un acteur **ajouté** reçoit un identifiant stable neuf résolu de la
    même façon, et l'**identité effective** (incarnation, règle 8) se résout sur ce même identifiant.
    Un libellé qui ne correspond pas à un identifiant connu fait retomber la case sur la **couleur
    neutre** ; de même, un **index de cycle non mappé** (y compris après **suppression** de l'acteur
    qui y était mappé) = pas de fond → teinte neutre, **sans nom fantôme**. Une case grise là où un
    responsable est affecté trahit un libellé fourni à la place de l'identifiant — c'est le défaut à
    localiser, pas la résolution elle-même.

20. **Set de couleurs par défaut, recoloriable** — Un jeu de couleurs par défaut, déclaré avec le
    foyer, différencie d'emblée les responsables tant qu'aucune personnalisation n'a été faite ; c'est
    ce set qui s'applique avant l'ouverture de l'accès. Ce set est **recoloriable** depuis l'écran de
    config et la grille suit ; la **personnalisation par utilisateur authentifié** (règle 21) reste un
    pas distinct lié à l'ouverture de l'accès.

21. **Personnalisation des couleurs par utilisateur** — Une fois identifié, chaque acteur peut
    personnaliser les couleurs qu'il assigne aux acteurs qu'il voit ; cette personnalisation dépend de
    l'authentification (séquence, ouverture de l'accès) et n'altère pas le set par défaut vu par les
    autres.

22. **Thème en accord avec le domaine** — L'app porte un **thème** cohérent avec son domaine (garde
    d'enfants), au service de la lisibilité d'usage. C'est une **ergonomie de surface subordonnée à
    l'usage** par l'arbitre : il n'ouvre aucune règle métier. Un **thème sombre avec bascule
    clair/sombre** (et persistance de la préférence) en est une **évolution additive**, consignée et
    non priorisée, à rattacher au futur écran de préférences utilisateur. L'**harmonisation de teinte**
    entre la pastille de légende et le fond de case (cf. Risques) relève du même registre d'ergonomie
    de surface, pas d'une règle métier.

## Survol & détail de la case

23. **Survol : du nom complet au résumé de la journée** — Au survol d'une case, l'app expose un
    complément d'information sans quitter la grille. Le **survol simple** affiche le **nom complet** du
    responsable (utile quand le nom est tronqué) : c'est le comportement **livré et conforme**. Un
    **survol enrichi** est une **évolution** prévue (séquencée, skippée tant que le PO ne la réclame
    pas) : après un survol prolongé (~1 s), afficher un **résumé de la journée**. Le périmètre de ce
    résumé (périodes, slots, responsable, transferts) est à cadrer au moment de le scénariser ; ce
    n'est pas un correctif du survol simple, qui n'est pas défaillant.

## Transferts

24. **Transfert dérivé par défaut** — Le transfert d'un enfant (qui dépose, qui récupère, à quelle
    heure) est dérivé automatiquement du planning par défaut : c'est le cas nominal, sans saisie.

25. **Transfert modifiable et ponctuel, saisi en contexte** — Un transfert auto-calculé peut être
    modifié, et des transferts ponctuels peuvent être proposés ou ajoutés à l'exception (urgence,
    changement d'emploi du temps). La saisie d'un transfert se fait, comme le slot et la période, **en
    contexte** : la **3ᵉ dialog « Définir un transfert »** ouverte depuis une case (pré-remplie sur la
    date de la case) est **livrée**, et l'ancienne page de saisie dédiée a été **retirée** (épic
    « écriture en contexte » refermé). Au **succès**, un **accusé « Transfert défini »** s'affiche **à
    part, non bloquant** (registre avertissement, règle 16) ; au **refus domaine ou API injoignable**,
    la dialog **reste ouverte** avec message et saisie conservée (règle 28). Elle réutilise la
    **commande/handler `DefinirTransfert`** et le **canal HTTP** déjà livrés (**aucun handler neuf**) ;
    le transfert **reste InMemory** (règle 30, borne anti-cliquet).

## Modifications & notifications

26. **Modification directe** — Un Parent applique son changement immédiatement ; les autres acteurs
    concernés sont notifiés (pas de workflow de validation). (Une demande de workflow demande/accord
    avant réaffectation d'une période à l'autre parent est en attente comme révision de règle,
    rattachée au palier « imprévu & échange » — cf. Risques & questions ouvertes.)

27. **Notifications & événements à venir** — Les notifications in-app couvrent les changements de
    planning et les rappels de transfert ; les transferts et changements à venir sont consultables dans
    un panneau d'événements accessible via une cloche.

## Accès aux données & exploitation

28. **Écriture par le canal, échec clair si l'API est injoignable** — Toute écriture passe par le
    **canal requête/réponse** vers l'API distante ; aucune vue n'écrit le domaine en direct, et
    **l'écriture est toujours émise sous l'identité réelle** (l'impersonation, règle 8, ne touche
    **pas** ce canal : pas d'écriture « au nom de »). La **réponse du canal porte l'issue de sa propre
    écriture** (succès, et le cas échéant un avertissement de chevauchement — cf. règle 16 ; surface
    autorisée = le **contrat de réponse** du canal, **sans** recalcul métier ni nouvel endpoint de
    lecture). Si l'API est **injoignable**, ou si le domaine **refuse** la commande, l'échec est
    **clair** : message à l'écran, saisie **non appliquée** et **conservée** à resoumettre — en
    contexte, la **dialog reste ouverte**, le message s'affiche **dans la dialog**, la **grille reste
    inchangée**, **sans mise en file ni rejeu** à ce stade. Cette issue d'échec vaut pour les **trois
    dialogs** (slot, période, transfert) **et pour la suppression d'acteur** (API injoignable →
    suppression non appliquée, acteur toujours listé, store réel inchangé, aucune fausse
    confirmation). Le hors-ligne rejouable (cache + file d'écritures) est un palier technique
    ultérieur, derrière l'usage.

29. **API explorable et origine du front autorisée** — L'API expose un **document de description**
    (OpenAPI) **et** une **UI interactive** d'exploration des endpoints (type Swagger-UI / Scalar), et
    autorise l'**origine du front** (CORS). C'est un garde-fou d'outillage, sans observable métier :
    aucune règle de gestion n'en dépend.

30. **Données derrière les ports, durables — exception bornée pour la config foyer** — Les slots,
    périodes, transferts, **cycle de fond** et la configuration du foyer vivent derrière des **ports** ;
    la donnée du foyer n'est jamais une constante figée dans le code. Leur stockage est durable **à
    terme**, sans toucher au domaine ni aux règles. **Exception bornée actée et livrée** : la **config
    foyer** (référentiel des acteurs — noms, couleurs, acteurs ajoutés) est **persistée maintenant**,
    **devant l'usage**, parce qu'elle porte un observable direct (survie au redémarrage) et qu'elle est
    le **premier client** du store durable. Le **reste du domaine** (slots, périodes, transferts **et
    le cycle de fond**) reste **en mémoire** (adaptateur InMemory), sa durabilité **séquencée derrière
    l'usage** : la **borne anti-cliquet** empêche que cette exception entraîne le reste devant l'usage.
    L'**écriture en contexte** (palier 7, **transfert compris**) l'a confirmée : déplacer la saisie en
    dialogs n'a tiré **aucune** persistance (slots / périodes / **transferts** restent InMemory). La
    **suppression d'acteur** (palier 8) a opéré sur la config foyer **déjà durable** — elle a
    **exercé** une persistance acquise, **sans créer de cliquet**. L'**impersonation bornée lecture**
    (palier 8) n'a tiré **aucune persistance neuve** : l'**état d'incarnation** vit en **session /
    mémoire**, rien ne subsiste au redémarrage. Cette règle porte la **durabilité** ; elle reste
    **distincte de l'édition** (règle 5) — rendre une donnée éditable (cf. le cycle de fond, éditable
    mais volatil) n'oblige pas à la rendre durable dans le même incrément, sauf quand, comme pour la
    config foyer, la durabilité porte un observable direct et reste bornée.

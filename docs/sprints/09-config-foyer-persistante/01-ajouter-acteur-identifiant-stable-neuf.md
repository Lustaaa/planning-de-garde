# Sc.1 — Ajouter la nounou au foyer génère un identifiant stable neuf

`@nominal` `🖥️ IHM`

↩ Retour : [00-sprint09-suivi.md](00-sprint09-suivi.md)

**Routage** : **scénario fondation** de l'ajout. Tranche **backend** (`tdd-auto`, 3 drivers :
handler `AjouterActeur` qui génère un id neuf résolvable, id **opaque** non dérivé du libellé,
**énumération** des acteurs incluant l'ajouté) **+** acceptation **runtime IHM** (`ihm-builder` :
Carla **apparaît immédiatement dans la liste** de l'écran de configuration, énumérée **depuis le
store durable**, jamais la liste statique front).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Le symptôme PO est un fait d'usage **runtime** : depuis l'écran de config, j'ajoute « Carla »
> (rose) et je valide ; **sans recharger**, l'écran de config **réellement câblé** (front WASM +
> API distante + store durable réel) **liste Carla** parmi les acteurs. **Pas** un test bUnit à
> doublure (qui ne prouve ni la DI réelle, ni le chemin HTTP d'écriture, ni l'énumération depuis
> le store). Si l'écran énumère encore la **liste statique front** (`Foyer.ActeursEditables`),
> Carla n'apparaît pas → rouge.

`Should_Afficher_Carla_dans_la_liste_des_acteurs_de_l_ecran_de_configuration_sans_recharger_la_page_When_un_parent_ajoute_l_actrice_Carla_en_rose_depuis_l_ecran_de_configuration` — ⏳ Pending *(runtime, routé `ihm-builder`)*

> **Acceptation backend (boucle externe à la frontière Application, menée par `tdd-auto`)** —
> filet sociable traduisant le Gherkin sans IHM : via le handler `AjouterActeur` sur le **store
> réel** (`ConfigurationFoyerEnMemoire` seedé depuis `Foyer`), Carla ajoutée en rose est ensuite
> **énumérée** depuis le store, résolue par son nom **et** sa couleur sur un identifiant **neuf**,
> distinct des seeds et **non dérivé** du libellé.
> `Acceptation_Should_Enumerer_Carla_resolue_par_nom_et_couleur_sur_un_identifiant_neuf_distinct_des_seeds_et_non_derive_du_libelle_When_un_parent_ajoute_Carla_en_rose` — ✅ GREEN

- **Niveau** : E2E/runtime sur l'app câblée (écran de config réel énumérant le **store durable**,
  ajout émis via le **canal HTTP** `POST /api/canal/ajouter-acteur`).
- **Observable** : après validation, sans rechargement, la liste de l'écran de config contient
  « Carla » portée par un **identifiant neuf** (distinct des seeds, non égal à « Carla »).

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Faire_exister_l_acteur_ajoute_resolu_par_son_nom_et_sa_couleur_sur_un_identifiant_neuf_When_un_parent_ajoute_une_actrice_avec_un_nom_et_une_couleur` | (rien) → commande orchestrée qui crée | **Driver** — il n'existe **aucune** commande/handler `AjouterActeur` : le store ne sait que **renommer/recolorier** des seeds existants. Force l'orchestration : un ajout génère un identifiant et persiste le nom (+ couleur fournie) via le port d'écriture, de sorte que `NomDe(idNeuf)` = « Carla » et `CouleurDe(idNeuf)` = rose. | ✅ GREEN |
| 2 | `Should_Porter_un_identifiant_opaque_distinct_du_libelle_et_des_acteurs_deja_presents_When_une_actrice_est_ajoutee_au_foyer` | valeur figée → identité générée unique | **Driver** — l'impl minimale du #1 prend le **raccourci `id = nom`** (libellé-comme-identité, anti-pattern corrigé au s06) : l'id serait « Carla ». Force un identifiant **opaque généré** (GUID ou séquence « autre-N »), **≠ libellé** et **≠** les ids des seeds (`parent-a`, `parent-b`, `grand-pere`). *(⚠️ l'unicité vs-existants est mécaniquement satisfaite si GUID — driver réel si séquence ; assertion cohésive de ce test.)* | ✅ GREEN |
| 3 | `Should_Restituer_l_acteur_ajoute_parmi_les_acteurs_enumeres_du_foyer_When_l_ecran_de_configuration_enumere_les_acteurs_depuis_le_store` | lecture figée → énumération du store | **Driver** — aucun **accès de lecture d'énumération** n'existe : l'écran liste une **liste statique front** (`Foyer.ActeursEditables`) qui ignore les ajouts. Force un accès d'énumération **sur le store** restituant les acteurs **dont l'ajouté** (sur son id neuf). | ✅ GREEN |

> **Caractérisations anticipées (non listées, ⚠️ early green attendu chez `tdd-auto`)** :
> *(a)* la **diffusion temps réel sur ajout abouti** (`INotificateurPlanning`, Spy) suit le
> patron éprouvé de `EditerActeurHandler` (notifier après mutation réussie) — si codée d'emblée
> elle est verte sans rouge ; *(b)* la **case + légende qui suivent** est une re-projection de
> `GrilleAgendaQuery` **inchangé** — prouvée au runtime (et au Sc.4), pas de rouge backend.

## Fichiers à créer / modifier (backend uniquement ici)

- **`AjouterActeurCommand` + `AjouterActeurHandler` (Application, NEUF)** — `{ nom, couleur? }` ;
  génère l'**identifiant stable neuf opaque** (forme exacte laissée à `tdd-auto`), persiste nom
  (+ couleur) via le port d'écriture, déclenche `INotificateurPlanning` sur succès, renvoie
  `Result<…>` (convention de refus existante, exercée au Sc.8).
- **Port d'écriture d'ajout + accès d'énumération (Application)** — surface d'**ajout** (créer un
  acteur sur un id neuf) et de **lecture d'énumération** des acteurs du foyer, consommées par le
  handler / l'écran de config ; doublures `Fake` côté tests. *(Forme exacte — extension de
  `IEditeurConfigurationFoyer` ou port dédié — laissée à `tdd-auto`.)*
- **Store réalisant l'ajout + l'énumération (Infrastructure)** — `ConfigurationFoyerEnMemoire`
  (et l'adaptateur durable Mongo, Sc.3) gagnent l'opération d'ajout sur id neuf et l'énumération.
- **Doublures tests** — étendre `FakeConfigurationFoyer` (ajout + énumération) ; réutiliser
  `FakeNotificateurPlanning` (Spy déjà présent).
- **Volet runtime IHM (routé `ihm-builder`, hors backend)** — endpoint `POST
  /api/canal/ajouter-acteur` (Api `CanalEcriture`) ; DTO `AjouterActeurRequete` (Web) ; écran
  `ConfigurationFoyer.razor` énumérant le **store durable** (remplace `Foyer.ActeursEditables`) ;
  diffusion SignalR sur ajout abouti.

## Design notes

- **Id stable neuf opaque, jamais le libellé** (invariant cardinal, anti-pattern s06). L'id est
  **généré** et **unique** (jamais un id existant). La couleur résolue se fait sur cet id, comme
  pour les seeds (règle 19).
- **Couleur fournie persistée ; absente → repli neutre** par le contrat `IPaletteCouleurs.CouleurDe`
  (clé absente → `CouleurNeutre`). L'ajout sans couleur (Sc.5) ne **n'enregistre rien** côté
  couleur — le neutre tombe par contrat, pas par un calcul neuf.
- **Énumération depuis le store, pas la liste front.** Le risque spec : l'écran énumère
  aujourd'hui `Foyer.ActeursEditables` (statique) — un ajout n'y apparaîtrait pas. L'accès
  d'énumération relit **le store** (en mémoire, puis durable Mongo au Sc.3).
- **Ports de lecture inchangés** (`IReferentielResponsables` / `IPaletteCouleurs`) : seule leur
  **réalisation** gagne l'acteur ajouté ; `GrilleAgendaQuery` n'est **pas** touché (CQRS, règle 12).
- **Diffusion = effet de l'écriture aboutie** (Spy backend ; suivi live réel prouvé au runtime).

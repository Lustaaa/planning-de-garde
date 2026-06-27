# Suivi Sprint 10 — Récurrence des périodes · définir le cycle de fond

> **Cadrage scaffolding (décidé CP — couche de résolution « fond » sous les périodes, cycle EN
> MÉMOIRE).** Le sujet ajoute une **couche de résolution du responsable de fond** sous les
> périodes explicites : `index = ISOWeek(jour) mod N`, **alternance hebdo paire/impaire** (1
> responsable de fond par semaine). Priorité de résolution **surcharge (période saisie) > fond
> (cycle) > neutre**. Le domaine et la convention de refus (`Result`) **existent déjà** ; le CQRS
> de lecture (`GrilleAgendaQuery`) est **étendu**, pas réécrit.
> - **Fonction pure de parité ISO (Domain, NEUF)** — `index = System.Globalization.ISOWeek.GetWeekOfYear(date) mod N`,
>   sans framework, déterministe (testée sur ISO paire ET impaire via la date injectée à
>   `Projeter`, jamais `Now`). Invariant **N ≥ 1** (gardé par le handler d'écriture, **pas** la
>   projection). Forme exacte (value object `CycleDeFond` portant N + mapping + méthode pure, ou
>   fonction libre) laissée à `tdd-auto`.
> - **Port cycle `IReferentielCycleDeFond` (Application, NEUF)** — lecture du cycle courant + surface
>   d'écriture (`DefinirCycle`). Contrat : `CycleDeFond { int NombreSemaines (N ≥ 1) ; mapping index
>   0..N-1 → responsableId (identifiant stable, jamais le libellé ; index non mappé = pas de fond) }`.
>   Repli **index non mappé → pas de fond → neutre** mirroir du contrat `IPaletteCouleurs.CouleurDe`
>   (clé absente → `CouleurNeutre`) et `IReferentielResponsables.NomDe`.
> - **`DefinirCycleHandler` (Application, NEUF)** — `DefinirCycleCommand(nombreSemaines, affectations:
>   index→responsableId)` → `Result` ; **garde N ≥ 1 CONDITIONNELLE** (le nominal « définir un cycle
>   de 2 semaines » est exercé dès Sc.1 → un refus inconditionnel régresserait ce nominal). Sur refus :
>   aucune écriture (le cycle précédent reste inchangé). Diffusion temps réel sur succès (Spy backend).
> - **`GrilleAgendaQuery` ÉTENDU (Application)** — injecte le port cycle ; dans la branche **`periode
>   is null`** de `CaseJourAu`, résout le responsable de fond `mapping[ISOWeek(jour) mod N]` (au lieu du
>   neutre/nom vide actuel) — nom + couleur sur l'**identifiant stable**. La **légende** est étendue aux
>   responsables de fond présents dans la fenêtre (`LegendeDesPresents` n'agrège aujourd'hui que les
>   périodes). La **priorité surcharge > fond est STRUCTURELLE** : la branche `else période` reste
>   intacte → une période explicite prime sans code neuf (cf. Sc.2, early green).
> - **Adaptateur InMemory singleton (Infrastructure, NEUF)** réalisant le port cycle — **PAS Mongo**
>   (cycle volatile ici, **borne anti-cliquet règle 30** : durabilité portée par le palier 9). Câblage
>   `ServiceCollectionExtensions`.
> - **Read model / ports acteurs INCHANGÉS** — `IReferentielResponsables` / `IPaletteCouleurs` :
>   résolution nom/couleur sur l'id stable (jamais le libellé, règle 19) ; légende dédoublonnée par id.
>
> **Routage backend (`tdd-auto`) vs IHM/runtime (`ihm-builder`) — axe explicite.**
> - **Drivers backend réels** (`tdd-auto`, frontière Application, port cycle doublé en mémoire) :
>   **Sc.1** (résolution du fond par parité ISO : 3 drivers — case index impair, case index pair
>   forçant le calcul ISO réel, présence en légende), **Sc.7** (garde N ≥ 1 conditionnelle).
> - **Caractérisations backend** (filet anti-régression, ⚠️ early green **attendu**, **pas** driver —
>   composent du code déjà vert) : **Sc.2** (surcharge > fond — **structurelle**, branche `periode is
>   null` first → surcharge intacte ; garde-fou non-régression des périodes explicites), **Sc.3**
>   (re-projection après ré-définition du mapping), **Sc.4** (index non mappé → neutre — **contrat de
>   repli**, comme `CouleurDe` ; driver SEULEMENT si la résolution indexe en dur), **Sc.5** (N=1 → `mod 1
>   = 0` → même responsable partout), **Sc.6** (dernière écriture gagne par construction du singleton —
>   patron s08 Sc.7), **Sc.7 #2** (cycle précédent inchangé sur refus).
> - **Drivers IHM/runtime** (`ihm-builder`, app réellement câblée — DI réelle, front WASM + API
>   distante + SignalR) : **Sc.1** (grille affiche le fond, case + légende, sans saisie de période),
>   **Sc.3** (inversion du mapping → grille suit **sans rechargement** — SignalR), **Sc.6** (deux écrans
>   → **convergence sans rechargement** — SignalR), **Sc.7** (message « le cycle doit compter au moins
>   une semaine » à l'écran), **Sc.8** (**service injoignable** : échec clair, saisie conservée, aucun
>   cycle enregistré — patron transport s09 Sc.9, **backend néant**).
>
> **Note IHM hors périmètre backend.** Aucun `.razor` ni câblage SignalR réel dans les « Fichiers à
> créer » des scénarios **backend purs** ; la diffusion temps réel se vérifie en backend par un **Spy**
> sur `INotificateurPlanning`. Rendu / interactivité / convergence sans rechargement relèvent
> d'`ihm-builder` sur l'app câblée.
>
> **Divergence assumée vs analyse `/2`** : l'analyse technique annonçait **Sc.2 comme driver**.
> L'inspection de `GrilleAgendaQuery.CaseJourAu` (`periode is null ? … : période`) montre que la
> **priorité surcharge > fond est structurelle** — la branche `else période` reste intacte quand le
> fond est ajouté dans la branche `periode is null`. **Sc.2 est donc une caractérisation** (garde-fou
> non-régression), pas un driver. **Drivers réels = 4** (Sc.1 ×3 + Sc.7 ×1), pas 3+Sc.2.

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [Définir un cycle de 2 semaines : le fond alterne par parité ISO](01-definir-cycle-2-semaines-alternance-iso.md) | `@nominal` 🖥️ IHM · backend `tdd-auto` (3 drivers) + runtime `ihm-builder` | ✅ GREEN backend (frontière Application : définir un cycle 2 sem. → ISO 27 impaire = Parent B orange, ISO 28 paire = Parent A bleu, case + légende) · ⏳ runtime `ihm-builder` (grille câblée affiche le fond sans saisie de période) | 4/4 | ✅ GREEN (backend) |
| 2 | [Une surcharge ponctuelle prime sur le fond puis le cycle reprend](02-surcharge-prime-puis-cycle-reprend.md) | `@nominal` 🖥️ IHM · caractérisation `tdd-auto` (non-régression) + runtime `ihm-builder` | ✅ GREEN backend (caractérisation — priorité structurelle `periode is null` first : surcharge 8/07 = Parent B/orange, fond 7 & 9/07 = Parent A/bleu) · ⏳ runtime `ihm-builder` | 1/1 | ✅ GREEN (backend) |
| 3 | [Inverser le mapping du cycle met à jour la grille sans rechargement](03-inverser-mapping-met-a-jour-grille.md) | `@nominal` 🖥️ IHM · caractérisation `tdd-auto` + runtime driver `ihm-builder` | ✅ GREEN backend (caractérisation — re-projection après ré-définition : cycle pair→A bleu sur ISO 28 puis inversion pair→B → ISO 28 affiche Parent B orange, case + légende ; le store écrase, la grille relit) · ⏳ runtime `ihm-builder` (grille suit **sans rechargement**, SignalR) | 1/1 | ✅ GREEN (backend) |
| 4 | [Un index de cycle sans responsable retombe sur la teinte neutre](04-index-sans-responsable-teinte-neutre.md) | `@limite` · DRIVER `tdd-auto` (repli neutre — indexation en dur) + runtime `ihm-builder` | ✅ GREEN backend (DRIVER, PAS early green : index non mappé levait `KeyNotFoundException` → repli neutre émergé `TryGetValue → null`, priorité fond > neutre ; ISO 27 index impair non affecté = gris sans nom ni légende fantôme, ISO 28 contrôle positif Parent A bleu) · ⏳ runtime `ihm-builder` | 1/1 | ✅ GREEN (backend) |
| 5 | [Cycle d'une seule semaine : aucune alternance, même responsable partout](05-cycle-une-semaine-sans-alternance.md) | `@limite` · caractérisation `tdd-auto` + runtime `ihm-builder` | ⏳ backend (⚠️ early green — N=1 → `mod 1 = 0` → Parent A sur ISO 27→31, légende = Parent A seul) · ⏳ runtime `ihm-builder` | 0/1 | ⏳ Pending |
| 6 | [Deux parents éditent le cycle en même temps : dernière écriture gagne](06-deux-parents-editent-derniere-ecriture-gagne.md) | `@limite` 🖥️ IHM · caractérisation `tdd-auto` + runtime driver `ihm-builder` | ⏳ backend (⚠️ early green — dernière écriture gagne par construction du singleton, patron s08 Sc.7) · ⏳ runtime `ihm-builder` (convergence des deux grilles vers Parent C vert **sans rechargement**, SignalR) | 0/1 | ⏳ Pending |
| 7 | [Définir un cycle de zéro semaine est refusé](07-cycle-zero-semaine-refuse.md) | `@erreur` 🖥️ IHM · backend `tdd-auto` (1 driver + 1 caract.) + runtime `ihm-builder` | ⏳ backend (garde N ≥ 1 conditionnelle : refus motif clair, cycle précédent inchangé) · ⏳ runtime `ihm-builder` (message « le cycle doit compter au moins une semaine » à l'écran) | 0/2 | ⏳ Pending |
| 8 | [Édition du cycle impossible si le service de configuration est injoignable](08-service-injoignable-edition-impossible.md) | `@erreur` 🖥️ IHM · driver runtime `ihm-builder` (backend néant) | ⏳ runtime `ihm-builder` (échec clair, saisie du cycle conservée à resoumettre, aucun cycle enregistré ; canal d'écriture `catch (HttpRequestException)` — patron s09 Sc.9) | runtime | ⏳ Pending |

**Total** : 8 scénarios · **11 tests unitaires backend** (4 drivers réels : Sc.1 ×3 résolution du fond
par parité ISO + Sc.7 ×1 garde N ≥ 1 ; 7 caractérisations early-green : Sc.2, Sc.3, Sc.4, Sc.5, Sc.6,
Sc.7 #2 + l'alternance-se-poursuit de Sc.1 #4). **Sc.8 = 0 backend** (100 % runtime échec transport).

**Acceptation** : runtime IHM Sc.1/3/6/7/8 sur l'app réellement câblée (front WASM + API distante +
SignalR), grille affichant le **fond résolu** (case nommée + colorée sur l'identifiant stable) sans
saisie de période, convergence **sans rechargement** sur édition concurrente. La preuve d'un bug runtime
n'est **jamais** un test bUnit à doublure (render mode, DI réelle, SignalR).

**Statuts** : ⏳ Pending · 🔴 Red · ✅ Green.

**Légende routage** : `tdd-auto` = cycles unitaires backend (résolution du fond dans `GrilleAgendaQuery`,
garde N ≥ 1 du handler, diffusion par Spy — port cycle doublé en mémoire) ; `ihm-builder` = acceptation
runtime/E2E sur l'app câblée (grille affichant le fond, édition du mapping depuis l'écran config,
convergence SignalR, messages d'échec). Un scénario `🖥️` n'est **jamais** prouvé par bUnit seul.

**Borne anti-cliquet (règle 30)** : le cycle de fond vit **EN MÉMOIRE** (adaptateur InMemory singleton).
**PAS de Mongo** ici — sa durabilité est portée par le palier 9. Slots / périodes / transferts restent
InMemory.

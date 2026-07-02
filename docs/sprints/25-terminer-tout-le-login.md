# Sprint 25 — `terminer-tout-le-login`

> **Sprint goal (G2, tranché PO via « Autre »)** : **« Terminer TOUT le login »**, d'un seul tenant,
> OAuth externe et envoi mail inclus. Le PO **assume explicitement le risque de preuve** pour les
> volets non testables en runtime local (OAuth providers réels, envoi mail).
>
> **Assise livrée (ne pas redévelopper)** : `CompteUtilisateur` (id stable, email, statut Actif/Inactif,
> `ActeurId` 1-1) persisté Mongo (s22) · `AdministrationFoyer` admin=Parent (s22) · connexion locale
> email-only `SeConnecterCommand`/`SessionOuverte` + logout + `ResoudreActeurParDefautQuery` (s23) ·
> activation `Inactif→Actif` (s24) · page `/connexion` landing + `MenuUtilisateur` + état connexion dans
> `SessionPlanning` (s24). Ports `IEnumerationComptes`/`IEditeurComptes` (InMemory + Mongo).

## ⚠️ Tension méthodo ACTÉE — stratégie de preuve des volets non testables en runtime local

Le rempart d'acceptation runtime (« prouver sur câblage réel, pas par doublures ») **ne peut pas
s'appliquer intégralement** aux volets **OAuth externe** (secrets, callbacks, redirections vers
Google/Microsoft/Apple réels) ni à l'**envoi de mail** (SMTP / provider mail réel). C'est une **entorse
assumée par le PO au gate G2**.

**Stratégie de preuve retenue pour ces volets (Sc.11→Sc.16)** :
- **Doublure de bout d'adaptateur au niveau du PORT** : définir un **port de droite** par capacité
  (`IEnvoiMail`, `IFournisseurOAuth`) et prouver toute la logique Application/frontière (résolution du
  compte, ouverture de session, garde d'échec) contre une **doublure du port** — logique **réellement
  verte** au niveau frontière, **pas** un faux « runtime vert » de bout en bout.
- **Marquage explicite** : chaque scénario OAuth/mail porte le tag `@preuve-doublure` et la mention
  **« preuve par doublure d'adaptateur ; câblage réel (provider/SMTP) vérifié manuellement »**. Le
  tableau d'avancement distingue ces lignes (colonne Preuve = `doublure+manuel`).
- **Preuve manuelle documentée** : le câblage réel (adaptateur concret branché sur un provider/SMTP de
  test) est vérifié **à la main** au gate G3 et **consigné** ; on **ne prétend jamais** que le vert de
  suite couvre le provider réel.

**Volets 1-3 (protection routes, bug rôle, libre-service + mot de passe local) restent prouvables
runtime normalement** (`@ihm`/`@back` classiques, câblage réel + store Mongo).

**⚠️ Risque flake connu (non traité ce sprint, choix PO)** : OAuth/mail ajoutent des chemins d'auth et
des clients SignalR ; le flake P1 `*TempsReel*` (déjà jusqu'à 6 simultanés, suite exige souvent un 2ᵉ
run) **empirera probablement**. La dev-team applique la **règle flake durcie** (tout rouge `*TempsReel*`
re-testé en isolation ×2-3 → vert → classé flake, jamais investigué `src/`). Le **rétrofit flake reste
au backlog** (non pris ce sprint).

**⚠️ Débordement probable > 1 sprint d'1h** : le périmètre couvre 5 volets. Scénarios **séquencés** :
volets 1-3 (runtime) d'abord (Sc.1→Sc.10), puis 4-5 (OAuth/mail, doublure) ensuite (Sc.11→Sc.16). Le
PO garde la main pour **scinder** (tranche 25a runtime / 25b OAuth+mail) si la vélocité l'impose.

---

## Avancement — 15/16

| # | Scénario | Type | Preuve | Statut |
|--:|----------|:----:|:------:|:------:|
| 1 | Route protégée : non connecté → redirection `/connexion` | 🖥️ IHM | runtime | ✅ |
| 2 | Route protégée : connecté → accès rétabli | 🖥️ IHM | runtime | ✅ |
| 3 | `/connexion` reste librement accessible (pas de boucle de redirection) | 🖥️ IHM | runtime | ✅ |
| 4 | Déconnexion → re-verrouillage immédiat des routes | 🖥️ IHM | runtime | ✅ |
| 5 | Bug rôle : connexion « Mamie » (type Autre) → identité effective ET rôle = Mamie | @back | runtime | ✅ |
| 6 | Bug rôle : non-régression impersonation bornée s14 au-dessus de la connexion | @back | runtime | ✅ |
| 7 | Login email + **mot de passe** : bon couple → session ouverte | @back | runtime | ✅ |
| 8 | Login email + mot de passe : mauvais mot de passe → refus, aucune session, motif clair | @back | runtime | ✅ |
| 9 | **Création de compte libre-service** : email neuf + mot de passe → compte créé (Inactif) | @back | runtime | ✅ |
| 10 | Libre-service : email déjà porteur → rejet sans écriture, motif clair | @back | runtime | ✅ |
| 11 | **Récupération mot de passe** : demande sur email connu → jeton + mail émis (port) | @back | doublure+manuel | ✅ |
| 12 | Récupération : email inconnu → réponse neutre, aucun mail, aucune fuite | @back | doublure+manuel | ✅ |
| 13 | Récupération : jeton valide → mot de passe redéfini ; jeton consommé | @back | runtime | ✅ |
| 14 | **OAuth** : callback provider, compte lié Actif → `SessionOuverte` | @back | doublure+manuel | ✅ |
| 15 | OAuth : identité provider inconnue / compte Inactif → refus, aucune session | @back | doublure+manuel | ✅ |
| 16 | **IHM login** : bouton « Se connecter avec Google/Microsoft/Apple » sur `/connexion` | 🖥️ IHM | doublure+manuel | ⏳ |

---

# Scénarios

## Volet 1 — Protection d'accès aux routes (retour PO s24) — @ihm runtime

```gherkin
@ihm @vert
Scénario 1 — Route protégée sans session → redirection vers /connexion
  Étant donné une application démarrée sans session ouverte (aucun compte connecté)
  Quand je navigue directement vers une route protégée (ex. "/planning" ou config foyer)
  Alors je suis redirigé vers "/connexion"
  Et aucun contenu de la route protégée n'est rendu (pas de fuite d'un flash de grille)

@ihm @vert
Scénario 2 — Route protégée avec session → accès rétabli
  Étant donné un compte Actif connecté (SessionOuverte via SeConnecterCommand s23)
  Quand je navigue vers une route protégée
  Alors la route s'affiche normalement
  Et la navigation entre routes protégées ne redéclenche aucune redirection

@ihm @vert
Scénario 3 — /connexion librement accessible (pas de boucle de redirection)
  Étant donné une application sans session ouverte
  Quand je navigue vers "/connexion"
  Alors la page de connexion s'affiche (aucune redirection, aucune boucle)
  Et "/" redirige vers "/connexion" (landing s24 préservée)

@ihm @vert
Scénario 4 — Déconnexion → re-verrouillage immédiat
  Étant donné un compte connecté consultant une route protégée
  Quand je me déconnecte (MenuUtilisateur s24 → destruction de session s23)
  Alors la session est détruite (EstConnecte = faux, aucune identité résiduelle)
  Et un accès ultérieur à une route protégée redirige de nouveau vers "/connexion"
```

## Volet 2 — Bug rôle ≠ acteur du compte connecté (retour PO s24, CORRECTION) — @back runtime

> **Cause identifiée (assise s24)** : `SessionPlanning.Connecter(nom, acteurId)` incarne l'acteur du
> compte, mais `EstParent`/le rôle affiché dérivent de `IdentiteEffective.Type` **composé avec le
> sélecteur de rôle démo `Role`** (Parent par défaut) et l'`IdentiteReelle` reste **codée en dur**
> `("configurateur", …, TypeActeur.Parent)`. Connexion « Mamie » (type Autre) → le rôle/gating peut
> rester « Parent ». Le fix ancre l'**identité réelle de la session sur l'acteur du compte connecté**
> (relation 1-1 s22) au lieu du configurateur en dur ; le rôle/gating effectif suit le **type réel** de
> cet acteur. **Aucune règle de résolution grille/légende touchée** (le rôle n'y intervient pas, s21).

```gherkin
@back @vert
Scénario 5 — Connexion « Mamie » (type Autre) → identité effective ET rôle reflètent Mamie
  Étant donné un CompteUtilisateur Actif lié 1-1 à l'acteur "Mamie" de type Autre (s22)
  Quand je me connecte avec cet email (SeConnecterCommand s23)
  Alors l'identité effective de la session est Mamie (id stable de l'acteur du compte)
  Et le rôle / le gating d'écriture reflètent le type RÉEL de Mamie (Autre → pas les droits Parent)
  Et non un rôle Parent hérité par défaut

@back @vert
Scénario 6 — Non-régression impersonation bornée s14 au-dessus de la connexion
  Étant donné un compte Actif de type Parent connecté (identité réelle = l'acteur du compte, corrigé Sc.5)
  Quand j'incarne un autre acteur déclaré (impersonation bornée lecture s14)
  Alors la vue suit l'identité effective incarnée (gating règle 9 piloté par l'incarné)
  Et le retour à l'identité réelle me ramène à l'acteur du compte connecté (pas au configurateur en dur)
  Et la suppression concurrente de l'acteur incarné replie sur l'identité réelle du compte (SignalR s14)
```

## Volet 3 — Mot de passe local + création de compte libre-service (retours PO s23/s24) — @back runtime

> Introduit un **facteur d'authentification par mot de passe** distinct de l'email-only livré s23. Le
> mot de passe est **stocké haché** (jamais en clair) sur `CompteUtilisateur`. `SeConnecterCommand`
> évolue pour vérifier le couple email+mot de passe (l'email-only s23 reste couvert pour les comptes
> sans mot de passe / OAuth, à trancher au make-gherkin par la dev-team — décision technique déléguée
> au SM si friction). Le port mail (`IEnvoiMail`, volet 5) est réutilisé par la récupération.

```gherkin
@back @vert
Scénario 7 — Login email + mot de passe : bon couple → session ouverte
  Étant donné un CompteUtilisateur Actif avec un mot de passe défini (haché)
  Quand je me connecte avec le bon email et le bon mot de passe
  Alors une SessionOuverte est créée (identité réelle = acteur du compte, cf. Sc.5)
  Et le mot de passe n'est JAMAIS retourné ni exposé par le canal

@back @vert
Scénario 8 — Login : mauvais mot de passe → refus, aucune session
  Étant donné un CompteUtilisateur Actif avec un mot de passe défini
  Quand je me connecte avec le bon email mais un mauvais mot de passe
  Alors aucune session n'est ouverte
  Et le motif est clair et NEUTRE (ne distingue pas "email inconnu" de "mauvais mot de passe" — anti-énumération)

@back @vert
Scénario 9 — Création de compte libre-service : email neuf + mot de passe → compte Inactif
  Étant donné aucun compte pour l'email fourni
  Quand je crée un compte en libre-service (email + mot de passe, "si on en a pas encore")
  Alors un CompteUtilisateur est créé avec statut Inactif (défaut s22) et mot de passe haché
  Et ActeurId reste nullable (association / activation ultérieures, s22/s24)
  Et l'email unique + le mot de passe requis sont validés (garde s22 étendue)

@back @vert
Scénario 10 — Libre-service : email déjà porteur → rejet sans écriture
  Étant donné un CompteUtilisateur existant pour l'email fourni
  Quand je tente une création libre-service avec le même email
  Alors la création est rejetée SANS aucune écriture (invariant email unique s22)
  Et le motif est clair

@back @vert
Scénario 13 — Récupération : jeton valide → mot de passe redéfini, jeton consommé
  Étant donné un jeton de réinitialisation valide émis pour un compte (cf. Sc.11)
  Quand je soumets un nouveau mot de passe avec ce jeton
  Alors le mot de passe du compte est redéfini (haché)
  Et le jeton est consommé (une seconde utilisation échoue)
  Et un jeton expiré / inconnu est rejeté sans mutation
```

## Volet 5 — Récupération de mot de passe par email + envoi mail (retours PO s23/s24) — @back @preuve-doublure

> **⚠️ Non testable en runtime local (envoi SMTP réel)** — preuve par **doublure du port `IEnvoiMail`**
> au niveau Application/frontière + **preuve manuelle** du câblage réel (SMTP de test) au G3.
> Adaptateur de droite d'envoi de mail derrière `IEnvoiMail` (InMemory/doublure pour les tests, concret
> pour le runtime). **Réponse neutre anti-énumération** : la demande ne révèle jamais si l'email existe.

```gherkin
@back @preuve-doublure @vert
Scénario 11 — Demande de récupération sur email connu → jeton + mail émis
  # PREUVE PAR DOUBLURE D'ADAPTATEUR ; câblage SMTP réel vérifié manuellement (G3)
  Étant donné un CompteUtilisateur existant pour l'email fourni et un port IEnvoiMail (doublure)
  Quand je demande une récupération de mot de passe pour cet email
  Alors un jeton de réinitialisation à usage unique et expiration est généré côté serveur
  Et un mail contenant le lien/jeton est remis au port IEnvoiMail (doublure enregistre l'envoi)
  Et la réponse au client est NEUTRE (ne confirme pas l'existence du compte)

@back @preuve-doublure @vert
Scénario 12 — Demande sur email inconnu → réponse neutre, aucun mail
  # PREUVE PAR DOUBLURE D'ADAPTATEUR ; câblage SMTP réel vérifié manuellement (G3)
  Étant donné aucun compte pour l'email fourni
  Quand je demande une récupération de mot de passe pour cet email
  Alors aucun jeton n'est généré et le port IEnvoiMail ne reçoit AUCUN envoi
  Et la réponse au client est la MÊME réponse neutre qu'au Sc.11 (anti-énumération, aucune fuite)
```

## Volet 4 — OAuth externe Google / Microsoft / Apple (retours PO s21/s24) — @back/@ihm @preuve-doublure

> **⚠️ Non testable en runtime local (providers réels, secrets, callbacks)** — preuve par **doublure du
> port `IFournisseurOAuth`** au niveau Application/frontière + **preuve manuelle** du câblage réel (≥1
> provider, ex. Google) au G3. Branché **derrière** `SessionOuverte` s23 : le callback provider résout
> une identité externe → compte lié Actif → ouverture de session (même chemin de session que s23).
> Compte inconnu / Inactif → refus (cohérent Sc.8/s23/s24).

```gherkin
@back @preuve-doublure @vert
Scénario 14 — Callback OAuth, identité liée à un compte Actif → SessionOuverte
  # PREUVE PAR DOUBLURE D'ADAPTATEUR ; câblage provider réel (≥1, ex. Google) vérifié manuellement (G3)
  Étant donné un IFournisseurOAuth (doublure) restituant une identité externe liée à un compte Actif
  Quand le callback OAuth est traité côté serveur
  Alors une SessionOuverte est créée (identité réelle = acteur du compte, cf. Sc.5)
  Et le chemin de session est le MÊME que la connexion locale s23 (aucun agrégat durable neuf)

@back @preuve-doublure @vert
Scénario 15 — Callback OAuth : identité inconnue ou compte Inactif → refus
  # PREUVE PAR DOUBLURE D'ADAPTATEUR ; câblage provider réel vérifié manuellement (G3)
  Étant donné un IFournisseurOAuth (doublure) restituant une identité externe SANS compte lié, OU liée à un compte Inactif
  Quand le callback OAuth est traité
  Alors aucune session n'est ouverte
  Et le motif est clair (cohérent avec le refus email inconnu / Inactif s23/s24)

@ihm @preuve-doublure @pending
Scénario 16 — Boutons « Se connecter avec Google / Microsoft / Apple » sur /connexion
  # PREUVE PAR DOUBLURE ; redirection vers le provider réel vérifiée manuellement (G3)
  Étant donné la page /connexion (landing s24)
  Quand elle s'affiche
  Alors des boutons "Se connecter avec Google / Microsoft / Apple" sont présents à côté du login local
  Et un clic déclenche le flux OAuth (redirection vers le provider ; en test, la doublure court-circuite)
```

---

# Retours produit (PO)

<!-- Rempli après le gate G3 à la clôture. -->

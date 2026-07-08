# Sprint 28 — Câblage auth réel : reset mot de passe E2E + mot de passe local + rapprochement Google

> **Goal (G2 tranché PO, enrichi via « Autre »)** : rendre le login **opérationnel en runtime
> réel** — la logique auth de s25 est verte **par doublure de port**, mais **rien n'est branché**.
> Trois volets :
> 1. **Reset mot de passe réel bout-à-bout** : adaptateur **SMTP concret** (dev SMTP type Smtp4dev,
>    Docker) remplaçant la doublure `IEnvoiMail` ; **store de jetons reset durable** (Mongo)
>    réalisant `IReferentielJetonsReset` ; **expiration 60 min** prouvée sur store réel ; **DI** des
>    handlers `DemanderRecuperationMotDePasseHandler` / `RedefinirMotDePasseHandler` (dé-doublés) +
>    endpoints Api ; **écrans IHM** « mot de passe oublié » et « redéfinir par jeton » RED→GREEN.
> 2. **Mot de passe pour la connexion locale** : **poser** un mot de passe (haché PBKDF2, port
>    `IEditeurComptes.RedefinirMotDePasse` déjà présent) sur un compte + **s'authentifier** avec
>    (email+mot de passe — `SeConnecterHandler` vérifie déjà, motif neutre anti-énumération) : champ
>    mot de passe ajouté à l'écran de connexion (RED→GREEN runtime). Au-delà du simple reset.
> 3. **Rapprochement compte local ↔ Google (OAuth)** — **tranche minimale** : le callback Google
>    dont l'email vérifié correspond à un **compte local existant** ouvre la session sur **ce même
>    compte** (rattachement d'identité, **pas** de double compte — `ConnexionOAuthHandler` délègue au
>    même chemin s23) ; endpoint `api/oauth/google/demarrer` + DI du handler + port `IFournisseurOAuth`
>    branchés. Le boutons OAuth de `Connexion.razor` existent déjà.

## Périmètre — DANS / HORS scope

- **DANS** : volet 1 complet (SMTP dev + jetons Mongo + 60 min + DI + 2 écrans), volet 2 (poser mot
  de passe + login email+mot de passe IHM), volet 3 **rapprochement Google** (logique de rattachement
  + endpoint `demarrer` + DI + callback).
- **HORS scope (dette suiveuse, backlog)** : **providers OAuth Microsoft / Apple réels** (boutons
  présents, non câblés) ; **échange de secret Google réel** (client secret / callback en environnement
  cloud) **non testable en runtime local** → preuve par **doublure `IFournisseurOAuth` + vérif
  manuelle G3** ; **écran d'inscription libre-service** (le handler `CreerCompteLibreServiceHandler`
  existe et est déjà DI, mais l'écran dédié n'est **pas** construit ici) ; **mail d'activation**
  (évoqué PO, non spécifié) ; cycle de fond riche.

> **⚠️ GARDE preuve par doublure (entorse G2 de preuve, volet 3 uniquement).** Le **provider Google
> réel** (secrets/callbacks) n'est **pas** franchissable en runtime local → la **logique de
> rapprochement** (S9) est prouvée **verte contre une doublure du port `IFournisseurOAuth`**, et le
> **câblage réel Google** est vérifié **manuellement au G3**. Un `✅` **franc est INTERDIT** sur les
> lignes de volet 3 tant que le provider réel n'est pas branché + prouvé manuellement : statut cible
> **`✅ logique / ⚠️ câblage`**. Voir la **dette de câblage** sous le tableau.
> **Volets 1 et 2 : preuve = runtime réel** (Smtp4dev capté + Mongo durable + PBKDF2 réel, Docker
> actif) → `✅` **francs autorisés** une fois prouvés — ces volets **soldent** la part correspondante
> de la dette P0 s25.

## Avancement — 10/10

| # | Scénario | Type | Preuve | Statut |
|---|----------|------|--------|--------|
| S1 | Adaptateur SMTP concret remet le mail de récupération (jeton) | @back | runtime réel (Smtp4dev) | ✅ |
| S2 | Store de jetons reset durable (Mongo) — émission, relecture, consommation usage-unique | @back | runtime réel (Mongo) | ✅ |
| S3 | Jeton expiré (> 60 min) rejeté sans mutation, sur store réel | @back | runtime réel (Mongo) | ✅ |
| S4 | Réponse NEUTRE anti-énumération — email inconnu : aucun mail, aucun jeton | @back | runtime réel (Smtp4dev) | ✅ |
| S5 | Écran « mot de passe oublié » — demande envoyée, message neutre | 🖥️ IHM | runtime réel | ✅ |
| S6 | Écran « redéfinir par jeton » — nouveau mot de passe posé, connexion réussit | 🖥️ IHM | runtime réel | ✅ |
| S7 | Poser un mot de passe sur un compte → login email+mot de passe (bon/mauvais couple) | @back | runtime réel (PBKDF2) | ✅ |
| S8 | Écran de connexion — champ mot de passe, login email+mot de passe | 🖥️ IHM | runtime réel | ✅ |
| S9 | Rapprochement Google — callback (email connu) ouvre la session sur le compte local existant | @back | **doublure + manuel** | ✅ logique / ⚠️ câblage |
| S10 | Endpoint `api/oauth/google/demarrer` + DI du handler OAuth branchés | @back | **doublure + manuel** | ✅ logique / ⚠️ câblage |

> **Dette de câblage explicite (volet 3, à brancher/prouver manuellement au G3 ; le reste → backlog
> P0 à la clôture)** : (a) **adaptateur `IFournisseurOAuth` Google réel** (échange client secret /
> redirect_uri / callback en env. déployé) — S9/S10 prouvés par **doublure**, le provider réel reste
> **manuel** ; (b) **Microsoft / Apple** réels **non câblés** (boutons présents) ; (c) **écran
> inscription libre-service** non construit. Tant que (a) n'est pas branché + prouvé, S9/S10 restent
> **`✅ logique / ⚠️ câblage`**, **jamais** un `✅` franc.

## Scénarios

### Volet 1 — Reset mot de passe réel bout-à-bout (câblage réel, solde la dette s25)

```gherkin
@back @vert
Scénario: L'adaptateur SMTP concret remet le mail de récupération porteur du jeton
  Étant donné un compte utilisateur Actif d'email « papa@foyer.fr »
  Et l'adaptateur d'envoi de mail réel branché sur un serveur SMTP de développement (Smtp4dev, Docker actif)
  Quand une demande de récupération est émise pour « papa@foyer.fr » (POST /api/canal/demander-recuperation)
  Alors un mail de récupération est capté par le serveur SMTP de développement, adressé à « papa@foyer.fr »
  Et ce mail porte un jeton de réinitialisation à usage unique
  Et la réponse au client est un succès NEUTRE (aucun jeton, aucun indice d'existence — anti-énumération)
  # Câble IEnvoiMail (adaptateur SMTP concret) + DI de DemanderRecuperationMotDePasseHandler ;
  # remplace la doublure s25 — preuve runtime réelle via SMTP dev capté.
```

```gherkin
@back @vert
Scénario: Le store de jetons reset durable émet, relit et consomme le jeton (usage unique)
  Étant donné le mode de persistance Mongo (store réel, Docker actif) réalisant IReferentielJetonsReset
  Et un jeton de réinitialisation émis pour un compte, d'expiration à 60 minutes
  Quand le jeton est présenté pour redéfinir le mot de passe (POST /api/canal/redefinir-mot-de-passe)
  Alors le mot de passe du compte visé est redéfini (haché PBKDF2) et le jeton est consommé
  Et un second usage du MÊME jeton est rejeté sans mutation (usage unique, jeton consommé)
  # Câble IReferentielJetonsReset (store Mongo durable) + DI de RedefinirMotDePasseHandler.
```

```gherkin
@back @vert
Scénario: Un jeton expiré (au-delà de 60 minutes) est rejeté sans mutation
  Étant donné un jeton de réinitialisation émis avec une expiration à 60 minutes (store Mongo réel)
  Et l'horloge injectée positionnée 61 minutes après l'émission
  Quand le jeton expiré est présenté pour redéfinir le mot de passe
  Alors la redéfinition est rejetée (motif clair) sans aucune mutation du compte
  Et le mot de passe antérieur reste inchangé
  # Confirme l'expiration 60 min (défaut suggéré s25) prouvée contre l'horloge injectée, sur store réel.
```

```gherkin
@back @vert
Scénario: Réponse neutre anti-énumération — email inconnu ne déclenche ni mail ni jeton
  Étant donné qu'aucun compte ne porte l'email « inconnu@foyer.fr »
  Et l'adaptateur SMTP réel et le store de jetons Mongo branchés
  Quand une demande de récupération est émise pour « inconnu@foyer.fr »
  Alors aucun mail n'est capté par le serveur SMTP de développement
  Et aucun jeton n'est enregistré dans le store durable
  Et la réponse au client est le MÊME succès neutre que pour un email connu (aucune fuite d'existence)
```

```gherkin
@ihm @vert
Scénario: L'écran « mot de passe oublié » émet la demande et affiche un message neutre
  Étant donné la page de connexion « /connexion »
  Quand l'utilisateur suit le lien « Mot de passe oublié ? » et saisit son email puis valide
  Alors la demande de récupération est émise via le canal (POST /api/canal/demander-recuperation)
  Et un message NEUTRE est affiché (« Si un compte existe, un mail a été envoyé. ») — aucune fuite d'existence
  # RED→GREEN runtime. Écran mot-de-passe-oublié non construit à ce jour (dette s25 IHM).
```

```gherkin
@ihm @vert
Scénario: L'écran « redéfinir par jeton » pose un nouveau mot de passe et la connexion réussit
  Étant donné un jeton de réinitialisation valide reçu par mail (route « /reinitialiser-mot-de-passe?jeton=… »)
  Quand l'utilisateur saisit un nouveau mot de passe et valide
  Alors le mot de passe est redéfini via le canal (POST /api/canal/redefinir-mot-de-passe) et le jeton consommé
  Et l'utilisateur peut se connecter avec « email + nouveau mot de passe »
  Et une seconde tentative de redéfinition avec le même jeton échoue (usage unique)
  # RED→GREEN runtime, bout-à-bout avec le login (email + mot de passe, volet 2).
```

### Volet 2 — Mot de passe pour la connexion locale

```gherkin
@back @vert
Scénario: Poser un mot de passe sur un compte permet la connexion email + mot de passe
  Étant donné un compte utilisateur Actif d'email « maman@foyer.fr » sans mot de passe (email-only s23)
  Quand un mot de passe est posé sur ce compte (haché PBKDF2, port IEditeurComptes.RedefinirMotDePasse)
  Alors se connecter avec « maman@foyer.fr » + le bon mot de passe ouvre une session (identité réelle = acteur du compte)
  Et se connecter avec « maman@foyer.fr » + un mauvais mot de passe est refusé avec un motif NEUTRE (anti-énumération, même motif qu'un email inconnu)
  Et un compte SANS mot de passe reste connectable email-only (s23 non régressé)
  # SeConnecterHandler vérifie déjà email+mot de passe ; ajoute le chemin « définir un mot de passe »
  # (commande + endpoint) + DI. Motif neutre partagé mauvais-mot-de-passe / email-inconnu.
```

```gherkin
@ihm @vert
Scénario: L'écran de connexion accepte un mot de passe et connecte sur le bon couple
  Étant donné la page de connexion « /connexion »
  Quand l'utilisateur saisit email + mot de passe et valide
  Alors la connexion est émise avec le couple (POST /api/canal/se-connecter, email + mot de passe)
  Et sur un bon couple la session s'ouvre et redirige vers « /planning »
  Et sur un mauvais couple un motif NEUTRE est affiché, on reste sur « /connexion », aucune session
  # RED→GREEN runtime. Connexion.razor est aujourd'hui email-only : ajoute le champ mot de passe
  # (la requête n'envoie que l'email actuellement).
```

### Volet 3 — Rapprochement compte local ↔ Google (tranche minimale, preuve doublure + manuel)

```gherkin
@back @vert @preuve-doublure
Scénario: Le callback Google d'un email connu ouvre la session sur le compte local existant
  Étant donné un compte local Actif d'email « papa@foyer.fr » lié à un acteur du foyer
  Et une doublure de IFournisseurOAuth restituant l'identité externe « papa@foyer.fr » (email vérifié Google)
  Quand le callback OAuth est traité (ConnexionOAuthHandler, même chemin de session que s23)
  Alors une session s'ouvre sur CE compte local existant (identité réelle = son acteur) — rattachement, PAS de double compte
  Et un callback dont l'email est inconnu, ou dont le compte est Inactif, est refusé (motif clair, aucune session)
  # Logique verte contre doublure. Le PROVIDER GOOGLE RÉEL (secrets/callbacks) est câblé mais vérifié
  # MANUELLEMENT au G3 → statut cible « ✅ logique / ⚠️ câblage », jamais ✅ franc (dette de câblage).
```

```gherkin
@back @vert @preuve-doublure
Scénario: L'endpoint de démarrage OAuth Google et le handler de callback sont branchés en DI
  Étant donné le port IFournisseurOAuth enregistré en DI et ConnexionOAuthHandler résolvable
  Quand le navigateur atteint « api/oauth/google/demarrer »
  Alors le serveur démarre le flux OAuth Google (redirection vers l'authorize du provider)
  Et le callback du provider est routé vers ConnexionOAuthHandler qui ouvre (ou refuse) la session
  # Boutons OAuth déjà présents dans Connexion.razor (naviguent vers api/oauth/{provider}/demarrer,
  # endpoint aujourd'hui INEXISTANT). L'échange secret Google réel = vérif manuelle G3 (dette de câblage).
```

## Notes d'ancrage (état réel du code)

- **Logique s25 présente, non branchée** : handlers `DemanderRecuperationMotDePasseHandler`,
  `RedefinirMotDePasseHandler` (expiration via `IDateTimeProvider`, consommation usage-unique),
  `ConnexionOAuthHandler` (délègue à `SeConnecterHandler` — rattachement par email), et
  `SeConnecterHandler` (**vérifie déjà** email+mot de passe, motif neutre partagé). Ports `IEnvoiMail`,
  `IReferentielJetonsReset`, `IFournisseurOAuth` **définis** dans Application.
- **Ce qui manque (dette P0 s25)** : les 3 ports **non enregistrés en DI** ; les 3 handlers
  reset/OAuth **non enregistrés** (seul `CreerCompteLibreServiceHandler` l'est) ; **aucun adaptateur
  concret** (SMTP / jetons Mongo / OAuth) ; **aucun endpoint Api** pour récupération / redéfinition /
  définition de mot de passe / oauth. `IHacheurMotDePasse` (PBKDF2) **est** déjà câblé (`ServiceCollectionExtensions`).
- **IHM** : `Connexion.razor` a **déjà les boutons OAuth** (navigation vers `api/oauth/{provider}/demarrer`
  — **endpoint inexistant**) mais **pas de champ mot de passe** (la requête n'envoie que l'email) ni
  d'écran « mot de passe oublié ». Écrans à mener RED→GREEN runtime (S5/S6/S8).
- **Backend d'abord** (S1→S4, S7, S9→S10 à la frontière Application/Api), **IHM en fin** (S5/S6/S8).
- **Preuve** : volets 1-2 = **runtime réel** (Smtp4dev capté + Mongo durable + PBKDF2, Docker actif),
  `✅` francs autorisés → **soldent** leur part de la dette s25 ; volet 3 = **doublure + manuel**
  (Google réel), `✅ logique / ⚠️ câblage` seulement, dette de câblage portée au backlog à la clôture.
- **Mode opératoire OAuth** : `docs/guides/auth-social-oauth-mode-operatoire.md`.

# Retours produit (PO)

<!-- À remplir au gate G3 : retours, bugs, évolutions, nouveaux besoins. Vidé vers docs/BACKLOG.md à la clôture. -->

# Sprint 23 — Auth · tranche 2a : session locale & acteur-par-défaut = moi (`auth-session-locale-acteur-par-defaut`)

> **Avancement : 7/9 ⏳**

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | **Connexion locale par email** d'un `CompteUtilisateur` **Actif** existant → session serveur ouverte, identité effective = l'acteur lié au compte | @back | ✅ |
| 2 | **Rejet connexion : email inconnu** (aucun compte) → refus, motif clair, **aucune session** ouverte | @back | ✅ |
| 3 | **Rejet connexion : compte Inactif** (statut par défaut s22) → refus, motif clair, **aucune session** (l'activation reste hors scope) | @back | ✅ |
| 4 | **Acteur par défaut = utilisateur connecté** : session ouverte → l'acteur par défaut résolu côté serveur = l'acteur lié 1-1 au compte connecté (relation s22) | @back | ✅ |
| 5 | **Logout** : la session ouverte est détruite → l'identité effective retombe sur le comportement **non connecté** (aucune fuite d'identité) | @back | ✅ |
| 6 | **Non connecté = pas de régression** : sans session, l'identité effective et l'impersonation bornée (s14) se comportent exactement comme aujourd'hui | @back | ✅ |
| 7 | IHM **bandeau de connexion custom** : saisir un email + « Se connecter » → connecté (bandeau « Connecté : … ») ; email inconnu/compte inactif → le bandeau affiche un motif clair, reste déconnecté | 🖥️ @ihm | ✅ |
| 8 | IHM **déconnexion + acteur par défaut** : « Se déconnecter » repasse déconnecté ; une fois connecté, le sélecteur d'acteur (config/dialogs) est pré-positionné sur **l'acteur du compte connecté** | 🖥️ @ihm | ⏳ |
| 9 | **Temps réel SignalR préservé** : la connexion/déconnexion n'altère pas la propagation lecture (grille/légende/config convergent sur un 2ᵉ écran sans rechargement) | 🖥️ @ihm | ⏳ |

---

> Sujet `/planning` = `auth-session-locale-acteur-par-defaut` (épic É10, tranche 2a ; recoupe É2/É5), goal tranché **G2 (PO)** sur le 1er candidat.
> **Origine** : retours produit s21 — **auth marquée URGENT** (connexion réelle + acteur par défaut = utilisateur connecté). La **tranche 1** (s22) a posé la **fondation identité** (`CompteUtilisateur` id stable opaque + email + statut Actif/Inactif + `ActeurId` 1-1, persisté Mongo config foyer ; invariant admin=parent). Cette **tranche 2a** ajoute la **connexion réelle testable en runtime local** : login par email d'un compte **Actif**, **session HTTP côté serveur**, logout, et le couplage **« acteur par défaut = utilisateur connecté »** que la relation 1-1 s22 rendait trivial.
>
> **Découpage 2a / 2b (décision CP, dépendances externes).** Les 3 **OAuth externes** (Google / Apple / Microsoft : secrets, callbacks, providers réels) sont **difficiles à mener et prouver en runtime local** (rempart acceptation runtime) → **écartés de ce sprint** = **tranche 2b** ultérieure. La 2a prend **exactement** la part testable localement : connexion par email d'un compte Actif + session serveur + acteur-par-défaut. Elle **débloque « acteur par défaut = moi »** et pose le socle de session que la 2b brancherait derrière un provider.
>
> **Décisions CP (déterministes).**
> - **Session = identité côté serveur, pas seulement `SessionPlanning` front.** Le login pose une **session HTTP réelle** (portée serveur / hôte API) ; l'**identité effective** de la session serveur prime. `SessionPlanning` (identité réelle/effective + impersonation, s14) est **conservée et alimentée** par la session serveur, jamais contournée : le login **ancre l'identité réelle** de session sur l'acteur du compte connecté ; l'impersonation lecture (s14) reste possible **au-dessus** de cette identité réelle.
> - **Login = commande applicative** (canal requête/réponse, comme toute écriture) : « se connecter par email » réussit **ssi** un compte d'email donné existe **ET** est **Actif**. Échec (email inconnu / compte Inactif) → **aucune session**, motif clair. Réutilise `IEnumerationComptes` (s22) en lecture — **aucun nouvel agrégat de config**.
> - **Compte Inactif ⇒ refus de connexion.** Le statut `Inactif` (défaut s22) **borne** la connexion : un compte non activé ne peut pas ouvrir de session. L'**activation** (Inactif→Actif, prise en main de compte) reste **hors scope** (palier 13) — ce sprint suppose au moins un compte déjà Actif (par seed de test / bascule d'admin hors périmètre IHM de ce sprint, à trancher au fil sans nouvel écran d'activation).
> - **Acteur par défaut = utilisateur connecté** (retour URGENT #2) : au login, l'**acteur par défaut** résolu côté serveur = l'acteur **lié 1-1** au compte (relation s22). Sans session, le défaut retombe sur le **comportement actuel** (aucune régression). Le défaut alimente le **sélecteur d'acteur** (config + dialogs) — source unique `IEnumerationActeursFoyer` (convergence s20) préservée.
> - **Logout = destruction de session.** Après logout, l'identité effective **retombe** exactement sur le chemin non connecté (Sc.5/Sc.6) — pas d'identité résiduelle. Non-régression **impersonation bornée s14** (retour auto sur suppression concurrente, gating effectif) explicitement gardée.
> - **Aucune persistance neuve hors config foyer.** La session est un état d'**hôte / requête**, pas un agrégat durable de domaine (borne anti-cliquet règle 30 respectée). Les comptes restent le seul store touché, en **lecture** (déjà Mongo config foyer s22).
> - **Placement IHM** : un **bandeau de connexion custom** (email + « Se connecter » / « Se déconnecter » + état « Connecté : … »), cohérent avec le bandeau d'impersonation existant (`PlanningPartage`, s14). **Temps réel SignalR lecture préservé** (s20) — connexion/déconnexion n'altèrent pas la propagation.
> - **Rétrofit flake TempsReel (dette P1, +2)** : NON pris ce sprint (auth priorisée au G2). Garde-fou triage durci (rétro s21) inchangé : re-run **en isolation x2-3** avant tout étiquetage « flake » ; **N/N rouge = régression, STOP** — un rouge déterministe sur `*TempsReel*` est une **régression**, jamais un flake.
>
> **Hors scope (tranche 2b / paliers ultérieurs)** : **OAuth Google / Apple / Microsoft** (3 intégrations externes + secrets + callbacks, non testables runtime local) = tranche 2b ; **activation / prise en main de compte** (Inactif→Actif, palier 13) ; **landing page** dédiée multi-provider (le bandeau custom suffit à la 2a) ; **droits par rôle après prise en main** (É10, palier 13) ; toute **persistance hors config foyer** ; **sprint de design** (retour PO P1, séparé) ; **cohérence config→planning** (retour PO, séparé). L'acceptation reste **runtime réel** (session serveur + store Mongo config), pas par doublure.
>
> **Garde de cohérence date↔index/parité de cycle** : aucun scénario de ce sprint ne nomme conjointement une date ET un index/parité de cycle de fond — garde sans objet ici (auth pure, pas de résolution de cycle).

---

## Scénarios

### @back — connexion, session serveur & acteur par défaut (frontière Application/ports + hôte)

```gherkin
@back @vert
Scénario 1 — Connexion locale par email d'un compte Actif
  Étant donné un foyer configuré (store de config durable) avec un CompteUtilisateur d'email « alice@foyer.fr », statut « Actif », lié 1-1 à l'acteur « Alice » (id stable, s22)
  Quand un visiteur se connecte avec l'email « alice@foyer.fr »
  Alors une session serveur est ouverte
    Et l'identité réelle de la session est l'acteur « Alice » (id stable du compte)
    Et l'identité effective résout comme aujourd'hui au-dessus de cette identité réelle (s14 non contournée)
```

```gherkin
@back @vert
Scénario 2 — Rejet : email inconnu, aucune session
  Étant donné un référentiel de comptes ne contenant aucun compte d'email « inconnu@foyer.fr »
  Quand un visiteur tente de se connecter avec l'email « inconnu@foyer.fr »
  Alors la commande de connexion échoue avec un motif clair (email inconnu)
    Et aucune session n'est ouverte (le visiteur reste non connecté)
```

```gherkin
@back @vert
Scénario 3 — Rejet : compte Inactif, aucune session
  Étant donné un CompteUtilisateur d'email « bob@foyer.fr » de statut « Inactif » (défaut de création s22)
  Quand un visiteur tente de se connecter avec l'email « bob@foyer.fr »
  Alors la commande de connexion échoue avec un motif clair (compte non activé)
    Et aucune session n'est ouverte
    Et l'activation Inactif→Actif reste hors scope (aucun chemin d'activation déclenché)
```

```gherkin
@back @vert
Scénario 4 — Acteur par défaut = utilisateur connecté
  Étant donné une session serveur ouverte pour le compte « alice@foyer.fr » lié à l'acteur « Alice » (1-1, s22)
  Quand on résout l'acteur par défaut de cette session côté serveur
  Alors l'acteur par défaut est « Alice » (l'acteur lié au compte connecté)
    Et cet acteur par défaut est celui exposé au sélecteur (config/dialogs), source unique IEnumerationActeursFoyer (s20)
```

```gherkin
@back @vert
Scénario 5 — Logout : la session est détruite, l'identité retombe non connectée
  Étant donné une session serveur ouverte pour le compte « alice@foyer.fr »
  Quand le compte connecté se déconnecte (logout)
  Alors la session serveur est détruite
    Et l'identité effective retombe exactement sur le comportement non connecté
    Et aucune identité résiduelle ne subsiste (l'acteur par défaut n'est plus « Alice »)
```

```gherkin
@back @vert
Scénario 6 — Non connecté = aucune régression (impersonation bornée s14)
  Étant donné aucune session serveur ouverte
  Quand on résout l'identité effective et l'acteur par défaut
  Alors le comportement est identique à celui d'aujourd'hui (avant l'auth session)
    Et l'impersonation bornée lecture (s14) reste disponible et gatée comme avant (retour auto sur suppression concurrente, gating effectif)
    Et l'acteur par défaut retombe sur le défaut actuel (pas de couplage compte connecté)
```

### @ihm — bandeau de connexion custom & temps réel (RED→GREEN runtime)

```gherkin
@ihm @vert
Scénario 7 — Bandeau de connexion : succès et refus lisibles
  Étant donné l'application ouverte, un visiteur non connecté, un compte Actif « alice@foyer.fr » et un compte Inactif « bob@foyer.fr »
  Quand le visiteur saisit « alice@foyer.fr » dans le bandeau et clique « Se connecter »
  Alors le bandeau affiche l'état « Connecté : Alice »
  Quand un visiteur non connecté saisit « inconnu@foyer.fr » puis « bob@foyer.fr » et clique « Se connecter »
  Alors le bandeau reste « non connecté » et affiche un motif clair (email inconnu / compte non activé)
```

```gherkin
@ihm @pending
Scénario 8 — Déconnexion + acteur par défaut pré-positionné
  Étant donné un visiteur connecté en tant que compte « alice@foyer.fr » (lié à l'acteur « Alice »)
  Quand il ouvre le sélecteur d'acteur (config ou dialog d'écriture)
  Alors l'acteur par défaut sélectionné est « Alice » (l'acteur du compte connecté)
  Quand il clique « Se déconnecter »
  Alors le bandeau repasse « non connecté »
    Et le sélecteur d'acteur retombe sur le défaut non connecté (plus de pré-positionnement sur « Alice »)
```

```gherkin
@ihm @pending
Scénario 9 — Temps réel SignalR préservé sous connexion/déconnexion
  Étant donné deux écrans ouverts sur le même foyer, l'un connecté en tant que « Alice »
  Quand une modification de config (renommer/recolorier un acteur) est appliquée sur un écran
  Alors les deux écrans convergent (grille + légende + config) sans rechargement
    Et la connexion/déconnexion d'un écran n'altère pas la propagation lecture de l'autre (non-régression temps réel s20)
```

---

# Retours produit (PO)

<!-- Rempli au gate G3 / à la clôture : bugs, évolutions, nouveaux besoins, questions. -->

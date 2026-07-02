# Mode opératoire — Authentification sociale (Google, Apple, Microsoft)

> Guide **opérationnel** pour récupérer les identifiants OAuth/OIDC des trois fournisseurs et câbler
> la connexion sociale dans **planning-de-garde** (API .NET détachée + front Blazor WebAssembly).
>
> ⚠️ Ce document n'est **pas** une spec produit. Il décrit les manips console + le câblage technique.
> La règle métier de connexion (comptes, rôles, acteurs) reste pilotée par les sprints.

---

## 0. Ce qu'on récupère et pourquoi

Ce ne sont **pas** des « API keys » classiques mais des **identifiants clients OAuth 2.0 / OpenID Connect** :

| Fournisseur | Protocole | Ce qu'on récupère | Secret côté serveur ? | Coût |
|---|---|---|---|---|
| **Google** | OpenID Connect | `Client ID` + `Client Secret` | Oui | Gratuit |
| **Microsoft** | OpenID Connect (Entra ID) | `Application (client) ID` + `Client Secret` + `Tenant` | Oui | Gratuit |
| **Apple** | Sign in with Apple (OIDC) | `Services ID` + `Team ID` + `Key ID` + clé privée `.p8` | Secret **calculé** (JWT signé) | **99 €/an** (Apple Developer Program) |

### Choix d'architecture — BFF (recommandé)

L'app a une **API détachée** (`PlanningDeGarde.Api`) et un **front WASM** (`PlanningDeGarde.Web`) qui la
consomme à distance. Le WASM tourne dans le navigateur : **il ne peut pas garder un `Client Secret`**
(tout code WASM est lisible côté client).

→ **Pattern retenu : Backend-For-Frontend (BFF).** L'**API** porte le flux OAuth (Authorization Code),
détient les secrets, échange le code contre les tokens, puis ouvre une **session applicative** (cookie
sécurisé `HttpOnly` ou jeton relayé au canal d'écriture existant). Le WASM ne voit jamais les secrets.

C'est cohérent avec la règle d'or du projet : **le front ne parle jamais au domaine en direct, tout
passe par l'API**.

### URLs de redirection (callback) à déclarer chez chaque fournisseur

L'URL de callback pointe sur l'**API** (c'est elle qui traite le retour OAuth) :

| Environnement | Base API | Google | Microsoft | Apple |
|---|---|---|---|---|
| **Dev local** | `https://localhost:7180` | `…/signin-google` | `…/signin-oidc` | `…/signin-apple` |
| **Prod** | `https://<domaine-api>` | idem | idem | idem |

> Adapter si les ports changent (`src/PlanningDeGarde.Api/Properties/launchSettings.json`).
> Déclarer **dev ET prod** dans chaque console. Apple **refuse `localhost`** → voir §3.

---

## 1. Google

**Console : https://console.cloud.google.com/**

1. **Créer/choisir un projet** (menu projet en haut → *Nouveau projet*, ex. `planning-de-garde`).
2. **APIs & Services → OAuth consent screen**
   - *User Type* : **External** (comptes Google publics).
   - Renseigner nom de l'app, e-mail support, domaine. En dev, laisser en mode *Testing* et
     ajouter ton compte Google dans *Test users*.
   - *Scopes* : ajouter `openid`, `email`, `profile`.
3. **APIs & Services → Credentials → Create Credentials → OAuth client ID**
   - *Application type* : **Web application**.
   - *Authorized redirect URIs* : ajouter
     `https://localhost:7180/signin-google` **et** l'URL de prod.
4. **Récupérer** : `Client ID` + `Client Secret` (bouton *Download JSON* ou copie directe).

**Valeurs à conserver** : `Google:ClientId`, `Google:ClientSecret`.

---

## 2. Microsoft (Entra ID / Azure AD)

**Console : https://portal.azure.com/ → Microsoft Entra ID → App registrations**

1. **New registration**
   - *Name* : `planning-de-garde`.
   - *Supported account types* : **Accounts in any organizational directory and personal Microsoft
     accounts** (si comptes perso @outlook/@hotmail attendus) — sinon single-tenant.
   - *Redirect URI* : type **Web** → `https://localhost:7180/signin-oidc`.
2. Après création, noter dans *Overview* :
   - **Application (client) ID**
   - **Directory (tenant) ID** (ou `common` si multi-tenant + comptes perso).
3. **Certificates & secrets → New client secret**
   - Description + expiration (max 24 mois). **Copier la *Value* immédiatement** (invisible ensuite).
4. **Authentication** : vérifier la redirect URI Web (dev + prod). Cocher **ID tokens** si flux hybride.
5. **API permissions** : `openid`, `email`, `profile` (Microsoft Graph → delegated) suffisent.

**Valeurs à conserver** : `Microsoft:ClientId`, `Microsoft:ClientSecret`, `Microsoft:TenantId`
(ou `Authority = https://login.microsoftonline.com/common/v2.0`).

---

## 3. Apple (Sign in with Apple)

⚠️ Nécessite un **compte Apple Developer payant (99 €/an)**. Le « secret » Apple n'est **pas** une
chaîne fixe : c'est un **JWT signé** avec une clé privée `.p8`, à **régénérer périodiquement**
(validité max **6 mois**).

**Console : https://developer.apple.com/account/ → Certificates, Identifiers & Profiles**

1. **App ID** (*Identifiers → +* → *App IDs*)
   - Activer la capability **Sign In with Apple**. Noter le **Bundle ID** (ex. `fr.gold.planningdegarde`).
2. **Services ID** (*Identifiers → +* → *Services IDs*)
   - Sert de **`Client ID`** OAuth. Ex. `fr.gold.planningdegarde.web`.
   - *Configure* Sign In with Apple :
     - *Primary App ID* : celui du point 1.
     - *Domains and Subdomains* : `<domaine-prod>` (Apple **refuse `localhost`**).
     - *Return URLs* : `https://<domaine-prod>/signin-apple`.
   - 🔧 **Dev local** : Apple exigeant un domaine public HTTPS, tester via un tunnel
     (ex. `ngrok`/`dev tunnels`) et déclarer l'URL du tunnel, **ou** ne valider Apple qu'en préprod/prod.
3. **Key** (*Keys → +*)
   - Activer **Sign In with Apple**, associer l'App ID.
   - **Télécharger le fichier `.p8` (une seule fois)** + noter le **Key ID**.
4. **Team ID** : visible en haut à droite du compte développeur (*Membership*).

**Valeurs à conserver** : `Apple:ClientId` (= Services ID), `Apple:TeamId`, `Apple:KeyId`,
fichier `AuthKey_<KeyId>.p8` (**hors dépôt git**).

> Le client secret Apple = JWT `ES256` (header `kid`=KeyId, `iss`=TeamId, `sub`=ServicesID,
> `aud`=`https://appleid.apple.com`, `exp` ≤ 6 mois). À générer au démarrage de l'API et à
> renouveler avant expiration (packages comme `AspNet.Security.OAuth.Apple` le calculent).

---

## 4. Stockage des secrets — JAMAIS dans le dépôt

Aucun `Client Secret` / `.p8` ne doit être commité.

**Dev local** — `dotnet user-secrets` (hors arborescence, par machine) :

```bash
cd src/PlanningDeGarde.Api
dotnet user-secrets init
dotnet user-secrets set "Auth:Google:ClientId"       "xxx.apps.googleusercontent.com"
dotnet user-secrets set "Auth:Google:ClientSecret"   "GOCSPX-xxx"
dotnet user-secrets set "Auth:Microsoft:ClientId"    "xxxxxxxx-xxxx-..."
dotnet user-secrets set "Auth:Microsoft:ClientSecret" "xxx"
dotnet user-secrets set "Auth:Microsoft:TenantId"    "common"
dotnet user-secrets set "Auth:Apple:ClientId"        "fr.gold.planningdegarde.web"
dotnet user-secrets set "Auth:Apple:TeamId"          "XXXXXXXXXX"
dotnet user-secrets set "Auth:Apple:KeyId"           "YYYYYYYYYY"
```

Le `.p8` : hors repo (ex. `~/.secrets/AuthKey_YYYYYYYYYY.p8`), chemin passé en secret.

**Prod** : variables d'environnement / coffre (Azure Key Vault, GitHub Actions secrets…). Ne jamais
mettre les valeurs dans `appsettings.json` versionné — seule la **structure** (clés vides) peut y figurer.

`appsettings.json` (structure documentaire, sans valeurs) :

```json
{
  "Auth": {
    "Google":    { "ClientId": "", "ClientSecret": "" },
    "Microsoft": { "ClientId": "", "ClientSecret": "", "TenantId": "common" },
    "Apple":     { "ClientId": "", "TeamId": "", "KeyId": "", "PrivateKeyPath": "" }
  }
}
```

---

## 5. Câblage .NET (API détachée = BFF)

**Packages NuGet** (dernière version stable, sur le projet `PlanningDeGarde.Api`) :

```bash
dotnet add package Microsoft.AspNetCore.Authentication.Google
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect   # Microsoft
dotnet add package AspNet.Security.OAuth.Apple                          # gère le JWT .p8
```

**`Program.cs`** (schéma de principe — cookie applicatif + 3 handlers externes) :

```csharp
var auth = builder.Configuration.GetSection("Auth");

builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(o =>
    {
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.None;      // front WASM sur autre origine
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .AddGoogle(o =>
    {
        o.ClientId     = auth["Google:ClientId"]!;
        o.ClientSecret = auth["Google:ClientSecret"]!;
        o.CallbackPath = "/signin-google";
    })
    .AddOpenIdConnect("Microsoft", o =>
    {
        o.Authority    = $"https://login.microsoftonline.com/{auth["Microsoft:TenantId"]}/v2.0";
        o.ClientId     = auth["Microsoft:ClientId"]!;
        o.ClientSecret = auth["Microsoft:ClientSecret"]!;
        o.CallbackPath = "/signin-oidc";
        o.ResponseType = "code";
        o.Scope.Add("email"); o.Scope.Add("profile");
    })
    .AddApple(o =>
    {
        o.ClientId    = auth["Apple:ClientId"]!;
        o.TeamId      = auth["Apple:TeamId"]!;
        o.KeyId       = auth["Apple:KeyId"]!;
        o.UsePrivateKey(keyId =>
            new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                Path.GetDirectoryName(auth["Apple:PrivateKeyPath"]!)!)
            .GetFileInfo(Path.GetFileName(auth["Apple:PrivateKeyPath"]!)));
        o.CallbackPath = "/signin-apple";
    });

builder.Services.AddAuthorization();
// … après build :
app.UseAuthentication();
app.UseAuthorization();
```

**CORS** : autoriser l'origine du front WASM **avec credentials** (le cookie de session doit passer) :

```csharp
policy.WithOrigins("https://localhost:7133")
      .AllowCredentials()
      .AllowAnyHeader().AllowAnyMethod();
```

**Endpoints de déclenchement** (le front les ouvre, l'API redirige vers le fournisseur) :

```csharp
app.MapGet("/api/auth/{provider}/login", (string provider, string? returnUrl) =>
    Results.Challenge(
        new() { RedirectUri = returnUrl ?? "https://localhost:7133/" },
        new[] { provider }));   // "Google" | "Microsoft" | "Apple"
```

Au retour du fournisseur, mapper l'identité externe (email vérifié) sur un **CompteUtilisateur** du
foyer, puis ouvrir la session via le **canal d'écriture** existant (cohérent avec le login
email/mot de passe des sprints en cours). L'appariement compte↔identité externe est une **règle
métier** → à porter par un sprint, pas par ce guide.

---

## 6. Côté front (Blazor WASM)

- Le WASM **n'embarque aucun secret**. Il propose 3 boutons qui redirigent le navigateur vers
  `GET {API}/api/auth/Google/login` (resp. `Microsoft`, `Apple`).
- Les appels API se font **avec credentials** (`HttpClient` + cookie) pour porter la session BFF.
- La garde de route (Sc.1-4 du sprint 25) protège déjà l'accès : pas de session → `/connexion`.

---

## 7. Checklist de validation

- [ ] Google : consent screen configuré, redirect URI dev+prod, ClientId/Secret en user-secrets.
- [ ] Microsoft : app registration, redirect URI, secret copié (Value), TenantId choisi.
- [ ] Apple : Services ID + Key `.p8` + TeamId + KeyId, domaine public (tunnel en dev).
- [ ] Aucun secret dans un fichier versionné (`git status` propre, `.p8` ignoré).
- [ ] API démarrée : `/api/auth/Google/login` redirige bien vers l'écran Google.
- [ ] Retour callback → session ouverte → route protégée accessible.
- [ ] CORS `AllowCredentials` + `SameSite=None; Secure` (sinon le cookie ne passe pas cross-origin).
- [ ] Apple : prévoir la **rotation du JWT** (≤ 6 mois) — automatisée par `AspNet.Security.OAuth.Apple`.

---

## 8. Points de vigilance

- **Apple = payant + pas de `localhost`** : c'est le fournisseur le plus contraignant. Le tester en
  dernier, via tunnel HTTPS ou directement en préprod.
- **Secret Microsoft expirable** : noter la date, prévoir le renouvellement (sinon panne de login).
- **email non vérifié** : certains fournisseurs renvoient un email non vérifié — refuser côté API.
- **Un même email chez plusieurs fournisseurs** : décider la règle d'appariement (1 compte ↔ N
  identités externes ?) — **décision produit**, à trancher en sprint.

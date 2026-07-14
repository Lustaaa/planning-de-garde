using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Collection xUnit <b>non parallèle CIBLÉE</b> regroupant les tests d'acceptation runtime *TempsReel*
/// (front WASM réel + API distante réelle + <b>vrai client SignalR</b> long-polling vers un TestServer
/// en mémoire, convergence multi-clients). Elle solde à la cause la dette de flake P1 *TempsReel*
/// (rétrofit s39) : ces tests lourds établissent chacun une (des) connexion(s) SignalR persistante(s) et
/// attendent une convergence asynchrone (<c>WaitForAssertion</c> jusqu'à 10-15 s). En parallèle, la
/// multiplication de ces boucles de long-polling concurrentes affamait le pool de threads → les allers-retours
/// HTTP / <c>StateHasChanged</c> d'un test « victime » dépassaient occasionnellement leur délai (baseline s39 :
/// ~40 % de rouge full-suite EN PARALLÈLE, victime intermittente confirmée vert 3/3 en isolation ⇒ course de
/// charge, PAS une régression déterministe).
///
/// <para><b>Périmètre MINIMAL et explicite (garde « ciblée, pas rideau », décision SM s39).</b> Seuls les tests
/// à <b>I/O SignalR réel *TempsReel*</b> sont sérialisés — pas le harnais entier. <see cref="DisableParallelization"/>
/// = <c>true</c> : quand un test de cette collection s'exécute, aucun autre test ne tourne concurremment, donc la
/// charge SignalR concurrente qui provoquait la course est neutralisée à la racine. Les tests de composant
/// PURS (bUnit sans hub réel : modals, invariants d'affichage) <b>gardent leur parallélisme</b>.</para>
///
/// <para><b>Ne masque AUCUNE régression</b> : un rouge déterministe resterait rouge en série (triage durci s21
/// maintenu). La sérialisation ne tue qu'une course de test.</para>
/// </summary>
[CollectionDefinition("SignalRTempsReel", DisableParallelization = true)]
public sealed class SignalRTempsReelCollection
{
}

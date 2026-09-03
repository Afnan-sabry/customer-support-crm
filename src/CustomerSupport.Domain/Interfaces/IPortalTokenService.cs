using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface IPortalTokenService
{
    (string AccessToken, string RefreshToken) GenerateTokens(PortalUser user);
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

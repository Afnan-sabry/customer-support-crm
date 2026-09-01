using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface ITokenService
{
    Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(ApplicationUser user);
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

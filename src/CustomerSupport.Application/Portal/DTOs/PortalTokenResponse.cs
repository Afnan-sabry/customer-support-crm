namespace CustomerSupport.Application.Portal.DTOs;

public record PortalTokenResponse(string AccessToken, string RefreshToken, PortalUserDto User);

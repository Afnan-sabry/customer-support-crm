namespace CustomerSupport.Application.Auth.DTOs;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User);

namespace CustomerSupport.Application.Roles.DTOs;

public record RoleDto(Guid Id, string Name, string NameAr, bool IsSystem, List<PermissionDto> Permissions);

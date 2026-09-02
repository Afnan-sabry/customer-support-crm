namespace CustomerSupport.Application.Portal.DTOs;

public record PortalTicketDto(
    Guid Id, string TicketNumber, string Subject,
    string CategoryName, string PriorityName, string StatusName,
    DateTime CreatedAt, DateTime UpdatedAt);

public record PortalTicketDetailDto(
    Guid Id, string TicketNumber, string Subject, string Description,
    string CategoryName, string PriorityName, string StatusName,
    DateTime CreatedAt, DateTime UpdatedAt,
    List<PortalCommentDto> Comments);

public record PortalCommentDto(Guid Id, string Content, string AuthorName, DateTime CreatedAt, bool IsAgent);

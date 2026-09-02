namespace CustomerSupport.Application.Dashboard.DTOs;

public record SlaSummaryDto(
    int TotalTracked, int FirstResponseOnTime, int FirstResponseBreached,
    int ResolutionOnTime, int ResolutionBreached,
    double FirstResponseCompliancePercent, double ResolutionCompliancePercent);

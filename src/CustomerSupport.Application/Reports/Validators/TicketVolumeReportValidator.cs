using CustomerSupport.Application.Reports.Queries;
using FluentValidation;

namespace CustomerSupport.Application.Reports.Validators;

public class TicketVolumeReportValidator : AbstractValidator<GetTicketVolumeReportQuery>
{
    public TicketVolumeReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
        RuleFor(x => x.GroupBy).Must(g => g is "Day" or "Week" or "Month").WithMessage("GroupBy must be Day, Week, or Month.");
    }
}

public class SlaPerformanceReportValidator : AbstractValidator<GetSlaPerformanceReportQuery>
{
    public SlaPerformanceReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class AgentPerformanceReportValidator : AbstractValidator<GetAgentPerformanceReportQuery>
{
    public AgentPerformanceReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class ChannelAnalyticsReportValidator : AbstractValidator<GetChannelAnalyticsReportQuery>
{
    public ChannelAnalyticsReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class AiUsageReportValidator : AbstractValidator<GetAiUsageReportQuery>
{
    public AiUsageReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

public class CsatReportValidator : AbstractValidator<GetCsatReportQuery>
{
    public CsatReportValidator()
    {
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("Start date must be before end date.");
    }
}

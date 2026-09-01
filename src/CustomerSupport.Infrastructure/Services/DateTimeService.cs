using CustomerSupport.Domain.Interfaces;

namespace CustomerSupport.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}

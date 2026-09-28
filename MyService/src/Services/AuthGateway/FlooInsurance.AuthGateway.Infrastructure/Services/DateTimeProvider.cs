using FlooInsurance.AuthGateway.Application.Common.Interfaces;

namespace FlooInsurance.AuthGateway.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

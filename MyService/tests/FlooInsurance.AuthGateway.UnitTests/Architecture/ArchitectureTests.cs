using System.Reflection;
using FluentAssertions;
using FlooInsurance.AuthGateway.Application;
using FlooInsurance.AuthGateway.Domain.Entities;
using Xunit;

namespace FlooInsurance.AuthGateway.UnitTests.Architecture;

public class ArchitectureTests
{
    [Fact]
    public void Domain_MustNotReference_ExternalOrOtherLayers()
    {
        // Arrange
        var domainAssembly = typeof(User).Assembly;
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        // Assert
        referencedAssemblies.Should().NotContain(a => a.Name!.Contains("Infrastructure"), "Domain must not reference Infrastructure");
        referencedAssemblies.Should().NotContain(a => a.Name!.Contains("Application"), "Domain must not reference Application");
        referencedAssemblies.Should().NotContain(a => a.Name!.Contains("API"), "Domain must not reference API");
        referencedAssemblies.Should().NotContain(a => a.Name!.Contains("EntityFrameworkCore"), "Domain must not reference EF Core");
        referencedAssemblies.Should().NotContain(a => a.Name!.Contains("AspNetCore"), "Domain must not reference ASP.NET Core");
        referencedAssemblies.Should().NotContain(a => a.Name!.Contains("Serilog"), "Domain must not reference Serilog");
    }

    [Fact]
    public void Application_MustNotReference_InfrastructureOrApi()
    {
        // Arrange
        var applicationAssembly = typeof(DependencyInjection).Assembly;
        var referencedAssemblies = applicationAssembly.GetReferencedAssemblies();

        // Assert
        referencedAssemblies.Should().NotContain(a => a.Name!.Contains("Infrastructure"), "Application must not reference Infrastructure");
        referencedAssemblies.Should().NotContain(a => a.Name!.Contains("API"), "Application must not reference API");
        referencedAssemblies.Should().NotContain(a => a.Name!.Contains("EntityFrameworkCore"), "Application must not reference EF Core");
    }
}

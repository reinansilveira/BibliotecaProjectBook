using System.Reflection;
using Library.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace Library.ArchitectureTests;

public class ConventionTests
{
    private static readonly Assembly Application = typeof(Library.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Library.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    [Fact]
    public void Controllers_inherit_controller_base_and_end_with_controller()
    {
        var result = Types.InAssembly(Api)
            .That()
            .ResideInNamespace("Library.Api.Controllers")
            .Should()
            .Inherit(typeof(ControllerBase))
            .And()
            .HaveNameEndingWith("Controller")
            .GetResult();

        ArchitectureAssert.Passes(result);
    }

    [Fact]
    public void Controllers_do_not_access_the_database_directly()
    {
        var result = Types.InAssembly(Api)
            .That()
            .ResideInNamespace("Library.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOnAny(typeof(LibraryDbContext).Namespace!, "Microsoft.EntityFrameworkCore")
            .GetResult();

        ArchitectureAssert.Passes(result);
    }

    [Fact]
    public void Application_services_are_sealed()
    {
        var result = Types.InAssembly(Application)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Service")
            .Should()
            .BeSealed()
            .GetResult();

        ArchitectureAssert.Passes(result);
    }

    [Fact]
    public void Application_services_implement_an_interface()
    {
        var services = Application.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith("Service", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(services);
        Assert.All(services, service => Assert.Contains(service.GetInterfaces(), contract => contract.Name == $"I{service.Name}"));
    }

    [Fact]
    public void Repository_implementations_are_sealed_and_live_in_infrastructure()
    {
        var result = Types.InAssembly(Infrastructure)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Repository")
            .Should()
            .BeSealed()
            .And()
            .ResideInNamespaceStartingWith("Library.Infrastructure")
            .GetResult();

        ArchitectureAssert.Passes(result);
    }

    [Fact]
    public void Repository_contracts_are_interfaces_in_the_application_layer()
    {
        var result = Types.InAssembly(Application)
            .That()
            .HaveNameEndingWith("Repository")
            .Should()
            .BeInterfaces()
            .GetResult();

        ArchitectureAssert.Passes(result);
    }
}

using System.Reflection;
using Library.Domain.Entities;
using NetArchTest.Rules;

namespace Library.ArchitectureTests;

public class LayerDependencyTests
{
    private const string DomainNamespace = "Library.Domain";
    private const string ApplicationNamespace = "Library.Application";
    private const string InfrastructureNamespace = "Library.Infrastructure";
    private const string ApiNamespace = "Library.Api";

    private static readonly Assembly Domain = typeof(Book).Assembly;
    private static readonly Assembly Application = typeof(Library.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Library.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_any_other_layer()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        ArchitectureAssert.Passes(result);
    }

    [Fact]
    public void Domain_does_not_depend_on_frameworks()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "FluentValidation", "Dapper", "RabbitMQ")
            .GetResult();

        ArchitectureAssert.Passes(result);
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        ArchitectureAssert.Passes(result);
    }

    [Fact]
    public void Application_does_not_depend_on_persistence_web_or_broker_libraries()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Dapper", "RabbitMQ", "SendGrid")
            .GetResult();

        ArchitectureAssert.Passes(result);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        ArchitectureAssert.Passes(result);
    }

    [Fact]
    public void Domain_entities_live_in_the_entities_namespace()
    {
        var result = Types.InAssembly(Domain)
            .That()
            .AreClasses()
            .And()
            .DoNotInherit(typeof(Exception))
            .And()
            .DoNotInherit(typeof(Enum))
            .Should()
            .ResideInNamespace($"{DomainNamespace}.Entities")
            .GetResult();

        ArchitectureAssert.Passes(result);
    }
}

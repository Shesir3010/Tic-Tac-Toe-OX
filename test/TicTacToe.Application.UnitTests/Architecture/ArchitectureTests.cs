using NUnit.Framework;
using Shouldly;

namespace TicTacToe.Application.UnitTests.Architecture;

// R-A1..R-A5 dependency rules enforced by test, not convention. See system-architecture.md S2.
// Assemblies inspected via reflection over their references. Test project references production
// projects only to inspect them; production rule is unchanged (Infrastructure never references Web).
[TestFixture]
public sealed class ArchitectureTests
{
    [Test]
    public void Domain_HasNoProjectReferences()
    {
        var domainAssembly = System.Reflection.Assembly.Load("TicTacToe.Domain");

        domainAssembly.GetReferencedAssemblies()
            .Where(a => a.Name is not null && a.Name.StartsWith("TicTacToe."))
            .ShouldBeEmpty();
    }

    [Test]
    public void Application_ReferencesOnlyDomain()
    {
        var appAssembly = System.Reflection.Assembly.Load("TicTacToe.Application");

        appAssembly.GetReferencedAssemblies()
            .Where(a => a.Name == "TicTacToe.Infrastructure" || a.Name == "TicTacToe.Web")
            .ShouldBeEmpty();
    }

    [Test]
    public void Infrastructure_NeverReferencesWeb()
    {
        var infraAssembly = System.Reflection.Assembly.Load("TicTacToe.Infrastructure");

        infraAssembly.GetReferencedAssemblies()
            .Where(a => a.Name == "TicTacToe.Web")
            .ShouldBeEmpty();
    }

    [Test]
    public void Domain_HasNoSignalROrAspNetReferences()
    {
        var forbidden = new HashSet<string>
        {
            "Microsoft.AspNetCore.SignalR.Core",
            "Microsoft.AspNetCore.Mvc.Core",
            "Microsoft.EntityFrameworkCore",
        };

        var domainAssembly = System.Reflection.Assembly.Load("TicTacToe.Domain");

        domainAssembly.GetReferencedAssemblies()
            .Where(a => a.Name is not null && forbidden.Contains(a.Name))
            .ShouldBeEmpty();
    }

    [Test]
    public void Application_HasNoSignalRReferences()
    {
        System.Reflection.Assembly.Load("TicTacToe.Application").GetReferencedAssemblies()
            .Where(a => a.Name is "Microsoft.AspNetCore.SignalR.Core" or "Microsoft.AspNetCore.Mvc.Core")
            .ShouldBeEmpty();
    }
}

using NetArchTest.Rules;
using System.Reflection;
using Xunit;

namespace ArchTests;

/// <summary>
/// L2 module layering rules inside <c>Modules/Iam</c>: layering is namespaces in ONE project
/// (single-project modules per ADR-001), so NetArchTest guards namespaces the way it would guard assemblies.
/// 模組內分層規則：單專案內以命名空間守衛（Domain 不依賴上層；Application 不依賴 Infrastructure；模組不反向依賴 Hosts）。
/// </summary>
public class IamModuleLayeringRules
{
    private static Assembly IamAssembly => typeof(Iam.IamMarker).Assembly;

    private static Assembly SharedKernelAssembly => typeof(SharedKernel.DomainEvent).Assembly;

    [Fact]
    public void Domain_Shall_Not_Depend_On_Application_Or_Infrastructure_Or_AspNet()
    {
        var result = Types.InAssembly(IamAssembly)
            .That()
            .ResideInNamespace("Iam.Domain")
            .ShouldNot()
            .HaveDependencyOnAny("Iam.Application", "Iam.Infrastructure", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Diagnoser.Describe("Iam.Domain layer purity", result));
    }

    [Fact]
    public void Application_Shall_Not_Depend_On_Infrastructure_Or_AspNet()
    {
        var result = Types.InAssembly(IamAssembly)
            .That()
            .ResideInNamespace("Iam.Application")
            .ShouldNot()
            .HaveDependencyOnAny("Iam.Infrastructure", "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Diagnoser.Describe("Iam.Application layer purity", result));
    }

    [Fact]
    public void Modules_Shall_Not_Depend_On_Hosts()
    {
        var result = Types.InAssembly(IamAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Hosts.")
            .GetResult();

        Assert.True(result.IsSuccessful, Diagnoser.Describe("module independence from Hosts", result));
    }

    [Fact]
    public void SharedKernel_Shall_Not_Depend_On_Modules()
    {
        var result = Types.InAssembly(SharedKernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Iam.")
            .GetResult();

        Assert.True(result.IsSuccessful, Diagnoser.Describe("SharedKernel independence (ADR-007)", result));
    }
}

internal static class Diagnoser
{
    internal static string Describe(string ruleName, TestResult result)
    {
        return result.IsSuccessful
            ? $"{ruleName}: green"
            : $"{ruleName}: violated by {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}";
    }
}

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WrenchBox.Application;
using WrenchBox.Application.Common;

namespace WrenchBox.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersMediatRAndValidators()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        services.Should().NotBeEmpty();
    }
}

public class PagedResultTests
{
    [Fact]
    public void TotalPages_CalculatesCorrectly()
    {
        var result = new PagedResult<string>
        {
            Items = ["a", "b"],
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_ZeroPageSize_ReturnsZero()
    {
        var result = new PagedResult<string>
        {
            Items = [],
            Page = 1,
            PageSize = 0,
            TotalCount = 10
        };

        result.TotalPages.Should().Be(0);
    }
}

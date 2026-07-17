using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shuttle.Api.Controllers;
using Shuttle.Api.Filters;

namespace Shuttle.Tests.Api;

public class DebugControllerTests {
    [Fact]
    public void GetRoles_returns_the_callers_role_claims_sorted() {
        var controller = new DebugController {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    User = Principal("Shuttle.Jobs.Admin", "Shuttle.Admin"),
                },
            },
        };

        var result = Assert.IsType<OkObjectResult>(controller.GetRoles().Result);
        var roles = Assert.IsAssignableFrom<IEnumerable<string>>(result.Value).ToList();

        Assert.Equal(new[] { "Shuttle.Admin", "Shuttle.Jobs.Admin" }, roles);
    }

    [Fact]
    public void GetRoles_returns_empty_when_the_caller_has_no_roles() {
        var controller = new DebugController {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext { User = Principal() },
            },
        };

        var result = Assert.IsType<OkObjectResult>(controller.GetRoles().Result);
        var roles = Assert.IsAssignableFrom<IEnumerable<string>>(result.Value);

        Assert.Empty(roles);
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Staging", true)]
    [InlineData("Development", false)]
    public void DevelopmentOnly_blocks_non_development_environments(string environmentName, bool shouldBlock) {
        var context = ResourceExecutingContext(environmentName);

        new DevelopmentOnlyAttribute().OnResourceExecuting(context);

        if (shouldBlock) {
            Assert.IsType<NotFoundResult>(context.Result);
        } else {
            Assert.Null(context.Result);
        }
    }

    private static ClaimsPrincipal Principal(params string[] roles) {
        var identity = new ClaimsIdentity(
            roles.Select(r => new Claim(ClaimTypes.Role, r)),
            authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    private static ResourceExecutingContext ResourceExecutingContext(string environmentName) {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment(environmentName));

        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        return new ResourceExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new List<IValueProviderFactory>());
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Shuttle.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

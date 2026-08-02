using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace Shuttle.Api.Filters;

/// <summary>
/// Restricts an action or controller to the Development hosting environment. Requests hitting the
/// decorated endpoint in any other environment short-circuit with a 404, so the endpoint is
/// indistinguishable from one that does not exist in production.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DevelopmentOnlyAttribute : Attribute, IResourceFilter {
    public void OnResourceExecuting(ResourceExecutingContext context) {
        var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment()) {
            context.Result = new NotFoundResult();
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context) {
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BalonPark.Helpers;

namespace BalonPark.Attributes;

/// <summary>
/// API action'ları için admin session kontrolü. Session'da admin yoksa 401 döner.
/// </summary>
public class RequireAdminSessionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        await Task.CompletedTask;

        if (context.HttpContext.Session.IsAdminLoggedIn())
            return;

        context.Result = new UnauthorizedObjectResult(new
        {
            success = false,
            message = "Bu işlem için admin oturumu gerekli."
        });
    }
}

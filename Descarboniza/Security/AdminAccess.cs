using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Descarboniza.Security
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
    }

    /// <summary>
    /// Restringe uma action/controller ao administrador.
    /// Para qualquer outra pessoa (deslogada ou usuário comum) a resposta é 404,
    /// então o endereço /Admin nem aparenta existir.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SomenteAdminAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var usuario = context.HttpContext.User;

            if (usuario.Identity?.IsAuthenticated != true ||
                !usuario.IsInRole(AppRoles.Admin))
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}

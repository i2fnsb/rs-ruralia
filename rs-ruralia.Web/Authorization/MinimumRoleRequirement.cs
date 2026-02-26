using Microsoft.AspNetCore.Authorization;

namespace rs_ruralia.Web.Authorization;

public class MinimumRoleRequirement : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; }

    public MinimumRoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }
}

public class MinimumRoleHandler : AuthorizationHandler<MinimumRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumRoleRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            foreach (var role in requirement.AllowedRoles)
            {
                if (context.User.IsInRole(role))
                {
                    context.Succeed(requirement);
                    break;
                }
            }
        }

        return Task.CompletedTask;
    }
}

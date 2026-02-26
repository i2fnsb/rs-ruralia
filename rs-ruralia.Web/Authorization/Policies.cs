namespace rs_ruralia.Web.Authorization;

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string FnsbUserOrAbove = "FnsbUserOrAbove";
    public const string CommissionerOrAbove = "CommissionerOrAbove";
    public const string VendorOrAbove = "VendorOrAbove";
    public const string AuthenticatedUser = "AuthenticatedUser";
}

namespace rs_ruralia.Web.Authorization;

public static class Roles
{
    public const string Admin = "admin";
    public const string FnsbUser = "fnsbuser";
    public const string Commissioner = "commissioner";
    public const string Vendor = "vendor";
    public const string Public = "public";

    public static readonly string[] AllRoles = [Admin, FnsbUser, Commissioner, Vendor, Public];

    public static readonly string[] AdminAndAbove = [Admin];
    public static readonly string[] FnsbUserAndAbove = [Admin, FnsbUser];
    public static readonly string[] CommissionerAndAbove = [Admin, FnsbUser, Commissioner];
    public static readonly string[] VendorAndAbove = [Admin, FnsbUser, Commissioner, Vendor];
    public static readonly string[] AllIncludingPublic = AllRoles;
}

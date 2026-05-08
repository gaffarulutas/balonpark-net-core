namespace BalonPark.Helpers;

public static class SessionExtensions
{
    private const string AdminIdKey = "AdminId";
    private const string AdminUserNameKey = "AdminUserName";
    private const string AdminEmailKey = "AdminEmail";
    private const string SelectedAccountingCompanyIdKey = "SelectedAccountingCompanyId";

    public static void SetAdminSession(this ISession session, int adminId, string userName, string email)
    {
        session.SetInt32(AdminIdKey, adminId);
        session.SetString(AdminUserNameKey, userName);
        session.SetString(AdminEmailKey, email);
    }

    public static int? GetAdminId(this ISession session)
    {
        return session.GetInt32(AdminIdKey);
    }

    public static string? GetAdminUserName(this ISession session)
    {
        return session.GetString(AdminUserNameKey);
    }

    public static string? GetAdminEmail(this ISession session)
    {
        return session.GetString(AdminEmailKey);
    }

    public static bool IsAdminLoggedIn(this ISession session)
    {
        return session.GetInt32(AdminIdKey).HasValue;
    }

    public static void ClearAdminSession(this ISession session)
    {
        session.Remove(AdminIdKey);
        session.Remove(AdminUserNameKey);
        session.Remove(AdminEmailKey);
        session.Remove(SelectedAccountingCompanyIdKey);
    }

    public static void SetSelectedAccountingCompanyId(this ISession session, int companyId)
    {
        session.SetInt32(SelectedAccountingCompanyIdKey, companyId);
    }

    public static int? GetSelectedAccountingCompanyId(this ISession session)
    {
        return session.GetInt32(SelectedAccountingCompanyIdKey);
    }

    public static void RemoveSelectedAccountingCompanyId(this ISession session)
    {
        session.Remove(SelectedAccountingCompanyIdKey);
    }
}


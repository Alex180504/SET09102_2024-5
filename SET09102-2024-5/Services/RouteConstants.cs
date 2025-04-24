namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Centralizes all application route definitions to avoid hardcoded strings
    /// </summary>
    public static class RouteConstants
    {
        // Authentication routes
        public const string Login = "/LoginPage";
        public const string Register = "/RegisterPage";
        
        // Main application routes
        public const string MainPage = "/MainPage";
        
        // Admin routes
        public const string AdminDashboard = "/AdminDashboardPage";
        public const string RoleManagement = "/RoleManagementPage";
        public const string UserRoleManagement = "/UserRoleManagementPage";
        
        // Collection of admin routes for permission checks
        public static readonly string[] AdminRoutes = new[]
        {
            AdminDashboard,
            RoleManagement,
            UserRoleManagement
        };
        
        // Collection of public routes (no authentication needed)
        public static readonly string[] PublicRoutes = new[]
        {
            Login,
            Register
        };
    }
}
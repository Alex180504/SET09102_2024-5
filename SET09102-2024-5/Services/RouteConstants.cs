namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Centralizes all application route definitions to avoid hardcoded strings
    /// </summary>
    public static class RouteConstants
    {
        // Authentication routes - updated to use standardized naming
        public const string LoginPage = "LoginPage";
        public const string RegisterPage = "RegisterPage";
        
        // Main application routes
        public const string MainPage = "MainPage";
        public const string MapPage = "MapPage";
        
        // Admin routes
        public const string AdminDashboardPage = "AdminDashboardPage";
        public const string RoleManagementPage = "RoleManagementPage";
        public const string UserRoleManagementPage = "UserRoleManagementPage";
        
        // Collection of admin routes for permission checks
        public static readonly string[] AdminRoutes = new[]
        {
            AdminDashboardPage,
            RoleManagementPage,
            UserRoleManagementPage
        };
        
        // Collection of public routes (no authentication needed)
        public static readonly string[] PublicRoutes = new[]
        {
            LoginPage,
            RegisterPage
        };
        
        // Collection of all routes for pre-registration
        public static readonly string[] AllRoutes = new[]
        {
            LoginPage,
            RegisterPage,
            MainPage,
            MapPage,
            AdminDashboardPage,
            RoleManagementPage,
            UserRoleManagementPage
        };
        
        // Legacy route mapping for backward compatibility
        public static class Legacy
        {
            // Include old route names for backward compatibility
            public const string Login = "/LoginPage";
            public const string Register = "/RegisterPage";
            public const string AdminDashboard = "/AdminDashboardPage";
            public const string RoleManagement = "/RoleManagementPage";
            public const string UserRoleManagement = "/UserRoleManagementPage";
        }
    }
}
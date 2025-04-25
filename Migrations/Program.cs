using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using SET09102_2024_5.Data;

namespace SET09102_2024_5.Migrations
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SET09102 Sensor Monitoring - Database Migrations Tool");
            Console.WriteLine("===================================================");

            try
            {
                // Create a DbContext using the factory
                var factory = new SensorMonitoringContextFactory();
                using var context = factory.CreateDbContext(args);

                // Check database connection
                Console.WriteLine("Checking database connection...");
                bool canConnect = await context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Cannot connect to the database. Please check your connection string.");
                    Console.ResetColor();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Successfully connected to the database.");
                Console.ResetColor();

                // Simple menu
                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("\nPlease select an option:");
                    Console.WriteLine("1. Apply migrations (update database)");
                    Console.WriteLine("2. List pending migrations");
                    Console.WriteLine("3. List applied migrations");
                    Console.WriteLine("4. Exit");
                    Console.Write("\nYour choice: ");

                    string? choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            await ApplyMigrationsAsync(context);
                            break;
                        case "2":
                            await ListPendingMigrationsAsync(context);
                            break;
                        case "3":
                            await ListAppliedMigrationsAsync(context);
                            break;
                        case "4":
                            exit = true;
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Invalid option. Please try again.");
                            Console.ResetColor();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }

        private static async Task ApplyMigrationsAsync(SensorMonitoringContext context)
        {
            Console.WriteLine("Applying migrations...");
            try
            {
                await context.Database.MigrateAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Migrations applied successfully.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error applying migrations: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task ListPendingMigrationsAsync(SensorMonitoringContext context)
        {
            Console.WriteLine("Pending migrations:");
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

            if (!pendingMigrations.Any())
            {
                Console.WriteLine("No pending migrations found.");
                return;
            }

            foreach (var migration in pendingMigrations)
            {
                Console.WriteLine($"- {migration}");
            }
        }

        private static async Task ListAppliedMigrationsAsync(SensorMonitoringContext context)
        {
            Console.WriteLine("Applied migrations:");
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            if (!appliedMigrations.Any())
            {
                Console.WriteLine("No applied migrations found.");
                return;
            }

            foreach (var migration in appliedMigrations)
            {
                Console.WriteLine($"- {migration}");
            }
        }
    }
}

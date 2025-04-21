using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Data
{
    public class SensorMonitoringContext : DbContext
    {
        public SensorMonitoringContext(DbContextOptions<SensorMonitoringContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Sensor> Sensors { get; set; } = null!;
        public DbSet<SensorFirmware> SensorFirmwares { get; set; } = null!;
        public DbSet<Measurand> Measurands { get; set; } = null!;
        public DbSet<Maintenance> Maintenances { get; set; } = null!;
        public DbSet<Measurement> Measurements { get; set; } = null!;
        public DbSet<Incident> Incidents { get; set; } = null!;
        public DbSet<IncidentMeasurement> IncidentMeasurements { get; set; } = null!;
        public DbSet<Configuration> Configurations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("role");
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.RoleName).HasColumnName("role_name").IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("user");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100);
                entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100);
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
                entity.Property(e => e.PasswordSalt).HasColumnName("password_salt").HasMaxLength(255);

                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Measurand>(entity =>
            {
                entity.ToTable("measurand");
                entity.HasKey(e => e.MeasurandId);
                entity.Property(e => e.MeasurandId).HasColumnName("measurand_id");
                entity.Property(e => e.QuantityType).HasColumnName("quantity_type").HasMaxLength(100);
                entity.Property(e => e.QuantityName).HasColumnName("quantity_name").HasMaxLength(100);
                entity.Property(e => e.Symbol).HasColumnName("symbol").HasMaxLength(20);
                entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(50);
            });

            modelBuilder.Entity<Sensor>(entity =>
            {
                entity.ToTable("sensor");
                entity.HasKey(e => e.SensorId);
                entity.Property(e => e.SensorId).HasColumnName("sensor_id");
                entity.Property(e => e.SensorType).HasColumnName("sensor_type").HasMaxLength(100);
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
                entity.Property(e => e.DeploymentDate).HasColumnName("deployment_date");
                entity.Property(e => e.MeasurandId).HasColumnName("measurand_id");

                entity.HasOne(s => s.Measurand)
                      .WithMany(m => m.Sensors)
                      .HasForeignKey(s => s.MeasurandId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Configuration>(entity =>
            {
                entity.ToTable("configuration");
                entity.HasKey(e => e.SensorId);
                entity.Property(e => e.SensorId).HasColumnName("sensor_id");
                entity.Property(e => e.Latitude).HasColumnName("latitude");
                entity.Property(e => e.Longitude).HasColumnName("longitude");
                entity.Property(e => e.Altitude).HasColumnName("altitude");
                entity.Property(e => e.Orientation).HasColumnName("orientation");
                entity.Property(e => e.MeasurementFrequency).HasColumnName("measurment_frequency");
                entity.Property(e => e.MinThreshold).HasColumnName("min_threshold");
                entity.Property(e => e.MaxThreshold).HasColumnName("max_threshold");

                entity.HasOne(c => c.Sensor)
                      .WithOne(s => s.Configuration)
                      .HasForeignKey<Configuration>(c => c.SensorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SensorFirmware>(entity =>
            {
                entity.ToTable("sensor_firmware");
                entity.HasKey(e => e.SensorId);
                entity.Property(e => e.SensorId).HasColumnName("sensor_id");
                entity.Property(e => e.FirmwareVersion).HasColumnName("firmware_version").HasMaxLength(50);
                entity.Property(e => e.LastUpdateDate).HasColumnName("last_update_date");

                entity.HasOne(f => f.Sensor)
                      .WithOne(s => s.Firmware)
                      .HasForeignKey<SensorFirmware>(f => f.SensorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Measurement>(entity =>
            {
                entity.ToTable("measurement");
                entity.HasKey(e => e.MeasurementId);
                entity.Property(e => e.MeasurementId).HasColumnName("measurement_id");
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.Value).HasColumnName("value");
                entity.Property(e => e.SensorId).HasColumnName("sensor_id");

                entity.HasOne(m => m.Sensor)
                      .WithMany(s => s.Measurements)
                      .HasForeignKey(m => m.SensorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Maintenance>(entity =>
            {
                entity.ToTable("maintenance");
                entity.HasKey(e => e.MaintenanceId);
                entity.Property(e => e.MaintenanceId).HasColumnName("maintenance_id");
                entity.Property(e => e.SensorId).HasColumnName("sensor_id");
                entity.Property(e => e.MaintenanceDate).HasColumnName("maintenance_date");
                entity.Property(e => e.MaintainerId).HasColumnName("maintainer_id");
                entity.Property(e => e.MaintainerComments).HasColumnName("maintainer_comments");

                entity.HasOne(m => m.Sensor)
                      .WithMany(s => s.Maintenances)
                      .HasForeignKey(m => m.SensorId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Maintainer)
                      .WithMany(u => u.Maintenances)
                      .HasForeignKey(m => m.MaintainerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Incident>(entity =>
            {
                entity.ToTable("incident");
                entity.HasKey(e => e.IncidentId);
                entity.Property(e => e.IncidentId).HasColumnName("incident_id");
                entity.Property(e => e.ResponderId).HasColumnName("responder_id");
                entity.Property(e => e.ResponderComments).HasColumnName("responder_comments");
                entity.Property(e => e.ResolvedDate).HasColumnName("resolved_date");
                entity.Property(e => e.Priority).HasColumnName("priority").HasMaxLength(50);

                entity.HasOne(i => i.Responder)
                      .WithMany(u => u.RespondedIncidents)
                      .HasForeignKey(i => i.ResponderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<IncidentMeasurement>(entity =>
            {
                entity.ToTable("incident_measurement");
                entity.HasKey(e => new { e.MeasurementId, e.IncidentId });
                entity.Property(e => e.MeasurementId).HasColumnName("measurement_id");
                entity.Property(e => e.IncidentId).HasColumnName("incident_id");

                entity.HasOne(im => im.Measurement)
                      .WithMany(m => m.IncidentMeasurements)
                      .HasForeignKey(im => im.MeasurementId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(im => im.Incident)
                      .WithMany(i => i.IncidentMeasurements)
                      .HasForeignKey(im => im.IncidentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Models;
using System.IO;
using System.Security.Cryptography.X509Certificates;

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
        public DbSet<ConfigurationSetting> ConfigurationSettings { get; set; } = null!;
        public DbSet<Maintenance> Maintenances { get; set; } = null!;
        public DbSet<PhysicalQuantity> PhysicalQuantities { get; set; } = null!;
        public DbSet<Measurement> Measurements { get; set; } = null!;
        public DbSet<Incident> Incidents { get; set; } = null!;
        public DbSet<IncidentMeasurement> IncidentMeasurements { get; set; } = null!;
        public DbSet<AccessPrivilege> AccessPrivileges { get; set; } = null!;
        public DbSet<RolePrivilege> RolePrivileges { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("role");
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.RoleName).HasColumnName("role_name").IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("user");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100);
                entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100);
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
                entity.Property(e => e.RoleId).HasColumnName("role_id").IsRequired();
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
                entity.Property(e => e.PasswordSalt).HasColumnName("password_salt").HasMaxLength(255);

                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Sensor>(entity =>
            {
                entity.ToTable("sensor");
                entity.HasKey(e => e.SensorId);
                entity.Property(e => e.SensorId).HasColumnName("sensor_id");
                entity.Property(e => e.SensorType).HasColumnName("sensor_type").HasMaxLength(100);
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
                entity.Property(e => e.DeploymentDate).HasColumnName("deployment_date");
            });

            modelBuilder.Entity<ConfigurationSetting>(entity =>
            {
                entity.ToTable("configuration_setting");
                entity.HasKey(e => e.SettingId);
                entity.Property(e => e.SettingId).HasColumnName("setting_id");
                entity.Property(e => e.SensorId).HasColumnName("sensor_id");
                entity.Property(e => e.SettingName).HasColumnName("setting_name").HasMaxLength(100);
                entity.Property(e => e.MinimumValue).HasColumnName("minimum_value");
                entity.Property(e => e.MaximumValue).HasColumnName("maximum_value");
                entity.Property(e => e.CurrentValue).HasColumnName("current_value");

                entity.HasOne(c => c.Sensor)
                      .WithMany(s => s.ConfigurationSettings)
                      .HasForeignKey(c => c.SensorId)
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

            modelBuilder.Entity<PhysicalQuantity>(entity =>
            {
                entity.ToTable("physical_quantity");
                entity.HasKey(e => e.QuantityId);
                entity.Property(e => e.QuantityId).HasColumnName("quantity_id");
                entity.Property(e => e.SensorId).HasColumnName("sensor_id");
                entity.Property(e => e.LowerWarningThreshold).HasColumnName("lower_warning_threshold");
                entity.Property(e => e.UpperWarningThreshold).HasColumnName("upper_warning_threshold");
                entity.Property(e => e.LowerEmergencyThreshold).HasColumnName("lower_emergency_threshold");
                entity.Property(e => e.UpperEmergencyThreshold).HasColumnName("upper_emergency_threshold");
                entity.Property(e => e.QuantityName).HasColumnName("quantity_name").HasMaxLength(100);

                entity.HasOne(p => p.Sensor)
                      .WithMany(s => s.PhysicalQuantities)
                      .HasForeignKey(p => p.SensorId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Measurement>(entity =>
            {
                entity.ToTable("measurement");
                entity.HasKey(e => e.MeasurementId);
                entity.Property(e => e.MeasurementId).HasColumnName("measurement_id");
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.UnitOfMeasurement).HasColumnName("unit_of_measurement").HasMaxLength(50);
                entity.Property(e => e.Value).HasColumnName("value");
                entity.Property(e => e.QuantityId).HasColumnName("quantity_id");

                entity.HasOne(m => m.PhysicalQuantity)
                      .WithMany(q => q.Measurements)
                      .HasForeignKey(m => m.QuantityId)
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
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
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

            modelBuilder.Entity<AccessPrivilege>(entity =>
            {
                entity.ToTable("access_privilege");
                entity.HasKey(e => e.AccessPrivilegeId);
                entity.Property(e => e.AccessPrivilegeId).HasColumnName("access_privilege_id");
                entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
                entity.Property(e => e.ModuleName).HasColumnName("module_name").HasMaxLength(100);
            });

            modelBuilder.Entity<RolePrivilege>(entity => 
            {
                entity.ToTable("role_privilege");
                entity.HasKey(e => new { e.RoleId, e.AccessPrivilegeId });
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.AccessPrivilegeId).HasColumnName("access_privilege_id");

                entity.HasOne(rp => rp.Role)
                      .WithMany(r => r.RolePrivileges)
                      .HasForeignKey(rp => rp.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.AccessPrivilege)
                      .WithMany(ap => ap.RolePrivileges)
                      .HasForeignKey(rp => rp.AccessPrivilegeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

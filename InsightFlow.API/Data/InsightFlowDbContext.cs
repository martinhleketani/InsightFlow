using InsightFlow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace InsightFlow.API.Data
{
    public class InsightFlowDbContext : DbContext
    {
        public InsightFlowDbContext(
            DbContextOptions<InsightFlowDbContext> options)
            : base(options)
        {
        }

        // DATABASE TABLES
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<DailyRecord> DailyRecords { get; set; }
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // DEPARTMENTS
            // =====================================================
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.DepartmentId);

                entity.Property(e => e.DepartmentId)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.DepartmentName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.DepartmentCode)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.HasIndex(e => e.DepartmentName)
                    .IsUnique();

                entity.HasIndex(e => e.DepartmentCode)
                    .IsUnique();
            });

            // =====================================================
            // EMPLOYEES
            // =====================================================
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.AccountId);

                // BIGINT AUTO_INCREMENT in MySQL
                entity.Property(e => e.AccountId)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.EmployeeId)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(e => e.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.CompanyEmail)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.JobTitle)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.Role)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                // Employee ID must be unique
                entity.HasIndex(e => e.EmployeeId)
                    .IsUnique();

                // Company email must be unique
                entity.HasIndex(e => e.CompanyEmail)
                    .IsUnique();

                entity.HasIndex(e => e.DepartmentId);

                // Department → Employees
                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Employees)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =====================================================
            // DAILY RECORDS
            // =====================================================
            modelBuilder.Entity<DailyRecord>(entity =>
            {
                entity.HasKey(e => e.RecordId);

                // BIGINT AUTO_INCREMENT
                entity.Property(e => e.RecordId)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ActivityType)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(e => e.Reference)
                    .HasMaxLength(255);

                entity.Property(e => e.BusinessValue)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.BusinessImpact)
                    .HasMaxLength(100);

                entity.Property(e => e.Notes)
                    .HasMaxLength(2000);

                // Helpful indexes for large amounts of records
                entity.HasIndex(e => e.EmployeeAccountId);

                entity.HasIndex(e => e.RecordDate);

                entity.HasIndex(e => e.Status);

                // Employee → Daily Records
                entity.HasOne(e => e.Employee)
                    .WithMany(e => e.DailyRecords)
                    .HasForeignKey(e => e.EmployeeAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =====================================================
            // PASSWORD RESET REQUESTS
            // =====================================================
            modelBuilder.Entity<PasswordResetRequest>(entity =>
            {
                entity.HasKey(e => e.ResetRequestId);

                // BIGINT AUTO_INCREMENT
                entity.Property(e => e.ResetRequestId)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.TokenHash)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(e => e.EmployeeAccountId);

                entity.HasIndex(e => e.TokenHash);

                entity.HasIndex(e => e.ExpiresAt);

                // Employee → Password Reset Requests
                entity.HasOne(e => e.Employee)
                    .WithMany(e => e.PasswordResetRequests)
                    .HasForeignKey(e => e.EmployeeAccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================================
            // AUDIT LOGS
            // =====================================================
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.AuditLogId);

                // BIGINT AUTO_INCREMENT
                entity.Property(e => e.AuditLogId)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Action)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.EntityType)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Details)
                    .HasMaxLength(4000);

                entity.HasIndex(e => e.EmployeeAccountId);

                entity.HasIndex(e => e.CreatedAt);

                entity.HasIndex(e => e.Action);

                // Employee → Audit Logs
                entity.HasOne(e => e.Employee)
                    .WithMany(e => e.AuditLogs)
                    .HasForeignKey(e => e.EmployeeAccountId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
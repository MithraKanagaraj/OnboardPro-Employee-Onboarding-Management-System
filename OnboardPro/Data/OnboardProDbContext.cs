using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnboardPro.Models;

namespace OnboardPro.Data;

public sealed class OnboardProDbContext(DbContextOptions<OnboardProDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Role> OnboardingRoles => Set<Role>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<TaskDefinition> TaskDefinitions => Set<TaskDefinition>();
    public DbSet<ChecklistTemplateTask> ChecklistTemplateTasks => Set<ChecklistTemplateTask>();
    public DbSet<EmployeeChecklist> EmployeeChecklists => Set<EmployeeChecklist>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetAllocation> AssetAllocations => Set<AssetAllocation>();
    public DbSet<BuddyAssignment> BuddyAssignments => Set<BuddyAssignment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Role>()
            .HasIndex(role => role.Name)
            .IsUnique();

        builder.Entity<Employee>(entity =>
        {
            entity.HasIndex(employee => employee.Email).IsUnique();
            entity.HasIndex(employee => employee.IdentityUserId).IsUnique();
            entity.Property(employee => employee.Email).HasMaxLength(256);
            entity.HasOne(employee => employee.Role)
                .WithMany(role => role.Employees)
                .HasForeignKey(employee => employee.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(employee => employee.IdentityUser)
                .WithOne(user => user.Employee)
                .HasForeignKey<Employee>(employee => employee.IdentityUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ChecklistTemplate>()
            .HasOne(template => template.Role)
            .WithMany()
            .HasForeignKey(template => template.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ChecklistTemplateTask>(entity =>
        {
            entity.HasKey(link => new { link.ChecklistTemplateId, link.TaskDefinitionId });
            entity.HasOne(link => link.ChecklistTemplate)
                .WithMany(template => template.TemplateTasks)
                .HasForeignKey(link => link.ChecklistTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.TaskDefinition)
                .WithMany(task => task.TemplateTasks)
                .HasForeignKey(link => link.TaskDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EmployeeChecklist>(entity =>
        {
            entity.HasIndex(checklist => new { checklist.EmployeeId, checklist.ChecklistTemplateId })
                .IsUnique();
            entity.HasOne(checklist => checklist.Employee)
                .WithMany(employee => employee.Checklists)
                .HasForeignKey(checklist => checklist.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(checklist => checklist.ChecklistTemplate)
                .WithMany(template => template.EmployeeChecklists)
                .HasForeignKey(checklist => checklist.ChecklistTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OnboardingTask>(entity =>
        {
            entity.Property(task => task.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(task => task.Employee)
                .WithMany(employee => employee.OnboardingTasks)
                .HasForeignKey(task => task.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(task => task.EmployeeChecklist)
                .WithMany(checklist => checklist.Tasks)
                .HasForeignKey(task => task.EmployeeChecklistId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(task => task.TaskDefinition)
                .WithMany()
                .HasForeignKey(task => task.TaskDefinitionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Document>(entity =>
        {
            entity.Property(document => document.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(document => document.Employee)
                .WithMany(employee => employee.Documents)
                .HasForeignKey(document => document.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(document => document.ReviewedBy)
                .WithMany()
                .HasForeignKey(document => document.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Asset>()
            .HasIndex(asset => asset.SerialNumber)
            .IsUnique();

        builder.Entity<AssetAllocation>(entity =>
        {
            entity.HasIndex(allocation => allocation.AssetId)
                .IsUnique()
                .HasFilter("[ReturnedOn] IS NULL");
            entity.HasOne(allocation => allocation.Employee)
                .WithMany(employee => employee.AssetAllocations)
                .HasForeignKey(allocation => allocation.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(allocation => allocation.Asset)
                .WithMany(asset => asset.Allocations)
                .HasForeignKey(allocation => allocation.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BuddyAssignment>(entity =>
        {
            entity.HasIndex(assignment => assignment.EmployeeId)
                .IsUnique()
                .HasFilter("[EndedOn] IS NULL");
            entity.HasOne(assignment => assignment.Employee)
                .WithMany(employee => employee.BuddyAssignments)
                .HasForeignKey(assignment => assignment.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(assignment => assignment.BuddyEmployee)
                .WithMany(employee => employee.BuddyForAssignments)
                .HasForeignKey(assignment => assignment.BuddyEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_BuddyAssignment_DifferentEmployees",
                "[EmployeeId] <> [BuddyEmployeeId]"));
        });
    }
}

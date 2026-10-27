using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using OnboardPro.Data;
using OnboardPro.Models;
using OnboardPro.Validation;
using OnboardPro.ViewModels;
using OnboardPro.Services;
using Xunit;

namespace OnboardPro.Tests;

public sealed class OnboardingValidationTests
{
    [Fact]
    public void Return_date_before_allocation_date_is_rejected()
    {
        var allocation = new AssetAllocationFormViewModel
        {
            AllocatedOn = new DateOnly(2026, 10, 10),
            ReturnedOn = new DateOnly(2026, 10, 9)
        };

        Assert.Contains(allocation.Validate(new ValidationContext(allocation)),
            result => result.MemberNames.Contains(nameof(allocation.ReturnedOn)));
    }

    [Fact]
    public void Date_not_before_attribute_rejects_an_earlier_date()
    {
        var allocation = new AssetAllocation
        {
            AllocatedOn = new DateOnly(2026, 10, 10),
            ReturnedOn = new DateOnly(2026, 10, 9)
        };
        var context = new ValidationContext(allocation) { MemberName = nameof(AssetAllocation.ReturnedOn) };
        var attribute = new DateNotBeforeAttribute(nameof(AssetAllocation.AllocatedOn));

        Assert.NotNull(attribute.GetValidationResult(allocation.ReturnedOn, context));
    }

    [Fact]
    public void Dashboard_progress_is_zero_for_an_employee_without_tasks()
    {
        var employee = new EmployeeProgressViewModel { TotalTasks = 0, CompletedTasks = 0 };

        Assert.Equal(0, employee.ProgressPercent);
    }

    [Fact]
    public void Employee_email_and_current_asset_allocation_have_unique_database_indexes()
    {
        var options = new DbContextOptionsBuilder<OnboardProDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=OnboardProTests;Trusted_Connection=True")
            .Options;
        using var context = new OnboardProDbContext(options);
        var employeeIndexes = context.Model.FindEntityType(typeof(Employee))!.GetIndexes();
        var allocationIndexes = context.Model.FindEntityType(typeof(AssetAllocation))!.GetIndexes();

        Assert.Contains(employeeIndexes, index => index.IsUnique &&
            index.Properties.Count == 1 && index.Properties[0].Name == nameof(Employee.Email));
        Assert.Contains(allocationIndexes, index => index.IsUnique &&
            index.Properties.Count == 1 && index.Properties[0].Name == nameof(AssetAllocation.AssetId) &&
            index.GetFilter() == "[ReturnedOn] IS NULL");
    }

    [Fact]
    public async Task Assigning_a_template_copies_task_values_and_keeps_them_unchanged()
    {
        var options = new DbContextOptionsBuilder<OnboardProDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new OnboardProDbContext(options);
        var role = new Role { Name = "New Joiner" };
        var employee = new Employee
        {
            FirstName = "Sam",
            LastName = "Example",
            Email = "sam@example.test",
            JobTitle = "Engineer",
            Department = "Engineering",
            StartDate = new DateOnly(2026, 11, 1),
            Role = role
        };
        var definition = new TaskDefinition { Title = "Read handbook", Category = "HR", DueOffsetDays = 2 };
        var template = new ChecklistTemplate
        {
            Name = "Engineer onboarding",
            Role = role,
            TemplateTasks = [new ChecklistTemplateTask { TaskDefinition = definition, SortOrder = 1 }]
        };
        context.AddRange(employee, template);
        await context.SaveChangesAsync();

        var service = new ChecklistService(context);
        Assert.True(await service.AssignAsync(employee.Id, template.Id));

        definition.Title = "Updated task title";
        template.Name = "Updated template name";
        await context.SaveChangesAsync();

        var checklist = await context.EmployeeChecklists
            .Include(item => item.Tasks)
            .SingleAsync(item => item.EmployeeId == employee.Id);
        var assignedTask = Assert.Single(checklist.Tasks);
        Assert.Equal("Engineer onboarding", checklist.TemplateNameSnapshot);
        Assert.Equal("Read handbook", assignedTask.TitleSnapshot);
        Assert.Equal(new DateOnly(2026, 11, 3), assignedTask.DueDate);
    }

    [Fact]
    public async Task Template_cannot_be_assigned_to_an_employee_with_a_different_role()
    {
        var options = new DbContextOptionsBuilder<OnboardProDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new OnboardProDbContext(options);
        var employeeRole = new Role { Name = "New Joiner" };
        var templateRole = new Role { Name = "Buddy" };
        var employee = new Employee
        {
            FirstName = "Sam",
            LastName = "Example",
            Email = "sam@example.test",
            JobTitle = "Engineer",
            Department = "Engineering",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Role = employeeRole
        };
        var template = new ChecklistTemplate { Name = "Buddy checklist", Role = templateRole };
        context.AddRange(employee, template);
        await context.SaveChangesAsync();

        var service = new ChecklistService(context);

        Assert.False(await service.AssignAsync(employee.Id, template.Id));
    }
}

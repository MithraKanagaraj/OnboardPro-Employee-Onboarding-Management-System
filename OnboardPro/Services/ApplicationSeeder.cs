using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnboardPro.Data;
using OnboardPro.Models;

namespace OnboardPro.Services;

public static class AppRoles
{
    public const string Hr = "HR";
    public const string ItAdmin = "IT Admin";
    public const string NewJoiner = "New Joiner";
    public const string Buddy = "Buddy";
    public static readonly string[] All = [Hr, ItAdmin, NewJoiner, Buddy];
}

public static class ApplicationSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OnboardProDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var identityRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                EnsureSucceeded(identityRoleResult, $"Could not create Identity role {roleName}.");
            }

            if (!await db.OnboardingRoles.AnyAsync(role => role.Name == roleName))
                db.OnboardingRoles.Add(new Role { Name = roleName });
        }
        await db.SaveChangesAsync();

        var adminEmail = configuration["Onboarding:BootstrapAdminEmail"];
        var adminPassword = configuration["Onboarding:BootstrapAdminPassword"];
        if (!string.IsNullOrWhiteSpace(adminEmail) || !string.IsNullOrWhiteSpace(adminPassword))
        {
            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                throw new InvalidOperationException("Set both bootstrap admin email and password, or leave both empty.");

            var user = await userManager.FindByEmailAsync(adminEmail);
            if (user is null)
            {
                user = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var createResult = await userManager.CreateAsync(user, adminPassword);
                EnsureSucceeded(createResult, "Could not create the configured bootstrap administrator.");
            }
            if (!await userManager.IsInRoleAsync(user, AppRoles.Hr))
            {
                var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Hr);
                EnsureSucceeded(roleResult, "Could not assign the HR role to the bootstrap administrator.");
            }
        }

        if (configuration.GetValue<bool>("Onboarding:SeedDemoData"))
            await SeedDemoDataAsync(db);

        logger.LogInformation("OnboardPro roles and configured development data are ready.");
    }

    private static async Task SeedDemoDataAsync(OnboardProDbContext db)
    {
        if (await db.Employees.AnyAsync())
            return;

        var newJoinerRoleId = await db.OnboardingRoles.Where(role => role.Name == AppRoles.NewJoiner)
            .Select(role => role.Id).SingleAsync();
        var buddyRoleId = await db.OnboardingRoles.Where(role => role.Name == AppRoles.Buddy)
            .Select(role => role.Id).SingleAsync();
        var joiner = new Employee
        {
            FirstName = "Alex",
            LastName = "Morgan",
            Email = "alex.morgan@example.test",
            JobTitle = "Software Engineer",
            Department = "Engineering",
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            RoleId = newJoinerRoleId
        };
        var buddy = new Employee
        {
            FirstName = "Taylor",
            LastName = "Reed",
            Email = "taylor.reed@example.test",
            JobTitle = "Senior Software Engineer",
            Department = "Engineering",
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            RoleId = buddyRoleId
        };

        var definitions = new[]
        {
            new TaskDefinition { Title = "Complete employee profile", Category = "HR", DueOffsetDays = 1 },
            new TaskDefinition { Title = "Review employee handbook", Category = "HR", DueOffsetDays = 3 },
            new TaskDefinition { Title = "Prepare workstation", Category = "IT", DueOffsetDays = 0 }
        };
        var template = new ChecklistTemplate
        {
            Name = "Software Engineer Onboarding",
            Description = "A starter checklist for engineering new joiners.",
            RoleId = newJoinerRoleId,
            TemplateTasks = definitions.Select((task, index) => new ChecklistTemplateTask
            {
                TaskDefinition = task,
                SortOrder = index + 1
            }).ToList()
        };
        var assignment = new BuddyAssignment
        {
            Employee = joiner,
            BuddyEmployee = buddy
        };

        db.Employees.AddRange(joiner, buddy);
        db.TaskDefinitions.AddRange(definitions);
        db.ChecklistTemplates.Add(template);
        db.BuddyAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var employeeChecklist = new EmployeeChecklist
        {
            EmployeeId = joiner.Id,
            ChecklistTemplateId = template.Id,
            TemplateNameSnapshot = template.Name,
            AssignedAtUtc = DateTime.UtcNow
        };
        foreach (var definition in definitions)
        {
            employeeChecklist.Tasks.Add(new OnboardingTask
            {
                EmployeeId = joiner.Id,
                TaskDefinitionId = definition.Id,
                TitleSnapshot = definition.Title,
                DescriptionSnapshot = definition.Description,
                CategorySnapshot = definition.Category,
                IsRequiredSnapshot = definition.IsRequired,
                AssignedAtUtc = employeeChecklist.AssignedAtUtc,
                DueDate = joiner.StartDate.AddDays(definition.DueOffsetDays)
            });
        }
        db.EmployeeChecklists.Add(employeeChecklist);
        db.Assets.AddRange(
            new Asset { Name = "Laptop", Category = "Computer", SerialNumber = "DEMO-LAP-001" },
            new Asset { Name = "Monitor", Category = "Display", SerialNumber = "DEMO-MON-001" });
        await db.SaveChangesAsync();
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"{message} {string.Join("; ", result.Errors.Select(error => error.Description))}");
    }
}

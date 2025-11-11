using Microsoft.AspNetCore.Identity;
using BCSApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BCSApp.Data
{
    public static class SeedData
    {
        public static async System.Threading.Tasks.Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            string[] roles = { "Admin", "ProjectManager", "Contractor", "Client" };
            foreach (string role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed default users
            await SeedDefaultUsers(userManager, context);

            // Seed additional data
            await SeedAdditionalData(context, userManager);

            await context.SaveChangesAsync();
        }

        private static async System.Threading.Tasks.Task SeedDefaultUsers(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            // Admin user
            var adminUser = await userManager.FindByEmailAsync("admin@majoko.co.za");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@majoko.co.za",
                    Email = "admin@majoko.co.za",
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = "Admin",
                    Company = "Dr Majoko Projects (Pty) Ltd",
                    PhoneNumber = "+27123456789",
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Project Manager user
            var pmUser = await userManager.FindByEmailAsync("pm@majoko.co.za");
            if (pmUser == null)
            {
                pmUser = new ApplicationUser
                {
                    UserName = "pm@majoko.co.za",
                    Email = "pm@majoko.co.za",
                    FirstName = "John",
                    LastName = "Manager",
                    Role = "ProjectManager",
                    Company = "Dr Majoko Projects (Pty) Ltd",
                    PhoneNumber = "+27123456790",
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(pmUser, "Pm123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(pmUser, "ProjectManager");
                }
            }

            // Contractor user
            var contractorUser = await userManager.FindByEmailAsync("contractor@majoko.co.za");
            if (contractorUser == null)
            {
                contractorUser = new ApplicationUser
                {
                    UserName = "contractor@majoko.co.za",
                    Email = "contractor@majoko.co.za",
                    FirstName = "Mike",
                    LastName = "Builder",
                    Role = "Contractor",
                    Company = "BuildRight Construction",
                    PhoneNumber = "+27123456791",
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(contractorUser, "Contractor123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(contractorUser, "Contractor");
                }
            }

            // Client user
            var clientUser = await userManager.FindByEmailAsync("client@example.com");
            if (clientUser == null)
            {
                clientUser = new ApplicationUser
                {
                    UserName = "client@example.com",
                    Email = "client@example.com",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Role = "Client",
                    Company = "Johnson Properties",
                    PhoneNumber = "+27123456792",
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(clientUser, "Client123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(clientUser, "Client");
                }
            }

            await context.SaveChangesAsync();
        }

        private static async System.Threading.Tasks.Task SeedAdditionalData(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // Get users
            var adminUser = await userManager.FindByEmailAsync("admin@majoko.co.za");
            var pmUser = await userManager.FindByEmailAsync("pm@majoko.co.za");
            var contractorUser = await userManager.FindByEmailAsync("contractor@majoko.co.za");
            var clientUser = await userManager.FindByEmailAsync("client@example.com");

            // Seed Projects (if none exist)
            if (!context.Projects.Any())
            {
                var projects = new[]
                {
                    new Project
                    {
                        Name = "Johannesburg Commercial Complex",
                        Description = "Construction of a 15-story commercial office building in Sandton",
                        Location = "Sandton, Johannesburg",
                        StartDate = DateTime.Now.AddDays(-60),
                        EndDate = DateTime.Now.AddDays(180),
                        Budget = 25000000m,
                        Status = "In Progress",
                        ProjectManagerId = pmUser.Id,
                        ClientId = clientUser.Id,
                        CreatedAt = DateTime.Now.AddDays(-60),
                        UpdatedAt = DateTime.Now.AddDays(-10)
                    },
                    new Project
                    {
                        Name = "Cape Town Residential Development",
                        Description = "Construction of luxury apartments in Sea Point",
                        Location = "Sea Point, Cape Town",
                        StartDate = DateTime.Now.AddDays(-30),
                        EndDate = DateTime.Now.AddDays(240),
                        Budget = 18000000m,
                        Status = "Planning",
                        ProjectManagerId = pmUser.Id,
                        ClientId = clientUser.Id,
                        CreatedAt = DateTime.Now.AddDays(-30),
                        UpdatedAt = DateTime.Now.AddDays(-5)
                    }
                };

                await context.Projects.AddRangeAsync(projects);
                await context.SaveChangesAsync();
            }

            // Get projects for relationships
            var projectsList = await context.Projects.ToListAsync();
            var jhbProject = projectsList.First(p => p.Name.Contains("Johannesburg"));

            // Seed SIMPLE Tasks (avoiding the naming conflict)
            if (!context.Tasks.Any())
            {
                // Use fully qualified name to avoid conflict
                var tasks = new[]
                {
                    new BCSApp.Models.Task
                    {
                        Title = "Site Excavation",
                        Description = "Complete site excavation work",
                        DueDate = DateTime.Now.AddDays(7),
                        Status = "In Progress",
                        Priority = "High",
                        EstimatedCost = 450000m,
                        AssignedToId = contractorUser.Id,
                        CreatedById = pmUser.Id,
                        CreatedAt = DateTime.Now.AddDays(-50),
                        UpdatedAt = DateTime.Now.AddDays(-5)
                    },
                    new BCSApp.Models.Task
                    {
                        Title = "Foundation Work",
                        Description = "Pour concrete foundation",
                        DueDate = DateTime.Now.AddDays(14),
                        Status = "Pending",
                        Priority = "High",
                        EstimatedCost = 320000m,
                        AssignedToId = contractorUser.Id,
                        CreatedById = pmUser.Id,
                        CreatedAt = DateTime.Now.AddDays(-40),
                        UpdatedAt = DateTime.Now.AddDays(-40)
                    }
                };

                await context.Tasks.AddRangeAsync(tasks);
                await context.SaveChangesAsync();
            }

            // Seed SIMPLE Maintenance Requests
            if (!context.MaintenanceRequests.Any())
            {
                var maintenanceRequests = new[]
                {
                    new MaintenanceRequest
                    {
                        Title = "Roof Leak Repair",
                        Description = "Fix roof leakage issue",
                        Location = "Main Building",
                        PropertyType = "Commercial",
                        Status = "In Progress",
                        Priority = "High",
                        DueDate = DateTime.Now.AddDays(3),
                        ClientId = clientUser.Id,
                        ProjectId = jhbProject.Id,
                        AssignedToId = contractorUser.Id,
                        CreatedAt = DateTime.Now.AddDays(-2),
                        UpdatedAt = DateTime.Now.AddDays(-1)
                    }
                };

                await context.MaintenanceRequests.AddRangeAsync(maintenanceRequests);
                await context.SaveChangesAsync();
            }
        }
    }
}
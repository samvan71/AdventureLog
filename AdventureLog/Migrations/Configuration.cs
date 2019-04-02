namespace AdventureLog.Migrations
{
    using AdventureLog.Models;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<AdventureLog.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(AdventureLog.Models.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.
            SeedRoles(context);
            SeedAdminUser(context);
            SeedPlayerRole(context);
        }

        private void SeedRoles(ApplicationDbContext context)
        {
            if (!context.Roles.Any(r => r.Name == "Admin"))
            {
                var store = new RoleStore<IdentityRole>(context);
                var manager = new RoleManager<IdentityRole>(store);
                var role = new IdentityRole { Name = "Admin" };

                manager.Create(role);

            }
        }

        /// <summary>
        /// test@gmail.com
        /// Test123456!
        /// </summary>
        /// <param name="context"></param>
        private void SeedAdminUser(ApplicationDbContext context)
        {
            if (!context.Users.Any(u => u.UserName == "AdventureLogAdmin"))
            {
                var store = new UserStore<ApplicationUser>(context);
                var manager = new UserManager<ApplicationUser>(store);
                var user = new ApplicationUser
                {
                    UserName = "AdventureLogAdmin",
                    Email = "adventurequestlog@gmail.com"
                };

                manager.Create(user, "Test123456!");
                manager.AddToRole(user.Id, "Admin");
            }
        }

        private void SeedPlayerRole(ApplicationDbContext context)
        {
            if (!context.PlayerRoles.Any(r => r.PlayerRole_PK == (int)PlayerRole.PlayerRoleKey.Player))
            {
                var role = new PlayerRole()
                {
                    PlayerRole_PK = (int)PlayerRole.PlayerRoleKey.Player,
                    RoleName = "Player",
                    RoleDescription = "Player in a game, can comment and view",
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    LastModifiedUser = "SysAdmin"
                };

                context.PlayerRoles.Add(role);
            }

            if (!context.PlayerRoles.Any(r => r.PlayerRole_PK == (int)PlayerRole.PlayerRoleKey.Gamemaster))
            {
                var role = new PlayerRole()
                {
                    PlayerRole_PK = (int)PlayerRole.PlayerRoleKey.Gamemaster,
                    RoleName = "Gamemaster",
                    RoleDescription = "Gamemaster of a game, can control all features of the adventure",
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    LastModifiedUser = "SysAdmin"
                };

                context.PlayerRoles.Add(role);
            }

            context.SaveChanges();
        }
    }
}

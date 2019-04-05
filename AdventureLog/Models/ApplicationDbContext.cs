using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace AdventureLog.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Adventure> Adventures { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerRole> PlayerRoles { get; set; }
        //public DbSet<Hotspot> Hotspots { get; set; }
        //public DbSet<Coordinate> Coordinates { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemNote> ItemNotes { get; set; }
        public DbSet<AdventureNote> AdventureNotes { get; set; }

        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}
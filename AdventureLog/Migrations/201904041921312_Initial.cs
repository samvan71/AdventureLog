namespace AdventureLog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AdventureNotes",
                c => new
                    {
                        AdventureNote_PK = c.Long(nullable: false, identity: true),
                        Adventure_PK = c.Long(nullable: false),
                        UserId_PK = c.String(nullable: false, maxLength: 128),
                        ParentAdventureNote_PK = c.Long(),
                        Text = c.String(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        LastModifiedDate = c.DateTime(nullable: false),
                        LastModifiedUser = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.AdventureNote_PK)
                .ForeignKey("dbo.Adventures", t => t.Adventure_PK, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId_PK, cascadeDelete: true)
                .ForeignKey("dbo.AdventureNotes", t => t.ParentAdventureNote_PK)
                .Index(t => t.Adventure_PK)
                .Index(t => t.UserId_PK)
                .Index(t => t.ParentAdventureNote_PK);
            
            CreateTable(
                "dbo.Adventures",
                c => new
                    {
                        Adventure_PK = c.Long(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 256),
                        SystemName = c.String(maxLength: 256),
                        ShortDescription = c.String(maxLength: 4000),
                        Description = c.String(),
                        InvitePassword = c.String(maxLength: 100),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        LastModifiedDate = c.DateTime(nullable: false),
                        LastModifiedUser = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Adventure_PK);
            
            CreateTable(
                "dbo.Items",
                c => new
                    {
                        Item_PK = c.Long(nullable: false, identity: true),
                        Adventure_PK = c.Long(nullable: false),
                        ParentItem_PK = c.Long(),
                        Name = c.String(nullable: false, maxLength: 256),
                        ShortDescription = c.String(maxLength: 4000),
                        Description = c.String(),
                        MapFileName = c.String(),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        LastModifiedDate = c.DateTime(nullable: false),
                        LastModifiedUser = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Item_PK)
                .ForeignKey("dbo.Items", t => t.ParentItem_PK)
                .ForeignKey("dbo.Adventures", t => t.Adventure_PK, cascadeDelete: true)
                .Index(t => t.Adventure_PK)
                .Index(t => t.ParentItem_PK);
            
            CreateTable(
                "dbo.ItemNotes",
                c => new
                    {
                        ItemNote_PK = c.Long(nullable: false, identity: true),
                        Item_PK = c.Long(nullable: false),
                        UserId_PK = c.String(nullable: false, maxLength: 128),
                        ParentItemNote_PK = c.Long(),
                        Text = c.String(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        LastModifiedDate = c.DateTime(nullable: false),
                        LastModifiedUser = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.ItemNote_PK)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId_PK, cascadeDelete: true)
                .ForeignKey("dbo.ItemNotes", t => t.ParentItemNote_PK)
                .ForeignKey("dbo.Items", t => t.Item_PK, cascadeDelete: true)
                .Index(t => t.Item_PK)
                .Index(t => t.UserId_PK)
                .Index(t => t.ParentItemNote_PK);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        JoinDate = c.DateTime(nullable: false),
                        LastLogin = c.DateTime(nullable: false),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.Players",
                c => new
                    {
                        Player_PK = c.Long(nullable: false, identity: true),
                        UserId_PK = c.String(nullable: false, maxLength: 128),
                        Adventure_PK = c.Long(nullable: false),
                        PlayerRole_PK = c.Long(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Player_PK)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId_PK, cascadeDelete: true)
                .ForeignKey("dbo.PlayerRoles", t => t.PlayerRole_PK, cascadeDelete: true)
                .ForeignKey("dbo.Adventures", t => t.Adventure_PK, cascadeDelete: true)
                .Index(t => t.UserId_PK)
                .Index(t => t.Adventure_PK)
                .Index(t => t.PlayerRole_PK);
            
            CreateTable(
                "dbo.PlayerRoles",
                c => new
                    {
                        PlayerRole_PK = c.Long(nullable: false, identity: true),
                        RoleName = c.String(nullable: false, maxLength: 128),
                        RoleDescription = c.String(nullable: false, maxLength: 256),
                        CreatedDate = c.DateTime(nullable: false),
                        LastModifiedDate = c.DateTime(nullable: false),
                        LastModifiedUser = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.PlayerRole_PK);
            
            CreateTable(
                "dbo.Coordinates",
                c => new
                    {
                        Coordinate_PK = c.Long(nullable: false, identity: true),
                        Hotspot_PK = c.Long(nullable: false),
                        XCoord = c.Int(nullable: false),
                        YCoord = c.Int(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        LastModifiedDate = c.DateTime(nullable: false),
                        LastModifiedUser = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Coordinate_PK)
                .ForeignKey("dbo.Hotspots", t => t.Hotspot_PK, cascadeDelete: true)
                .Index(t => t.Hotspot_PK);
            
            CreateTable(
                "dbo.Hotspots",
                c => new
                    {
                        Hotspot_PK = c.Long(nullable: false, identity: true),
                        IsActive = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        LastModifiedDate = c.DateTime(nullable: false),
                        LastModifiedUser = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Hotspot_PK);
            
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.Coordinates", "Hotspot_PK", "dbo.Hotspots");
            DropForeignKey("dbo.AdventureNotes", "ParentAdventureNote_PK", "dbo.AdventureNotes");
            DropForeignKey("dbo.AdventureNotes", "UserId_PK", "dbo.AspNetUsers");
            DropForeignKey("dbo.Players", "Adventure_PK", "dbo.Adventures");
            DropForeignKey("dbo.Players", "PlayerRole_PK", "dbo.PlayerRoles");
            DropForeignKey("dbo.Players", "UserId_PK", "dbo.AspNetUsers");
            DropForeignKey("dbo.Items", "Adventure_PK", "dbo.Adventures");
            DropForeignKey("dbo.ItemNotes", "Item_PK", "dbo.Items");
            DropForeignKey("dbo.ItemNotes", "ParentItemNote_PK", "dbo.ItemNotes");
            DropForeignKey("dbo.ItemNotes", "UserId_PK", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Items", "ParentItem_PK", "dbo.Items");
            DropForeignKey("dbo.AdventureNotes", "Adventure_PK", "dbo.Adventures");
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropIndex("dbo.Coordinates", new[] { "Hotspot_PK" });
            DropIndex("dbo.Players", new[] { "PlayerRole_PK" });
            DropIndex("dbo.Players", new[] { "Adventure_PK" });
            DropIndex("dbo.Players", new[] { "UserId_PK" });
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.ItemNotes", new[] { "ParentItemNote_PK" });
            DropIndex("dbo.ItemNotes", new[] { "UserId_PK" });
            DropIndex("dbo.ItemNotes", new[] { "Item_PK" });
            DropIndex("dbo.Items", new[] { "ParentItem_PK" });
            DropIndex("dbo.Items", new[] { "Adventure_PK" });
            DropIndex("dbo.AdventureNotes", new[] { "ParentAdventureNote_PK" });
            DropIndex("dbo.AdventureNotes", new[] { "UserId_PK" });
            DropIndex("dbo.AdventureNotes", new[] { "Adventure_PK" });
            DropTable("dbo.AspNetRoles");
            DropTable("dbo.Hotspots");
            DropTable("dbo.Coordinates");
            DropTable("dbo.PlayerRoles");
            DropTable("dbo.Players");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.ItemNotes");
            DropTable("dbo.Items");
            DropTable("dbo.Adventures");
            DropTable("dbo.AdventureNotes");
        }
    }
}

using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using AdventureLog.Models;
using System.Data.Entity;
using System.Collections.Generic;
using System.Security.Principal;

namespace AdventureLog.Controllers
{
    /// <summary>
    /// Primary controller for managing adventures
    /// Index Route: ~/adventure
    /// </summary>
    public class AdventureHomeController : Controller
    {
        public AdventureHomeController()
        {
        }

        #region Index
        // ~/adventure
        [Authorize]
        [Route("adventure", Name ="adventurehome")]
        public ActionResult Index()
        {
            var model = new AdventureHomeModel();
            var view = View(model);

            if (Request.IsAuthenticated)
            {
                User.Identity.GetUserId();

                using (ApplicationDbContext dbContext = new ApplicationDbContext())
                {
                    var currentUserId = User.Identity.GetUserId();

                    var adventures = (from p in dbContext.Players
                                      where p.UserId_PK == currentUserId
                                        && p.IsActive
                                        && p.Adventure.IsActive
                                      select p.Adventure).AsEnumerable();

                    model.Adventures = adventures.ToList();
                }

                // Populate View with Administration messages.
                model.Messages.Add(new KeyValuePair<string, string>("Test", "testing messages"));
            }

            return view;
        }

        #endregion

        #region Adventure Actions

        #region Create

        // Get: Adventure/Create
        [Authorize]
        [Route("adventure/Create")]
        public ActionResult Create()
        {
            var model = new Adventure()
            {
                CreatedDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                LastModifiedUser = User.Identity.GetUserName()
            };
            var view = View("Create", model);

            return View();
        }
        
        // Post: AdventureHome/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("adventure/Create")]
        public ActionResult Create(Adventure model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;

                var newPlayer = new Player()
                {
                    UserId_PK = User.Identity.GetUserId(),
                    Adventure = model,
                    PlayerRole_PK = (int)PlayerRole.PlayerRoleKey.Gamemaster,
                    IsActive = true
                };

                using (var dbContext = new ApplicationDbContext())
                {
                    dbContext.Adventures.Add(model);
                    dbContext.Players.Add(newPlayer);
                    dbContext.SaveChanges();
                }

                return RedirectToAction("Index");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        #endregion

        #region Details

        /// <summary>
        /// Detail view of a specified Adventure
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [Route("adventure/{id:long}")]
        public ActionResult Details(long id)
        {
            Adventure adventure = null;
            ActionResult view = RedirectToAction("Index");

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                adventure = (from a in dbContext.Adventures
                             where a.Adventure_PK == id
                                && a.Players.Where(p => p.UserId_PK == userId).FirstOrDefault().IsActive
                                && a.IsActive
                             select a)
                             // Include Items and areas as they are displayed in cards.
                             .Include(a => a.AdventureNotes.Select(n => n.ApplicationUser))
                             .Include(a => a.AdventureNotes.Select(n => n.ChildNotes))
                             .Include(a => a.Items.Select(w => w.ChildItems))
                             .FirstOrDefault();
            }

            if (adventure != null)
            {
                view = View("Details", adventure);
            }

            return view;
        }

        #endregion

        #region Edit

        /// <summary>
        /// HTTP Get for editing an adventure
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [Route("adventure/{id:long}/Edit")]
        public ActionResult Edit(long id)
        {
            Adventure adventure = null;
            ActionResult view = RedirectToAction("Index");

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                adventure = (from a in dbContext.Adventures
                             where a.Adventure_PK == id
                                && a.Players.Any(p => p.UserId_PK == userId)
                                && (a.Players.FirstOrDefault(p => p.UserId_PK == userId).PlayerRole.PlayerRole_PK
                                        == (long)PlayerRole.PlayerRoleKey.Gamemaster)
                                && a.IsActive
                             select a)
                             // Persist Player Role and Application User for displaying.
                             .Include(a => a.Players.Select(p => p.PlayerRole))
                             .Include(a => a.Players.Select(p => p.ApplicationUser))
                             .FirstOrDefault();
            }

            if (adventure != null)
            {
                view = View("Edit", adventure);
            }
            
            return view;
        }

        /// <summary>
        /// HTTP Post for saving an edited adventure.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Route("adventure/{id:long}/Edit")]
        public ActionResult Edit(Adventure model, long[] activePlayers = null)
        {
            ActionResult result = View(model);

            // If the model is valid, update database, and return to detail view.
            if (ModelState.IsValid)
            {
                using (var dbContext = new ApplicationDbContext())
                {
                    // Update model values
                    model.LastModifiedDate = DateTime.Now;
                    model.LastModifiedUser = User.Identity.GetUserName();
                    
                    // If everthing goes well, update database with listed properties.
                    dbContext.Entry(model).State = EntityState.Modified;

                    // Update active player list.
                    var players = (from p in dbContext.Players
                                   where p.Adventure_PK == model.Adventure_PK
                                   select p).AsEnumerable();

                    foreach (var player in players)
                    {
                        // If there has been a change to the players active status...
                        if (player.IsActive != activePlayers.Any(ap => ap.Equals(player.Player_PK)))
                        {
                            dbContext.Entry(player).Entity.IsActive = !dbContext.Entry(player).Entity.IsActive;
                            dbContext.Entry(player).State = EntityState.Modified;
                        }
                    }


                    // Save Changes
                    dbContext.SaveChanges();
                }

                // Return to the detail view
                result = RedirectToAction("Details", model.Adventure_PK);
            }
            
            return result;
        }

        // Get: adventure/{id}/Invite/{Invite Password}
        [Authorize]
        [Route("{id:long}/Invite/{invitePassword?}")]
        public ActionResult Invite(long id, string invitePassword = "")
        {
            ActionResult result = View();

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                // User must be logged in to use Invite Links
                if (userId != null)
                {
                    Adventure adventure = null;

                    // Find the adventure
                    adventure = (from a in dbContext.Adventures
                                 where a.Adventure_PK == id
                                    && a.IsActive
                                 select a).FirstOrDefault();

                    // If the adventure was found and the password is valid.
                    if (adventure != null
                            && adventure.InvitePassword == invitePassword)
                    {
                        var player = adventure.Players.Where(p => p.UserId_PK == userId).FirstOrDefault();

                        // If the player is disabled or already exists. Update that record.
                        if (player != null)
                        {
                            player.IsActive = true;

                            dbContext.Entry(player).State = EntityState.Modified;
                        }
                        else
                        {
                            // Add the user as a player.
                            var newPlayer = new Player()
                            {
                                UserId_PK = userId,
                                Adventure = adventure,
                                PlayerRole_PK = (int)PlayerRole.PlayerRoleKey.Player,
                                IsActive = true
                            };

                            dbContext.Players.Add(newPlayer);
                        }

                        dbContext.SaveChanges();

                        // Redirect to the details page of the newly joined adventure.
                        result = RedirectToAction("Details", id);
                    }
                }

            }

            return result;
        }

        #endregion

        #region Comments
        [Authorize, HttpPost, ValidateInput(false)]
        public ActionResult CreateComment(long Adventure_PK, string newComment, long? parentComment = null)
        {
            ActionResult result = RedirectToAction("Details", new { id = Adventure_PK });

            if (!string.IsNullOrWhiteSpace(newComment))
            {
                var note = new AdventureNote()
                {
                    Adventure_PK = Adventure_PK,
                    UserId_PK = User.Identity.GetUserId(),
                    ParentAdventureNote_PK = parentComment,
                    Text = newComment,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    LastModifiedUser = User.Identity.GetUserName()
                };

                using (var dbContext = new ApplicationDbContext())
                {
                    dbContext.AdventureNotes.Add(note);
                    dbContext.SaveChanges();
                }
            }

            return result;
        }
        #endregion

        #region Delete Adventure

        [Authorize, HttpPost]
        public ActionResult DeleteAdventure(long adventure_PK)
        {
            ActionResult result = RedirectToAction("Details", new { id = adventure_PK });

            using (var dbContext = new ApplicationDbContext())
            {
                var adventure = (from a in dbContext.Adventures
                                 where a.Adventure_PK == adventure_PK
                                    && a.IsActive
                                 select a).FirstOrDefault();

                adventure.IsActive = false;

                dbContext.Entry(adventure).State = EntityState.Modified;
                dbContext.SaveChanges();
            }

            return result;
        }

        #endregion

        #region Search

        [Authorize, HttpPost]
        public ActionResult AdventureSearch(long adventure_PK, string searchText)
        {
            ActionResult result = RedirectToAction("SearchResults", new { id = adventure_PK });

            using (var dbContext = new ApplicationDbContext())
            {
                var results = (from i in dbContext.Items
                               where i.Adventure_PK == adventure_PK
                                        && i.Name == searchText
                               select i).AsEnumerable();

                if (results.Count() > 1)
                {
                    result = RedirectToAction("ItemDetails", new { id = results.First().Item_PK });
                }
                else
                {
                    result = RedirectToAction("SearchResults", new { id = adventure_PK, results = results});
                }
            }

            return result;
        }

        [HttpGet]
        [Route("adventure/{id:long}/Search/Results")]
        public ActionResult SearchResults(long id, IEnumerable<Item> results = null)
        {
            ActionResult result = null;

            result = View("SearchResults", results);

            return result;
        }

        #endregion

        #endregion

        #region Item Actions

        #region Create

        /// <summary>
        /// HTTP Get.  /adventure/{adventureId}/Item/Create
        /// </summary>
        /// <param name="adventureId"></param>
        [Authorize]
        [Route("adventure/{adventureId:long}/CreateItem/{parentId:long?}", Name ="ItemCreate")]
        public ActionResult ItemCreate(long adventureId, long? parentId)
        {
            Adventure adventure = null;
            ActionResult result = RedirectToAction("Details", adventureId);

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                adventure = (from a in dbContext.Adventures
                             where a.Adventure_PK == adventureId
                                && a.Players.Any(p => p.UserId_PK == userId)
                                && a.IsActive
                                && (a.Players.FirstOrDefault(p => p.UserId_PK == userId).PlayerRole.PlayerRole_PK
                                    == (long)PlayerRole.PlayerRoleKey.Gamemaster)
                             select a).FirstOrDefault();
            }

            if (adventure != null)
            {
                var model = new Item()
                {
                    Adventure = adventure,
                    Adventure_PK = adventure.Adventure_PK,
                    ParentItem_PK = parentId,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    LastModifiedUser = User.Identity.GetUserName()
                };

                result = View("ItemCreate", model);
            }

            return result;
        }

        /// <summary>
        /// HTTP Post for Creating a new Item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [Route("adventure/{adventureId:long}/CreateItem")]
        public ActionResult ItemCreate(Item model)
        {
            ActionResult result = View(model);

            if (ModelState.IsValid)
            {
                model.IsActive = true;

                using (var dbContext = new ApplicationDbContext())
                {
                    dbContext.Items.Add(model);
                    dbContext.SaveChanges();
                }

                if (model.ParentItem_PK != null)
                {
                    result = RedirectToAction("ItemDetails", new { id = model.ParentItem_PK });
                }
                else
                {
                    result = RedirectToAction("Details", new { id = model.Adventure_PK });
                }
            }

            // If we got this far, redisplay the view for corrections.
            return result;
        }

        #endregion

        #region Details
        [Authorize]
        [Route("Item/{id:long}", Name = "ItemDetails")]
        public ActionResult ItemDetails(long id)
        {
            Item Item = null;
            ActionResult result = RedirectToAction("Index");

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                Item = (from w in dbContext.Items
                         where w.Item_PK == id
                            && w.IsActive
                            // Assure that the adventure is one the player can view AND is not deleted.
                            && w.Adventure.IsActive
                            && w.Adventure.Players.Any(p => p.UserId_PK == userId)
                         select w)
                         .Include(w => w.ItemNotes.Select(n => n.ApplicationUser))
                         .Include(w => w.ItemNotes.Select(n => n.ChildNotes))
                         // Load 2 levels of children
                         .Include(w => w.ChildItems.Select(c => c.ChildItems))
                         .FirstOrDefault();
            }

            if (Item != null)
            {
                result = View("ItemDetails", Item);
            }

            return result;
        }
        #endregion

        #region Edit
        [Authorize]
        [Route("Item/{id:long}/Edit", Name="ItemEdit")]
        public ActionResult ItemEdit(long id)
        {
            Item Item = null;
            ActionResult view = RedirectToAction("ItemDetails", id);

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                Item = (from w in dbContext.Items
                             where w.Item_PK == id
                                // Assure that the adventure is one the player can edit AND is not deleted.
                                && w.IsActive
                                && w.Adventure.IsActive
                                && w.Adventure.Players.Any(p => p.UserId_PK == userId)
                                && (w.Adventure.Players.FirstOrDefault(p => p.UserId_PK == userId).PlayerRole.PlayerRole_PK
                                        == (long)PlayerRole.PlayerRoleKey.Gamemaster)
                             select w).FirstOrDefault();
            }

            if (Item != null)
            {
                view = View("ItemEdit", Item);
            }

            return view;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Route("Item/{id:long}/Edit")]
        public ActionResult ItemEdit(Item model)
        {
            ActionResult result = View(model);

            // If the model is valid, update database, and return to detail view.
            if (ModelState.IsValid)
            {
                using (var dbContext = new ApplicationDbContext())
                {
                    // Update model values
                    model.LastModifiedDate = DateTime.Now;
                    model.LastModifiedUser = User.Identity.GetUserName();

                    // If everthing goes well, update database with listed properties.
                    dbContext.Entry(model).State = EntityState.Modified;
                    dbContext.SaveChanges();
                }

                result = RedirectToAction("ItemDetails", model.Item_PK);
            }

            return result;
        }
        #endregion

        #region Comments
        [Authorize, HttpPost, ValidateInput(false)]
        public ActionResult CreateItemComment(long Item_PK, string newComment, long? parentComment = null)
        {
            ActionResult result = RedirectToAction("ItemDetails", new { id = Item_PK });

            if (!string.IsNullOrWhiteSpace(newComment))
            {
                var note = new ItemNote()
                {
                    Item_PK = Item_PK,
                    UserId_PK = User.Identity.GetUserId(),
                    ParentItemNote_PK = parentComment,
                    Text = newComment,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    LastModifiedUser = User.Identity.GetUserName()
                };

                using (var dbContext = new ApplicationDbContext())
                {
                    dbContext.ItemNotes.Add(note);
                    dbContext.SaveChanges();
                }
            }

            return result;
        }
        #endregion

        #region Delete Adventure

        [Authorize, HttpPost]
        public ActionResult DeleteItem(long item_PK, long adventure_pk)
        {
            // If it is successfully deleted, go to the adventure detail page.
            ActionResult result = RedirectToAction("Details", new { id = adventure_pk });

            using (var dbContext = new ApplicationDbContext())
            {
                var item = (from i in dbContext.Items
                            where i.Item_PK == item_PK
                            select i).FirstOrDefault();

                item.IsActive = false;

                dbContext.Entry(item).State = EntityState.Modified;
                dbContext.SaveChanges();
            }

            return result;
        }

        #endregion

        #endregion

        #region Utilities
        public static bool IsPlayer(IIdentity user, long adventure_Pk)
        {
            bool isPlayer = true;
            string userId = user.GetUserId();

            using (var dbContext = new ApplicationDbContext())
            {
                isPlayer = (from p in dbContext.Players
                            where p.UserId_PK == userId
                                && p.Adventure_PK == adventure_Pk
                                && p.PlayerRole_PK == (int)PlayerRole.PlayerRoleKey.Player
                            select p).Any();
            }

            return isPlayer;
        }

        public static bool IsGamemaster(IIdentity user, long adventure_Pk)
        {
            bool isPlayer = true;
            string userId = user.GetUserId();

            using (var dbContext = new ApplicationDbContext())
            {
                isPlayer = (from p in dbContext.Players
                            where p.UserId_PK == userId
                                && p.Adventure_PK == adventure_Pk
                                && p.PlayerRole_PK == (int)PlayerRole.PlayerRoleKey.Gamemaster
                            select p).Any();
            }

            return isPlayer;
        }
        #endregion
    }
}
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
using System.Text.RegularExpressions;

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
        /// <summary>
        /// navigates to the ~/adventure index page.
        /// </summary>
        /// <returns>Index View</returns>
        [Authorize]
        [Route("adventure", Name ="adventurehome")]
        public ActionResult Index()
        {
            // Instantiate the model.
            var model = new AdventureHomeModel();
            // Create the view.
            var view = View(model);

            // Users must be logged in.
            if (Request.IsAuthenticated)
            {
                User.Identity.GetUserId();

                // Get the users adventures.
                using (ApplicationDbContext dbContext = new ApplicationDbContext())
                {
                    var currentUserId = User.Identity.GetUserId();

                    var adventures = (from a in dbContext.Adventures
                                      let player = a.Players.FirstOrDefault(p => p.UserId_PK == currentUserId)
                                      where player.IsActive
                                         && a.IsActive
                                      select a).AsEnumerable();

                    model.Adventures = adventures.ToList();
                }

                // Populate View with Administration messages.
                model.Messages.Add(new KeyValuePair<string, string>(
                    "First Deployment on Azure", 
                    "Adventure Log is now running on Azure!  This is a big step forward for Advnture Log."
                    + "Our next push will be for an official product with the features we wanted to create in Adventure Log."));
            }

            return view;
        }

        #endregion

        #region Adventure Actions

        #region Create
        
        /// <summary>
        /// HTTP Get: for the create adventure page.  Route: ~/adventure/Create
        /// </summary>
        [Authorize]
        [Route("adventure/Create")]
        public ActionResult Create()
        {
            // Create empty adventure with default values.
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
        /// <summary>
        /// HTTP Post for create adventure page.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("adventure/Create")]
        public ActionResult Create(Adventure model)
        {
            // If required fields are filled out.
            if (ModelState.IsValid)
            {
                // Activate the adventure.
                model.IsActive = true;

                // Add the user as the gamemaster of the adventure.
                var newPlayer = new Player()
                {
                    UserId_PK = User.Identity.GetUserId(),
                    Adventure = model,
                    PlayerRole_PK = (int)PlayerRole.PlayerRoleKey.Gamemaster,
                    IsActive = true
                };

                // Update the database.
                using (var dbContext = new ApplicationDbContext())
                {
                    dbContext.Adventures.Add(model);
                    dbContext.Players.Add(newPlayer);
                    dbContext.SaveChanges();
                }

                // Go to the adventure home page.
                return RedirectToAction("Index");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        #endregion

        #region Details

        /// <summary>
        /// Http Get for detail view of a specified Adventure
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [Route("adventure/{id:long}")]
        public ActionResult Details(long id)
        {
            Adventure adventure = null;
            // Display the access invalid view if the adventure could not be found or the user does not have access to view it.
            ActionResult view = View("AccessInvalid");

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                // Get the adventure.
                adventure = (from a in dbContext.Adventures
                             let player = a.Players.FirstOrDefault(p => p.UserId_PK == userId)
                             where a.Adventure_PK == id
                                && ( a.IsPublic || player.IsActive)
                                && a.IsActive
                             select a)
                             // Include Items and areas as they are displayed in cards.
                             .Include(a => a.AdventureNotes.Select(n => n.ApplicationUser))
                             .Include(a => a.AdventureNotes.Select(n => n.ChildNotes))
                             .Include(a => a.Items.Select(w => w.ChildItems))
                             .FirstOrDefault();
            }

            // If the adventure was found.
            if (adventure != null)
            {
                // Display the adventure.
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
            // Display the access invalid view if the adventure could not be found or the user does not have access to edit it.
            ActionResult view = View("AccessInvalid");

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                // Get the adventure.
                adventure = (from a in dbContext.Adventures
                             let player = a.Players.FirstOrDefault(p => p.UserId_PK == userId)
                             where a.Adventure_PK == id
                                && player.IsActive
                                // Assure the user is a gamemaster
                                && player.PlayerRole.PlayerRole_PK == (long)PlayerRole.PlayerRoleKey.Gamemaster
                                && a.IsActive
                             select a)
                             // Persist Player Role and Application User for displaying.
                             .Include(a => a.Players.Select(p => p.PlayerRole))
                             .Include(a => a.Players.Select(p => p.ApplicationUser))
                             .FirstOrDefault();
            }

            // If the adventure was found.
            if (adventure != null)
            {
                // Display the edit view.
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

        #endregion

        #region Invite

        // Get: adventure/{id}/Invite/{Invite Password}
        [Authorize]
        [Route("adventure/{id:long}/Invite/{invitePassword?}")]
        public ActionResult Invite(long id, string invitePassword = "")
        {
            ActionResult result = View("InviteFailed");

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                // User must be logged in to use Invite Links
                if (userId != null)
                {
                    // Find the adventure
                    var adventure = (from a in dbContext.Adventures
                                     where a.Adventure_PK == id
                                        && a.IsActive
                                     select a).FirstOrDefault();

                    // If the adventure was found and the password is valid.
                    if (adventure != null
                            && (string.IsNullOrWhiteSpace(adventure.InvitePassword) 
                                    || string.Equals(adventure.InvitePassword, invitePassword)))
                    {
                        var player = adventure.Players.Where(p => p.UserId_PK == userId).FirstOrDefault();

                        // Add the player if they do not exist.
                        if (player == null)
                        {
                            // Add the user as a new player.
                            var newPlayer = new Player()
                            {
                                UserId_PK = userId,
                                Adventure = adventure,
                                PlayerRole_PK = (int)PlayerRole.PlayerRoleKey.Player
                            };

                            // If the adventure is secured, do not whitelist the player.
                            newPlayer.IsActive = !adventure.IsSecured;

                            dbContext.Players.Add(newPlayer);
                            dbContext.SaveChanges();
                        }

                        if (!adventure.IsSecured || player.IsActive)
                        {
                            // Redirect to the details page of the newly joined adventure.
                            result = RedirectToAction("Details", id);
                        }
                        else
                        {
                            // Redirect to the success page.
                            result = View("InviteSuccess");
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Comments
        /// <summary>
        /// HTTP Post for creating a comment.
        /// </summary>
        [Authorize, HttpPost, ValidateInput(false)]
        public ActionResult CreateComment(long Adventure_PK, string newComment, long? parentComment = null)
        {
            // Return to the detail view of the adventure.
            ActionResult result = RedirectToAction("Details", new { id = Adventure_PK });

            // The comment must contain something.
            if (!string.IsNullOrWhiteSpace(newComment))
            {
                var userId = User.Identity.GetUserId();

                // Create the comment.
                var note = new AdventureNote()
                {
                    Adventure_PK = Adventure_PK,
                    UserId_PK = userId,
                    ParentAdventureNote_PK = parentComment,
                    Text = newComment,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    LastModifiedUser = User.Identity.GetUserName()
                };

                using (var dbContext = new ApplicationDbContext())
                {
                    // Check if the adventure exists that meets the qualifications.
                    var isPlayer = (from a in dbContext.Adventures
                                    let player = a.Players.FirstOrDefault(p => p.UserId_PK == userId)
                                    where a.Adventure_PK == Adventure_PK
                                        && a.IsActive
                                        && player.IsActive
                                    select a).Any();

                    // Add the comment.
                    if (isPlayer)
                    {
                        dbContext.AdventureNotes.Add(note);
                        dbContext.SaveChanges();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// HTTP Post for deleting a comment.
        /// </summary>
        [Authorize, HttpPost]
        public ActionResult DeleteComment(long AdventureNote_PK)
        {
            ActionResult result = RedirectToAction("Index");

            using (var dbContext = new ApplicationDbContext())
            {
                // Get the comment.
                var note = (from n in dbContext.AdventureNotes
                            where n.AdventureNote_PK == AdventureNote_PK
                                && n.IsActive
                            select n).FirstOrDefault();
                
                if (note != null)
                {
                    // If the user is the gamemaster.
                    var isGamemaster = IsInRole(User.Identity, note.Adventure_PK, PlayerRole.PlayerRoleKey.Gamemaster);

                    if (isGamemaster)
                    {
                        // Delete all replies including the deleted comment.
                        DeleteOrphanAdventureNotes(note.AdventureNote_PK, dbContext);
                        dbContext.SaveChanges();
                        result = RedirectToAction("Details", new { id = note.Adventure_PK });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Http Post for editing the comment.
        /// </summary>
        [Authorize, HttpPost, ValidateInput(false)]
        public ActionResult EditComment(long AdventureNote_PK, string commentText, long? parentComment = null)
        {
            ActionResult result = null;
            var userId = User.Identity.GetUserId();

            using (var dbContext = new ApplicationDbContext())
            {
                // Get the comment to edit.
                var note = (from n in dbContext.AdventureNotes
                            where n.AdventureNote_PK == AdventureNote_PK
                            select n)
                            .FirstOrDefault();

                if (note != null)
                {
                    // Check if the adventure exists that meets the qualifications.
                    var isPlayer = (from a in dbContext.Adventures
                                    let player = a.Players.FirstOrDefault(p => p.UserId_PK == userId)
                                    where a.Adventure_PK == note.Adventure_PK
                                        && a.IsActive
                                        && player.IsActive
                                    select a).Any();

                    // If the user is the player that posted the comment, edit it.
                    if (isPlayer)
                    {
                        note.Text = commentText;

                        dbContext.Entry(note).Property("Text").IsModified = true;
                        dbContext.SaveChanges();

                        result = RedirectToAction("Details", new { id = note.Adventure_PK});
                    }
                }
            }


            return result;
        }

        /// <summary>
        /// Recursively deletes all items associated with an comment.
        /// </summary>
        /// <param name="AdventureNote_PK">Comment to delete.</param>
        /// <param name="dbContext">database access to use.</param>
        private void DeleteOrphanAdventureNotes(long AdventureNote_PK, ApplicationDbContext dbContext)
        {
            var note = (from n in dbContext.AdventureNotes
                        where n.AdventureNote_PK == AdventureNote_PK
                        select n)
                        .Include(n => n.ChildNotes)
                        .FirstOrDefault();

            foreach (var child in note.ChildNotes)
            {
                DeleteOrphanAdventureNotes(child.AdventureNote_PK, dbContext);
            }

            note.IsActive = false;
            dbContext.Entry(note).Property("IsActive").IsModified = true;
        }

        #endregion

        #region Delete Adventure

        /// <summary>
        /// Http Post to delete an adventure.
        /// </summary>
        /// <param name="adventure_PK"></param>
        [Authorize, HttpPost]
        public ActionResult DeleteAdventure(long adventure_PK)
        {
            ActionResult result = RedirectToAction("Index");

            using (var dbContext = new ApplicationDbContext())
            {
                var userId = User.Identity.GetUserId();

                // Find the adventure.
                var adventure = (from a in dbContext.Adventures
                                 let player = a.Players.FirstOrDefault(p => p.UserId_PK == userId)
                                 where a.Adventure_PK == adventure_PK
                                    && a.IsActive
                                    && player.IsActive
                                    // Assure the user is the gamemaster.
                                    && player.PlayerRole.PlayerRole_PK == (long)PlayerRole.PlayerRoleKey.Gamemaster
                                 select a)
                                 .Include(a => a.Items)
                                 .Include(a => a.AdventureNotes)
                                 .FirstOrDefault();

                if (adventure != null)
                {
                    // disable the adventure.
                    adventure.IsActive = false;

                    // update the database.
                    dbContext.Entry(adventure).Property("IsActive").IsModified = true;

                    // Delete all sub-items.
                    foreach (var item in adventure.Items)
                    {
                        DeleteOrphanItems(item.Item_PK, dbContext);
                    }

                    // Delete all notes associated with the adventure.
                    foreach (var note in adventure.AdventureNotes)
                    {
                        DeleteOrphanAdventureNotes(note.AdventureNote_PK, dbContext);
                    }

                    dbContext.SaveChanges();
                }
            }

            return result;
        }

        #endregion

        #region Search
        /// <summary>
        /// Http Post for searching for an item in an adventure.
        /// </summary>
        /// <param name="adventure_PK"></param>
        /// <param name="searchText"></param>
        /// <returns></returns>
        [Authorize, HttpPost]
        public ActionResult AdventureSearch(long adventure_PK, string searchText)
        {
            ActionResult result = RedirectToAction("SearchResults", new { id = adventure_PK });

            using (var dbContext = new ApplicationDbContext())
            {
                var userId = User.Identity.GetUserId();

                // Check to see if there is an exact match on the name.
                var results = (from i in dbContext.Items
                               let player = i.Adventure.Players.FirstOrDefault(p => p.UserId_PK == userId)
                               where i.Adventure_PK == adventure_PK
                                && i.Name == searchText
                                && i.IsActive
                                && (i.Adventure.IsPublic || player.IsActive)
                               select i)
                               .ToList();

                if (results.Count() == 1)
                {
                    // If there is only one exact match, go to the detail page.
                    result = RedirectToAction("ItemDetails", new { id = results.First().Item_PK });
                }
                else
                {
                    // If there are multiple matches, start the fuzzy search.
                    // Get all items to search through.
                    results = (from i in dbContext.Items
                               where i.Adventure_PK == adventure_PK
                                && i.IsActive
                               select i)
                               .ToList();

                    // Find a match on any names that contain the non-case-sensitive text of the search.
                    Regex regex = new Regex("^.*" + searchText.ToLower() + ".*$");

                    // Remove all items that do not match.
                    results = results.Where(i => regex.IsMatch(i.Name.ToLower())).ToList();

                    // Save the list for the view in temporary data
                    TempData["searchResults"] = results;
                    result = RedirectToAction("SearchResults", new { id = adventure_PK });
                }
            }

            return result;
        }

        /// <summary>
        /// Http Get for the search results page.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("adventure/{id:long}/Search/Results")]
        public ActionResult SearchResults(long id)
        {
            // Get the results from temporary data.
            var searchResults = TempData["searchResults"] as List<Item>;
            // Create the view model.
            var viewModel = new SearchResultsViewModel(null, searchResults);

            using (var dbContext = new ApplicationDbContext())
            {
                var adventure = (from a in dbContext.Adventures
                                 where a.Adventure_PK == id
                                    && a.IsActive
                                 select a)
                                 // Get the list of items for the search menu.
                                 .Include(a => a.Items)
                                 .FirstOrDefault();

                if (adventure != null)
                {
                    viewModel.Adventure = adventure;
                }
            }

            return View(viewModel);
        }

        #endregion

        #endregion

        #region Item Actions

        #region Create

        /// <summary>
        /// HTTP Get. for creating an item. Route: ~/adventure/{adventureId}/Item/Create
        /// </summary>
        /// <param name="adventureId"></param>
        [Authorize]
        [Route("adventure/{adventureId:long}/CreateItem/{parentId:long?}", Name ="ItemCreate")]
        public ActionResult ItemCreate(long adventureId, long? parentId)
        {
            Adventure adventure = null;
            // Display the access invalid view if the adventure could not be found or the user does not have access to view it.
            ActionResult result = View("AccessInvalid");

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                adventure = (from a in dbContext.Adventures
                             let player = a.Players.FirstOrDefault(p => p.UserId_PK == userId)
                             where a.Adventure_PK == adventureId
                                && a.IsActive
                                && player.IsActive
                                // Ensure the user is a gamemaster.
                                && player.PlayerRole.PlayerRole_PK == (long)PlayerRole.PlayerRoleKey.Gamemaster
                             select a).FirstOrDefault();
            }

            if (adventure != null)
            {
                // Create the default item with preset values.
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
                    // Return to the parent.
                    result = RedirectToAction("ItemDetails", new { id = model.ParentItem_PK });
                }
                else
                {
                    // Return to the advenure view.
                    result = RedirectToAction("Details", new { id = model.Adventure_PK });
                }
            }

            // If we got this far, redisplay the view for corrections.
            return result;
        }

        #endregion

        #region Details
        /// <summary>
        /// HTTP Get for item details.  Route: ~/Item/id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [Route("Item/{id:long}", Name = "ItemDetails")]
        public ActionResult ItemDetails(long id)
        {
            Item Item = null;
            ActionResult result = View("AccessInvalid");

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                Item = (from i in dbContext.Items
                        let player = i.Adventure.Players.FirstOrDefault(p => p.UserId_PK == userId)
                        where i.Item_PK == id
                            && i.IsActive
                            // Assure that the adventure is one the player can view AND is not deleted.
                            && i.Adventure.IsActive
                            && (i.Adventure.IsPublic || player.IsActive)
                        select i)
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
        /// <summary>
        /// HTTP Get for edititng an item. Rotue: ~/item/id/Edit
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [Route("Item/{id:long}/Edit", Name="ItemEdit")]
        public ActionResult ItemEdit(long id)
        {
            Item Item = null;
            // If there is an issue, go to the detail view of the item.
            ActionResult view = RedirectToAction("ItemDetails", id);

            using (var dbContext = new ApplicationDbContext())
            {
                string userId = User.Identity.GetUserId();

                Item = (from i in dbContext.Items
                        let player = i.Adventure.Players.FirstOrDefault(p => p.UserId_PK == userId)
                        where i.Item_PK == id
                            // Assure that the adventure is one the player can edit AND is not deleted.
                            && i.IsActive
                            && i.Adventure.IsActive
                            && player.IsActive
                            // Ensure the user is a gamemaster.
                            && player.PlayerRole.PlayerRole_PK == (long)PlayerRole.PlayerRoleKey.Gamemaster
                         select i).FirstOrDefault();
            }

            if (Item != null)
            {
                view = View("ItemEdit", Item);
            }

            return view;
        }


        /// <summary>
        /// Http Post for editing an item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, ValidateAntiForgeryToken, Authorize]
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

        #region Delete Item
        /// <summary>
        /// Http Post for deleting an item.
        /// </summary>
        /// <param name="item_PK"></param>
        /// <param name="adventure_pk"></param>
        /// <returns></returns>
        [Authorize, HttpPost]
        public ActionResult DeleteItem(long item_PK, long adventure_pk)
        {
            // If it is successfully deleted, go to the adventure detail page.
            ActionResult result = RedirectToAction("Details", new { id = adventure_pk });

            using (var dbContext = new ApplicationDbContext())
            {
                var userId = User.Identity.GetUserId();

                // Find the item.
                var item = (from i in dbContext.Items
                            let player = i.Adventure.Players.FirstOrDefault(p => p.UserId_PK == userId)
                            where i.Item_PK == item_PK
                                && player.IsActive
                                // ensure the user is the gamemaster.
                                && player.PlayerRole.PlayerRole_PK == (long)PlayerRole.PlayerRoleKey.Gamemaster
                            select i).FirstOrDefault();

                if (item != null)
                {
                    // Delete all child items.
                    DeleteOrphanItems(item.Item_PK, dbContext);
                    dbContext.SaveChanges();
                }
            }

            return result;
        }

        /// <summary>
        /// Recursively delete all child items of an item.
        /// </summary>
        /// <param name="Item_PK">item to delete.</param>
        /// <param name="dbContext">database context to use.</param>
        private void DeleteOrphanItems(long Item_PK, ApplicationDbContext dbContext)
        {
            var item = (from i in dbContext.Items
                        where i.Item_PK == Item_PK
                        select i)
                        .Include(i => i.ChildItems)
                        .Include(i => i.ItemNotes)
                        .FirstOrDefault();
            
            // Delete childs of the children.
            foreach (var child in item.ChildItems)
            {
                DeleteOrphanItems(child.Item_PK, dbContext);
            }

            // Delete all notes on the item.
            foreach (var note in item.ItemNotes)
            {
                DeleteOrphanComments(note.ItemNote_PK, dbContext);
            }

            // Delete the item.
            item.IsActive = false;
            dbContext.Entry(item).Property("IsActive").IsModified = true;
        }

        #endregion

        #region Comments
        /// <summary>
        /// Http Post for creating a comment.
        /// </summary>
        /// <param name="Item_PK"></param>
        /// <param name="newComment"></param>
        /// <param name="parentComment"></param>
        /// <returns></returns>
        [Authorize, HttpPost, ValidateInput(false)]
        public ActionResult CreateItemComment(long Item_PK, string newComment, long? parentComment = null)
        {
            ActionResult result = RedirectToAction("ItemDetails", new { id = Item_PK });

            if (!string.IsNullOrWhiteSpace(newComment))
            {
                var userId = User.Identity.GetUserId();

                var note = new ItemNote()
                {
                    Item_PK = Item_PK,
                    UserId_PK = userId,
                    ParentItemNote_PK = parentComment,
                    Text = newComment,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    LastModifiedUser = User.Identity.GetUserName()
                };

                using (var dbContext = new ApplicationDbContext())
                {
                    // Check if an item exists that meets the qualifications.
                    var isPlayer = (from i in dbContext.Items
                                    let player = i.Adventure.Players.FirstOrDefault(p => p.UserId_PK == userId)
                                    where i.Item_PK == Item_PK
                                        && i.IsActive
                                        && i.Adventure.IsActive
                                        && player.IsActive
                                    select i).Any();

                    if (isPlayer)
                    {
                        dbContext.ItemNotes.Add(note);
                        dbContext.SaveChanges();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Http Post for deleting an item.
        /// </summary>
        /// <param name="ItemNote_PK"></param>
        /// <returns></returns>
        [Authorize, HttpPost]
        public ActionResult DeleteItemComment(long ItemNote_PK)
        {
            ActionResult result = RedirectToAction("Index");

            using (var dbContext = new ApplicationDbContext())
            {
                var note = (from n in dbContext.ItemNotes
                            where n.ItemNote_PK == ItemNote_PK
                                && n.IsActive
                            select n)
                            .Include(n => n.Item)
                            .FirstOrDefault();

                if (note != null)
                {
                    // Ensure the user is the gamemaster.
                    var isGamemaster = IsInRole(User.Identity, note.Item.Adventure_PK, PlayerRole.PlayerRoleKey.Gamemaster);

                    if (isGamemaster)
                    {
                        // Delete the comment and all child comments.
                        DeleteOrphanComments(note.ItemNote_PK, dbContext);
                        dbContext.SaveChanges();
                        result = RedirectToAction("ItemDetails", new { id = note.Item_PK });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Http Post to edit the comment.
        /// </summary>
        /// <param name="ItemNote_PK"></param>
        /// <param name="commentText"></param>
        /// <param name="parentComment"></param>
        /// <returns></returns>
        [Authorize, HttpPost, ValidateInput(false)]
        public ActionResult EditItemComment(long ItemNote_PK, string commentText, long? parentComment = null)
        {
            ActionResult result = null;
            var userId = User.Identity.GetUserId();

            using (var dbContext = new ApplicationDbContext())
            {
                var note = (from n in dbContext.ItemNotes
                            where n.ItemNote_PK == ItemNote_PK
                            select n)
                            .Include(n => n.Item)
                            .FirstOrDefault();

                if (note != null)
                {
                    // Check if the adventure exists that meets the qualifications.
                    var isPlayer = (from a in dbContext.Adventures
                                    let player = a.Players.FirstOrDefault(p => p.UserId_PK == userId)
                                    where a.Adventure_PK == note.Item.Adventure_PK
                                        && a.IsActive
                                        && player.IsActive
                                    select a).Any();

                    if (isPlayer)
                    {
                        note.Text = commentText;

                        dbContext.Entry(note).Property("Text").IsModified = true;
                        dbContext.SaveChanges();

                        result = RedirectToAction("ItemDetails", new { id = note.Item_PK });
                    }
                }
            }


            return result;
        }

        /// <summary>
        /// Delete all child comments of a comment.
        /// </summary>
        /// <param name="ItemNote_PK"></param>
        /// <param name="dbContext"></param>
        private void DeleteOrphanComments(long ItemNote_PK, ApplicationDbContext dbContext)
        {
            var note = (from n in dbContext.ItemNotes
                        where n.ItemNote_PK == ItemNote_PK
                        select n)
                        .Include(n => n.ChildNotes)
                        .FirstOrDefault();

            foreach (var child in note.ChildNotes)
            {
                DeleteOrphanComments(child.ItemNote_PK, dbContext);
            }

            note.IsActive = false;
            dbContext.Entry(note).Property("IsActive").IsModified = true;
        }

        #endregion

        #endregion

        #region Utilities
        /// <summary>
        /// Method to ascertain if a user is in a role for a specified adventure.
        /// </summary>
        /// <param name="user">User Id to check for.</param>
        /// <param name="adventure_Pk">Specified Adventure.</param>
        /// <param name="playerRoleKey">Player Role to check for.</param>
        /// <returns></returns>
        public static bool IsInRole(IIdentity user, long adventure_Pk, PlayerRole.PlayerRoleKey playerRoleKey)
        {
            bool isInRole = true;
            string userId = user.GetUserId();

            using (var dbContext = new ApplicationDbContext())
            {
                isInRole = (from p in dbContext.Players
                            where p.UserId_PK == userId
                                && p.Adventure_PK == adventure_Pk
                                && p.PlayerRole_PK == (int)playerRoleKey
                                && p.IsActive
                            select p).Any();
            }

            return isInRole;
        }

        /// <summary>
        /// Check if a user is in any role for a specified adventure.
        /// </summary>
        /// <param name="user">User Id to check for.</param>
        /// <param name="adventure_Pk">Specified adventure.</param>
        /// <param name="playerRoleKeys">enumerable item list of player roles to check for.</param>
        /// <returns></returns>
        public static bool IsInAnyRole(IIdentity user, long adventure_Pk, IEnumerable<PlayerRole.PlayerRoleKey> playerRoleKeys)
        {
            bool isInAnyRole = false;

            foreach (var key in playerRoleKeys)
            {
                if (IsInRole(user, adventure_Pk, key))
                {
                    isInAnyRole = true;
                    break;
                }
            }

            return isInAnyRole;
        }

        /// <summary>
        /// Checks if a user is the original creator of a comment.
        /// </summary>
        /// <param name="user">User Id to check for.</param>
        /// <param name="AdventureNote_PK">key of adventure comment to check for.</param>
        /// <returns></returns>
        public static bool IsAdventureNoteOwner(IIdentity user, long AdventureNote_PK)
        {
            bool isAdventureNoteOwner = true;
            string userId = user.GetUserId();

            using (var dbContext = new ApplicationDbContext())
            {
                isAdventureNoteOwner = (from n in dbContext.AdventureNotes
                                        where n.AdventureNote_PK == AdventureNote_PK
                                            && n.UserId_PK == userId
                                        select n).Any();
            }

            return isAdventureNoteOwner;
        }

        /// <summary>
        /// Checks if a user is the original creator of a comment.
        /// </summary>
        /// <param name="user">User Id to check for.</param>
        /// <param name="ItemNote_PK">Key of adventure comment to check for.</param>
        /// <returns></returns>
        public static bool IsItemNoteOwner(IIdentity user, long ItemNote_PK)
        {
            bool isAdventureNoteOwner = true;
            string userId = user.GetUserId();

            using (var dbContext = new ApplicationDbContext())
            {
                isAdventureNoteOwner = (from n in dbContext.ItemNotes
                                        where n.ItemNote_PK == ItemNote_PK
                                            && n.UserId_PK == userId
                                        select n).Any();
            }

            return isAdventureNoteOwner;
        }
        #endregion
    }
}
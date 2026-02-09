using Microsoft.AspNetCore.Mvc;
using NUTRIBITE.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NUTRIBITE.Controllers
{
    // Partial extension for UsersController — provides support / audit endpoints.
    public partial class UsersController
    {
        // GET /Users/SupportActions?userId=1001
        [HttpGet]
        public IActionResult SupportActions(int userId = 0)
        {
            ViewBag.UserId = userId;
            return View();
        }

        // GET /Users/GetSupportActionsData?userId=1001
        [HttpGet]
        public IActionResult GetSupportActionsData(int userId = 0, int limit = 50)
        {
            // Sample/mock data. Replace with DB query to support audit log storage.
            var now = DateTime.UtcNow;
            var list = new List<SupportActionModel>();

            for (int i = 0; i < Math.Min(limit, 12); i++)
            {
                list.Add(new SupportActionModel
                {
                    ActionId = 1000 + i,
                    UserId = userId,
                    Admin = (i % 3 == 0) ? "system" : $"admin{i % 4}",
                    Timestamp = now.AddHours(-i * 6),
                    ActionType = (i % 4 == 0) ? "Note" : (i % 3 == 0 ? "Unblock" : "Warning"),
                    Note = (i % 4 == 0) ? $"Internal note #{i} — context and decision." : $"Admin action {i}",
                    Result = (i % 3 == 0) ? "Completed" : "Recorded",
                    Severity = (i % 5 == 0) ? "warning" : (i % 3 == 0 ? "success" : "info")
                });
            }

            return Json(new { items = list.OrderByDescending(x => x.Timestamp).ToArray() });
        }

        // POST /Users/AddSupportNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddSupportNote(int userId, string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return Json(new { success = false, message = "Note required" });

            // TODO: persist to DB with current admin identity
            return Json(new
            {
                success = true,
                action = new SupportActionModel
                {
                    ActionId = new Random().Next(20000, 99999),
                    UserId = userId,
                    Admin = User?.Identity?.Name ?? "admin",
                    Timestamp = DateTime.UtcNow,
                    ActionType = "Note",
                    Note = note,
                    Result = "Recorded",
                    Severity = "info"
                }
            });
        }

        // POST /Users/PerformAdminAction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PerformAdminAction(int userId, string actionType, string note)
        {
            if (string.IsNullOrWhiteSpace(actionType)) return Json(new { success = false, message = "Action required" });

            // TODO: perform the admin action (unblock, warn, restrict) and persist outcomes
            return Json(new
            {
                success = true,
                action = new SupportActionModel
                {
                    ActionId = new Random().Next(20000, 99999),
                    UserId = userId,
                    Admin = User?.Identity?.Name ?? "admin",
                    Timestamp = DateTime.UtcNow,
                    ActionType = actionType,
                    Note = note ?? "",
                    Result = "Completed",
                    Severity = actionType.Equals("Unblock", StringComparison.OrdinalIgnoreCase) ? "success" : "info"
                }
            });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using NUTRIBITE.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NUTRIBITE.Controllers
{
    // Partial controller file that adds blocked-users functionality.
    // Drop this file into Controllers\ and it will augment existing UsersController.
    public partial class UsersController
    {
        // GET: /Users/BlockedUsers
        [HttpGet]
        public IActionResult BlockedUsers()
        {
            return View();
        }

        // GET: /Users/GetBlockedUsersData
        [HttpGet]
        public IActionResult GetBlockedUsersData(string q = "", DateTime? from = null, DateTime? to = null, string status = "All")
        {
            // Replace this mock with DB queries in production.
            var now = DateTime.UtcNow.Date;
            var sample = new List<BlockedUserModel>
            {
                new BlockedUserModel { UserId = 1002, UserName = "Ravi Kumar", Email = "ravi@example.com", Phone = "9988776655", Reason = "Multiple coupon misuse", BlockedAt = now.AddDays(-10), ExpiresAt = now.AddDays(20), BlockedBy = "admin" , AdminNote = "Warned 2026-01-05" },
                new BlockedUserModel { UserId = 1010, UserName = "Pooja Sharma", Email = "pooja@example.com", Phone = "9123456789", Reason = "Chargebacks", BlockedAt = now.AddDays(-60), ExpiresAt = null, BlockedBy = "system", AdminNote = "Manual review required" },
                new BlockedUserModel { UserId = 1021, UserName = "Deepak Jain", Email = "deepak@example.com", Phone = "9012345678", Reason = "Abusive behaviour", BlockedAt = now.AddDays(-2), ExpiresAt = now.AddDays(5), BlockedBy = "mod1", AdminNote = "" }
            };

            // Apply filters
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.Trim().ToLowerInvariant();
                sample = sample.Where(u =>
                    (u.UserName ?? "").ToLowerInvariant().Contains(qq) ||
                    (u.Email ?? "").ToLowerInvariant().Contains(qq) ||
                    (u.Phone ?? "").ToLowerInvariant().Contains(qq) ||
                    (u.Reason ?? "").ToLowerInvariant().Contains(qq)
                ).ToList();
            }

            if (from.HasValue)
                sample = sample.Where(u => u.BlockedAt.Date >= from.Value.Date).ToList();

            if (to.HasValue)
                sample = sample.Where(u => u.BlockedAt.Date <= to.Value.Date).ToList();

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                    sample = sample.Where(u => u.IsActiveBlock).ToList();
                else if (string.Equals(status, "Expired", StringComparison.OrdinalIgnoreCase))
                    sample = sample.Where(u => !u.IsActiveBlock).ToList();
            }

            // return JSON
            return Json(new { total = sample.Count, items = sample.OrderByDescending(u => u.BlockedAt).ToArray() });
        }

        // POST: /Users/UnblockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnblockUser(int userId)
        {
            // TODO: implement DB update to remove block/flag
            // For now return success for UI.
            return Json(new { success = true, userId = userId });
        }

        // POST: /Users/AddBlockNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddBlockNote(int userId, string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return Json(new { success = false, message = "Note required" });

            // TODO: persist admin note in DB (append with timestamp)
            return Json(new { success = true, userId = userId, note = note });
        }
    }
}
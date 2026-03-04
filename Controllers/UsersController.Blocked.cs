using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using NUTRIBITE.Models.Users;

namespace NUTRIBITE.Controllers
{
    // Partial controller file — provides endpoints related to blocked users.
    // NOTE: The main UsersController already contains the server-rendered BlockedUsers() action.
    public partial class UsersController
    {
        // GET: /Users/GetBlockedUsersData
        [HttpGet]
        public IActionResult GetBlockedUsersData(string q = "", DateTime? from = null, DateTime? to = null, string status = "All")
        {
            // Query blocked users from database
            var query = _context.UserSignups
                        .Where(u => u.Status == "Blocked")
                        .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.Trim().ToLowerInvariant();
                query = query.Where(u =>
                    (u.Name ?? "").ToLower().Contains(qq) ||
                    (u.Email ?? "").ToLower().Contains(qq) ||
                    (u.Phone ?? "").ToLower().Contains(qq));
            }

            if (from.HasValue)
            {
                var f = from.Value.Date;
                query = query.Where(u => (u.CreatedAt ?? DateTime.MinValue) >= f);
            }

            if (to.HasValue)
            {
                var t = to.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(u => (u.CreatedAt ?? DateTime.MinValue) <= t);
            }

            List<BlockedUserModel> list = query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new BlockedUserModel
                {
                    UserId = u.Id,
                    UserName = u.Name,
                    Email = u.Email,
                    Phone = u.Phone,
                    Reason = "", // Reason not present in schema
                    BlockedAt = u.CreatedAt,
                    ExpiresAt = null,
                    BlockedBy = "admin",
                    AdminNote = ""
                })
                .ToList();

            // Apply status filter if requested (Active/Expired)
            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(status, "Expired", StringComparison.OrdinalIgnoreCase))
                {
                    // No expiry data in schema — return empty list for Expired
                    list = new List<BlockedUserModel>();
                }
                else if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(x => x.IsActiveBlock).ToList();
                }
            }

            return Json(new { total = list.Count, items = list.ToArray() });
        }

        // POST: /Users/UnblockUser
        // Renamed server method to avoid duplicate method signature with the GET UnblockUser in UsersController.cs.
        // Route kept as '/Users/UnblockUser' so existing AJAX callers continue to work.
        [HttpPost]
        [Route("Users/UnblockUser")]
        [ValidateAntiForgeryToken]
        public IActionResult UnblockUserPost(int userId)
        {
            var user = _context.UserSignups.FirstOrDefault(u => u.Id == userId);
            if (user == null) return Json(new { success = false, message = "User not found" });

            user.Status = "Active";
            _context.SaveChanges();

            return Json(new { success = true, userId = userId });
        }

        // POST: /Users/AddBlockNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddBlockNote(int userId, string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return Json(new { success = false, message = "Note required" });

            var user = _context.UserSignups.FirstOrDefault(u => u.Id == userId);
            if (user == null) return Json(new { success = false, message = "User not found" });

            // Schema doesn't include admin notes; no destructive changes here.
            // Ideally add a separate table/column via migration for notes.
            _context.SaveChanges();

            return Json(new { success = true, userId = userId, note = note });
        }
    }
}
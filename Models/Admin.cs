using System;
using System.Collections.Generic;

namespace NUTRIBITE.Models;

public partial class Admin
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!; // Email

    public string Password { get; set; } = null!;

    public string? Name { get; set; }

    public string? Phone { get; set; }

    public string? AvatarPath { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastLogin { get; set; }

    public string? SettingsJson { get; set; }
}

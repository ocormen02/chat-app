using Microsoft.AspNetCore.Identity;

namespace ChatApp.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}

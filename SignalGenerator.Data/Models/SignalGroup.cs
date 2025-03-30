using System.ComponentModel.DataAnnotations;

namespace SignalGenerator.Data.Models
{
    public class SignalGroup
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public required string CreatedById { get; set; }
        public required virtual ApplicationUser CreatedBy { get; set; }

        public virtual ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
    }
} 
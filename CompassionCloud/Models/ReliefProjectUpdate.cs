using System;
using System.ComponentModel.DataAnnotations;

namespace CompassionCloud.Models
{
    public class ReliefProjectUpdate
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Location { get; set; }

        public DateTime DatePosted { get; set; } = DateTime.Now;

        public string? PostedByUserId { get; set; }
    }
}
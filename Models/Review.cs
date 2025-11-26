using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniPool01.Models
{
    public class Review
    {
        public int Id { get; set; }

        [ForeignKey("Ride")]
        public int RideId { get; set; }
        public Ride Ride { get; set; }

        [ForeignKey("Reviewer")]
        public int ReviewerId { get; set; }
        public User Reviewer { get; set; }

        [ForeignKey("Driver")]
        public int DriverId { get; set; }
        public User Driver { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
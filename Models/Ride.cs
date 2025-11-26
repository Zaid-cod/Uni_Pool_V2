using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniPool01.Models
{
    public class Ride
    {
        public int Id { get; set; }
        public string DriverName { get; set; } = "";
        public string CarModel { get; set; } = "";
        public string Departure { get; set; } = "";
        public string Destination { get; set; } = "";
        public DateTime DepartureTime { get; set; }
        public int AvailableSeats { get; set; }
        public string ModeOfTransport { get; set; } = "";
        public string DriverEmail { get; set; } = "";

        // NEW: Add this property
        public bool IsCompleted { get; set; } = false;

        [NotMapped]
        public List<string> Passengers { get; set; } = new List<string>();

        // Relationships
        public List<Booking> Bookings { get; set; } = new List<Booking>();

        [NotMapped]
        public User? Driver { get; set; }
    }
}
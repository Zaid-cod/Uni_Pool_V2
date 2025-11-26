using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniPool01.Models 
{
    public class Booking
    {
        public int Id { get; set; }

        [ForeignKey("Ride")]
        public int RideId { get; set; }
        public Ride Ride { get; set; }

        [ForeignKey("Passenger")]
        public int PassengerId { get; set; } 
        public User Passenger { get; set; } 

        public string BookingStatus { get; set; } = "Confirmed";
    }

}

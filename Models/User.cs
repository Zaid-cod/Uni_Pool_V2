namespace UniPool01.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";

        // NEW: Add these properties
        public string Role { get; set; } = "Student";
        public string? CarImagePath { get; set; }
    }
}
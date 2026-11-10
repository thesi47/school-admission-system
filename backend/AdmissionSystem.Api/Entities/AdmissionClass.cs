namespace AdmissionSystem.Api.Entities;

// One admission class / shift, for example "Class 6 - Morning Shift".
public class AdmissionClass
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Hold> Holds { get; set; } = new();
}

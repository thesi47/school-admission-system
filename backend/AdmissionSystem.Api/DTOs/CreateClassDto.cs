namespace AdmissionSystem.Api.DTOs;

// Body of POST /classes. Fields are nullable so a missing value can be
// reported as a clear 400 error instead of silently becoming 0 or "".
public class CreateClassDto
{
    public string? Name { get; set; }
    public int? TotalSeats { get; set; }
}

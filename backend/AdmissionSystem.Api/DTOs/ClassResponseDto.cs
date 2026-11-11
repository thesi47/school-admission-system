namespace AdmissionSystem.Api.DTOs;

// Response of POST /classes.
public record CreatedClassDto(Guid Id, string Name, int TotalSeats);

// Response of GET /classes and GET /classes/{classId}.
public record ClassResponseDto(
    Guid Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);

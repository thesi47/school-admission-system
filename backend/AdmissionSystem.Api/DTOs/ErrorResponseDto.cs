namespace AdmissionSystem.Api.DTOs;

// Every error response has this shape: { "error": "message for the user" }
public record ErrorResponseDto(string Error);

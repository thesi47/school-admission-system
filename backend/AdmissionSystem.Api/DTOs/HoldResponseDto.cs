namespace AdmissionSystem.Api.DTOs;

// Response of POST /classes/{classId}/holds.
public record ApplyHoldResponseDto(
    Guid HoldId,
    Guid ClassId,
    string StudentId,
    string Status,
    DateTime ExpiresAt);

// Response of GET /holds/{holdId}.
public record HoldStatusResponseDto(
    Guid HoldId,
    Guid ClassId,
    string Status,
    DateTime ExpiresAt);

// Response of POST /holds/{holdId}/confirm.
public record ConfirmHoldResponseDto(Guid HoldId, string Status);

// One item of GET /me/holds.
public record MyHoldResponseDto(
    Guid HoldId,
    Guid ClassId,
    string ClassName,
    string Status,
    DateTime ExpiresAt);

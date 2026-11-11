using System.Data;
using System.Text.RegularExpressions;
using AdmissionSystem.Api.Data;
using AdmissionSystem.Api.DTOs;
using AdmissionSystem.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdmissionSystem.Api.Controllers;

// HOW SEATS ARE PROTECTED (read this first)
// -----------------------------------------
// Every operation that can change how many seats a class uses (apply, confirm,
// cancel) does the same three steps inside ONE database transaction:
//
//   1. BEGIN a transaction.
//   2. Lock the class row:  SELECT 1 FROM "Classes" WHERE "Id" = @id FOR UPDATE
//   3. Count seats, check the rules, change the hold, COMMIT.
//
// "FOR UPDATE" makes PostgreSQL hand the class row to one request at a time.
// If 100 parents apply at once, 99 of them wait at step 2 while one request
// finishes steps 3 and commits. The next request then runs its count and sees
// the committed hold (READ COMMITTED takes a fresh snapshot for every
// statement), so two requests can never both take the last seat.
//
// The lock is per class, so applications to different classes do not block
// each other. Because every operation locks only this one row, there is no
// risk of a deadlock between two locks taken in different orders.
[ApiController]
public class HoldsController : ControllerBase
{
    private const int DefaultHoldDurationSeconds = 600;
    private static readonly Regex StudentIdPattern = new Regex("^[0-9]{17}$");

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public HoldsController(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    // POST /classes/{classId}/holds
    [HttpPost("/classes/{classId:guid}/holds")]
    public async Task<IActionResult> ApplyForSeat(Guid classId)
    {
        var studentId = ReadStudentId(out var headerError);
        if (studentId == null)
        {
            return BadRequest(new ErrorResponseDto(headerError));
        }

        var holdDurationSeconds = GetHoldDurationSeconds();

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        // Step 2: lock the class row. Everything below is now exclusive for this class.
        await LockClassRow(classId);

        var admissionClass = await _db.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classId);
        if (admissionClass == null)
        {
            return NotFound(new ErrorResponseDto("This class was not found."));
        }

        var now = DateTime.UtcNow;

        // Old HELD holds of this student whose time ran out are stored as EXPIRED now.
        // This keeps the "one active application" database index from blocking a new
        // application after an expired one.
        var staleHolds = await _db.Holds
            .Where(h => h.ClassId == classId
                        && h.StudentId == studentId
                        && h.Status == HoldStatus.Held
                        && h.ExpiresAt <= now)
            .ToListAsync();
        if (staleHolds.Count > 0)
        {
            foreach (var staleHold in staleHolds)
            {
                staleHold.Status = HoldStatus.Expired;
            }
            await _db.SaveChangesAsync();
        }

        // Rule 4: one active application per student per class.
        var alreadyApplied = await _db.Holds.AnyAsync(h =>
            h.ClassId == classId
            && h.StudentId == studentId
            && (h.Status == HoldStatus.Confirmed
                || (h.Status == HoldStatus.Held && h.ExpiresAt > now)));
        if (alreadyApplied)
        {
            return Conflict(new ErrorResponseDto("This student already has an active application for this class."));
        }

        // Rule 1: never oversell. Counted while holding the lock.
        var heldSeats = await _db.Holds.Where(h => h.ClassId == classId).ActiveHeld(now).CountAsync();
        var confirmedSeats = await _db.Holds.Where(h => h.ClassId == classId).ConfirmedOnly().CountAsync();
        var availableSeats = admissionClass.TotalSeats - heldSeats - confirmedSeats;
        if (availableSeats <= 0)
        {
            return Conflict(new ErrorResponseDto("Sorry, no seats are currently available for this class."));
        }

        var hold = new Hold
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(holdDurationSeconds)
        };
        _db.Holds.Add(hold);

        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            // Only reachable if the unique "one active application" index fires.
            return Conflict(new ErrorResponseDto("This student already has an active application for this class."));
        }

        var response = new ApplyHoldResponseDto(hold.Id, hold.ClassId, hold.StudentId, hold.Status, hold.ExpiresAt);
        return CreatedAtAction(nameof(GetHold), new { holdId = hold.Id }, response);
    }

    // GET /holds/{holdId}
    [HttpGet("/holds/{holdId:guid}")]
    public async Task<IActionResult> GetHold(Guid holdId)
    {
        var hold = await _db.Holds.AsNoTracking().FirstOrDefaultAsync(h => h.Id == holdId);
        if (hold == null)
        {
            return NotFound(new ErrorResponseDto("This application was not found."));
        }

        var status = HoldStatus.Effective(hold, DateTime.UtcNow);
        return Ok(new HoldStatusResponseDto(hold.Id, hold.ClassId, status, hold.ExpiresAt));
    }

    // ---- helpers ----

    // Takes a row lock on the class until the current transaction ends.
    private async Task LockClassRow(Guid classId)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Classes\" WHERE \"Id\" = {classId} FOR UPDATE");
    }

    // Reads and validates the X-Student-Id header.
    // Returns null (and an error message) when it is missing or not a 17-digit string.
    private string? ReadStudentId(out string error)
    {
        error = string.Empty;

        if (!Request.Headers.TryGetValue("X-Student-Id", out var values))
        {
            error = "The X-Student-Id header is required.";
            return null;
        }

        var studentId = values.ToString().Trim();
        if (studentId.Length == 0)
        {
            error = "The X-Student-Id header is required.";
            return null;
        }
        if (!StudentIdPattern.IsMatch(studentId))
        {
            error = "The student birth registration number must be exactly 17 digits.";
            return null;
        }

        return studentId;
    }

    // Rule 2: the hold length comes from HOLD_DURATION_SECONDS (default 600).
    private int GetHoldDurationSeconds()
    {
        var text = _configuration["HOLD_DURATION_SECONDS"];
        if (int.TryParse(text, out var seconds) && seconds > 0)
        {
            return seconds;
        }
        return DefaultHoldDurationSeconds;
    }
}

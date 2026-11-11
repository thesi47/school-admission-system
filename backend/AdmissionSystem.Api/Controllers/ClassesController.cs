using AdmissionSystem.Api.Data;
using AdmissionSystem.Api.DTOs;
using AdmissionSystem.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdmissionSystem.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private const int MaxSeats = 100000;

    private readonly ApplicationDbContext _db;

    public ClassesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // POST /classes
    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassDto? dto)
    {
        if (dto == null)
        {
            return BadRequest(new ErrorResponseDto("Please send the class name and the total number of seats."));
        }

        var name = dto.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return BadRequest(new ErrorResponseDto("Class name is required."));
        }
        if (name.Length > 100)
        {
            return BadRequest(new ErrorResponseDto("Class name must be 100 characters or fewer."));
        }
        if (dto.TotalSeats == null)
        {
            return BadRequest(new ErrorResponseDto("Total seats is required."));
        }
        if (dto.TotalSeats.Value <= 0)
        {
            return BadRequest(new ErrorResponseDto("Total seats must be greater than zero."));
        }
        if (dto.TotalSeats.Value > MaxSeats)
        {
            return BadRequest(new ErrorResponseDto($"Total seats must not be more than {MaxSeats}."));
        }

        var admissionClass = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = name,
            TotalSeats = dto.TotalSeats.Value,
            CreatedAt = DateTime.UtcNow
        };

        _db.Classes.Add(admissionClass);
        await _db.SaveChangesAsync();

        var response = new CreatedClassDto(admissionClass.Id, admissionClass.Name, admissionClass.TotalSeats);
        return CreatedAtAction(nameof(GetClass), new { classId = admissionClass.Id }, response);
    }

    // GET /classes
    [HttpGet]
    public async Task<IActionResult> GetClasses()
    {
        var now = DateTime.UtcNow;

        // Seat counts are always calculated from the Holds table.
        // A HELD hold only counts while it has not expired.
        var rows = await _db.Classes
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                Held = c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                Confirmed = c.Holds.Count(h => h.Status == HoldStatus.Confirmed)
            })
            .ToListAsync();

        var result = new List<ClassResponseDto>();
        foreach (var row in rows)
        {
            result.Add(new ClassResponseDto(
                row.Id,
                row.Name,
                row.TotalSeats,
                row.Held,
                row.Confirmed,
                row.TotalSeats - row.Held - row.Confirmed));
        }

        return Ok(result);
    }

    // GET /classes/{classId}
    [HttpGet("{classId:guid}")]
    public async Task<IActionResult> GetClass(Guid classId)
    {
        var now = DateTime.UtcNow;

        var row = await _db.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                Held = c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                Confirmed = c.Holds.Count(h => h.Status == HoldStatus.Confirmed)
            })
            .FirstOrDefaultAsync();

        if (row == null)
        {
            return NotFound(new ErrorResponseDto("This class was not found."));
        }

        var response = new ClassResponseDto(
            row.Id,
            row.Name,
            row.TotalSeats,
            row.Held,
            row.Confirmed,
            row.TotalSeats - row.Held - row.Confirmed);

        return Ok(response);
    }
}

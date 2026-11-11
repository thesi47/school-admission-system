using AdmissionSystem.Api.Entities;

namespace AdmissionSystem.Api.Data;

// Small helpers so the seat rules are written in exactly one place.
//   heldSeats      = HELD holds that have not expired yet
//   confirmedSeats = CONFIRMED holds
//   availableSeats = totalSeats - heldSeats - confirmedSeats
public static class HoldQueries
{
    public static IQueryable<Hold> ActiveHeld(this IQueryable<Hold> holds, DateTime nowUtc)
    {
        return holds.Where(h => h.Status == HoldStatus.Held && h.ExpiresAt > nowUtc);
    }

    public static IQueryable<Hold> ConfirmedOnly(this IQueryable<Hold> holds)
    {
        return holds.Where(h => h.Status == HoldStatus.Confirmed);
    }
}

namespace PersonalGTD.Shared.Services;

public class ReviewStateService
{
    private DateTime? _reviewDoneAt = null;

    public event Action? OnStateChanged;
    public event Action? OnAutoReset;

    /// <summary>
    /// Returns the most recent Friday at 16:00 that has already passed.
    /// </summary>
    public static DateTime GetLastReviewTrigger()
    {
        var now = DateTime.Now;
        
        // How many days ago was last Friday?
        int daysToFriday = ((int)now.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7;
        var lastFriday = now.Date.AddDays(-daysToFriday);
        var lastTrigger = lastFriday.AddHours(16);
        
        // If today is Friday but before 16:00, go back to the previous Friday
        if (lastTrigger > now)
            lastTrigger = lastTrigger.AddDays(-7);
        
        return lastTrigger;
    }

    public bool IsReviewDoneThisWeek
    {
        get
        {
            if (_reviewDoneAt == null) return false;
            // Valid only if done AFTER the last Friday 16:00
            return _reviewDoneAt.Value >= GetLastReviewTrigger();
        }
    }

    public void MarkReviewDone()
    {
        _reviewDoneAt = DateTime.Now;
        OnStateChanged?.Invoke();
    }

    public void ResetReview()
    {
        _reviewDoneAt = null;
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Called by a timer (every minute). Fires OnAutoReset when crossing Friday 16:00.
    /// </summary>
    public void Tick()
    {
        var now = DateTime.Now;
        // Trigger: it's Friday, exactly 16:00 (within this minute), review was previously done
        if (now.DayOfWeek == DayOfWeek.Friday &&
            now.Hour == 16 && now.Minute == 0 &&
            _reviewDoneAt != null)
        {
            _reviewDoneAt = null;
            OnAutoReset?.Invoke();
            OnStateChanged?.Invoke();
        }
    }
}

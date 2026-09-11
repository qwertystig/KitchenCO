namespace YourKitchenCo.Services;

public class OrderSchedulingService : IOrderSchedulingService
{
    /// <summary>
    /// Latest time of day an order can still count as placed "today".
    /// The kitchen's real cutoff is 9:00 AM — orders placed after that
    /// roll to the next day's cutoff window. Two business days' notice is
    /// required (e.g. order Wednesday by 9am for Friday delivery; order
    /// Thursday by 9am for Monday delivery, since weekends don't count).
    /// </summary>
    public TimeOnly CutoffTime { get; set; } = new TimeOnly(9, 0);

    public bool IsOrderingOpen(DateTime? now = null) => true;

    public DateOnly GetEffectiveOrderDate(DateTime? now = null)
    {
        var current = now ?? DateTime.Now;
        var date = DateOnly.FromDateTime(current);

        var isWeekend = current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var pastCutoff = TimeOnly.FromDateTime(current) > CutoffTime;

        if (isWeekend || pastCutoff)
        {
            date = NextWeekday(date);
        }

        return date;
    }

    public DateOnly GetDeliveryDateForOrderDate(DateOnly orderDate) => AddBusinessDays(orderDate, 2);

    public DateOnly GetNextAvailableDeliveryDate(DateTime? now = null) =>
        GetDeliveryDateForOrderDate(GetEffectiveOrderDate(now));

    public List<DateOnly> GetOrderableDeliveryDates(int windowInWeekdays, DateTime? now = null)
    {
        var dates = new List<DateOnly>(windowInWeekdays);
        var date = GetNextAvailableDeliveryDate(now);

        for (var i = 0; i < windowInWeekdays; i++)
        {
            dates.Add(date);
            date = NextWeekday(date);
        }

        return dates;
    }

    public bool IsValidDeliveryDate(DateOnly requestedDeliveryDate, int windowInWeekdays, DateTime? now = null) =>
        GetOrderableDeliveryDates(windowInWeekdays, now).Contains(requestedDeliveryDate);

    private static DateOnly NextWeekday(DateOnly date)
    {
        var next = date.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            next = next.AddDays(1);
        return next;
    }

    private static DateOnly AddBusinessDays(DateOnly date, int businessDays)
    {
        var result = date;
        var added = 0;
        while (added < businessDays)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                added++;
        }
        return result;
    }
}

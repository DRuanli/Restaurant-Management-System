using Microsoft.AspNetCore.Mvc;

namespace RestaurantSystem.Enums
{
    public enum OrderStatus
    {
        Draft = 0,
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Cancelled = 4
    }
}

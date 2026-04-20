namespace Modules.Catalog.Domain.Reservation;

public enum ReservationStatus { Pending, Confirmed, Released }

public class Reservation
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
    public ReservationStatus Status { get; set; }
}
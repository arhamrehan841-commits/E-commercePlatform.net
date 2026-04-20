using SharedKernel.Contracts;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Domain.Reservation;
using Modules.Catalog.Domain.StockItems;
using Modules.Catalog.Infrastructure.Data;

namespace Modules.Catalog.Infrastructure.Contracts;

internal sealed class CatalogReservationService(CatalogDbContext context) : IStockReservationContract
{
    public async Task<Guid> ReserveStockAsync(Guid itemId, int quantity, CancellationToken ct)
    {
        // Note: In production, you would use raw SQL for SELECT FOR UPDATE to lock the row
        var stock = await context.Set<StockItem>().FirstOrDefaultAsync(x => x.Id == itemId, ct)
            ?? throw new Exception($"Item {itemId} not found.");

        if (stock.AvailableQty < quantity)
            throw new Exception("OutOfStockException: Insufficient stock available.");

        stock.AvailableQty -= quantity;
        stock.ReservedQty += quantity;

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            Quantity = quantity,
            Status = ReservationStatus.Pending
        };

        context.Set<Reservation>().Add(reservation);
        await context.SaveChangesAsync(ct);

        return reservation.Id;
    }

    public async Task ConfirmReservationAsync(Guid reservationId, CancellationToken ct)
    {
        var reservation = await context.Set<Reservation>().FindAsync([reservationId], ct);
        if (reservation != null && reservation.Status == ReservationStatus.Pending)
        {
            reservation.Status = ReservationStatus.Confirmed;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task ReleaseReservationAsync(Guid reservationId, CancellationToken ct)
    {
        var reservation = await context.Set<Reservation>().FindAsync([reservationId], ct);
        if (reservation != null && reservation.Status == ReservationStatus.Pending)
        {
            reservation.Status = ReservationStatus.Released;
            
            var stock = await context.Set<StockItem>().FindAsync([reservation.ItemId], ct);
            if (stock != null)
            {
                stock.AvailableQty += reservation.Quantity;
                stock.ReservedQty -= reservation.Quantity;
            }
            await context.SaveChangesAsync(ct);
        }
    }
}
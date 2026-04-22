using SharedKernel.Contracts;
using SharedKernel.Exceptions;
using Microsoft.EntityFrameworkCore;
using Modules.Catalog.Domain.Reservation;
using Modules.Catalog.Domain.StockItems;
using Modules.Catalog.Infrastructure.Data;

namespace Modules.Catalog.Infrastructure.Contracts;

internal sealed class CatalogReservationService(CatalogDbContext context) : IStockReservationContract
{
    public async Task<BulkReservationResponse> ReserveStockStrictAsync(
        IEnumerable<BulkReservationRequest> requests, CancellationToken ct)
    {
        var reservations = new List<ReservationResult>(); // <-- UPDATED
        var rejections = new List<StockRejection>();

        foreach (var req in requests)
        {
            var stock = await context.StockItems.FindAsync(new object[] { req.ItemId }, ct);
            
            if (stock == null || stock.AvailableQty < req.Quantity)
            {
                rejections.Add(new StockRejection(req.ItemId, "Product Name", req.Quantity, stock?.AvailableQty ?? 0));
            }
            else
            {
                stock.AvailableQty -= req.Quantity;
                stock.ReservedQty += req.Quantity;
                
                // Explicitly set Status
                var res = new Reservation { Id = Guid.NewGuid(), ItemId = req.ItemId, Quantity = req.Quantity, Status = ReservationStatus.Pending };
                context.Reservations.Add(res);
                
                // Pair the Item with the Reservation
                reservations.Add(new ReservationResult(req.ItemId, res.Id)); // <-- UPDATED
            }
        }

        await context.SaveChangesAsync(ct);

        return new BulkReservationResponse(
            AllReserved: !rejections.Any(),
            Reservations: reservations, // <-- UPDATED
            Rejections: rejections);
    }

    public async Task ConfirmReservationsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        foreach (var reservationId in ids)
        {
            var reservation = await context.Set<Reservation>().FindAsync(new object[] { reservationId }, ct);

            if (reservation != null && reservation.Status == ReservationStatus.Pending)
            {
                reservation.Status = ReservationStatus.Confirmed;
            }
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task ReleaseReservationsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        foreach (var reservationId in ids)
        {
            var reservation = await context.Set<Reservation>().FindAsync(new object[] { reservationId }, ct);

            if (reservation != null && reservation.Status == ReservationStatus.Pending)
            {
                reservation.Status = ReservationStatus.Released;

                var stock = await context.Set<StockItem>().FindAsync(new object[] { reservation.ItemId }, ct);

                if (stock != null)
                {
                    stock.AvailableQty += reservation.Quantity;
                    stock.ReservedQty -= reservation.Quantity;
                }
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
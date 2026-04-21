using SharedKernel.Exceptions;
namespace SharedKernel.Contracts;

// This allows Orders to command Catalog synchronously without a direct project reference
// The response now helps the Handler decide whether to proceed or fail
public record BulkReservationRequest(Guid ItemId, int Quantity);

public record BulkReservationResponse(
    bool AllReserved, 
    List<Guid> ReservationIds, 
    List<StockRejection> Rejections);

public interface IStockReservationContract
{
    Task<BulkReservationResponse> ReserveStockStrictAsync(
        IEnumerable<BulkReservationRequest> requests, 
        CancellationToken ct = default);

    Task ConfirmReservationsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task ReleaseReservationsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
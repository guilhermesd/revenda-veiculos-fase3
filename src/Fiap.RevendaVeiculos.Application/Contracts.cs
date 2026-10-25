using Fiap.RevendaVeiculos.Domain;

namespace Fiap.RevendaVeiculos.Application;

public interface IVehicleRepository
{
    Task AddAsync(Vehicle vehicle, CancellationToken ct);
    Task<Vehicle?> GetAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Vehicle>> ListAsync(VehicleStatus status, int page, int pageSize, CancellationToken ct);
    Task<bool> ReplaceAsync(Vehicle vehicle, int expectedVersion, CancellationToken ct);
}
public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken ct);
    Task<Sale?> GetByPaymentCodeAsync(string paymentCode, CancellationToken ct);
    Task<IReadOnlyList<Sale>> ListPaidAsync(int page, int pageSize, CancellationToken ct);
    Task<bool> ReplaceAsync(Sale sale, CancellationToken ct);
}
public interface IIdGenerator { string NewId(); }
public interface IClock { DateTime UtcNow { get; } }
public sealed class NotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);

public record VehicleInput(string Brand, string Model, int Year, string Color, decimal Price);
public record PurchaseInput(string UserId, string PaymentCode);
public record VehicleResult(string Id, string Brand, string Model, int Year, string Color, decimal Price, string Status, DateTime CreatedAt, DateTime UpdatedAt);
public record SaleResult(string Id, string VehicleId, DateTime SaleDate, decimal AgreedPrice, string PaymentCode, string PaymentStatus);
public record SoldVehicleResult(string VehicleId, string Brand, string Model, int Year, string Color, decimal Price, DateTime SaleDate);

public sealed class VehicleService(IVehicleRepository vehicles, ISaleRepository sales, IIdGenerator ids, IClock clock)
{
    public async Task<VehicleResult> CreateAsync(VehicleInput input, CancellationToken ct)
    {
        var vehicle = Vehicle.Create(ids.NewId(), input.Brand, input.Model, input.Year, input.Color, input.Price, clock.UtcNow);
        await vehicles.AddAsync(vehicle, ct); return ToResult(vehicle);
    }
    public async Task<VehicleResult> GetAsync(string id, CancellationToken ct) => ToResult(await GetVehicle(id, ct));
    public async Task<VehicleResult> UpdateAsync(string id, VehicleInput input, CancellationToken ct)
    {
        var vehicle = await GetVehicle(id, ct); var version = vehicle.Version;
        vehicle.Update(input.Brand, input.Model, input.Year, input.Color, input.Price, clock.UtcNow);
        if (!await vehicles.ReplaceAsync(vehicle, version, ct)) throw new ConflictException("Vehicle was modified concurrently.");
        return ToResult(vehicle);
    }
    public async Task<IReadOnlyList<VehicleResult>> ListAvailableAsync(int page, int pageSize, CancellationToken ct) =>
        (await vehicles.ListAsync(VehicleStatus.Available, page, pageSize, ct)).Select(ToResult).ToList();
    public async Task<SaleResult> StartSaleAsync(string vehicleId, PurchaseInput input, CancellationToken ct)
    {
        var vehicle = await GetVehicle(vehicleId, ct); var version = vehicle.Version; vehicle.Reserve(clock.UtcNow);
        if (!await vehicles.ReplaceAsync(vehicle, version, ct)) throw new ConflictException("Vehicle is no longer available.");
        var sale = Sale.Create(ids.NewId(), vehicle.Id, input.UserId, clock.UtcNow, vehicle.Price, input.PaymentCode, clock.UtcNow);
        try { await sales.AddAsync(sale, ct); }
        catch
        {
            vehicle.Release(clock.UtcNow); await vehicles.ReplaceAsync(vehicle, vehicle.Version - 1, ct); throw;
        }
        return ToResult(sale);
    }
    public async Task ProcessPaymentAsync(string paymentCode, PaymentStatus status, CancellationToken ct)
    {
        var sale = await sales.GetByPaymentCodeAsync(paymentCode, ct) ?? throw new NotFoundException("Payment was not found.");
        var changed = sale.Apply(status, clock.UtcNow); if (!changed) return;
        if (!await sales.ReplaceAsync(sale, ct)) throw new ConflictException("Payment was modified concurrently.");
        var vehicle = await GetVehicle(sale.VehicleId, ct); var version = vehicle.Version;
        if (status == PaymentStatus.Paid) vehicle.ConfirmSale(clock.UtcNow); else vehicle.Release(clock.UtcNow);
        if (!await vehicles.ReplaceAsync(vehicle, version, ct)) throw new ConflictException("Vehicle was modified concurrently.");
    }
    public async Task<IReadOnlyList<SoldVehicleResult>> ListSoldAsync(int page, int pageSize, CancellationToken ct)
    {
        var paid = await sales.ListPaidAsync(page, pageSize, ct); var result = new List<SoldVehicleResult>();
        foreach (var sale in paid) { var vehicle = await GetVehicle(sale.VehicleId, ct); result.Add(new(vehicle.Id, vehicle.Brand, vehicle.Model, vehicle.Year, vehicle.Color, sale.AgreedPrice, sale.SaleDate)); }
        return result.OrderBy(x => x.Price).ThenBy(x => x.VehicleId).ToList();
    }
    private async Task<Vehicle> GetVehicle(string id, CancellationToken ct) => await vehicles.GetAsync(id, ct) ?? throw new NotFoundException("Vehicle was not found.");
    private static VehicleResult ToResult(Vehicle x) => new(x.Id, x.Brand, x.Model, x.Year, x.Color, x.Price, x.Status.ToString(), x.CreatedAt, x.UpdatedAt);
    private static SaleResult ToResult(Sale x) => new(x.Id, x.VehicleId, x.SaleDate, x.AgreedPrice, x.PaymentCode, x.PaymentStatus.ToString());
}

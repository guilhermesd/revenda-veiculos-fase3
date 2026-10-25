using Fiap.RevendaVeiculos.Application;
using Fiap.RevendaVeiculos.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Fiap.RevendaVeiculos.Infrastructure;

public sealed class MongoSettings { public string ConnectionString { get; set; } = "mongodb://localhost:27017"; public string DatabaseName { get; set; } = "revenda_veiculos"; }
public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
public sealed class MongoIdGenerator : IIdGenerator { public string NewId() => ObjectId.GenerateNewId().ToString(); }

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoInfrastructure(this IServiceCollection services, Action<MongoSettings> configure)
    {
        services.Configure(configure); services.AddSingleton<IMongoClient>(sp => new MongoClient(sp.GetRequiredService<IOptions<MongoSettings>>().Value.ConnectionString));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(sp.GetRequiredService<IOptions<MongoSettings>>().Value.DatabaseName));
        services.AddSingleton<IVehicleRepository, MongoVehicleRepository>(); services.AddSingleton<ISaleRepository, MongoSaleRepository>();
        services.AddSingleton<IClock, SystemClock>(); services.AddSingleton<IIdGenerator, MongoIdGenerator>(); services.AddSingleton<MongoInitializer>(); return services;
    }
}

public sealed class VehicleDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)] public string Id { get; set; } = null!;
    public string Brand { get; set; } = null!; public string Model { get; set; } = null!; public int Year { get; set; } public string Color { get; set; } = null!; public decimal Price { get; set; } public string Status { get; set; } = null!; public DateTime CreatedAt { get; set; } public DateTime UpdatedAt { get; set; } public int Version { get; set; }
    public static VehicleDocument From(Vehicle x) => new() { Id=x.Id, Brand=x.Brand, Model=x.Model, Year=x.Year, Color=x.Color, Price=x.Price, Status=x.Status.ToString(), CreatedAt=x.CreatedAt, UpdatedAt=x.UpdatedAt, Version=x.Version };
    public Vehicle ToDomain() => new(Id, Brand, Model, Year, Color, Price, Enum.Parse<VehicleStatus>(Status), CreatedAt, UpdatedAt, Version);
}
public sealed class SaleDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)] public string Id { get; set; } = null!;
    public string VehicleId { get; set; } = null!; public string UserId { get; set; } = null!; public DateTime SaleDate { get; set; } public decimal AgreedPrice { get; set; } public string PaymentCode { get; set; } = null!; public string PaymentStatus { get; set; } = null!; public DateTime CreatedAt { get; set; } public DateTime UpdatedAt { get; set; } public DateTime? PaidAt { get; set; } public DateTime? CanceledAt { get; set; }
    public static SaleDocument From(Sale x) => new() { Id=x.Id, VehicleId=x.VehicleId, UserId=x.UserId, SaleDate=x.SaleDate, AgreedPrice=x.AgreedPrice, PaymentCode=x.PaymentCode, PaymentStatus=x.PaymentStatus.ToString(), CreatedAt=x.CreatedAt, UpdatedAt=x.UpdatedAt, PaidAt=x.PaidAt, CanceledAt=x.CanceledAt };
    public Sale ToDomain() => new(Id, VehicleId, UserId, SaleDate, AgreedPrice, PaymentCode, Enum.Parse<PaymentStatus>(PaymentStatus), CreatedAt, UpdatedAt, PaidAt, CanceledAt);
}
public sealed class MongoVehicleRepository(IMongoDatabase db) : IVehicleRepository
{
    private readonly IMongoCollection<VehicleDocument> _collection = db.GetCollection<VehicleDocument>("vehicles");
    public Task AddAsync(Vehicle x, CancellationToken ct) => _collection.InsertOneAsync(VehicleDocument.From(x), cancellationToken: ct);
    public async Task<Vehicle?> GetAsync(string id, CancellationToken ct) => (await _collection.Find(x=>x.Id==id).FirstOrDefaultAsync(ct))?.ToDomain();
    public async Task<IReadOnlyList<Vehicle>> ListAsync(VehicleStatus status, int page, int size, CancellationToken ct) => (await _collection.Find(x=>x.Status==status.ToString()).SortBy(x=>x.Price).ThenBy(x=>x.Id).Skip((page-1)*size).Limit(size).ToListAsync(ct)).Select(x=>x.ToDomain()).ToList();
    public async Task<bool> ReplaceAsync(Vehicle x, int version, CancellationToken ct) => (await _collection.ReplaceOneAsync(v=>v.Id==x.Id && v.Version==version, VehicleDocument.From(x), cancellationToken:ct)).ModifiedCount==1;
}
public sealed class MongoSaleRepository(IMongoDatabase db) : ISaleRepository
{
    private readonly IMongoCollection<SaleDocument> _collection = db.GetCollection<SaleDocument>("sales");
    public Task AddAsync(Sale x, CancellationToken ct) => _collection.InsertOneAsync(SaleDocument.From(x), cancellationToken:ct);
    public async Task<Sale?> GetByPaymentCodeAsync(string code, CancellationToken ct) => (await _collection.Find(x=>x.PaymentCode==code).FirstOrDefaultAsync(ct))?.ToDomain();
    public async Task<IReadOnlyList<Sale>> ListPaidAsync(int page, int size, CancellationToken ct) => (await _collection.Find(x=>x.PaymentStatus==PaymentStatus.Paid.ToString()).SortBy(x=>x.AgreedPrice).ThenBy(x=>x.Id).Skip((page-1)*size).Limit(size).ToListAsync(ct)).Select(x=>x.ToDomain()).ToList();
    public async Task<bool> ReplaceAsync(Sale x, CancellationToken ct) => (await _collection.ReplaceOneAsync(s=>s.Id==x.Id && s.PaymentStatus==PaymentStatus.Pending.ToString(), SaleDocument.From(x), cancellationToken:ct)).ModifiedCount==1;
}
public sealed class MongoInitializer(IMongoDatabase db)
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        await db.GetCollection<VehicleDocument>("vehicles").Indexes.CreateOneAsync(new CreateIndexModel<VehicleDocument>(Builders<VehicleDocument>.IndexKeys.Ascending(x=>x.Status).Ascending(x=>x.Price).Ascending(x=>x.Id)), cancellationToken:ct);
        await db.GetCollection<SaleDocument>("sales").Indexes.CreateOneAsync(new CreateIndexModel<SaleDocument>(Builders<SaleDocument>.IndexKeys.Ascending(x=>x.PaymentCode), new CreateIndexOptions { Unique=true }), cancellationToken:ct);
    }
}

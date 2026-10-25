using Fiap.RevendaVeiculos.Domain.Enums;
using Fiap.RevendaVeiculos.Domain.Exceptions;

namespace Fiap.RevendaVeiculos.Domain.Entities;

public sealed class Vehicle
{
    public Vehicle(string id, string brand, string model, int year, string color, decimal price, VehicleStatus status, DateTime createdAt, DateTime updatedAt, int version = 0)
    { Id=id; Brand=Required(brand,nameof(brand)); Model=Required(model,nameof(model)); Year=ValidYear(year); Color=Required(color,nameof(color)); Price=ValidPrice(price); Status=status; CreatedAt=createdAt; UpdatedAt=updatedAt; Version=version; }
    public string Id { get; } public string Brand { get; private set; } public string Model { get; private set; } public int Year { get; private set; } public string Color { get; private set; } public decimal Price { get; private set; } public VehicleStatus Status { get; private set; } public DateTime CreatedAt { get; } public DateTime UpdatedAt { get; private set; } public int Version { get; private set; }
    public static Vehicle Create(string id,string brand,string model,int year,string color,decimal price,DateTime now)=>new(id,brand,model,year,color,price,VehicleStatus.Available,now,now);
    public void Update(string brand,string model,int year,string color,decimal price,DateTime now) { if(Status!=VehicleStatus.Available) throw new DomainException("Only available vehicles can be edited."); Brand=Required(brand,nameof(brand)); Model=Required(model,nameof(model)); Year=ValidYear(year); Color=Required(color,nameof(color)); Price=ValidPrice(price); UpdatedAt=now; Version++; }
    public void Reserve(DateTime now) { if(Status!=VehicleStatus.Available) throw new DomainException("Vehicle is not available for sale."); Status=VehicleStatus.PaymentPending; UpdatedAt=now; Version++; }
    public void ConfirmSale(DateTime now) { if(Status!=VehicleStatus.PaymentPending) throw new DomainException("Vehicle does not have a pending payment."); Status=VehicleStatus.Sold; UpdatedAt=now; Version++; }
    public void Release(DateTime now) { if(Status!=VehicleStatus.PaymentPending) throw new DomainException("Vehicle does not have a pending payment."); Status=VehicleStatus.Available; UpdatedAt=now; Version++; }
    private static string Required(string value,string name)=>string.IsNullOrWhiteSpace(value)?throw new DomainException($"{name} is required."):value.Trim();
    private static int ValidYear(int value)=>value is <1886 or >2100?throw new DomainException("year is invalid."):value;
    private static decimal ValidPrice(decimal value)=>value<=0||decimal.Round(value,2)!=value?throw new DomainException("price must be positive with two decimal places at most."):value;
}

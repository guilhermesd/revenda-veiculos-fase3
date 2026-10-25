using Fiap.RevendaVeiculos.Domain.Enums;
using Fiap.RevendaVeiculos.Domain.Exceptions;
using Fiap.RevendaVeiculos.Domain.ValueObjects;

namespace Fiap.RevendaVeiculos.Domain.Entities;

public sealed class Sale
{
    public Sale(string id,string vehicleId,string userId,DateTime saleDate,decimal agreedPrice,string paymentCode,PaymentStatus paymentStatus,DateTime createdAt,DateTime updatedAt,DateTime? paidAt=null,DateTime? canceledAt=null)
    { Id=id; VehicleId=vehicleId; UserId=string.IsNullOrWhiteSpace(userId)?throw new DomainException("userId is required."):userId.Trim(); SaleDate=saleDate; AgreedPrice=agreedPrice>0?agreedPrice:throw new DomainException("agreedPrice must be positive."); PaymentCode=string.IsNullOrWhiteSpace(paymentCode)?throw new DomainException("paymentCode is required."):paymentCode.Trim(); PaymentStatus=paymentStatus; CreatedAt=createdAt; UpdatedAt=updatedAt; PaidAt=paidAt; CanceledAt=canceledAt; }
    public string Id {get;} public string VehicleId {get;} public string UserId {get;} public DateTime SaleDate {get;} public decimal AgreedPrice {get;} public string PaymentCode {get;} public PaymentStatus PaymentStatus {get;private set;} public DateTime CreatedAt {get;} public DateTime UpdatedAt {get;private set;} public DateTime? PaidAt {get;private set;} public DateTime? CanceledAt {get;private set;}
    public static Sale Create(string id,string vehicleId,string userId,DateTime saleDate,decimal price,string paymentCode,DateTime now)=>new(id,vehicleId,userId,saleDate,price,paymentCode,PaymentStatus.Pending,now,now);
    public bool Apply(PaymentStatus status,DateTime now) { if(status==PaymentStatus.Pending) throw new DomainException("Payment status must be final."); if(PaymentStatus==status)return false; if(PaymentStatus!=PaymentStatus.Pending)throw new DomainException("Payment is already finalized with a different status."); PaymentStatus=status; UpdatedAt=now; if(status==PaymentStatus.Paid)PaidAt=now;else CanceledAt=now;return true; }
}

using Fiap.RevendaVeiculos.Domain;

namespace Fiap.RevendaVeiculos.UnitTests;

public class VehicleTests
{
    private static readonly DateTime Now = new(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Available_vehicle_can_be_reserved_confirmed_and_cannot_be_edited()
    {
        var vehicle = Vehicle.Create("507f1f77bcf86cd799439011", "Fiat", "Uno", 2020, "Prata", 30000m, Now);
        vehicle.Reserve(Now); vehicle.ConfirmSale(Now);
        Assert.Equal(VehicleStatus.Sold, vehicle.Status);
        Assert.Throws<DomainException>(() => vehicle.Update("Fiat", "Uno", 2020, "Preto", 31000m, Now));
    }

    [Fact]
    public void Canceled_payment_releases_vehicle()
    {
        var vehicle = Vehicle.Create("507f1f77bcf86cd799439011", "Fiat", "Uno", 2020, "Prata", 30000m, Now);
        vehicle.Reserve(Now); vehicle.Release(Now);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);
    }

    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("111.111.111-11", null)]
    public void Cpf_is_normalized_and_validated(string input, string? expected)
    {
        if (expected is null) Assert.Throws<DomainException>(() => Cpf.Normalize(input));
        else Assert.Equal(expected, Cpf.Normalize(input));
    }
}

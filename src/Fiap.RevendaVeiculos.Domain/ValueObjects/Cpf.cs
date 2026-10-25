using Fiap.RevendaVeiculos.Domain.Exceptions;

namespace Fiap.RevendaVeiculos.Domain.ValueObjects;

public static class Cpf
{
    public static string Normalize(string cpf)
    {
        var digits = new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 11 || digits.Distinct().Count() == 1 || !IsValid(digits)) throw new DomainException("buyerCpf is invalid.");
        return digits;
    }

    private static bool IsValid(string value)
    {
        int Digit(int length) { var sum = 0; for (var i = 0; i < length; i++) sum += (value[i] - '0') * (length + 1 - i); var remainder = sum % 11; return remainder < 2 ? 0 : 11 - remainder; }
        return Digit(9) == value[9] - '0' && Digit(10) == value[10] - '0';
    }
}

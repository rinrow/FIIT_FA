using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        var aDigits = a.GetDigits();
        var bDigits = b.GetDigits();
        uint[] res = new uint[aDigits.Length + bDigits.Length];
        uint carry = 0;
        for (int i = 0; i < aDigits.Length; i++, carry = 0)
        {
            for (int j = 0; j < bDigits.Length; j++)
            {
                var (low, high) = BetterBigInteger.MultiplyFull(aDigits[i], bDigits[j]);
                uint cur = res[i + j];
                var (sum, carry1) = BetterBigInteger.AddWithCarry(res[i + j], low, 0);
                var (total, carry2) = BetterBigInteger.AddWithCarry(sum, carry, 0);
                res[i + j] = total;
                carry = high + carry1 + carry2;
            }
            res[i + bDigits.Length] = carry;
        }
        return new BetterBigInteger(res, a.IsNegative ^ b.IsNegative);
    }
}
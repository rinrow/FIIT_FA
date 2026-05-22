using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;
using System.Numerics;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit = 0;

    private uint _smallValue = 0;
    private uint[]? _data = null;

    public bool IsNegative => _signBit == 1;
    private const int NumOfBits = sizeof(uint) * 8;
    private const int BitsPerByte = 8;
    private const int UIntBits = sizeof(uint) * BitsPerByte;
    private const int HalfUIntBits = UIntBits / 2;
    private const uint HighBitMask = 1u << (UIntBits - 1);

    public static (uint sum, uint carry) AddWithCarry(uint x, uint y, uint carryIn)
    {
        /*
        b < Base
        a + b - Base < a
        */
        uint s1 = x + carryIn;
        uint c1 = s1 < x ? 1u : 0u;

        uint s2 = s1 + y;
        uint c2 = s2 < s1 ? 1u : 0u;

        return (s2, c1 | c2);
    }

    public static (uint diff, uint borrow) SubWithBorrow(uint a, uint b, uint borrowIn)
    {
        uint d1 = a - borrowIn;
        uint b1 = d1 > a ? 1u : 0u;

        uint d2 = d1 - b;
        uint b2 = d2 > d1 ? 1u : 0u;

        return (d2, b1 | b2);
    }

    public static (uint low, uint high) MultiplyFull(uint x, uint y)
    {
        uint a0 = x & 0xFFFF, a1 = x >> 16;
        uint b0 = y & 0xFFFF, b1 = y >> 16;

        uint p00 = b0 * a0;
        uint p11 = a1 * b1;
        uint p01 = a0 * b1;
        uint p10 = a1 * b0;

        uint c0 = p00 & 0xFFFF;
        uint c1 = (p00 >> 16) + (p01 & 0xFFFF) + (p10 & 0xFFFF);
        uint c2 = (p01 >> 16) + (p10 >> 16) + (p11 & 0xFFFF) + (c1 >> 16);
        uint c3 = (p11 >> 16) + (c2 >> 16);

        uint low = c0 | (c1 << 16);
        uint high = (c2 & 0xFFFF) | (c3 << 16);

        return (low, high);
    }

    public static (uint quotient, uint remainder) DivideWithCarry(uint carry, uint digit, uint divisor)
    {
        if (divisor == 0) throw new DivideByZeroException();

        uint rem = 0;
        uint quot = 0;

        for (int bit = 2 * UIntBits - 1; bit >= 0; bit--)
        {
            bool overflow = (rem & HighBitMask) != 0;
            rem <<= 1;

            if (bit >= UIntBits)
                rem |= carry >> (bit - UIntBits) & 1;
            else 
                rem |= digit >> bit & 1;

            if (overflow || rem >= divisor)
            {
                rem -= divisor;
                if (bit < UIntBits)
                    quot |= 1u << bit;
            }
        }

        return (quot, rem);
    }

    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        int len = digits.Length;
        while (len > 0 && digits[len - 1] == 0) len--;
        _data = null;

        if (len == 0) return;

        _signBit = isNegative ? 1 : 0;

        if (len == 1)
        {
            _smallValue = digits[0];
            return;
        }

        _data = digits[..len];
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false) : this(digits.ToArray(), isNegative) { }

    public BetterBigInteger(string value, int radix)
    {
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix), "radix must be in [2..36]");
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("value cannot be empty");

        value = value.Trim();
        int start = 0;
        bool isNegative = false;

        if (value[start] == '+' || value[start] == '-')
        {
            isNegative = value[start] == '-';
            start++;
        }
        if (start == value.Length) throw new FormatException("Sign without digits");

        while (start < value.Length && value[start] == '0') start++;

        List<uint> words = new List<uint> { 0 };

        for (int i = start; i < value.Length; i++)
        {
            uint digitValue = ParseDigit(value[i], radix);

            MultiplyBySmall(words, (uint)radix);
            AddSmall(words, digitValue);
        }

        TrimLeadingZeros(words);

        if (words.Count == 1 && words[0] == 0)
        {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
            return;
        }

        _signBit = isNegative ? 1 : 0;

        if (words.Count == 1)
        {
            _smallValue = words[0];
            _data = null;
            return;
        }

        _data = words.ToArray();
    }

    private static uint ParseDigit(char c, int radix)
    {
        int value;
        if (c >= '0' && c <= '9')
            value = c - '0';
        else if (c >= 'A' && c <= 'Z')
            value = c - 'A' + 10;
        else if (c >= 'a' && c <= 'z')
            value = c - 'a' + 10;
        else
            throw new FormatException($"Invalid character {c}");

        if (value >= radix)
            throw new FormatException($"Digit {c} is not valid for radix {radix}");

        return (uint)value;
    }

    private static void MultiplyBySmall(List<uint> digits, uint multiplier)
    {
        if (multiplier == 0)
        {
            digits.Clear();
            digits.Add(0);
            return;
        }

        if (multiplier == 1)
            return;

        int len = digits.Count;
        uint[] result = new uint[len + 1];

        for (int i = 0; i < len; i++)
        {
            var (low, high) = MultiplyFull(digits[i], multiplier);
            uint sum = result[i] + low;
            uint c = sum < result[i] ? 1u : 0u;
            result[i] = sum;
            uint totalHigh = high + c;
            if (totalHigh > 0)
            {
                uint sum2 = result[i + 1] + totalHigh;
                result[i + 1] = sum2;
            }
        }

        digits.Clear();
        int actualLen = result.Length;
        while (actualLen > 0 && result[actualLen - 1] == 0) actualLen--;

        if (actualLen == 0)
        {
            digits.Add(0);
            return;
        }

        for (int i = 0; i < actualLen; i++)
            digits.Add(result[i]);
    }

    private static void AddSmall(List<uint> digits, uint value)
    {
        if (value == 0)
            return;

        uint carry = value;
        for (int i = 0; i < digits.Count && carry > 0; i++)
        {
            uint sum = digits[i] + carry;
            digits[i] = sum;
            carry = sum < digits[i] ? 1u : 0u;
        }

        if (carry > 0)
            digits.Add(carry);
    }

    private static void TrimLeadingZeros(List<uint> digits)
    {
        int i = digits.Count - 1;
        while (i > 0 && digits[i] == 0)
        {
            digits.RemoveAt(i);
            i--;
        }
    }

    public int GetValueFromDigit(char digit)
    {
        if (digit >= '0' && digit <= '9') return digit - '0';
        if (digit >= 'a' && digit <= 'z') return digit - 'a' + 10;
        if (digit >= 'A' && digit <= 'Z') return digit - 'A' + 10;
        return -1;
    }

    public ReadOnlySpan<uint> GetDigits()
        => _data ?? [_smallValue];

    public int CompareTo(IBigInteger? other)
    {
        if (other == null) return 1;
        if (this.IsNegative && !other.IsNegative) return -1;
        if (!this.IsNegative && other.IsNegative) return 1;

        int res = CompareMagnitudes(other);
        return IsNegative ? -res : res;
    }

    private int CompareMagnitudes(IBigInteger other)
    {
        ReadOnlySpan<uint> thisDigits = this.GetDigits();
        ReadOnlySpan<uint> otherDigits = other.GetDigits();
        if (thisDigits.Length != otherDigits.Length)
            return thisDigits.Length < otherDigits.Length ? -1 : 1;

        for (int i = thisDigits.Length - 1; i >= 0; i--)
        {
            if (thisDigits[i] < otherDigits[i]) return -1;
            if (thisDigits[i] > otherDigits[i]) return 1;
        }
        return 0;
    }

    public bool Equals(IBigInteger? other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(_signBit);
        foreach (uint item in GetDigits())
            hash.Add(item);
        return hash.ToHashCode();
    }

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        uint[] sum;
        if (a.IsNegative == b.IsNegative)
        {
            sum = AddMagnitudes(a, b);
            return new(sum, a.IsNegative);
        }

        int cmp = a.CompareMagnitudes(b);
        if (cmp == 0)
            return new("0", 10);
        if (cmp < 0)
        {
            sum = SubMagnitudes(b, a);
            return new(sum, b.IsNegative);
        }
        sum = SubMagnitudes(a, b);
        return new(sum, a.IsNegative);
    }

    private static uint[] AddMagnitudes(BetterBigInteger a, BetterBigInteger b)
        => AddMagnitudes(a.GetDigits(), b.GetDigits());

    public static uint[] AddMagnitudes(ReadOnlySpan<uint> aDigits, ReadOnlySpan<uint> bDigits)
    {
        uint[] sum = new uint[Math.Max(aDigits.Length, bDigits.Length) + 1];
        uint carry = 0;
        int minLen = Math.Min(aDigits.Length, bDigits.Length);
        int i = 0;
        for (; i < minLen; i++)
        {
            var (s, c) = AddWithCarry(aDigits[i], bDigits[i], carry);
            sum[i] = s;
            carry = c;
        }
        ReadOnlySpan<uint> rest = aDigits.Length >= bDigits.Length ? aDigits : bDigits;
        for (; i < rest.Length; i++)
        {
            var (s, c) = AddWithCarry(rest[i], 0, carry);
            sum[i] = s;
            carry = c;
        }
        if (carry > 0) sum[sum.Length - 1] = carry;
        return sum;
    }

    private static uint[] AddNumber(ReadOnlySpan<uint> aDigits, uint digit)
    {
        uint[] sum = new uint[aDigits.Length + 1];
        uint carry = digit;
        for (int i = 0; i < aDigits.Length; i++)
        {
            var (s, c) = AddWithCarry(aDigits[i], 0, carry);
            sum[i] = s;
            carry = c;
        }
        if (carry > 0) sum[sum.Length - 1] = carry;
        return sum;
    }

    private static uint[] SubMagnitudes(BetterBigInteger a, BetterBigInteger b)
        => SubMagnitudes(a.GetDigits(), b.GetDigits());

    public static uint[] SubMagnitudes(ReadOnlySpan<uint> aDigits, ReadOnlySpan<uint> bDigits)
    {
        uint[] sum = new uint[aDigits.Length];
        uint borrow = 0;
        int i = 0;
        for (; i < bDigits.Length; i++)
        {
            // aDigits[i] - bDigits[i] - borrow
            var (d, b) = SubWithBorrow(aDigits[i], bDigits[i], borrow);
            sum[i] = d;
            borrow = b;
        }
        for (; i < aDigits.Length; i++)
        {
            var (d, b) = SubWithBorrow(aDigits[i], 0, borrow);
            sum[i] = d;
            borrow = b;
        }
        return sum;
    }

    private static uint[] SubNumber(ReadOnlySpan<uint> aDigits, uint digit)
    {
        uint[] sum = new uint[aDigits.Length];
        uint borrow = digit;
        for (int i = 0; i < aDigits.Length; i++)
        {
            var (d, b) = SubWithBorrow(aDigits[i], 0, borrow);
            sum[i] = d;
            borrow = b;
        }
        return sum;
    }

    public static void SubMagnitudesInPlace(uint[] aDigits, uint[] bDigits)
    {
        uint borrow = 0;
        int i = 0;
        for (; i < bDigits.Length; i++)
        {
            var (d, b) = SubWithBorrow(aDigits[i], bDigits[i], borrow);
            aDigits[i] = d;
            borrow = b;
        }
        for (; i < aDigits.Length; i++)
        {
            if (borrow == 0) break;
            var (d, b) = SubWithBorrow(aDigits[i], 0, borrow);
            aDigits[i] = d;
            borrow = b;
        }
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        uint[] sum;
        if (a.IsNegative != b.IsNegative)
        {
            sum = AddMagnitudes(a, b);
            return new(sum, a.IsNegative);
        }
        int cmp = a.CompareMagnitudes(b);
        if (cmp == 0) return new("0", 10);
        if (cmp < 0)
        {
            sum = SubMagnitudes(b, a);
            return new(sum, !a.IsNegative);
        }
        sum = SubMagnitudes(a, b);
        return new(sum, a.IsNegative);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
        => new(a.GetDigits().ToArray(), !a.IsNegative);

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        if (bDigits.Length == 1 && bDigits[0] == 0) throw new DivideByZeroException(nameof(b));

        int cmp = a.CompareMagnitudes(b);
        if (cmp < 0) return new("0", 10);
        if (cmp == 0) return new([1], a.IsNegative != b.IsNegative);

        if (bDigits.Length == 1)
            return DivideByOneWord(a, b);
        else
            return DivideAlgorithm(a, b);
    }

    private static BetterBigInteger DivideByOneWord(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        uint bDigit = b.GetDigits()[0];

        uint carry = 0;
        uint[] res = new uint[aDigits.Length];
        for (int i = aDigits.Length - 1; i >= 0; i--)
        {
            var (q, r) = DivideWithCarry(carry, aDigits[i], bDigit);
            res[i] = q;
            carry = r;
        }

        return new(res, a.IsNegative != b.IsNegative);
    }

    private static BetterBigInteger DivideAlgorithm(BetterBigInteger a, BetterBigInteger b)
        => new(DivideAlgorithm(a.GetDigits(), b.GetDigits(), out _), a.IsNegative != b.IsNegative);

    private static uint[] DivideAlgorithm(ReadOnlySpan<uint> aDigitsInitial, ReadOnlySpan<uint> bDigitsInitial, out uint[] rem)
    {
        int shift = BitOperations.LeadingZeroCount(bDigitsInitial[^1]);
        uint[] aDigits = ShiftLeftSigned(aDigitsInitial, shift);
        uint[] bDigits = ShiftLeftSigned(bDigitsInitial, shift);
        Array.Resize(ref aDigits, aDigits.Length + 1);
        aDigits[^1] = 0;

        int resDigitsLen = aDigits.Length - bDigits.Length;
        uint[] res = new uint[resDigitsLen];

        uint bDigit1 = bDigits[^1];
        uint bDigit2 = bDigits[^2];
        for (int i = resDigitsLen - 1; i >= 0; i--)
        {
            uint aDigit1 = aDigits[i + bDigits.Length];
            uint aDigit2 = aDigits[i + bDigits.Length - 1];
            uint aDigit3 = aDigits[i + bDigits.Length - 2];

            // numerator = aDigit1 * Base + aDigit2, делим на bDigit1
            var (q, r) = DivideWithCarry(aDigit1, aDigit2, bDigit1);
            uint quotient = q;
            uint remainder = r;

            // Коррекция частного
            while (quotient == 0xFFFFFFFF || MultiplyCompare(quotient, bDigit2, remainder, aDigit3))
            {
                quotient--;
                var (sum, c) = AddWithCarry(remainder, bDigit1, 0);
                remainder = sum;
                if (c > 0) break;
            }

            // Вычитание quotient * bDigits из aDigits
            uint borrow = 0;
            uint carryMul = 0;
            for (int j = 0; j < bDigits.Length; j++)
            {
                var (low, high) = MultiplyFull(quotient, bDigits[j]);
                var (prod, c1) = AddWithCarry(low, carryMul, 0);
                carryMul = high + c1;

                var (diff, b1) = SubWithBorrow(aDigits[i + j], prod, borrow);
                aDigits[i + j] = diff;
                // SubWithBorrow возвращает borrow 0..2, но для одиночного заёма больше 1 не будет
                borrow = b1;
            }

            var (lastDiff, lastBorrow) = SubWithBorrow(aDigits[i + bDigits.Length], carryMul, borrow);
            aDigits[i + bDigits.Length] = lastDiff;

            if (lastBorrow > 0)
            {
                quotient--;
                uint carryAdd = 0;
                for (int j = 0; j < bDigits.Length; j++)
                {
                    var (s, c) = AddWithCarry(aDigits[i + j], bDigits[j], carryAdd);
                    aDigits[i + j] = s;
                    carryAdd = c;
                }
                aDigits[i + bDigits.Length] += carryAdd;
            }

            res[i] = quotient;
        }

        rem = new uint[bDigitsInitial.Length];
        Array.Copy(aDigits, rem, bDigitsInitial.Length);
        rem = ShiftLeftSigned(rem, -shift);

        return res;
    }

    // Проверка: quotient * bDigit2 > remainder * Base + aDigit3
    private static bool MultiplyCompare(uint quotient, uint bDigit2, uint remainder, uint aDigit3)
    {
        var (low, high) = MultiplyFull(quotient, bDigit2);
        // Сравниваем (high, low) с (remainder, aDigit3)
        if (high > remainder) return true;
        if (high < remainder) return false;
        return low > aDigit3;
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        if (bDigits.Length == 1 && bDigits[0] == 0) throw new DivideByZeroException(nameof(b));

        int cmp = a.CompareMagnitudes(b);
        if (cmp < 0) return new(a.GetDigits().ToArray(), a.IsNegative);
        if (cmp == 0) return new("0", 10);

        if (bDigits.Length == 1)
            return GetRemainderDivByOneWord(a, b);
        else
            return GetRemainderDivideAlgorithm(a, b);
    }

    private static BetterBigInteger GetRemainderDivByOneWord(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        uint bDigit = b.GetDigits()[0];

        uint carry = 0;
        for (int i = aDigits.Length - 1; i >= 0; i--)
        {
            var (_, r) = DivideWithCarry(carry, aDigits[i], bDigit);
            carry = r;
        }

        return new([carry], a.IsNegative);
    }

    private static BetterBigInteger GetRemainderDivideAlgorithm(BetterBigInteger a, BetterBigInteger b)
    {
        DivideAlgorithm(a.GetDigits(), b.GetDigits(), out uint[] rem);
        return new(rem, a.IsNegative);
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        IMultiplier multiplier = new SimpleMultiplier();
        return multiplier.Multiply(a, b);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        return new(a.IsNegative
            ? SubNumber(a.GetDigits(), 1)
            : AddNumber(a.GetDigits(), 1), !a.IsNegative);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        int length = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        ReadOnlySpan<uint> aDigits = ToTwosComplement(a, length);
        ReadOnlySpan<uint> bDigits = ToTwosComplement(b, length);

        uint[] res = new uint[length];
        for (int i = 0; i < length; i++)
            res[i] = aDigits[i] & bDigits[i];

        return FromTwosComplement(res);
    }

    private static ReadOnlySpan<uint> ToTwosComplement(BetterBigInteger a, int length)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        uint[] res = new uint[length];
        for (int i = 0; i < aDigits.Length; i++)
            res[i] = aDigits[i];

        if (!a.IsNegative) return res;

        for (int i = 0; i < res.Length; i++)
            res[i] = ~res[i];

        return AddNumber(res, 1);
    }

    private static BetterBigInteger FromTwosComplement(uint[] digits)
    {
        if (digits[^1] >> (NumOfBits - 1) == 0)
            return new(digits, false);

        uint[] res = new uint[digits.Length - 1];
        for (int i = 0; i < res.Length; i++)
            res[i] = ~digits[i];

        return new(AddNumberInPlace(res, 1), true);
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        int length = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        ReadOnlySpan<uint> aDigits = ToTwosComplement(a, length);
        ReadOnlySpan<uint> bDigits = ToTwosComplement(b, length);

        uint[] res = new uint[length];
        for (int i = 0; i < length; i++)
            res[i] = aDigits[i] | bDigits[i];

        return FromTwosComplement(res);
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        int length = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        ReadOnlySpan<uint> aDigits = ToTwosComplement(a, length);
        ReadOnlySpan<uint> bDigits = ToTwosComplement(b, length);

        uint[] res = new uint[length];
        for (int i = 0; i < length; i++)
            res[i] = aDigits[i] ^ bDigits[i];

        return FromTwosComplement(res);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift) => ShiftLeftSigned(a, shift);
    public static BetterBigInteger operator >>(BetterBigInteger a, int shift) => ShiftLeftSigned(a, -shift);

    private static BetterBigInteger ShiftLeftSigned(BetterBigInteger a, int shift)
        => new(ShiftLeftSigned(a.GetDigits(), shift, a.IsNegative), a.IsNegative);

    private static uint[] ShiftLeftSigned(ReadOnlySpan<uint> digits, int shift, bool isNegative = false)
    {
        if (shift == 0) return digits.ToArray();

        int unsignedShift = Math.Abs(shift);
        int newLength;
        if (shift < 0)
        {
            if (unsignedShift >= digits.Length * NumOfBits)
                return isNegative ? [1] : [0];
            newLength = digits.Length;
        }
        else
            newLength = digits.Length + (unsignedShift - 1) / NumOfBits + 1;

        uint[] newDigits = new uint[newLength];
        int indexShift = unsignedShift / NumOfBits;
        int localShift = unsignedShift % NumOfBits;
        uint carry = 0;
        bool add1ForTwosComplement = false;

        if (shift < 0 && isNegative)
        {
            for (int i = 0; i < indexShift; i++)
            {
                if (digits[i] != 0) { add1ForTwosComplement = true; break; }
            }
            if (!add1ForTwosComplement)
            {
                int mask = (1 << localShift) - 1;
                add1ForTwosComplement = (mask & digits[indexShift]) != 0;
            }
        }

        if (localShift == 0)
        {
            if (shift < 0)
                for (int i = digits.Length - 1; i >= indexShift; i--)
                    newDigits[i - indexShift] = digits[i];
            else
                for (int i = 0; i + indexShift < newDigits.Length; i++)
                    newDigits[i + indexShift] = digits[i];
        }
        else
        {
            if (shift < 0)
            {
                for (int i = digits.Length - 1; i >= indexShift; i--)
                {
                    newDigits[i - indexShift] = carry | (digits[i] >> localShift);
                    carry = digits[i] << (NumOfBits - localShift);
                }
            }
            else
            {
                for (int i = 0; i + indexShift < newDigits.Length - 1; i++)
                {
                    newDigits[i + indexShift] = carry | (digits[i] << localShift);
                    carry = digits[i] >> (NumOfBits - localShift);
                }
                if (carry != 0) newDigits[^1] = carry;
            }
        }

        if (add1ForTwosComplement)
            return AddNumberInPlace(newDigits, 1);
        return newDigits;
    }

    private static uint[] AddNumberInPlace(uint[] aDigits, uint digit)
    {
        uint carry = digit;
        for (int i = 0; i < aDigits.Length; i++)
        {
            var (s, c) = AddWithCarry(aDigits[i], 0, carry);
            aDigits[i] = s;
            carry = c;
        }
        if (carry > 0)
        {
            uint[] newDigits = new uint[aDigits.Length + 1];
            Array.Copy(aDigits, newDigits, aDigits.Length);
            newDigits[^1] = carry;
            return newDigits;
        }
        return aDigits;
    }

    public static void AddMagnitudesInPlace(uint[] aDigits, ReadOnlySpan<uint> bDigits, int startIndex)
    {
        uint carry = 0;
        int i = 0;
        for (; i < bDigits.Length; i++)
        {
            var (s, c) = AddWithCarry(aDigits[i + startIndex], bDigits[i], carry);
            aDigits[i + startIndex] = s;
            carry = c;
        }
        i += startIndex;
        for (; i < aDigits.Length && carry > 0; i++)
        {
            var (s, c) = AddWithCarry(aDigits[i], 0, carry);
            aDigits[i] = s;
            carry = c;
        }
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);
    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix), "radix must be in [2..36]");
        if (IsZero()) return "0";

        List<char> res = new();
        uint[] digits = GetDigits().ToArray();
        int start = digits.Length - 1;

        while (true)
        {
            while (start >= 0 && digits[start] == 0) start--;
            if (start < 0) break;

            uint rem = DivideInPlaceAndGetRem(digits, (uint)radix, start);
            res.Add(GetDigitFromValue(rem));
        }
        if (res[^1] == '0') res.RemoveAt(res.Count - 1);
        if (IsNegative) res.Add('-');
        res.Reverse();
        return new string(res.ToArray());
    }

    private static char GetDigitFromValue(uint val)
    {
        if (val >= 36) throw new ArgumentOutOfRangeException(nameof(val));
        if (val <= 9) return (char)('0' + val);
        return (char)(val - 10 + 'A');
    }

    private static uint DivideInPlaceAndGetRem(uint[] aDigits, uint bDigit, int start)
    {
        uint carry = 0;
        for (int i = start; i >= 0; i--)
        {
            var (q, r) = DivideWithCarry(carry, aDigits[i], bDigit);
            aDigits[i] = q;
            carry = r;
        }
        return carry;
    }

    public bool IsZero() => IsZero(GetDigits());

    private static bool IsZero(ReadOnlySpan<uint> digits)
    {
        foreach (uint item in digits)
            if (item > 0) return false;
        return true;
    }
}
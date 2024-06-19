using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ArenaOverhaul.Extensions
{
    public static class EnumExtensions
    {
        public static bool IsPowerOfTwo(ulong x)
        {
            return (x & (x - 1)) == 0;
        }
        public static IEnumerable<T> GetAllItems<T>(this T value, bool accountForZeros = false) where T : struct
        {
            if (value is not Enum)
            {
                throw new InvalidOperationException("The struct must be of Enum type!");
            }
            foreach (T item in Enum.GetValues(typeof(T)))
            {
                if (accountForZeros || Convert.ToInt32(item) != 0)
                {
                    yield return item;
                }
            }
        }
        public static bool Contains<T>(this T value, T request, bool accountForZeros = false) where T : struct
        {
            if (!typeof(T).IsEnum || !typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false))
            {
                throw new InvalidOperationException("The struct must be of Enum type with Flags attribute!");
            }
            int valueAsInt = Convert.ToInt32(value);
            int requestAsInt = Convert.ToInt32(request);
            return (accountForZeros || requestAsInt != 0) && requestAsInt == (valueAsInt & requestAsInt);
        }
        public static IEnumerable<T> GetSelectedItems<T>(this T value, bool accountForZeros = false, bool accountForNamedIntersections = false) where T : struct
        {
            if (!typeof(T).IsEnum || !typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false))
            {
                throw new InvalidOperationException("The struct must be of Enum type with Flags attribute!");
            }
            foreach (T item in Enum.GetValues(typeof(T)))
            {
                if ((accountForNamedIntersections || IsPowerOfTwo(Convert.ToUInt64(item))) && value.Contains(item, accountForZeros))
                {
                    yield return item;
                }
            }
        }
        public static IEnumerable<T> GetDefinedFlags<T>(this T value, bool accountForZeros = false) where T : struct
        {
            if (value is not Enum || !typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false))
            {
                throw new InvalidOperationException("The struct must be of Enum type with Flags attribute!");
            }
            foreach (T item in Enum.GetValues(typeof(T)))
            {
                if (IsPowerOfTwo(Convert.ToUInt64(item)) && (accountForZeros || Convert.ToInt32(item) != 0))
                {
                    yield return item;
                }
            }
        }
        public static IEnumerable<T> GetPossibleVariations<T>(this T value, bool accountForZeros = false) where T : struct
        {
            static int CountSetBits(int n)
            {
                int count = 0;
                while (n > 0)
                {
                    count += n & 1;
                    n >>= 1;
                }
                return count;
            }

            if (!typeof(T).IsEnum || !typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false))
            {
                throw new InvalidOperationException("The struct must be of Enum type with Flags attribute!");
            }
            Type type = value.GetType();
            for (int numberOfBitsSet = accountForZeros ? 0 : 1; numberOfBitsSet < (value.GetDefinedFlags(accountForZeros).Max(item => Convert.ToInt32(item)) << 1) - 1; numberOfBitsSet++)
            {
                for (int idx = value.GetDefinedFlags(accountForZeros).Min(item => Convert.ToInt32(item)); idx < value.GetDefinedFlags(accountForZeros).Max(item => Convert.ToInt32(item)) << 1; idx++)
                {
                    if ((accountForZeros || idx != 0) && CountSetBits(idx) == numberOfBitsSet)
                    {
                        yield return (T) Enum.Parse(typeof(T), idx.ToString(), true);
                    }
                }
            }
        }
        public static string GetDescription<T>(this T value, bool useLocalizedStrings = false) where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("The struct must be of Enum type!");
            }
            Type type = value.GetType();
            if (type.IsDefined(typeof(FlagsAttribute), inherit: false) && !IsPowerOfTwo(Convert.ToUInt64(value)) && !Enum.IsDefined(type, value))
            {
                StringBuilder ResultBuilder = new(string.Empty);
                foreach (T item in value.GetSelectedItems())
                {
                    ResultBuilder.Append(ResultBuilder.Length == 0 ? item.GetDescription(useLocalizedStrings) : ", " + item.GetDescription(useLocalizedStrings));
                }
                return ResultBuilder.ToString();
            }
            string name = Enum.GetName(type, value)!;
            if (name != null)
            {
                FieldInfo field = type.GetField(name)!;
                if (field != null)
                {
                    if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
                    {
                        return useLocalizedStrings ? attr.Description.ToLocalizedString() : attr.Description;
                    }
                }
            }
            return useLocalizedStrings ? value.ToString()!.ToLocalizedString() : value.ToString()!;
        }
    }
}

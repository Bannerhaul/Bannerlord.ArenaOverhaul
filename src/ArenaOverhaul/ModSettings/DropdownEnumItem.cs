using ArenaOverhaul.Extensions;

using System.Collections.Generic;
using System;
using System.Linq;

namespace ArenaOverhaul.ModSettings
{
    public class DropdownEnumItem<T> where T : struct
    {
        public T EnumValue { get; }
        public int Index { get; }
        public DropdownEnumItem(T enumValue, int index)
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("The struct must be of Enum type!");
            }
            EnumValue = enumValue;
            Index = index;
        }
        public override string ToString() => EnumValue.GetDescription(useLocalizedStrings: true);
        public static IEnumerable<DropdownEnumItem<T>> SetDropdownListFromEnum(bool accountForZeros = true, bool GetAllVariations = true)
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("The struct must be of Enum type!");
            }
            T enumValue = Enum.GetValues(typeof(T)).Cast<T>().First();
            var dropdownItems = (typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false) ? (GetAllVariations ? enumValue.GetPossibleVariations(accountForZeros) : enumValue.GetDefinedFlags(accountForZeros)) : enumValue.GetAllItems   (accountForZeros)).ToList();
            int idx = -1;
            foreach (T item in dropdownItems)
            {
                idx++;
                yield return new DropdownEnumItem<T>(item, idx);
            }
        }
        public static int GetEnumIndex(T enumValue, bool accountingForZeros = true, bool UsingAllVariations = true)
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("The struct must be of Enum type!");
            }
            var dropdownItems = (typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false) ? (UsingAllVariations ? enumValue.GetPossibleVariations(accountingForZeros) : enumValue.GetDefinedFlags(accountingForZeros)) : enumValue.GetAllItems(accountingForZeros)).ToList();
            int idx = -1;
            foreach (T item in dropdownItems)
            {
                idx++;
                if (Convert.ToInt32(item) == Convert.ToInt32(enumValue))
                {
                    return idx;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(enumValue), enumValue, "Value not found in dropdown list!");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Unity.CV.SyntheticHumans.Tags
{
    public static class SyntheticHumanAgeRangeExtension
    {
        static readonly Dictionary<SyntheticHumanAgeRange, (int, int)> s_SyntheticHumanAgeRangeToAgeRange = new()
        {
            //[min,max) - inclusive of min, exclusive of max
            {SyntheticHumanAgeRange.Newborn, (0, 1)},
            {SyntheticHumanAgeRange.Toddler, (1, 3)},
            {SyntheticHumanAgeRange.Child1, (3, 6)},
            {SyntheticHumanAgeRange.Child2, (6, 9)},
            {SyntheticHumanAgeRange.Preteen, (9, 13)},
            {SyntheticHumanAgeRange.Teen, (13, 20)},
            {SyntheticHumanAgeRange.Adult, (20, 65)},
            {SyntheticHumanAgeRange.Elderly, (65, int.MaxValue)},
        };

        public static SyntheticHumanAgeRange GetSyntheticHumanAgeRange(int age)
        {
            if (age < 0)
            {
                Debug.LogError("Invalid age for SyntheticHumanAgeRange");
                return SyntheticHumanAgeRange.None;
            }

            foreach (var pair in s_SyntheticHumanAgeRangeToAgeRange)
            {
                if (age >= pair.Value.Item1 && age < pair.Value.Item2)
                    return pair.Key;
            }
            return SyntheticHumanAgeRange.None;
        }

        public static (int, int) GetSyntheticHumanAgeRange(this SyntheticHumanAgeRange syntheticHumanAgeRange)
        {
            return s_SyntheticHumanAgeRangeToAgeRange.ContainsKey(syntheticHumanAgeRange)
                ? s_SyntheticHumanAgeRangeToAgeRange[syntheticHumanAgeRange]
                : (0, 0);
        }
    }
}

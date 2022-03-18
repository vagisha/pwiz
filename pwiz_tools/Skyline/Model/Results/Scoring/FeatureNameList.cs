﻿using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace pwiz.Skyline.Model.Results.Scoring
{
    public class FeatureNameList : AbstractReadOnlyList<string>
    {
        public static readonly FeatureNameList EMPTY = new FeatureNameList(ImmutableList.Empty<string>());

        private static readonly Dictionary<string, IPeakFeatureCalculator> _calculatorsByTypeName;
        static FeatureNameList()
        {
            _calculatorsByTypeName = PeakFeatureCalculator.Calculators.ToDictionary(calc => calc.FullyQualifiedName);
        }
        private readonly ImmutableList<string> _names;
        private readonly Dictionary<string, int> _dictionary;
        private readonly int _hashCode;

        public static FeatureNameList FromCalculators(IEnumerable<IPeakFeatureCalculator> calculators)
        {
            return FromScoreTypes(calculators.Select(calc => calc.GetType()));
        }

        public static FeatureNameList FromScoreTypes(IEnumerable<Type> types)
        {
            return new FeatureNameList(types.Select(type => type.FullName));
        }
        
        public FeatureNameList(IEnumerable<string> names)
        {
            _names = ImmutableList.ValueOfOrEmpty(names);
            _hashCode = _names.GetHashCode();
            _dictionary = new Dictionary<string, int>();
            for (int i = 0; i < _names.Count; i++)
            {
                string name = _names[i];
                if (name != null && !_dictionary.ContainsKey(name))
                {
                    _dictionary.Add(name, i);
                }
            }
        }

        public override int Count
        {
            get { return _names.Count; }
        }

        public override string this[int index] => _names[index];

        public override int IndexOf(string item)
        {
            if (_dictionary.TryGetValue(item, out int index))
            {
                return index;
            }

            return -1;
        }

        public int IndexOf(Type type)
        {
            return IndexOf(type.FullName);
        }

        public int IndexOf(IPeakFeatureCalculator calculator)
        {
            return IndexOf(calculator.FullyQualifiedName);
        }

        protected bool Equals(FeatureNameList other)
        {
            return Equals(_names, other._names);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FeatureNameList) obj);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public IEnumerable<IPeakFeatureCalculator> AsCalculators()
        {
            return this.Select(CalculatorFromTypeName);
        }

        public IEnumerable<Type> AsCalculatorTypes()
        {
            return AsCalculators().Select(calc => calc?.GetType());
        }

        public static IPeakFeatureCalculator CalculatorFromTypeName(string name)
        {
            _calculatorsByTypeName.TryGetValue(name, out var calculator);
            return calculator;
        }
    }
}

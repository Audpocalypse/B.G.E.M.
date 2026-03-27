using System;
using System.Collections.Generic;
using System.Linq;

namespace Material_Editor.Models
{
    internal enum BulkFindScope
    {
        All,
        ByFile,
        ByField
    }

    internal enum BulkFindValueType
    {
        Text,
        Number,
        Boolean
    }

    internal enum BulkFindReplaceAction
    {
        FindNext,
        FindPrevious,
        Replace,
        ReplaceAll,
        ApplyAsFilter
    }

    internal sealed class BulkFindReplaceRequest
    {
        public BulkFindScope Scope { get; set; } = BulkFindScope.All;
        public BulkFindValueType ValueType { get; set; } = BulkFindValueType.Text;
        public string FindText { get; set; } = string.Empty;
        public string ReplaceText { get; set; } = string.Empty;
        public bool FindBooleanValue { get; set; } = true;
        public bool ReplaceBooleanValue { get; set; }
        public List<string> FieldLabels { get; set; } = new();

        public BulkFindReplaceRequest Clone()
        {
            return new BulkFindReplaceRequest
            {
                Scope = Scope,
                ValueType = ValueType,
                FindText = FindText ?? string.Empty,
                ReplaceText = ReplaceText ?? string.Empty,
                FindBooleanValue = FindBooleanValue,
                ReplaceBooleanValue = ReplaceBooleanValue,
                FieldLabels = (FieldLabels ?? new List<string>()).ToList()
            };
        }

        public string GetFindValueText()
        {
            return ValueType == BulkFindValueType.Boolean
                ? (FindBooleanValue ? bool.TrueString : bool.FalseString)
                : FindText ?? string.Empty;
        }

        public string GetReplaceValueText()
        {
            return ValueType == BulkFindValueType.Boolean
                ? (ReplaceBooleanValue ? bool.TrueString : bool.FalseString)
                : ReplaceText ?? string.Empty;
        }
    }
}

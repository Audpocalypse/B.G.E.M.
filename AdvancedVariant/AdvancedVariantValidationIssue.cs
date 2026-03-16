namespace Material_Editor.AdvancedVariant
{
    public sealed class AdvancedVariantValidationIssue
    {
        public AdvancedVariantValidationIssue(string message)
        {
            Message = message ?? string.Empty;
        }

        public string Message { get; }
    }
}

namespace Material_Editor.Services
{
    public enum FieldCopyStatus
    {
        Success,
        Skipped,
        Failed
    }

    public sealed class FieldCopyResult
    {
        public FieldCopyResult(string targetPath, FieldCopyStatus status, string message)
        {
            TargetPath = targetPath;
            Status = status;
            Message = message;
        }

        public string TargetPath { get; }
        public FieldCopyStatus Status { get; }
        public string Message { get; }
    }
}

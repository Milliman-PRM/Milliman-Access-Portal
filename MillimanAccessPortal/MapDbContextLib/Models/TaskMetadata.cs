namespace MapDbContextLib.Models
{
    public enum TaskErrorReason
    {
        Default = 0,
        NoSelectedFieldValues = 1,
    }

    public class TaskMetadata
    {
        public TaskErrorReason ErrorReason { get; set; } = TaskErrorReason.Default;
    }
}

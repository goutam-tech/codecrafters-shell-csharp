using System.Diagnostics;
public class Job
{
    public int JobNumber { get; set; }
    public int ProcessId { get; set; }
    public string Command { get; set; } = "";
    public string Status { get; set; } = "Running";
    public Process Process { get; set; } = null!;
}
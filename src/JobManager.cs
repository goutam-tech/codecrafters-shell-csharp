using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;

public static class JobManager
{
    public class Job
    {
        public int JobNumber { get; set; }
        public int ProcessId { get; set; }
        public string Command { get; set; } = "";
        public string Status { get; set; } = "Running";
        public Process Process { get; set; } = null!;
    }

    private static readonly List<Job> jobs = new();

    public static Job AddJob(Process process, string command)
    {
        int jobNumber = jobs.Count == 0
            ? 1
            : jobs.Max(j => j.JobNumber) + 1;

        var job = new Job
        {
            JobNumber = jobNumber,
            ProcessId = process.Id,
            Command = command,
            Status = "Running",
            Process = process
        };

        jobs.Add(job);

        return job;
    }

    private static void MarkExited()
    {
        foreach (Job job in jobs)
        {
            if (job.Status != "Running")
            {
                continue;
            }

            bool exited;

            try
            {
                // WaitForExit(0) forces a real, non-cached exit check
                // (unlike HasExited, which can read a stale cached value).
                exited = job.Process.WaitForExit(0);

                if (!exited)
                {
                    job.Process.Refresh();
                    exited = job.Process.HasExited;
                }
            }
            catch
            {
                exited = true;
            }

            if (exited)
            {
                job.Status = "Done";
            }
        }
    }

    public static Job? GetJob(int jobNumber)
    {
        return jobs.FirstOrDefault(j => j.JobNumber == jobNumber);
    }
    public static void RemoveJob(int jobNumber)
    {
        jobs.RemoveAll(j => j.JobNumber == jobNumber);
    }

    public static void PrintJobs()
    {
        MarkExited();

        List<Job> snapshot = jobs.OrderBy(j => j.JobNumber).ToList();

        int currentJobNumber = snapshot.Count > 0 ? snapshot[^1].JobNumber : -1;
        int previousJobNumber = snapshot.Count > 1 ? snapshot[^2].JobNumber : -1;

        foreach (Job job in snapshot)
        {
            char marker = ' ';

            if (job.JobNumber == currentJobNumber)
            {
                marker = '+';
            }
            else if (job.JobNumber == previousJobNumber)
            {
                marker = '-';
            }

            string status = job.Status.PadRight(24);

            string commandDisplay = job.Status == "Running"
                ? job.Command + " &"
                : job.Command;

            Console.WriteLine($"[{job.JobNumber}]{marker}  {status}{commandDisplay}");
        }

        jobs.RemoveAll(j => j.Status == "Done");
    }

    public static void ReapExitedJobs()
    {
        MarkExited();

        List<Job> snapshot = jobs.OrderBy(j => j.JobNumber).ToList();

        int currentJobNumber = snapshot.Count > 0 ? snapshot[^1].JobNumber : -1;
        int previousJobNumber = snapshot.Count > 1 ? snapshot[^2].JobNumber : -1;

        foreach (Job job in snapshot)
        {
            if (job.Status != "Done")
            {
                continue;
            }

            char marker = ' ';

            if (job.JobNumber == currentJobNumber)
            {
                marker = '+';
            }
            else if (job.JobNumber == previousJobNumber)
            {
                marker = '-';
            }

            string status = job.Status.PadRight(24);

            Console.WriteLine($"[{job.JobNumber}]{marker}  {status}{job.Command}");
        }

        jobs.RemoveAll(j => j.Status == "Done");
    }
}
// New file: JobManager.cs
using System.Collections.Generic;
using System.Linq;
using System;

public static class JobManager
{
    private static readonly List<Job> jobs = new();
    private static int nextJobNumber = 1;

    public static Job AddJob(int processId, string command)
    {
        var job = new Job
        {
            JobNumber = nextJobNumber++,
            ProcessId = processId,
            Command = command,
            Status = "Running"
        };

        jobs.Add(job);

        return job;
    }

    public static void PrintJobs()
    {
        int currentJobNumber = jobs.Count > 0 ? jobs[^1].JobNumber : -1;
        int previousJobNumber = jobs.Count > 1 ? jobs[^2].JobNumber : -1;

        foreach (Job job in jobs.OrderBy(j => j.JobNumber))
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

            Console.WriteLine(
                $"[{job.JobNumber}]{marker}  {status}{job.Command} &"
            );
        }
    }
}
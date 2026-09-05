using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public static class ProcessManager
{
    private const int SIGHUP = 1;
    private const int SIGINT = 2;
    private const int SIGQUIT = 3;
    private const int SIGKILL = 9;
    private const int SIGUSR1 = 10;
    private const int SIGUSR2 = 12;
    private const int SIGTERM = 15;
    private const int SIGCONT = 18;
    private const int SIGSTOP = 19;

    private const int SC_CLK_TCK = 2;

    private static readonly Dictionary<string, int> SignalNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HUP"] = SIGHUP,
        ["INT"] = SIGINT,
        ["QUIT"] = SIGQUIT,
        ["KILL"] = SIGKILL,
        ["USR1"] = SIGUSR1,
        ["USR2"] = SIGUSR2,
        ["TERM"] = SIGTERM,
        ["CONT"] = SIGCONT,
        ["STOP"] = SIGSTOP
    };

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    [DllImport("libc")]
    private static extern long sysconf(int name);

    private struct ProcessRow
    {
        public int Pid;
        public string Status;
        public double CpuPercent;
        public double MemoryMb;
        public string Command;
    }

    private static bool SendSignal(int pid, int sig)
    {
        return kill(pid, sig) == 0;
    }

    private static int ParseSignal(string token)
    {
        string name = token.TrimStart('-');

        if (int.TryParse(name, out int number))
        {
            return number;
        }

        if (name.StartsWith("SIG", StringComparison.OrdinalIgnoreCase))
        {
            name = name[3..];
        }

        return SignalNames.TryGetValue(name, out int sig) ? sig : SIGTERM;
    }

    public static void HandleKill(List<string> arguments)
    {
        int signal = SIGTERM;
        var targets = new List<int>();

        for (int i = 1; i < arguments.Count; i++)
        {
            string token = arguments[i];

            if (token.StartsWith("-"))
            {
                signal = ParseSignal(token);
                continue;
            }

            if (int.TryParse(token, out int pid))
            {
                targets.Add(pid);
            }
        }

        if (targets.Count == 0)
        {
            Console.WriteLine("kill: usage: kill [-signal] pid ...");
            return;
        }

        foreach (int pid in targets)
        {
            if (!SendSignal(pid, signal))
            {
                Console.WriteLine($"kill: ({pid}) - No such process");
            }
        }
    }

    public static void HandleKillAll(List<string> arguments)
    {
        int signal = SIGTERM;
        string? name = null;

        for (int i = 1; i < arguments.Count; i++)
        {
            string token = arguments[i];

            if (token.StartsWith("-"))
            {
                signal = ParseSignal(token);
                continue;
            }

            name = token;
        }

        if (name == null)
        {
            Console.WriteLine("killall: usage: killall [-signal] name");
            return;
        }

        bool matched = false;

        foreach (ProcessRow row in ReadProcesses())
        {
            if (!row.Command.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            matched = true;
            SendSignal(row.Pid, signal);
        }

        if (!matched)
        {
            Console.WriteLine($"killall: {name}: no process found");
        }
    }

    public static void HandleFg(List<string> arguments)
    {
        if (arguments.Count < 2 || !int.TryParse(arguments[1], out int jobNumber))
        {
            Console.WriteLine("fg: usage: fg <job>");
            return;
        }

        JobManager.Job? job = JobManager.GetJob(jobNumber);

        if (job == null)
        {
            Console.WriteLine($"fg: {jobNumber}: no such job");
            return;
        }

        if (job.Status == "Stopped")
        {
            SendSignal(job.ProcessId, SIGCONT);
            job.Status = "Running";
        }

        Console.WriteLine(job.Command);

        try
        {
            job.Process.WaitForExit();
        }
        catch
        {
        }

        JobManager.RemoveJob(jobNumber);
    }

    public static void HandleBg(List<string> arguments)
    {
        if (arguments.Count < 2 || !int.TryParse(arguments[1], out int jobNumber))
        {
            Console.WriteLine("bg: usage: bg <job>");
            return;
        }

        JobManager.Job? job = JobManager.GetJob(jobNumber);

        if (job == null)
        {
            Console.WriteLine($"bg: {jobNumber}: no such job");
            return;
        }

        if (job.Status != "Stopped")
        {
            Console.WriteLine($"bg: job {jobNumber} is already running");
            return;
        }

        SendSignal(job.ProcessId, SIGCONT);
        job.Status = "Running";

        Console.WriteLine($"[{jobNumber}]+ {job.Command} &");
    }

    private static long ClockTicksPerSecond()
    {
        long ticks = sysconf(SC_CLK_TCK);
        return ticks > 0 ? ticks : 100;
    }

    private static double SystemUptimeSeconds()
    {
        try
        {
            string text = File.ReadAllText("/proc/uptime");
            string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return double.Parse(parts[0], CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }

    private static string MapStatus(char state)
    {
        return state switch
        {
            'R' => "Running",
            'S' => "Sleeping",
            'D' => "Waiting",
            'Z' => "Zombie",
            'T' => "Stopped",
            't' => "Tracing",
            'X' => "Dead",
            _ => "Unknown"
        };
    }

    private static IEnumerable<ProcessRow> ReadProcesses()
    {
        long hz = ClockTicksPerSecond();
        double uptime = SystemUptimeSeconds();
        var rows = new List<ProcessRow>();

        foreach (string dir in Directory.EnumerateDirectories("/proc"))
        {
            string pidText = Path.GetFileName(dir);

            if (!int.TryParse(pidText, out int pid))
            {
                continue;
            }

            try
            {
                string stat = File.ReadAllText($"/proc/{pid}/stat");

                int openParen = stat.IndexOf('(');
                int closeParen = stat.LastIndexOf(')');

                string rest = stat[(closeParen + 2)..];
                string[] fields = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                char state = fields[0][0];
                long utime = long.Parse(fields[11]);
                long stime = long.Parse(fields[12]);
                long startTime = long.Parse(fields[19]);

                double totalTime = (utime + stime) / (double)hz;
                double processAge = uptime - (startTime / (double)hz);

                double cpuPercent = processAge > 0
                    ? Math.Max(0, Math.Min(100, 100 * totalTime / processAge))
                    : 0;

                double memoryMb = 0;

                foreach (string line in File.ReadLines($"/proc/{pid}/status"))
                {
                    if (line.StartsWith("VmRSS:"))
                    {
                        string value = line.Split(':', StringSplitOptions.RemoveEmptyEntries)[1]
                            .Trim()
                            .Split(' ')[0];

                        memoryMb = long.Parse(value) / 1024.0;
                        break;
                    }
                }

                string command;
                byte[] cmdlineBytes = File.ReadAllBytes($"/proc/{pid}/cmdline");

                if (cmdlineBytes.Length > 0)
                {
                    command = string.Join(
                        " ",
                        Encoding.UTF8.GetString(cmdlineBytes)
                            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
                    );
                }
                else
                {
                    command = $"[{stat[(openParen + 1)..closeParen]}]";
                }

                rows.Add(new ProcessRow
                {
                    Pid = pid,
                    Status = MapStatus(state),
                    CpuPercent = cpuPercent,
                    MemoryMb = memoryMb,
                    Command = command
                });
            }
            catch
            {
                continue;
            }
        }

        return rows;
    }

    public static void PrintProcessTable()
    {
        List<ProcessRow> rows = ReadProcesses()
            .OrderBy(r => r.Pid)
            .ToList();

        Console.WriteLine(
            "PID".PadRight(8) +
            "STATUS".PadRight(13) +
            "CPU".PadRight(10) +
            "MEMORY".PadRight(12) +
            "COMMAND"
        );

        foreach (ProcessRow row in rows)
        {
            string cpu = $"{row.CpuPercent.ToString("F1", CultureInfo.InvariantCulture)}%";
            string memory = $"{row.MemoryMb.ToString("F0", CultureInfo.InvariantCulture)} MB";

            Console.WriteLine(
                row.Pid.ToString().PadRight(8) +
                row.Status.PadRight(13) +
                cpu.PadRight(10) +
                memory.PadRight(12) +
                row.Command
            );
        }
    }
}
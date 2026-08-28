class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");
            var command = Console.ReadLine();
            if (command == "exit")
            {
                break;
            }
            else if (command.StartsWith("echo "))
            {
                Console.WriteLine(command[5..]);
            }
            else if (command.StartsWith("type "))
            {
                HandleType(command[5..]);
            }
            else
            {
                Console.WriteLine($"{command}: command not found");
            }
        }
    }

    static void HandleType(string command)
    {
        if (command == "echo" || command == "exit" || command == "type")
        {
            Console.WriteLine($"{command} is a shell builtin");
        }
        else
        {
            Console.WriteLine($"{command}: not found");
        }
    }
}
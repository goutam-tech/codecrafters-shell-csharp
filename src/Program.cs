class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");
            var command = Console.ReadLine();
            Console.WriteLine($"{command}: command not found");
        }
    }
}
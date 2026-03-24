using LC3.Emulation.Core;

namespace LC3.Emulation.TestSystem
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var proc = new Processor();
            proc.OnDebugInfo += (info) => Console.WriteLine(info);
            while (true)
            {
                try
                {
                    proc.Step();
                } catch {
                    continue;
                }
            }
        }
    }
}

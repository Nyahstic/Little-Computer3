using LC3.Emulation.Core;
using System.Diagnostics;

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
                    Console.Write($"R_PC: {proc.RegisterFile[(int)Processor.Register.R_PC]:X4} ");
                    for (int i = 0; i < proc.RegisterFile.Length; i++)
                    {
                        Console.Write($"R{i}: {proc.RegisterFile[i]:X4} ");
                    }
                    Console.WriteLine();
                    proc.Step();
                    Thread.Sleep(250);
                    
                } catch(Exception ex) {
                    Debug.WriteLine(ex);
                    continue;
                }
            }
        }
    }
}

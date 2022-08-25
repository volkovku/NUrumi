using BenchmarkDotNet.Running;

namespace NUrumi.Benchmark
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
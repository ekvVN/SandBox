namespace Benchmarks
{
    using System.Numerics;
    using BenchmarkDotNet.Attributes;
    using GpuAleaExample;

    public class GpuAlea
    {
        private readonly AleaCalc _calc = new AleaCalc();

        [Params(10000)]
        public BigInteger Length { get; set; }

        [Benchmark(Description = "GPU")]
        public void TestGpu()
        {
            _calc.Length = Length;
            _calc.FactorialGpu();
        }

        [Benchmark(Description = "CPU")]
        public void TestCpu()
        {
            _calc.Length = Length;
            _calc.FactorialCpu();
        }
    }
}

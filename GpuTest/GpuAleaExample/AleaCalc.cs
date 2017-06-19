namespace GpuAleaExample
{
    using System;
    using System.Numerics;
    using Alea;

    public class AleaCalc
    {
        public static BigInteger Factorial(BigInteger number)
        {
            BigInteger bi = 1;
            for (var i = 1; i <= number; i++)
            {
                bi *= i;
            }
            return bi;
        }

        public BigInteger Length { get; set; }

        public void FactorialGpu()
        {
            var gpu = Gpu.Default;

            var func = new Func<BigInteger>(() => Factorial(Length));
            gpu.EvalFunc(func);
        }

        public void FactorialCpu()
        {
            Factorial(Length);
        }
    }
}

using BenchmarkDotNet.Attributes;
using _4DND;
using Microsoft.VSDiagnostics;

namespace _4DND.Benchmarks
{
    [CPUUsageDiagnoser]
    public class WorldGeneratorBenchmarks
    {
        private const int GridSize = 200;
        private const int Seed = 42;
        [Benchmark]
        public float GetNoiseBenchmark()
        {
            float sum = 0;
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    sum += WorldGenerator.GetBiome(x * 10f, y * 10f, Seed) == BiomeType.Forest ? 1f : 0f;
                }
            }

            return sum;
        }
    }
}
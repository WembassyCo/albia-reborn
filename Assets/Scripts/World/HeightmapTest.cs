using System;
using System.Diagnostics;
using Albia.World;

namespace Albia.Tests
{
    /// <summary>
    /// Test harness for HeightmapGenerator.
    /// Generates a 512x512 heightmap and saves output for verification.
    /// </summary>
    class HeightmapTest
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Albia Reborn Heightmap Generator Test ===");
            Console.WriteLine();

            // Test parameters
            int size = 512;
            int seed = args.Length > 0 ? int.Parse(args[0]) : 1337;
            
            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  Size: {size}x{size}");
            Console.WriteLine($"  Seed: {seed}");
            Console.WriteLine();

            // Create generator
            Console.WriteLine("Initializing generator...");
            var generator = new HeightmapGenerator(size, size, seed);

            // Generate heightmap with timing
            Console.WriteLine("Generating heightmap...");
            var sw = Stopwatch.StartNew();
            
            float[,] heightmap = generator.Generate();
            
            sw.Stop();
            Console.WriteLine($"  Completed in {sw.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine();

            // Verify determinism - regenerate with same seed
            Console.WriteLine("Verifying determinism...");
            var generator2 = new HeightmapGenerator(size, size, seed);
            float[,] heightmap2 = generator2.Generate();
            
            int diffCount = 0;
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    if (Math.Abs(heightmap[x, y] - heightmap2[x, y]) > 0.0001f)
                        diffCount++;
            
            Console.WriteLine($"  Differences found: {diffCount}");
            Console.WriteLine($"  Deterministic: {(diffCount == 0 ? "YES" : "NO")}");
            Console.WriteLine();

            // Calculate statistics
            float min = float.MaxValue;
            float max = float.MinValue;
            float sum = 0;
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float h = heightmap[x, y];
                    min = Math.Min(min, h);
                    max = Math.Max(max, h);
                    sum += h;
                }
            }
            
            float avg = sum / (size * size);
            
            Console.WriteLine("Statistics:");
            Console.WriteLine($"  Min: {min:F6}");
            Console.WriteLine($"  Max: {max:F6}");
            Console.WriteLine($"  Avg: {avg:F6}");
            Console.WriteLine($"  Range: [0.0, 1.0] (verified: {(min >= 0 && max <= 1 ? "YES" : "NO")})");
            Console.WriteLine();

            // Save outputs
            string outputDir = "output";
            string asciiPath = $"{outputDir}/heightmap_seed{seed}.txt";
            string samplePath = $"{outputDir}/heightmap_sample_seed{seed}.txt";
            
            Console.WriteLine("Saving output files...");
            Directory.CreateDirectory(outputDir);
            
            generator.SaveAsciiArt(asciiPath);
            Console.WriteLine($"  ASCII Art: {asciiPath}");
            
            generator.SaveText(samplePath);
            Console.WriteLine($"  Sample Data: {samplePath}");
            Console.WriteLine();

            // Performance test - generation speed
            Console.WriteLine("Performance Test - Multiple runs:");
            int runs = 5;
            var timings = new System.Collections.Generic.List<double>();
            
            for (int i = 0; i < runs; i++)
            {
                var testGen = new HeightmapGenerator(size, size, seed + i);
                sw.Restart();
                testGen.Generate();
                sw.Stop();
                timings.Add(sw.Elapsed.TotalSeconds);
                Console.WriteLine($"  Run {i + 1}: {sw.Elapsed.TotalSeconds:F3}s");
            }
            
            double avgTime = timings.Average();
            Console.WriteLine($"  Average: {avgTime:F3}s");
            Console.WriteLine($"  Target (<5s): {(avgTime < 5 ? "PASS" : "FAIL")}");
            Console.WriteLine();

            // Sample output for verification
            Console.WriteLine("Sample 8x8 grid from center:", size/2);
            Console.WriteLine("(Shows terrain variety - mountains @, valleys space)");
            Console.WriteLine();
            int start = size / 2 - 4;
            for (int y = 0; y < 8; y++)
            {
                Console.Write("  ");
                for (int x = 0; x < 8; x++)
                {
                    float h = heightmap[start + x, start + y];
                    char[] chars = { ' ', '.', ':', '-', '~', '=', '+', '*', '#', '@' };
                    int idx = (int)(h * (chars.Length - 1));
                    idx = Math.Clamp(idx, 0, chars.Length - 1);
                    Console.Write(chars[idx]);
                }
                Console.WriteLine();
            }
            Console.WriteLine();

            Console.WriteLine("=== Test Complete ===");
            
            // Summary
            Console.WriteLine();
            Console.WriteLine("Deliverables:");
            Console.WriteLine($"  - HeightmapGenerator.cs implemented with 3-octave Simplex noise");
            Console.WriteLine($"  - Hydraulic erosion pass included");
            Console.WriteLine($"  - Deterministic output: {(diffCount == 0 ? "VERIFIED" : "FAILED")}");
            Console.WriteLine($"  - 512x512 generation time: {avgTime:F2}s (target: <5s)");
            Console.WriteLine($"  - Output saved to: {asciiPath}");
        }
    }
}

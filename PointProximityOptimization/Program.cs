// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Drawing;

List<PointF> points = new List<PointF>();
Size size = new Size(10, 10);
const float radius = 1;

Random rng = new Random(0);
Stopwatch setupSW = Stopwatch.StartNew();
for (int i = 0; i < 10_000; i++)
{
    double x = rng.NextDouble() * size.Width;
    double y = rng.NextDouble() * size.Height;
    points.Add(new PointF((float)x, (float)y));
}
setupSW.Stop();
Console.WriteLine($"{points.Count} points in an area of {size.Width * size.Height} with dimensions [{size.Width};{size.Height}] ({setupSW.Elapsed.Milliseconds}ms)");

Console.WriteLine($"Search with radius of {radius:0.00}:");
List<Func<IReadOnlyList<PointF>, Size, float, int>> funcs = new() { BasicSearch, ParallelSearch };
foreach (var func in funcs)
{
    string name = func.Method.Name;
    Stopwatch sw = Stopwatch.StartNew();
    int count = func(points, size, radius);
    sw.Stop();
    long time = sw.ElapsedMilliseconds;
    Console.WriteLine($"\t{name} result for highest proximity count: {count} ({time}ms)");
}
Console.ReadKey(true);

int BasicSearch(IReadOnlyList<PointF> points, Size size, float radius)
{
    int total = points.Count;
    int highestCount = 0;
    for (int i = 0; i < total; i++)
    {
        int count = 0;
        for (int j = 0; j < total; j++)
        {
            if (j == i) continue;
            if (GetDistance(points[i], points[j]) < radius) count++;
        }
        if (count > highestCount) highestCount = count;
    }
    return highestCount;
}
int ParallelSearch(IReadOnlyList<PointF> points, Size size, float radius)
{
    int total = points.Count;
    int highestCount = 0;
    Parallel.For(0, total, (i) =>
    {
        int count = 0;
        for (int j = 0; j < total; j++)
        {
            if (j == i) continue;
            if (GetDistance(points[i], points[j]) < radius) count++;
        }
        if (count > highestCount) highestCount = count;
    });
    return highestCount;
}

float GetDistance(PointF p1, PointF p2)
{
    float dx = p1.X - p2.X;
    float dy = p1.Y - p2.Y;
    return MathF.Sqrt(dx * dx + dy * dy);
}
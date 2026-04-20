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
List<Func<IReadOnlyList<PointF>, Size, float, int>> funcs = new() { BasicSearch, ParallelSearch, GridSearch };
foreach (var func in funcs)
{
    string name = func.Method.Name[12..^4];
    Stopwatch sw = Stopwatch.StartNew();
    int count = func(points, size, radius);
    sw.Stop();
    long time = sw.ElapsedMilliseconds;
    Console.WriteLine($"\t{name} result for highest proximity count with radius of {radius}: {count} ({time}ms)");
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
int GridSearch(IReadOnlyList<PointF> points, Size size, float radius)
{
    //Create Offsets
    List<Point> offsets = new() {
        new Point(-1, -1), new Point( 0, -1), new Point( 1, -1),
        new Point(-1,  0), new Point( 0,  0), new Point( 1,  0),
        new Point(-1,  1), new Point( 0,  1), new Point( 1,  1),
    };
    //Create Grid
    float cellSize = radius;
    int width = (int)(size.Width / cellSize);
    int height = (int)(size.Height / cellSize);
    List<PointF>[,] grid = new List<PointF>[width, height];
    List<Point> coords = new List<Point>();
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            coords.Add(new Point(x, y));
            grid[x, y] = new();
        }
    }
    //Fill Grid
    foreach (PointF point in points) grid[(int)(point.X / cellSize), (int)(point.Y / cellSize)].Add(point);
    //Search Grid
    int total = points.Count;
    int highestCount = 0;
    foreach (var coord in coords)
    {
        List<PointF> list1 = grid[coord.X, coord.Y];
        foreach (var point1 in list1)
        {
            int count = 0;
            foreach (var offset in offsets)
            {
                int x = coord.X + offset.X;
                int y = coord.Y + offset.Y;
                if (x < 0 || y < 0) continue;
                if (x >= width || y >= height) continue;
                List<PointF> list2 = grid[x, y];
                foreach (var point2 in list2)
                {
                    if (point1 == point2) continue;
                    if (GetDistance(point1, point2) < radius) count++;
                }
            }
            if (count > highestCount) highestCount = count;
        }
    }
    return highestCount;
}

float GetDistance(PointF p1, PointF p2)
{
    float dx = p1.X - p2.X;
    float dy = p1.Y - p2.Y;
    return MathF.Sqrt(dx * dx + dy * dy);
}
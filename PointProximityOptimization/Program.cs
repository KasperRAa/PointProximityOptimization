// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using System.Drawing;

Size size = new Size(10, 10);
List<Func<Size, IReadOnlyList<PointF>, float, int>> allFuncs = new() { BasicSearch, ParallelSearch, GridSearch, ParallelGridSearch };

while (true)
{
    List<int?> counts = new() { null, 10, 50, 100, 500, 1_000, 5_000, 10_000, 50_000, 100_000 };
    int? count = PickFromOptions(counts, (i) => i.HasValue ? i.Value.ToString() : "Exit");
    if (count is null) break;
    Console.Clear();
    List<PointF> points = GetPoints(count.Value);
    while (true)
    {
        List<float?> radii = new() { null, 0.1f, 0.25f, 0.5f, 0.75f, 1f, 2f, 4f, 8f, 16f };
        float? radius = PickFromOptions(radii, (i) => i.HasValue ? i.Value.ToString() : "Go back to Count");
        if (radius is null) break;
        Console.Clear();
        while (true)
        {
            Console.WriteLine($"{points.Count} points in an area of {size.Width * size.Height} with dimensions [{size.Width};{size.Height}] and radius of {radius}");
            List<Action<Size, IReadOnlyList<PointF>, float, List<Func<Size, IReadOnlyList<PointF>, float, int>>>?> displayOptions = new() { null, TryAllSearches, TrySpecificSearch };
            var func = PickFromOptions(displayOptions, f => f is not null ? f.Method.Name[12..^4] : "Go back to Radius");
            if (func is null) break;
            Console.Clear();

            func(size, points, radius.Value, allFuncs);
            Console.ReadKey(true);
            Console.Clear();
        }
    }
}

List<PointF> GetPoints(int total)
{
    Random rng = new Random();
    List<PointF> points = new();
    Stopwatch setupSW = Stopwatch.StartNew();
    for (int i = 0; i < total; i++)
    {
        double x = rng.NextDouble() * size.Width;
        double y = rng.NextDouble() * size.Height;
        points.Add(new PointF((float)x, (float)y));
    }
    setupSW.Stop();
    Console.WriteLine($"{points.Count} points in an area of {size.Width * size.Height} with dimensions [{size.Width};{size.Height}] ({setupSW.Elapsed.Milliseconds}ms)");
    return points;
}


int BasicSearch(Size size, IReadOnlyList<PointF> points, float radius)
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
int ParallelSearch(Size size, IReadOnlyList<PointF> points, float radius)
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
int GridSearch(Size size, IReadOnlyList<PointF> points, float radius)
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
int ParallelGridSearch(Size size, IReadOnlyList<PointF> points, float radius)
{
    //Create Offsets
    List<Point> offsets = new() {
        new Point(-1, -1), new Point( 0, -1), new Point( 1, -1),
        new Point(-1,  0), new Point( 0,  0), new Point( 1,  0),
        new Point(-1,  1), new Point( 0,  1), new Point( 1,  1),
    };
    //Create Grid
    float cellSize = radius;
    int width = (int)(size.Width / cellSize) + 1;
    int height = (int)(size.Height / cellSize) + 1;
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
    Parallel.ForEach(coords, (coord) =>
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
    });
    return highestCount;
}

float GetDistance(PointF p1, PointF p2)
{
    float dx = p1.X - p2.X;
    float dy = p1.Y - p2.Y;
    return MathF.Sqrt(dx * dx + dy * dy);
}

void TryAllSearches(Size size, IReadOnlyList<PointF> points, float radius, List<Func<Size, IReadOnlyList<PointF>, float, int>> funcs)
{
    Console.WriteLine($"{points.Count} points in an area of {size.Width * size.Height} with dimensions [{size.Width};{size.Height}] and radius of {radius:0.00}:");
    foreach (var func in funcs)
    {
        Console.Write("\t");
        TrySearch(size, points, radius, func);
    }
}
void TrySpecificSearch(Size size, IReadOnlyList<PointF> points, float radius, List<Func<Size, IReadOnlyList<PointF>, float, int>> funcs)
{
    var func = PickFromOptions(funcs, GetNameOfFunc);
    Console.WriteLine($"{points.Count} points in an area of {size.Width * size.Height} with dimensions [{size.Width};{size.Height}] and radius of {radius:0.00}:");
    Console.Write("\t");
    TrySearch(size, points, radius, func);
}
void TrySearch(Size size, IReadOnlyList<PointF> points, float radius, Func<Size, IReadOnlyList<PointF>, float, int> func)
{
    string name = GetNameOfFunc(func);
    Stopwatch sw = Stopwatch.StartNew();
    int count = func(size, points, radius);
    sw.Stop();
    long time = sw.ElapsedMilliseconds;
    Console.WriteLine($"{name} result for highest proximity count with radius of {radius}: {count} ({time}ms)");
}

T? PickFromOptions<T>(List<T?> options, Func<T, string> getString)
{
    int count = options.Count;
    if (count > 10) throw new ArgumentOutOfRangeException("\"options\" must be less than 10");
    Console.WriteLine("Choose 1 of:");
    char? c = null;
    while (c is null || !char.IsDigit(c.Value) || int.Parse(c.Value.ToString()) >= count)
    {
        int n = 0;
        foreach (var option in options) Console.WriteLine($"\t{n++}: {getString(option)}");
        c = Console.ReadKey(true).KeyChar;
    }
    return options[int.Parse(c.Value.ToString())];
}

string GetNameOfFunc(Func<Size, IReadOnlyList<PointF>, float, int> func) => func.Method.Name[12..^4];
// See https://aka.ms/new-console-template for more information

using System.Drawing;

List<PointF> points = new List<PointF>();
Size size = new Size(10, 10);
const float radius = 1;

const string midFix = " search result for highest proximity count: ";

Random rng = new Random(0);

for (int i = 0; i < 10_000; i++)
{
    double x = rng.NextDouble() * size.Width;
    double y = rng.NextDouble() * size.Height;
    points.Add(new PointF((float)x, (float)y));
}

Console.WriteLine($"{points.Count} points in an area of {size.Width * size.Height} with dimensions [{size.Width};{size.Height}]");
BasicSearch(points, radius);

void BasicSearch(IReadOnlyList<PointF> points, float radius)
{
    int total = points.Count;
    int highestCount = 0;
    for (int i = 0; i < total; i++)
    {
        int count = 0;
        for (int j = 0; j < total; j++)
        {
            if (j == i) continue;
            if ((points[i].ToVector2() - points[j].ToVector2()).Length() < radius) count++;
            if (count > highestCount) highestCount = count;
        }
    }
    Console.WriteLine($"Basic{midFix}{highestCount}");
}
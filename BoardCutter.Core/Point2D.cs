namespace BoardCutter.Core;

public record Point2D(int X, int Y)
{
    public Point2D Add(Point2D input) => new(X + input.X, Y + input.Y);
}

    

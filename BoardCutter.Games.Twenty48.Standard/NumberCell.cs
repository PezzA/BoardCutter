using BoardCutter.Core;

namespace BoardCutter.Games.Twenty48.Standard
{
    public record NumberCell(int Id, int Value, Point2D Point, bool New, bool Destroy);
}

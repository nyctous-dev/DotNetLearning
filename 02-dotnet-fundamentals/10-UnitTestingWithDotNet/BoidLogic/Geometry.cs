namespace BoidLogic;

public static class Geometry
{
    public static double CalculateDistance(
        double x1,
        double y1,
        double x2,
        double y2)
    {
        double deltaX = x2 - x1;
        double deltaY = y2 - y1;

        return Math.Sqrt(
            deltaX * deltaX + deltaY * deltaY);
    }
}
using System;
using System.Collections.Generic;

namespace DelaunayVoronoi
{
    public class Vertex
    {
        public double X { get; }
        public double Y { get; }
        public HashSet<Triangle> AdjacentTriangles { get; } = new HashSet<Triangle>();

        public Vertex(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vertex(double x, double y, double color)
        {
            X = x;
            Y = y;
            Value = color;
        }

        public static Vertex operator+(Vertex p1, Vertex p2)
        {
            return new Vertex(
                p1.X + p2.X,
                p1.Y + p2.Y
            );
        }

        public static Vertex operator-(Vertex p1, Vertex p2)
        {
            return new Vertex(
                p1.X - p2.X,
                p1.Y - p2.Y
            );
        }

        public static Vertex operator/(Vertex p1, double k)
        {
            return new Vertex(
                p1.X / k,
                p1.Y / k
            );
        }

        public static Vertex operator*(double k, Vertex pt)
        {
            return new Vertex(
                k * pt.X ,
                k * pt.Y
            );
        }

        public static double DistanceSquared(Vertex p1, Vertex p2)
        {
            var x_diff = p1.X - p2.X;
            var y_diff = p1.Y - p2.Y;
            return x_diff * x_diff + y_diff * y_diff;
        }

        public static double Distance(Vertex p1, Vertex p2)
        {
            return Math.Sqrt(DistanceSquared(p1, p2));
        }
        public double Value { get; set; }

        static public bool operator <(Vertex p1, Vertex p2)
        {
            if (p1.Y < p2.Y)
                return true;
            if (p1.X < p2.X)
                return true;
            return false;
        }

        static public bool operator >(Vertex p1, Vertex p2)
        {
            if (p1 < p2)
                return false;
            if (p1.X == p2.X && p1.Y == p2.Y)
                return false;
            return true;
        }

    }
}
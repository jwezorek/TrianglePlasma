using System;
using System.Collections.Generic;
using System.Linq;
using TrianglePlasma;

namespace DelaunayVoronoi
{
    public class Triangle
    {
        public Vertex[] Vertices { get; } = new Vertex[3];
        public Vertex Circumcenter { get; private set; }
        public double RadiusSquared;

        public IEnumerable<Triangle> TrianglesWithSharedEdge {
            get {
                var neighbors = new HashSet<Triangle>();
                foreach (var vertex in Vertices)
                {
                    var trianglesWithSharedEdge = vertex.AdjacentTriangles.Where(o =>
                    {
                        return o != this && SharesEdgeWith(o);
                    });
                    neighbors.UnionWith(trianglesWithSharedEdge);
                }

                return neighbors;
            }
        }

        public double Area
        {
            get;
            private set;
        }

        private double CalcArea()
        {
            double x1 = Vertices[0].X;
            double y1 = Vertices[0].Y;
            double x2 = Vertices[1].X;
            double y2 = Vertices[1].Y;
            double x3 = Vertices[2].X;
            double y3 = Vertices[2].Y;

            return 0.5*((x2-x1)*(y3-y1) - (x3-x1)*(y2-y1));
        }

        public Vertex RandomPoint
        {
            get
            {
                // from https://www.cs.princeton.edu/~funk/tog02.pdf section 4.2
                var r1 = RandomHelper.Random.NextDouble();
                var r2 = RandomHelper.Random.NextDouble();
                var root_r1 = Math.Sqrt(r1);
                var A = Vertices[0];
                var B = Vertices[1];
                var C = Vertices[2];
                return (1.0 - root_r1) * A + root_r1 * (1.0 - r2) * B + root_r1 * r2 * C;
            }
        }

        public Triangle(Vertex point1, Vertex point2, Vertex point3)
        {
            if (!IsCounterClockwise(point1, point2, point3))
            {
                Vertices[0] = point1;
                Vertices[1] = point3;
                Vertices[2] = point2;
            }
            else
            {
                Vertices[0] = point1;
                Vertices[1] = point2;
                Vertices[2] = point3;
            }

            Vertices[0].AdjacentTriangles.Add(this);
            Vertices[1].AdjacentTriangles.Add(this);
            Vertices[2].AdjacentTriangles.Add(this);
            UpdateCircumcircle();
            Area = CalcArea();
        }

        private void UpdateCircumcircle()
        {
            // https://codefound.wordpress.com/2013/02/21/how-to-compute-a-circumcircle/#more-58
            // https://en.wikipedia.org/wiki/Circumscribed_circle
            var p0 = Vertices[0];
            var p1 = Vertices[1];
            var p2 = Vertices[2];
            var dA = p0.X * p0.X + p0.Y * p0.Y;
            var dB = p1.X * p1.X + p1.Y * p1.Y;
            var dC = p2.X * p2.X + p2.Y * p2.Y;

            var aux1 = (dA * (p2.Y - p1.Y) + dB * (p0.Y - p2.Y) + dC * (p1.Y - p0.Y));
            var aux2 = -(dA * (p2.X - p1.X) + dB * (p0.X - p2.X) + dC * (p1.X - p0.X));
            var div = (2 * (p0.X * (p2.Y - p1.Y) + p1.X * (p0.Y - p2.Y) + p2.X * (p1.Y - p0.Y)));

            if (div == 0)
            {
                throw new System.Exception();
            }

            var center = new Vertex(aux1 / div, aux2 / div);
            Circumcenter = center;
            RadiusSquared = (center.X - p0.X) * (center.X - p0.X) + (center.Y - p0.Y) * (center.Y - p0.Y);
        }

        private bool IsCounterClockwise(Vertex point1, Vertex point2, Vertex point3)
        {
            var result = (point2.X - point1.X) * (point3.Y - point1.Y) -
                (point3.X - point1.X) * (point2.Y - point1.Y);
            return result > 0;
        }

        public bool SharesEdgeWith(Triangle triangle)
        {
            var sharedVertices = Vertices.Where(o => triangle.Vertices.Contains(o)).Count();
            return sharedVertices == 2;
        }

        public bool IsPointInsideCircumcircle(Vertex point)
        {
            var d_squared = (point.X - Circumcenter.X) * (point.X - Circumcenter.X) +
                (point.Y - Circumcenter.Y) * (point.Y - Circumcenter.Y);
            return d_squared < RadiusSquared;
        }

    }
}
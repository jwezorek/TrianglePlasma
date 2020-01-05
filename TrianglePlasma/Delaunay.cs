using System;
using System.Collections.Generic;
using System.Linq;

namespace DelaunayVoronoi
{
    public class DelaunayTriangulator
    {
        private double _maxX;
        private double _maxY;
        private HashSet<Triangle> _triangles;
        private SortedSet<Triangle> _trianglesByArea;

        public DelaunayTriangulator(double maxX, double maxY)
        {
            _maxX = maxX;
            _maxY = maxY;

            var corner_points = new List<Vertex> {
                new Vertex(0, 0),
                new Vertex(0, _maxY),
                new Vertex(_maxX, _maxY),
                new Vertex(_maxX, 0)
            };

            var tri1 = new Triangle(corner_points[0], corner_points[1], corner_points[2]);
            var tri2 = new Triangle(corner_points[0], corner_points[2], corner_points[3]);
            _triangles = new HashSet<Triangle> { };
            _trianglesByArea = new SortedSet<Triangle>(new CompareByArea());
            Insert(tri1);
            Insert(tri2);
        }

        private void Insert(Triangle t)
        {
            _triangles.Add(t);
            _trianglesByArea.Add(t);
        }

        private void Remove(Triangle t)
        {
            _triangles.Remove(t);
            _trianglesByArea.Remove(t);
        }

        public Vertex SubdivideTriangle(Triangle tri)
        {
            var pt = tri.RandomPoint;

            var badTriangles = FindBadTriangles(pt, tri);
            var polygon = FindHoleBoundaries(badTriangles); 

            foreach (var bad in badTriangles)
                RemoveTriangle(bad);

            foreach (var edge in polygon)  {
                var triangle = new Triangle(pt, edge.Point1, edge.Point2);
                Insert(triangle);
            }

            return pt;
        }

        private void RemoveTriangle(Triangle tri)
        {
            foreach (var vertex in tri.Vertices)
                vertex.AdjacentTriangles.Remove(tri);
            Remove(tri);
        }

        public IEnumerable<Vertex> Vertices
        {
            get
            {
                return Triangles.SelectMany(tri => tri.Vertices).Distinct();
            }
        }

        public IEnumerable<Triangle> AddVertices(IEnumerable<Vertex> points)
        {
            foreach (var point in points)
            {
                var badTriangles = FindBadTriangles(point);
                var polygon = FindHoleBoundaries(badTriangles);

                foreach (var triangle in badTriangles)
                {
                    foreach (var vertex in triangle.Vertices)
                    {
                        vertex.AdjacentTriangles.Remove(triangle);
                    }
                }
                foreach (var bad in badTriangles)
                    Remove(bad);

                foreach (var edge in polygon)
                {
                    var triangle = new Triangle(point, edge.Point1, edge.Point2);
                    Insert(triangle);
                }
            }

            return _triangles;
        }

        private List<Edge> FindHoleBoundaries(ISet<Triangle> badTriangles)
        {
            var edges = new List<Edge>();
            foreach (var triangle in badTriangles)
            {
                edges.Add(new Edge(triangle.Vertices[0], triangle.Vertices[1]));
                edges.Add(new Edge(triangle.Vertices[1], triangle.Vertices[2]));
                edges.Add(new Edge(triangle.Vertices[2], triangle.Vertices[0]));
            }
            var boundary = new HashSet<Edge>();
            foreach(var edge in edges)
            {
                if (boundary.Contains(edge))
                    boundary.Remove(edge);
                else
                    boundary.Add(edge);
            }
            return boundary.ToList();
        }

        private HashSet<Triangle> FindBadTriangles(Vertex pt, Triangle sourceTriangle)
        {
            HashSet<Triangle> triangles = new HashSet<Triangle>();
            Action<Triangle> dfs = null;

            dfs = (Triangle t) => {
                if (!t.IsPointInsideCircumcircle(pt))
                    return;
                triangles.Add(t);
                foreach (var neighbor in t.TrianglesWithSharedEdge)
                    if (! triangles.Contains(neighbor))
                        dfs(neighbor);
            };
            dfs(sourceTriangle);

            return triangles;
        }

        private ISet<Triangle> FindBadTriangles(Vertex point)
        {
            var badTriangles = _triangles.Where(o => o.IsPointInsideCircumcircle(point));
            return new HashSet<Triangle>(badTriangles);
        }

        public IEnumerable<Triangle> Triangles
        {
            get
            {
                return _triangles;
            }
        }

        public Triangle Largest
        {
            get
            {
                return _trianglesByArea.Max;
            }
        }

        private class CompareByArea : IComparer<Triangle>
        {
            public int Compare(Triangle x, Triangle y)
            {
                var compared = x.Area.CompareTo(y.Area);
                if (compared != 0)
                    return compared;
                var hashCodeCompare = x.GetHashCode().CompareTo(y.GetHashCode());
                if (hashCodeCompare != 0)
                    return hashCodeCompare;

                if (Object.ReferenceEquals(x, y))
                    return 0;

                throw new Exception();
            }
        }
    }
}
namespace DelaunayVoronoi
{
    public class Edge
    {
        public Vertex Point1 { get; }
        public Vertex Point2 { get; }

        public Edge(Vertex point1, Vertex point2)
        {
            if (point1 < point2)
            {
                Point1 = point1;
                Point2 = point2;
            } else
            {
                Point1 = point2;
                Point2 = point1;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != GetType()) return false;
            var edge = obj as Edge;

            var samePoints = Point1 == edge.Point1 && Point2 == edge.Point2;
            var samePointsReversed = Point1 == edge.Point2 && Point2 == edge.Point1;
            return samePoints || samePointsReversed;
        }

        public override int GetHashCode()
        {
            int hCode = (int)Point1.X ^ (int)Point1.Y ^ (int)Point2.X ^ (int)Point2.Y;
            return hCode.GetHashCode();
        }
    }
}
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
            double eps = 0.0005;
            long hCode = (long) (Point1.X/eps) ^ (long) (Point1.Y/eps) ^ 
                (long) (Point2.X/eps) ^ (long)(Point2.Y/eps);
            return hCode.GetHashCode();
        }
    }
}
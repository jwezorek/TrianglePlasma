using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DelaunayVoronoi;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace TrianglePlasma
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new TrianglePlasmaOptions(args);
            if (!options.IsValid)
                return;

            var triangles = GenerateTrianglePlasma(
                options.Width, 
                options.Height, 
                options.InitialVertexDistance,
                options.MinAreaCutoff, 
                options.ValuePerturbationParam, 
                options.Contrast, 
                GetPerturbationFunction(options.ValuePerturbationMethod)
            );

            Console.WriteLine("plasma complete. Writing '{0}'...", options.OutputFileName);
            if (IsRasterFormat(options.OutputFileName))
            {
                var image = GenerateImage(triangles, options.Width, options.Height, GetColorFunction(options.ColorBlend));
                if (options.Scale > 0.0f)
                    image = ScaleImage(image, options.Scale);

                image.Save(options.OutputFileName);
            }
            else
            {
                var svg = GenerateSvg(triangles, options.Width, options.Height, options.Scale, GetColorFunction(options.ColorBlend));
                File.WriteAllText(options.OutputFileName, svg);
            }
        }

        static IEnumerable<Triangle> GenerateTrianglePlasma(int width, int height, float initial_sz_pcnt, double min_area_pcnt, double param,
                     double? contrast, Func<double,double,double,double> perturb)
        {
            var triangulator = new DelaunayTriangulator(width, height);
            triangulator.AddVertices(
                UniformPoissonDiskSampler.SampleRectangle(
                    new Vertex(0, 0), new Vertex(width, height), 
                    Math.Min(width,height) * initial_sz_pcnt
                )
            );
            foreach (var v in triangulator.Vertices)
                v.Value = perturb(0.5, 1.0, param);

            var areas = triangulator.Triangles.Select(t => t.Area);
            var base_area = areas.Sum() / (double) areas.Count();
            int count = triangulator.Triangles.Count();
            var min_area = min_area_pcnt * (width * height);
            bool done = false;

            Console.WriteLine("Generating plasma from {0} seed triangles.", triangulator.Triangles.Count());
            while (! done)
            {
                int new_count = triangulator.Triangles.Count();
                if (new_count - count > 10000 )
                {
                    count = new_count;
                    Console.WriteLine("   {0} triangles remaining ...", triangulator.Triangles.Where( tri => tri.Area > min_area ).Count());
                }

                var triangle = triangulator.Largest;
                if (triangle.Area > min_area)
                {
                    var new_point = triangulator.SubdivideTriangle(triangle);
                    new_point.Value = perturb(
                        InterpolateValues(new_point, triangle), 
                        triangle.Area / base_area, param
                    );
                } else  
                {
                    done = true;
                }
            }
            var vertices = triangulator.Vertices;
            NormalizeValues(vertices);
            if (contrast.HasValue)
                ApplyContrast(vertices, contrast.Value);

            return triangulator.Triangles;
        }

        static bool IsRasterFormat(string filename)
        {
            string ext = Path.GetExtension(filename).ToLower();
            return (ext != ".svg");
        }

        static Func<Vertex[], Color> GetColorFunction(ColorBlendParams blend)
        {
            if (blend == null)
                return MeanGray;
            return (Vertex[] verts) => {
                return BlendColors(verts, blend.Colors );
            };
        }

        static Func<double, double, double, double> GetPerturbationFunction(PerturbationMethod method)
        {
            switch (method)
            {
                case PerturbationMethod.Normal:
                    return NormalPerturbation;
                case PerturbationMethod.NormalClamped:
                    return (double a, double b, double c) => Clamp(NormalPerturbation(a, b, c));
                case PerturbationMethod.Uniform:
                    return UniformPerturbation;
                case PerturbationMethod.UniformClamped:
                    return (double a, double b, double c) => Clamp(UniformPerturbation(a, b, c));
            }
            return NormalPerturbation;
        }

        static Bitmap ScaleImage(Bitmap img, float scale)
        {
            int scaled_wd = (int)(img.Width * scale);
            int scaled_hgt = (int)(img.Height * scale);
            Bitmap scaled = new Bitmap(scaled_wd, scaled_hgt);
            Graphics g = Graphics.FromImage(scaled);

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, new Rectangle(0, 0, scaled_wd, scaled_hgt), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);

            return scaled;
        }

        static void NormalizeValues(IEnumerable<Vertex> vertices)
        {
            double min = vertices.Select(v => v.Value).Min();
            double max = vertices.Select(v => v.Value).Max();
            double range = max - min;
            foreach (var v in vertices)
                v.Value = (v.Value - min) / range;
        }

        static void ApplyContrast(IEnumerable<Vertex> vertices, double k)
        {
            foreach (var v in vertices)
                v.Value = ContrastCurve(v.Value, k);
        }

        static double Clamp(double v)
        {
            if (v < 0)
                return 0;
            if (v > 1)
                return 1;
            return v;
        }

        static double ContrastCurve(double val, double k = 1)
        {
            Func<double,double> logistic_func = (double x) => 1.0 / (1.0 + Math.Exp(-k * (x - 0.5)));
            var low = logistic_func(0);
            var high = logistic_func(1);
            var range = high - low;
            var value = logistic_func(val);
            return (value - low) / range;
        }

        static double UniformPerturbation(double value, double amt, double gamma)
        {
            var perturbation_factor = Math.Pow(amt, gamma);
            return value += perturbation_factor * (RandomHelper.Random.NextDouble() - 0.5);
        }

        static double NormalPerturbation(double value, double amt, double gamma)
        {
            var perturbation_factor = Math.Pow(amt, gamma);

            var u1 = RandomHelper.Random.NextDouble();
            var u2 = RandomHelper.Random.NextDouble();
            var temp1 = Math.Sqrt(-2 * Math.Log(u1));
            var temp2 = 2 * Math.PI * u2;

            var mu = value;
            var sigma = perturbation_factor;

            return mu + sigma * (temp1 * Math.Cos(temp2));

        }

        static double InterpolateValues(Vertex pt, Triangle tri)
        {
            var dist_to_vert = tri.Vertices.Select(vert => Vertex.Distance(pt, vert));
            var total_distance = dist_to_vert.Sum();
            var percent_of_vert = dist_to_vert.Select(dist => dist / total_distance).ToList();
            return tri.Vertices.Select((v, i) => v.Value * percent_of_vert[i]).Sum();
        }

        static IEnumerable<Triangle> FilterBorderTriangles(IEnumerable<Triangle> triangles, List<Vertex> corners, float eps)
        {
            return triangles.Where(
                tri =>
                {
                    foreach (var vert in tri.Vertices)
                    {
                        foreach (var corner in corners)
                        {
                            if (Vertex.DistanceSquared(corner, vert) < eps)
                                return false;
                        }
                    }
                    return true;
                }
            );
        }
      
        static string GenerateSvg(IEnumerable<Triangle> triangles, int width, int height, float scale, Func<Vertex[], Color> colorFunc)
        {
            if (scale == 0)
                scale = 1.0f;

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("<svg viewBox=\"0 0 {0} {1}\" xmlns=\"http://www.w3.org/2000/svg\">\n", scale*width, scale*height);

            foreach (var triangle in triangles)
            {
                sb.AppendFormat("<polygon points=\"{0}, {1} {2}, {3} {4}, {5}\" fill = \"{6}\" stroke = \"{6}\" />\n",
                    scale * triangle.Vertices[0].X, scale * triangle.Vertices[0].Y,
                    scale * triangle.Vertices[1].X, scale * triangle.Vertices[1].Y,
                    scale * triangle.Vertices[2].X, scale * triangle.Vertices[2].Y,
                    ColorToString( colorFunc(triangle.Vertices) )
                );
            }
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        static string ColorToString(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        static Color GrayFromIntensity(double intensity)
        {
            int gray_level = (int)Math.Round(intensity * 255.0);
            return Color.FromArgb(gray_level, gray_level, gray_level);
        }

        static Color MeanGray(Vertex[] v)
        {
            var intensity = (v[0].Value + v[1].Value + v[2].Value) / 3.0;
            return GrayFromIntensity(intensity);
        }

        static Color BlendColors(Vertex[] v, List<Color> colors)
        {
            var val = (v[0].Value + v[1].Value + v[2].Value) / 3.0;

            if (val == 0.0)
                return colors[0];

            if (val == 1.0)
                return colors.Last();

            int num_intervals = colors.Count - 1;
            int blend_from = (int) (val * num_intervals);
            int blend_to = blend_from + 1;
            double interval_wd = 1.0 / num_intervals;
            double t = (val - blend_from * interval_wd) / interval_wd;

            return BlendTwoColors(t, colors[blend_from], colors[blend_to]);
        }

        static Color BlendTwoColors(double val, Color blend_from, Color blend_to)
        {
            return Color.FromArgb(
                Blend(blend_from.R, blend_to.R, val),
                Blend(blend_from.G, blend_to.G, val),
                Blend(blend_from.B, blend_to.B, val)
            );
        }

        static int Blend(int src, int dst, double t )
        {
            float range = dst - src;
            return (int) Math.Round(src + range * t);
        }

        static Bitmap GenerateImage(IEnumerable<Triangle> triangles, int width, int height, Func<Vertex[], Color> colorFunc)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.HighSpeed;
                g.FillRectangle(new SolidBrush( Color.Black ), 0, 0, width, height);
                foreach (var triangle in triangles)
                {
                    g.FillPolygon(
                        new SolidBrush(colorFunc(triangle.Vertices)),
                        triangle.Vertices.Select(
                            v => new PointF((float)v.X, (float)v.Y)
                       ).ToArray()
                    );
                }
            }
            return bmp;
        }
    }
}

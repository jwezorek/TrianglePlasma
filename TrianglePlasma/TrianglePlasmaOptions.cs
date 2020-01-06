using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;

namespace TrianglePlasma
{
    enum PerturbationMethod
    {
        Uniform,
        UniformClamped,
        Normal,
        NormalClamped
    }

    class TrianglePlasmaOptions
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float InitialVertexDistance { get; private set; }
        public double MinAreaCutoff { get; private set; }
        public float? Contrast { get; private set; }
        public string OutputFileName { get; private set; }
        public PerturbationMethod ValuePerturbationMethod { get; private set; }
        public double ValuePerturbationParam { get; private set; }
        public bool IsValid { get; private set; }
        public float Scale { get; private set; }
        public ColorBlendParams ColorBlend { get; private set; }

        public TrianglePlasmaOptions(string[] args)
        {
            IsValid = true;
            Width = 512;
            Height = 512;
            InitialVertexDistance = 0.25f;
            MinAreaCutoff = 0.0005;
            Contrast = null;
            ValuePerturbationMethod = PerturbationMethod.Normal;
            ValuePerturbationParam = 0.25;
            Scale = 1;

            var p = new OptionSet() {
                { "w|width=", "width of output (pixels)", (int v) => Width = v },
                { "h|height=", "height of output (pixels)", (int v) => Height = v },
                { "i|initial-distance=", "initial vertex distance", (float v) => InitialVertexDistance=v  },
                { "m|min-area-cutoff=", "minimum area cutoff", (double v)  => MinAreaCutoff=v },
                { "c|contrast=", "contrast", (float v)  => Contrast=v },
                { "v|perturbation-method=", "value perturbation method", (string v) => GetValuePerturbationMethod(v) },
                { "p|perturbation-param=", "value perturbation param", (double v) => ValuePerturbationParam = v},
                { "s|scale=", "scale factor applied to output", (float v) => Scale = v},
                { "b|color-blend=", "generate a color blend", 
                    (string str) => {
                        ColorBlendParams blend = new ColorBlendParams(str);
                        if (blend.IsValid)
                            ColorBlend = blend;
                        else
                            ColorBlend = null;
                    } 
                }
            };

            if (! args.Any())
            {
                ShowHelp(p);
                IsValid = false;
                return;
            }

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                ShowHelp(p);
                IsValid = false;
                return;
            }

            if (extra.Any())
                OutputFileName = extra.First();
            else
                OutputFileName = "triangle-plasma.png";
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: TrianglePlasma [OPTIONS] output-file");
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }


        private PerturbationMethod GetValuePerturbationMethod(string str)
        {
            var tbl = new Dictionary<string, PerturbationMethod>
            {
                { "uniform", PerturbationMethod.Uniform },
                { "uniformclamped", PerturbationMethod.UniformClamped },
                { "normal", PerturbationMethod.Normal },
                { "normalclamped", PerturbationMethod.NormalClamped },
            };
            string key = str.ToLower();
            if (!tbl.ContainsKey(key))
                return PerturbationMethod.Normal;
            return tbl[key];
        }

    }

    class ColorBlendParams
    {
        public List<Color> Colors { get; private set; }
        public bool IsValid {get; private set;}

        public ColorBlendParams(string str)
        {
            IsValid = false;
            try
            {
                Colors = str.Split('-').Select(c => ColorFromHex(c)).ToList();
            }
            catch (Exception)
            {
                return;
            }
            IsValid = Colors.Count() > 1;
        }

        static private Color ColorFromHex(string hex)
        {
            int argb = Int32.Parse(hex.Replace("#", ""), NumberStyles.HexNumber);
            return Color.FromArgb(argb);
        }
    }
}

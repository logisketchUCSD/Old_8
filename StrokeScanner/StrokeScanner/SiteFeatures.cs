using Sketch;
using Featurefy;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrokeScanner
{
    class SiteFeatures
    {
        public static int NUM_FEATURES=11;

        public static List<double> evalStroke(Substroke s)
        {
            List<double> result = new List<double>();

            // Yay for featurefy doing lots of work for me
            // Commented out features are ones that can sometimes return NaN or Inf
            FeatureStroke f = new FeatureStroke(s);  

            result.Add(f.ArcLength.CircularInkDensity);
            result.Add(f.ArcLength.Diagonal);
            result.Add((double)f.ArcLength.Height);
            result.Add(f.ArcLength.InkDensity);
            result.Add(f.ArcLength.TotalLength);
            result.Add((double)f.ArcLength.Width);

            result.Add(f.Curvature.AverageCurvature);

            result.Add(f.Slope.AverageSlope);
            result.Add(f.Slope.Direction);
            result.Add(f.Slope.Var);
            
            result.Add(f.Spatial.DistanceFromFirstToLast);

            // Debugging output
#if DEBUG
            for (int i = 0; i < NUM_FEATURES; ++i)
            {
                if ((double.IsInfinity(result[i])) || (double.IsNaN(result[i])))
                {
                    Console.WriteLine("BAD FEATURE: {0}", i);                    
                }
            }
#endif
            return result;
        }       
    }
}

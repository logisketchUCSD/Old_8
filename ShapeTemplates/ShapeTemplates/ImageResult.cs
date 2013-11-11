using System;
using System.Collections.Generic;
using System.Text;

namespace ShapeTemplates
{
    public class ImageResult
    {
        #region Member Variables

        ImageSymbol _UnknownSymbol;

        ImageSymbol _TemplateSymbol;

        double _HausdorffDistance;

        double _ModifiedHausdorffDistance;

        double _TanimotoCoefficient;

        double _YuleCoefficient;

        double _Score;

        #endregion

        public ImageResult()
        {
        }

        public ImageResult(ImageSymbol unknown, ImageSymbol template, double haus, double modHaus, double tanimoto, double yule)
        {
            _UnknownSymbol = unknown;
            _TemplateSymbol = template;
            _HausdorffDistance = haus;
            _ModifiedHausdorffDistance = modHaus;
            _TanimotoCoefficient = tanimoto;
            _YuleCoefficient = yule;

            CalculateScore();
        }

        private void CalculateScore()
        {
            _Score = _HausdorffDistance + _ModifiedHausdorffDistance;
        }

        public double Score
        {
            get { return _Score; }
        }

        public string Name
        {
            get { return _TemplateSymbol.Name; }
        }
    }
}

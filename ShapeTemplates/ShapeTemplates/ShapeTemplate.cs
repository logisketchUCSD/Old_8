using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace ShapeTemplates
{
    public class ShapeTemplate
    {
        #region Member Variables

        ImageTemplate m_ImageTemplate;

        StructuralTemplate m_StructuralTemplate;

        #endregion

        #region Constructors

        public ShapeTemplate(List<Substroke> strokes)
        {
            m_ImageTemplate = new ImageTemplate(strokes);
            m_StructuralTemplate = new StructuralTemplate(strokes);
        }

        #endregion

        #region Interface

        public double Match()
        {
            throw new Exception("This method has not been implemented");
        }

        #endregion

        #region Getters and Setters

        #endregion
    }
}

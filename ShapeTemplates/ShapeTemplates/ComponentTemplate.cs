using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace ShapeTemplates
{
    public class ComponentTemplate
    {
        #region Member Variables

        // Primitives

        DollarTemplate m_DollarTemplate;

        #endregion

        #region Constructors

        public ComponentTemplate(Substroke stroke)
        {
        }

        #endregion

        #region Interface

        public double Match()
        {
            throw new Exception("This method has not been implemented yet");
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace ShapeTemplates
{
    public class StructuralTemplate
    {
        #region Member Variables

        List<ComponentTemplate> m_Components;

        List<ComponentRelation> m_Relations;



        #endregion

        #region Constructors

        public StructuralTemplate(List<Substroke> strokes)
        {
            m_Components = new List<ComponentTemplate>();
            m_Relations = new List<ComponentRelation>();

            foreach (Substroke s in strokes)
                m_Components.Add(new ComponentTemplate(s));
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace WireRecognizer
{
    class WireSemantics
    {
        private Sketch.Shape wire;

        private Dictionary<Guid, Dictionary<Guid, bool>> adjacency;

        public WireSemantics(Sketch.Shape s, Dictionary<Guid, Dictionary<Guid, bool>> adj)
        {
            wire = s;
            adjacency = adj;
        }
    }
}

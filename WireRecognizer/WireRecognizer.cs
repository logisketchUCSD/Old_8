/*
 * File: WireRecognizer
 *
 * Authors: Joshua Ehrlich.
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2009.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace WireRec
{
    public class WireRecognizer
    {
        Dictionary<Guid, Dictionary<Guid, int>> adjacency;

        private WireShape mesh;

        public WireRecognizer(Dictionary<Guid, Dictionary<Guid, int>> adj)
        {
            adjacency = adj;
        }

        public double RecognizeShape(Sketch.Shape s)
        {
            mesh = new WireShape(s, adjacency);
            if (!mesh.ValidMesh())
                return 0.0;
            return 1.0;// mesh.WellConnectedMesh();

        }
    }
}

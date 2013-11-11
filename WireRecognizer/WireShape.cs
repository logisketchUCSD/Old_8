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
    /// <summary>
    /// The possible polarities of a wire (both global and local/symbol scale)
    /// </summary>
    public enum WirePolarity
    {
        Input,
        Output,
        Internal,
        Unknown
    }


    class WireShape
    {
        #region Internals

        private Sketch.Shape mesh;

        private WirePolarity IOType;

        /// <summary>
        /// A List of SimpleSymbols and ComplexSymbols that are connected to the Mesh.
        /// </summary>
        private List<Sketch.Shape> Gates;

        /// <summary>
        /// A List of labels that are associated with the wires that are a part of the Mesh.
        /// </summary>
        private List<Sketch.Shape> Labels;

        /// <summary>
        /// A list of the wire substrokes
        /// </summary>
        private List<WireStroke> wires;

        Dictionary<Guid, Dictionary<Guid, int>> adjacency;

        private List<EndPoint> freeEnds;


        #endregion

        #region Constructors

        public WireShape(Sketch.Shape mesh, Dictionary<Guid, Dictionary<Guid, int>> adj)
        {
            this.mesh = mesh;
            adjacency = adj;
            this.IOType = WirePolarity.Unknown;
            this.freeEnds = new List<EndPoint>();
            wires = new List<WireStroke>();
            foreach (Sketch.Substroke s in this.mesh.SubstrokesL)
            {
                wires.Add(new WireStroke(s));
            }


            //foreach (Sketch.Shape s in this.mesh.ConnectedShapes)
            //{
            //    if (s.Type == "Wire")
            //    {
            //        //Do nothing for now
            //    }
            //    else if (s.Type == "Label")
            //        this.Labels.Add(s);
            //    else if (s.IsGate)
            //        this.Gates.Add(s);

            //}

        }


        #endregion

        #region Methods

         ///<summary>
         ///Checks that a given mesh is still valid in that every wire is connected to everyother wire
         ///This method should be called anytime the mesh is modified to assure validity
         ///</summary>
         ///<returns></returns>
        public bool ValidMesh()
        {
            ConstructTree();
            foreach (WireStroke w in this.wires)
            {
                if (!w.NoLoop) //there is a cycle somewhere
                    return false;
                if (w.ParentWire == null) //there is an unconnected wire
                    return false;
            }
            return true;
        }

        private void ConstructTree()
        {
            foreach (WireStroke w in this.wires)
            {
                // reset values just in case
                w.ChildrenWires = new List<WireStroke>();
                w.ParentWire = null;
            }
            WireStroke root = this.wires[0];
            root.ParentWire = new WireStroke();
            ConstructTree(root);

        }

        private void ConstructTree(WireStroke root)
        {
            List<WireStroke> connectedWires = new List<WireStroke>();
            if (root.ID == null)//Should not happend if everystroke is an actual stroke
                return;

            foreach (Guid id in adjacency[(Guid)root.ID].Keys)
            {
                if (adjacency[(Guid)root.ID][id] != 0 || adjacency[id][(Guid)root.ID] != 0)
                {
                    // This is currently a harsh rule in that many legal wires violate this
                    // but more importantly no gate will satisfy this
                    // To be fixed after adjacency is modified
                    //if (adjacency[(Guid)root.ID][id]>2 || adjacency[id][(Guid)root.ID]>2)
                    //{
                    //    root.NoLoop = false;
                    //    return;
                    //}
                    foreach (WireStroke w in this.wires)
                    {
                        if (w.ID == null)//Again should never happen
                            return;
                        if ((Guid)w.ID == id)
                        {
                            connectedWires.Add(w);
                        }
                    }
                }
            }
            foreach (WireStroke w in connectedWires)
            {
                if (!(w.Equals(root.ParentWire)))
                {
                    root.ChildrenWires.Add(w);
                    if (w.ParentWire == null)
                    {
                        w.ParentWire = root;
                        ConstructTree(w);
                    }
                    else
                    {
                        w.NoLoop = false;
                        return;
                    }
                    
                }

            }
        }

        #endregion
    }
}

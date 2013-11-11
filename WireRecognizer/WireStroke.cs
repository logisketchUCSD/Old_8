/*
 * File: WireStroke
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
    class WireStroke
    {
        /// <summary>
        /// A List of the wires directly connected to the Wire.
        /// </summary>
        private List<WireStroke> connectedWires;
 
        /// <summary>
        /// The list of children wires
        /// </summary>
        private List<WireStroke> childrenWires;

        /// <summary>
        /// The parent of this wire stroke
        /// </summary>
        private WireStroke parentWire;

        /// <summary>
        /// Substroke that makes up the wire
        /// </summary>
        private Sketch.Substroke substroke;

        /// <summary>
        /// List of the endpoints should be only two
        /// </summary>
        private List<EndPoint> endPt;

        /// <summary>
        /// Distance of each endpoint to nearest wire point.
        /// </summary>
        private List<double> endptMinDistWire;

        /// <summary>
        /// flag to indicate that this wire does not attach to the same wire twice
        /// </summary>
        private bool noloop;

        /// <summary>
        /// Says whether or not an endpoint is connected.  True for connected, false for unconnected (the index corresponds with EndPt's index).
        /// This is only for connections to symbols and wires, not to labels.
        /// </summary>
        private List<bool> endPtConnect;


        /// <summary>
        /// GUID of the BasicWire.
        /// </summary>
        private Guid? id;

        #region Constructor

        /// <summary>
        /// Constructor for Wire.
        /// </summary>
        /// <param name="newWire">The Shape object containing the Wire</param>
        /// <param name="marginvalue">The value of the margin for the wire</param>
        /// <param name="substroke_margin">The value of the margin for the substroke</param>

        public WireStroke(Sketch.Substroke newWire)
        {

            childrenWires = new List<WireStroke>();
            connectedWires = new List<WireStroke>();
            parentWire = null;
            substroke = newWire;
            id = newWire.XmlAttrs.Id;
            // Find endpoints and determines their attributes
            this.EndPt = findAllEndPoints();
            this.endPtConnect = new List<bool>();
            foreach (EndPoint ep in this.EndPt)
            {
                this.EndPtConnect.Add(false);
            }
            this.noloop = true;

        }
        /// <summary>
        /// Copy Constructor for Wire.
        /// </summary>
        /// <param name="newWire">The Shape object containing the Wire</param>
        /// <param name="marginvalue">The value of the margin for the wire</param>
        /// <param name="substroke_margin">The value of the margin for the substroke</param>

        public WireStroke(WireStroke other)
        {
            connectedWires = other.connectedWires;
            childrenWires = other.childrenWires;
            parentWire = other.parentWire;
            substroke = other.substroke;
            id = other.id;
            // Find endpoints and determines their attributes
            this.EndPt = other.endPt;
            this.endPtConnect = other.endPtConnect;
            this.noloop = other.noloop;

        }

        /// <summary>
        /// Constructor for Wire.
        /// </summary>
        public WireStroke()
        {
        }

        #endregion

        #region Methods
        /// <summary>
        /// Remove all hooks from substrokes and update the strokes and substrokes
        /// Uses the dehooking code in Featurefy.Compute (Eric's code)
        /// </summary>
        private void removeHooks()
        {
            Sketch.Substroke temp = Featurefy.Compute.DeHook(substroke);
            substroke = temp;
        }

        /// <summary>
        /// Finds all the endpoints of each substroke in the Wire.
        /// </summary>
        /// <returns>A List of all the possible endpoints, or both endpoints from each substroke in the wire.</returns>
        private List<EndPoint> findAllEndPoints()
        {
            // The start and end points of the substroke are always the first and last points in the PointsL List,
            // so add these to the possible endpoints for all the substrokes in the Wire.
            List<EndPoint> epts = new List<EndPoint>();

            //For now we don't need to remove hooks
            // Remove hooks in each substroke in the Wire.
            //removeHooks();

            // The endpoints are the first and last points in the list of points of each substroke
            epts.Add(new EndPoint(substroke.PointsL[0], substroke));
            epts.Add(new EndPoint(substroke.PointsL[substroke.PointsL.Count - 1], substroke));


            // Determine the local slope and type (ie left, right, etc) of each endpoint
            foreach (EndPoint ep in epts)
            {
                ep.DetermineSlope(ep.ParentSub);
            }


            // Finds the type of each endpoint
            foreach (EndPoint ep in epts)
            {
                foreach (EndPoint ep2 in epts)
                {
                    // Find the type of the endpoint
                    if (ep.ParentSub.XmlAttrs.Id.Equals(ep2.ParentSub.XmlAttrs.Id) && !ep.Equals(ep2))
                    {
                        ep.DetermineType(ep2);
                    }
                }
            }
            return epts;
        }


 


        public bool Equals(WireStroke w)
        {
            if (w == null)
                return false;
            if (w.ID == this.ID)
                return true;
            else
                return false;
        }

        #endregion


        public Sketch.Substroke Substroke
        {
            get { return this.substroke; }
        }
        public List<WireStroke> ConnectedWires
        {
            get { return this.connectedWires; }
            set { this.connectedWires = value; }
        }
        public List<WireStroke> ChildrenWires
        {
            get { return this.childrenWires; }
            set { this.childrenWires = value; }
        }
        public WireStroke ParentWire
        {
            get { return this.parentWire; }
            set { this.parentWire = value; }

        }

        public List<EndPoint> EndPt
        {
            get { return this.endPt; }
            set { this.endPt = value; }
        }
        /// <summary>
        /// GUID of the Wire
        /// </summary>
        /// <returns>The ID of the Wire</returns>
        public Guid? ID
        {
            get
            {
                return this.id;
            }
        }
        public List<bool> EndPtConnect
        {
            get { return this.endPtConnect; }
        }
        public bool NoLoop
        {
            get { return this.noloop; }
            set { this.noloop = value; }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Sketch;
using Utilities;

namespace ShapeTemplates
{
    [Serializable]
    [DebuggerDisplay("Type={_symbolType}, User={_user.Name}")]
    public class SymbolInfo
    {
        #region Member Variables

        /// <summary>
        /// Unique ID for this symbol
        /// </summary>
        Guid _symbolId;

        /// <summary>
        /// The type of symbol this ImageTemplate describes (e.g. AND, OR, Arrow, etc.)
        /// </summary>
        string _symbolType;

        /// <summary>
        /// The symbol's container class (e.g. AND --> Gate, Wire --> Connector)
        /// </summary>
        string _symbolClass;

        /// <summary>
        /// The user who drew this shape
        /// </summary>
        User _user;

        /// <summary>
        /// The platform this shape was drawn on (i.e. TabletPC or Wacom Tablet (paper))
        /// </summary>
        PlatformUsed _platformUsed;

        /// <summary>
        /// How complete this symbol is
        /// </summary>
        SymbolCompleteness _completeness;

        /// <summary>
        /// Under what circumstances was this shape drawn
        /// </summary>
        DrawingTask _drawingTask;

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor, sets values to defaults or "None"
        /// </summary>
        public SymbolInfo()
        {
            _symbolId = Guid.NewGuid();
            _symbolType = "None";
            _symbolClass = "None";
            _user = new User();
            _platformUsed = PlatformUsed.TabletPC;
            _completeness = SymbolCompleteness.Complete;
            _drawingTask = DrawingTask.Synthesize;
        }

        /// <summary>
        /// Typical Constructor
        /// </summary>
        /// <param name="user">User who drew the symbol</param>
        /// <param name="symbolType">Type of symbol drawn</param>
        /// <param name="symbolClass">Class of drawn symbol</param>
        public SymbolInfo(User user, string symbolType, string symbolClass)
        {
            _symbolId = Guid.NewGuid();
            _symbolType = symbolType;
            _symbolClass = symbolClass;
            _user = user;
            _platformUsed = PlatformUsed.TabletPC;
            _completeness = SymbolCompleteness.Complete;
            _drawingTask = DrawingTask.Synthesize;
        }

        #endregion

        #region Getters/Setters

        /// <summary>
        /// Unique ID for this symbol
        /// </summary>
        public Guid SymbolId
        {
            get { return _symbolId; }
            set { _symbolId = value; }
        }

        /// <summary>
        /// The type of symbol this ImageTemplate describes (e.g. AND, OR, Arrow, etc.)
        /// </summary>
        public string SymbolType
        {
            get { return _symbolType; }
            set { _symbolType = value; }
        }

        /// <summary>
        /// The symbol's container class (e.g. AND --> Gate, Wire --> Connector)
        /// </summary>
        public string SymbolClass
        {
            get { return _symbolClass; }
            set { _symbolClass = value; }
        }

        /// <summary>
        /// The user who drew this shape
        /// </summary>
        public User User
        {
            get { return _user; }
            set { _user = value; }
        }

        /// <summary>
        /// The platform this shape was drawn on (i.e. TabletPC or Wacom Tablet (paper))
        /// </summary>
        public PlatformUsed PlatformUsed
        {
            get { return _platformUsed; }
            set { _platformUsed = value; }
        }

        /// <summary>
        /// How complete this symbol is
        /// </summary>
        public SymbolCompleteness Completeness
        {
            get { return _completeness; }
            set { _completeness = value; }
        }

        /// <summary>
        /// Under what circumstances was this shape drawn
        /// </summary>
        public DrawingTask DrawTask
        {
            get { return _drawingTask; }
            set { _drawingTask = value; }
        }

        #endregion
    }
}

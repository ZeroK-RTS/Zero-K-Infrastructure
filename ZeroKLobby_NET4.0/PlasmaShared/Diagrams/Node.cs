// A Force-Directed Diagram Layout Algorithm
// Bradley Smith - 2010/07/01

using System;
using System.Collections.Generic;
using System.Drawing;

/// <summary>
/// Represents a node that can be added to a diagram and connected to other nodes. 
/// Provides logic for drawing itself and its connections to other nodes.
/// </summary>
/// 
namespace Diagrams
{
	public abstract class Node
	{

		Diagram mDiagram;			// the parent diagram
		Point mLocation;			// node position, relative to the origin
		List<Node> mConnections;	// list of references to connected nodes (children)

		/// <summary>
		/// Gets or sets the position of the node.
		/// </summary>
		public Point Location
		{
			get { return mLocation; }
			set { mLocation = value; }
		}
		/// <summary>
		/// Gets a read-only collection representing the (child) nodes that this node is connected to.
		/// </summary>
		public IList<Node> Connections
		{
			get { return mConnections.AsReadOnly(); }
		}
		/// <summary>
		/// Gets or sets the diagram to which this node belongs.
		/// </summary>
		public Diagram Diagram
		{
			get { return mDiagram; }
			set
			{
				if (mDiagram == value) return;

				if (mDiagram != null) mDiagram.RemoveNode(this);
				mDiagram = value;
				if (mDiagram != null) mDiagram.AddNode(this);
			}
		}
		/// <summary>
		/// Gets or sets the X coordinate of the node, relative to the origin.
		/// </summary>
		public int X
		{
			get { return mLocation.X; }
			set { mLocation.X = value; }
		}
		/// <summary>
		/// Gets or sets the Y coordinate of the node, relative to the origin.
		/// </summary>
		public int Y
		{
			get { return mLocation.Y; }
			set { mLocation.Y = value; }
		}
		/// <summary>
		/// Gets the size of the node (for drawing purposes).
		/// </summary>
		public abstract Size Size
		{
			get;
		}

		/// <summary>
		/// Initialises a new instance of the Node class.
		/// </summary>
		public Node()
		{
			mLocation = Point.Empty;
			mDiagram = null;
			mConnections = new List<Node>();
		}

		/// <summary>
		/// Connects the specified child node to this node.
		/// </summary>
		/// <param name="child">The child node to add.</param>
		/// <returns>True if the node was connected to this node.</returns>
		public bool AddChild(Node child)
		{
			if (child == null) throw new ArgumentNullException("child");
			if ((child != this) && !this.mConnections.Contains(child))
			{
				child.Diagram = this.Diagram;
				this.mConnections.Add(child);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Connects this node to the specified parent node.
		/// </summary>
		/// <param name="parent">The node to connect to this node.</param>
		/// <returns>True if the other node was connected to this node.</returns>
		public bool AddParent(Node parent)
		{
			if (parent == null) throw new ArgumentNullException("parent");
			return parent.AddChild(this);
		}

		/// <summary>
		/// Removes any connection between this node and the specified node.
		/// </summary>
		/// <param name="other">The other node whose connection is to be removed.</param>
		/// <returns>True if a connection existed.</returns>
		public bool Disconnect(Node other)
		{
			bool c = this.mConnections.Remove(other);
			bool p = other.mConnections.Remove(this);
			return c || p;
		}

		/// <summary>
		/// Draws a connector between this node and the specified child node using GDI+. 
		/// The source and destination coordinates (relative to the Graphics surface) are also specified.
		/// </summary>
		/// <param name="graphics">GDI+ Graphics surface.</param>
		/// <param name="from">Source coodinate.</param>
		/// <param name="to">Destination coordinate.</param>
		/// <param name="other">The other node.</param>
		public virtual void DrawConnector(Graphics graphics, Point from, Point to, Node other)
		{
			graphics.DrawLine(Pens.Gray, from, to);
		}

		/// <summary>
		/// Draws the node using GDI+, within the specified bounds.
		/// </summary>
		/// <param name="graphics">GDI+ Graphics surface.</param>
		/// <param name="bounds">The bounds in which to draw the node.</param>
		public abstract void DrawNode(Graphics graphics, Rectangle bounds);
	}

	/// <summary>
	/// Provides an example implementation of the Node class. SpotNode is an 8x8 circle that is stroked and filled.
	/// </summary>
	public class SpotNode : Node
	{

		private Brush mFill;
		private Pen mStroke;

		/// <summary>
		/// Gets or sets the System.Drawing.Brush used to fill the spot.
		/// </summary>
		public Brush Fill
		{
			get { return mFill; }
			set
			{
				if (value == null) throw new ArgumentNullException("value");
				mFill = value;
			}
		}
		/// <summary>
		/// Gets the size of the spot.
		/// </summary>
		public override Size Size
		{
			get
			{
				return new Size(8, 8);
			}
		}
		/// <summary>
		/// Gets or sets the System.Drawing.Pen used to stroke the spot.
		/// </summary>
		public Pen Stroke
		{
			get { return mStroke; }
			set
			{
				if (value == null) throw new ArgumentNullException("value");
				mStroke = value;
			}
		}

		/// <summary>
		/// Initalises a new instance of the SpotNode class using default values.
		/// </summary>
		public SpotNode() : this(Color.Black) { }

		/// <summary>
		/// Initalises a new instance of the SpotNode class using the specified fill color.
		/// </summary>
		/// <param name="color">The System.Drawing.Color to fill the spot with.</param>
		public SpotNode(Color color)
			: base()
		{
			mFill = new SolidBrush(color);
			mStroke = Pens.Black;
		}

		/// <summary>
		/// Draws the node using GDI+.
		/// </summary>
		/// <param name="graphics">GDI+ Graphics surface.</param>
		/// <param name="bounds">The bounds in which to draw the node.</param>
		public override void DrawNode(Graphics graphics, Rectangle bounds)
		{
			graphics.FillEllipse(mFill, bounds);
			graphics.DrawEllipse(mStroke, bounds);
		}
	}
}
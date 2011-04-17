// A Force-Directed Diagram Layout Algorithm
// Bradley Smith - 2010/07/01

using System;
using System.Collections.Generic;
using System.Drawing;

/// <summary>
/// Represents a simple diagram consisting of nodes and connections, implementing a 
/// force-directed algorithm for automatically arranging the nodes.
/// </summary>
public class Diagram {

	private const double ATTRACTION_CONSTANT = 0.1;		// spring constant
	private const double REPULSION_CONSTANT = 10000;	// charge constant

	private const double DEFAULT_DAMPING = 0.5;
	private const int DEFAULT_SPRING_LENGTH = 100;
	private const int DEFAULT_MAX_ITERATIONS = 500;

	private List<Node> mNodes;

	/// <summary>
	/// Gets a read-only collection of the nodes in this Diagram.
	/// </summary>
	public IList<Node> Nodes {
		get {
			return mNodes.AsReadOnly();
		}
	}

	/// <summary>
	/// Initialises a new instance of the Diagram class.
	/// </summary>
	public Diagram() {
		mNodes = new List<Node>();
	}

	/// <summary>
	/// Adds the specified Node to this Diagram.
	/// </summary>
	/// <param name="node">The Node to add to the diagram.</param>
	/// <returns>True if the node was added, false if the node is already on this Diagram.</returns>
	public bool AddNode(Node node) {
		if (node == null) throw new ArgumentNullException("node");
		
		if (!mNodes.Contains(node)) {
			// add node, associate with diagram, then add all connected nodes
			mNodes.Add(node);
			node.Diagram = this;
			foreach (Node child in node.Connections) AddNode(child);
			return true;
		}
		else {
			return false;
		}
	}

	/// <summary>
	/// Runs the force-directed layout algorithm on this Diagram, using the default parameters.
	/// </summary>
	public void Arrange() {
		Arrange(DEFAULT_DAMPING, DEFAULT_SPRING_LENGTH, DEFAULT_MAX_ITERATIONS, true);
	}

	/// <summary>
	/// Runs the force-directed layout algorithm on this Diagram, offering the option of a random or deterministic layout.
	/// </summary>
	/// <param name="deterministic">Whether to use a random or deterministic layout.</param>
	public void Arrange(bool deterministic) {
		Arrange(DEFAULT_DAMPING, DEFAULT_SPRING_LENGTH, DEFAULT_MAX_ITERATIONS, deterministic);
	}

	/// <summary>
	/// Runs the force-directed layout algorithm on this Diagram, using the specified parameters.
	/// </summary>
	/// <param name="damping">Value between 0 and 1 that slows the motion of the nodes during layout.</param>
	/// <param name="springLength">Value in pixels representing the length of the imaginary springs that run along the connectors.</param>
	/// <param name="maxIterations">Maximum number of iterations before the algorithm terminates.</param>
	/// <param name="deterministic">Whether to use a random or deterministic layout.</param>
	public void Arrange(double damping, int springLength, int maxIterations, bool deterministic) {
		// random starting positions can be made deterministic by seeding System.Random with a constant
		Random rnd = deterministic ? new Random(0) : new Random();

		// copy nodes into an array of metadata and randomise initial coordinates for each node
		NodeLayoutInfo[] layout = new NodeLayoutInfo[mNodes.Count];
		for (int i = 0; i < mNodes.Count; i++) {
			layout[i] = new NodeLayoutInfo(mNodes[i], new Vector(), Point.Empty);
			layout[i].Node.Location = new Point(rnd.Next(-50, 50), rnd.Next(-50, 50));
		}

		int stopCount = 0;
		int iterations = 0;

		while (true) {
			double totalDisplacement = 0;

			for (int i=0; i<layout.Length; i++) {
				NodeLayoutInfo current = layout[i];

				// express the node's current position as a vector, relative to the origin
				Vector currentPosition = new Vector(CalcDistance(Point.Empty, current.Node.Location), GetBearingAngle(Point.Empty, current.Node.Location));
				Vector netForce = new Vector(0, 0);

				// determine repulsion between nodes
				foreach (Node other in mNodes) {
					if (other != current.Node) netForce += CalcRepulsionForce(current.Node, other);
				}

				// determine attraction caused by connections
				foreach (Node child in current.Node.Connections) {
					netForce += CalcAttractionForce(current.Node, child, springLength);
				}
				foreach (Node parent in mNodes) {
					if (parent.Connections.Contains(current.Node)) netForce += CalcAttractionForce(current.Node, parent, springLength);
				}

				// apply net force to node velocity
				current.Velocity = (current.Velocity + netForce) * damping;

				// apply velocity to node position
				current.NextPosition = (currentPosition + current.Velocity).ToPoint();
			}

			// move nodes to resultant positions (and calculate total displacement)
			for (int i = 0; i < layout.Length; i++) {
				NodeLayoutInfo current = layout[i];

				totalDisplacement += CalcDistance(current.Node.Location, current.NextPosition);
				current.Node.Location = current.NextPosition;
			}

			iterations++;
			if (totalDisplacement < 10) stopCount++;
			if (stopCount > 15) break;
			if (iterations > maxIterations) break;
		}

		// center the diagram around the origin
		Rectangle logicalBounds = GetDiagramBounds();
		Point midPoint = new Point(logicalBounds.X + (logicalBounds.Width / 2), logicalBounds.Y + (logicalBounds.Height / 2));

		foreach (Node node in mNodes) {
			node.Location -= (Size)midPoint;
		}
	}

	/// <summary>
	/// Calculates the attraction force between two connected nodes, using the specified spring length.
	/// </summary>
	/// <param name="x">The node that the force is acting on.</param>
	/// <param name="y">The node creating the force.</param>
	/// <param name="springLength">The length of the spring, in pixels.</param>
	/// <returns>A Vector representing the attraction force.</returns>
	private Vector CalcAttractionForce(Node x, Node y, double springLength) {
		int proximity = Math.Max(CalcDistance(x.Location, y.Location), 1);

		// Hooke's Law: F = -kx
		double force = ATTRACTION_CONSTANT * Math.Max(proximity - springLength, 0);
		double angle = GetBearingAngle(x.Location, y.Location);

		return new Vector(force, angle);
	}

	/// <summary>
	/// Calculates the distance between two points.
	/// </summary>
	/// <param name="a">The first point.</param>
	/// <param name="b">The second point.</param>
	/// <returns>The pixel distance between the two points.</returns>
	public static int CalcDistance(Point a, Point b) {
		double xDist = (a.X - b.X);
		double yDist = (a.Y - b.Y);
		return (int)Math.Sqrt(Math.Pow(xDist, 2) + Math.Pow(yDist, 2));
	}

	/// <summary>
	/// Calculates the repulsion force between any two nodes in the diagram space.
	/// </summary>
	/// <param name="x">The node that the force is acting on.</param>
	/// <param name="y">The node creating the force.</param>
	/// <returns>A Vector representing the repulsion force.</returns>
	private Vector CalcRepulsionForce(Node x, Node y) {
		int proximity = Math.Max(CalcDistance(x.Location, y.Location), 1);

		// Coulomb's Law: F = k(Qq/r^2)
		double force = -(REPULSION_CONSTANT / Math.Pow(proximity, 2));
		double angle = GetBearingAngle(x.Location, y.Location);

		return new Vector(force, angle);
	}

	/// <summary>
	/// Removes all nodes and connections from the diagram.
	/// </summary>
	public void Clear() {
		mNodes.Clear();
	}

	/// <summary>
	/// Determines whether the diagram contains the specified node.
	/// </summary>
	/// <param name="node">The node to test.</param>
	/// <returns>True if the diagram contains the node.</returns>
	public bool ContainsNode(Node node) {
		return mNodes.Contains(node);
	}

	/// <summary>
	/// Draws the diagram using GDI+, centering and scaling within the specified bounds.
	/// </summary>
	/// <param name="graphics">GDI+ Graphics surface.</param>
	/// <param name="bounds">Bounds in which to draw the diagram.</param>
	public void Draw(Graphics graphics, Rectangle bounds) {
		Point center = new Point(bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2));

		// determine the scaling factor
		Rectangle logicalBounds = GetDiagramBounds();
		double scale = 1;
		if (logicalBounds.Width > logicalBounds.Height) {
			if (logicalBounds.Width != 0) scale = (double)Math.Min(bounds.Width, bounds.Height) / (double)logicalBounds.Width;
		}
		else {
			if (logicalBounds.Height != 0) scale = (double)Math.Min(bounds.Width, bounds.Height) / (double)logicalBounds.Height;
		}

		// draw all of the connectors first
		foreach (Node node in mNodes) {
			Point source = ScalePoint(node.Location, scale);

			// connectors
			foreach (Node other in node.Connections) {
				Point destination = ScalePoint(other.Location, scale);
				node.DrawConnector(graphics, center + (Size)source, center + (Size)destination, other);
			}
		}

		// then draw all of the nodes
		foreach (Node node in mNodes) {
			Point destination = ScalePoint(node.Location, scale);

			Size nodeSize = node.Size;
			Rectangle nodeBounds = new Rectangle(center.X + destination.X - (nodeSize.Width / 2), center.Y + destination.Y - (nodeSize.Height / 2), nodeSize.Width, nodeSize.Height);
			node.DrawNode(graphics, nodeBounds);
		}
	}

	/// <summary>
	/// Calculates the bearing angle from one point to another.
	/// </summary>
	/// <param name="start">The node that the angle is measured from.</param>
	/// <param name="end">The node that creates the angle.</param>
	/// <returns>The bearing angle, in degrees.</returns>
	private double GetBearingAngle(Point start, Point end) {
		Point half = new Point(start.X + ((end.X - start.X) / 2), start.Y + ((end.Y - start.Y) / 2));

		double diffX = (double)(half.X - start.X);
		double diffY = (double)(half.Y - start.Y);

		if (diffX == 0) diffX = 0.001;
		if (diffY == 0) diffY = 0.001;

		double angle;
		if (Math.Abs(diffX) > Math.Abs(diffY)) {
			angle = Math.Tanh(diffY / diffX) * (180.0 / Math.PI);
			if (((diffX < 0) && (diffY > 0)) || ((diffX < 0) && (diffY < 0))) angle += 180;
		}
		else {
			angle = Math.Tanh(diffX / diffY) * (180.0 / Math.PI);
			if (((diffY < 0) && (diffX > 0)) || ((diffY < 0) && (diffX < 0))) angle += 180;
			angle = (180 - (angle + 90));
		}

		return angle;
	}

	/// <summary>
	/// Determines the logical bounds of the diagram. This is used to center and scale the diagram when drawing.
	/// </summary>
	/// <returns>A System.Drawing.Rectangle that fits exactly around every node in the diagram.</returns>
	public Rectangle GetDiagramBounds() {
		int minX = Int32.MaxValue, minY = Int32.MaxValue;
		int maxX = Int32.MinValue, maxY = Int32.MinValue;
		foreach (Node node in mNodes) {
			if (node.X < minX) minX = node.X;
			if (node.X > maxX) maxX = node.X;
			if (node.Y < minY) minY = node.Y;
			if (node.Y > maxY) maxY = node.Y;
		}

		return Rectangle.FromLTRB(minX, minY, maxX, maxY);
	}

	/// <summary>
	/// Removes the specified node from the diagram. Any connected nodes will remain on the diagram.
	/// </summary>
	/// <param name="node">The node to remove from the diagram.</param>
	/// <returns>True if the node belonged to the diagram.</returns>
	public bool RemoveNode(Node node) {
		node.Diagram = null;
		foreach (Node other in mNodes) {
			if ((other != node) && other.Connections.Contains(node)) other.Disconnect(node);
		}
		return mNodes.Remove(node);
	}

	/// <summary>
	/// Applies a scaling factor to the specified point, used for zooming.
	/// </summary>
	/// <param name="point">The coordinates to scale.</param>
	/// <param name="scale">The scaling factor.</param>
	/// <returns>A System.Drawing.Point representing the scaled coordinates.</returns>
	private Point ScalePoint(Point point, double scale) {
		return new Point((int)((double)point.X * scale), (int)((double)point.Y * scale));
	}

	/// <summary>
	/// Private inner class used to track the node's position and velocity during simulation.
	/// </summary>
	private class NodeLayoutInfo {

		public Node Node;			// reference to the node in the simulation
		public Vector Velocity;		// the node's current velocity, expressed in vector form
		public Point NextPosition;	// the node's position after the next iteration

		/// <summary>
		/// Initialises a new instance of the Diagram.NodeLayoutInfo class, using the specified parameters.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="velocity"></param>
		/// <param name="nextPosition"></param>
		public NodeLayoutInfo(Node node, Vector velocity, Point nextPosition) {
			Node = node;
			Velocity = velocity;
			NextPosition = nextPosition;
		}
	}
}
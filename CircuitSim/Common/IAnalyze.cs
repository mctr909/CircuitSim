namespace Circuit {
	internal interface IAnalyze {
		/// <summary>
		/// is this a wire or equivalent to a wire?
		/// </summary>
		/// <returns></returns>
		bool IsWire { get; }

		/// <summary>
		/// number of voltage sources this element needs
		/// </summary>
		/// <returns></returns>
		int VoltageSourceCount { get; }

		/// <summary>
		/// number of internal nodes (nodes not visible in UI that are needed for implementation)
		/// </summary>
		/// <returns></returns>
		int InternalNodeCount { get; }

		/// <summary>
		/// get number of nodes that can be retrieved by ConnectionNode
		/// </summary>
		/// <returns></returns>
		int ConnectionNodeCount { get; }

		/// <summary>
		/// are n1 and n2 connected by this element?  this is used to determine
		/// unconnected nodes, and look for loops
		/// </summary>
		/// <param name="n1"></param>
		/// <param name="n2"></param>
		/// <returns></returns>
		bool GetConnection(int n1, int n2);

		/// <summary>
		/// stamp matrix values for linear elements.
		/// for non-linear elements, use this to stamp values that don't change each iteration,
		/// and call stampRightSide() or stampNonLinear() as needed
		/// </summary>
		void Stamp();

		/// <summary>
		/// notify this element that its pth node is n.
		/// This value n can be passed to stampMatrix()
		/// </summary>
		/// <param name="p"></param>
		/// <param name="n"></param>
		void SetNode(int p, int n);

		/// <summary>
		/// notify this element that its nth voltage source is v.
		/// This value v can be passed to stampVoltageSource(),
		/// etc and will be passed back in calls to setCurrent()
		/// </summary>
		/// <param name="n"></param>
		/// <param name="v"></param>
		void SetVoltageSource(int n, int v);

		/// <summary>
		/// get nodes that can be passed to getConnection(), to test if this element connects
		/// those two nodes; this is the same as getNode() for all but labeled nodes.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		int GetConnectionNode(int n);

		/// <summary>
		/// is n1 connected to ground somehow?
		/// </summary>
		/// <param name="n1"></param>
		/// <returns></returns>
		bool HasGroundConnection(int n1);

		/// <summary>
		/// handle reset button
		/// </summary>
		void Reset();

		void Shorted();
	}
}

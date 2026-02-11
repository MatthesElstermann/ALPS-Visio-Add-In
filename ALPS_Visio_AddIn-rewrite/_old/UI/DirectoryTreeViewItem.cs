namespace VisioAddIn
{
    /// <summary>
    /// A TreeViewItem for the WindowDirectory TreeView.
    /// Extends a normal TreeViewItem by the attributes DirectoryParent, Index and Depth
    /// </summary>
    class DirectoryTreeViewItem : System.Windows.Controls.TreeViewItem
    {
        private DirectoryTreeViewItem parent;
        private int depth;

        /// <summary>
        /// Creates a new DirectoryTreeViewItem.
        /// If the item has no parent, null should be used as parameter.
        /// </summary>
        /// <param name="parent"></param>
        public DirectoryTreeViewItem(DirectoryTreeViewItem parent) : base()
        {
            changeParent(parent);
            calcDepth();
        }

        /// <summary>
        /// Each item determines its index automatically.
        /// The index of an item is only valid on this layer and in relation to its parent.
        /// It describes the position of the child in the parent.items list.
        /// </summary>
        public int Index { get => getIndex(); }
        /// <summary>
        /// The Depth of an item describes the depth of the layer the item is located in inside the tree. (i.e. top layer has depth 0, second layer depth 1 etc.)
        /// </summary>
        public int Depth { get => depth; }

        /// <summary>
        /// Gets or sets the parent item of this item.
        /// If the current item has no parent, the current item itself is returned.
        /// </summary>
        public DirectoryTreeViewItem DirectoryParent { get => parent; set => changeParent(parent); }

        private int getIndex()
        {
            int i = 0;
            if (DirectoryParent != null)
            {
                foreach (DirectoryTreeViewItem item in DirectoryParent.Items)
                {
                    if (item.Equals(this))
                    {
                        return i;
                    }
                    i++;
                }
            }
            return 0;
        }

        private void calcDepth()
        {
            if (this.DirectoryParent == this) { this.depth = 0; }
            else this.depth = this.DirectoryParent.Depth + 1;
        }

        /// <summary>
        /// Changes the parent of the current item.
        /// Sets itself if null is passed.
        /// </summary>
        /// <param name="parent"></param>
        private void changeParent(DirectoryTreeViewItem parent)
        {
            // Sets itself as parent if there is no parent specified.
            if (parent == null)
            {
                this.parent = this;
            }
            else
            {
                this.parent = parent;
            }

            // Calculates the new depth in the tree
            calcDepth();
        }
    }
}

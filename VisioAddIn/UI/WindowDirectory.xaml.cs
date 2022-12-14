
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VisioAddIn.Snapping;

namespace VisioAddIn
{
    /// <summary>
    /// Interaktionslogik für WindowDirectory.xaml
    /// </summary>
    public partial class WindowDirectory : Window, IDialogCallback
    {
        /// <summary>The Visio Application property</summary>
        /// <value>Instance of the Visio Application object</value>
        public Microsoft.Office.Interop.Visio.Application ParentVisioApplication { get; set; }

        private readonly ThisAddIn addIn;
        private ModelController modelController;

        // TODO replace ThisAddin reference with callback interface
        public WindowDirectory(ThisAddIn addIn, ModelController modelController)
        {
            InitializeComponent();

            btnRefresh.Content = VisioAddIn.Resources.strings.Refresh;

            this.addIn = addIn;
            this.modelController = modelController;

            // Initialize treeView.
            treeViewDirectory.AllowDrop = true;

            // The clicks will be handled as clicks on the treeViewItems 
            treeViewDirectory.MouseDoubleClick += treeViewItemMouseDoubleClick;
            treeViewDirectory.MouseLeftButtonUp += leftMouseClick;
            treeViewDirectory.MouseRightButtonUp += rightMouseClick;
            expandTree();

        }

        /// <summary>
        /// displays the whole treeView
        /// </summary>
        /// <param name="models">big big dictionary full of information about existing pages etc.</param>
        internal void displayTreeView(IDictionary<IVisioProcessModel, IDictionary<SIDPage, IList<SBDPage>>> models)
        {
            treeViewDirectory.Items.Clear();
            modelController = Globals.ThisAddIn.getModelController();

            // In each TreeViewItem, the information about an object (Model, SIDPage or SBDPage) are saved inside the tag parameter.
            // The maximum depth of this treeView is 3. It consists of the 3 layers processModels, SID-pages, SBD-pages.

            if (checkPriorityValid(models))
            {

                foreach (var modelEntry in models)
                {
                    // layer 0, all the Model-entries are added
                    DirectoryTreeViewItem treeItemModel = new DirectoryTreeViewItem(null);
                    treeItemModel.Header = modelEntry.Key.getModelUri();
                    treeItemModel.Tag = modelEntry.Key;
                    treeItemModel.DirectoryParent = treeItemModel;


                    foreach (var sidEntry in modelEntry.Value.OrderBy(i => i.Key.getPriorityOrder()))
                    {
                        // layer 1, all the SID-pages are added
                        DirectoryTreeViewItem treeItemSID = new DirectoryTreeViewItem(treeItemModel)
                        {
                            Header = sidEntry.Key.getLayer(),
                            Tag = sidEntry.Key,
                            DirectoryParent = treeItemModel
                        };


                        foreach (SBDPage sbdEntry in sidEntry.Value)
                        {
                            // layer 3, all the SBD-pages are added
                            DirectoryTreeViewItem treeItemSbd = new DirectoryTreeViewItem(treeItemSID)
                            {
                                Header = sbdEntry.getNameU(),
                                Tag = sbdEntry,
                                DirectoryParent = treeItemSID
                            };

                            // Add all items belonging to sidEntry as child-nodes to the current treeItemSID-node
                            treeItemSID.Items.Add(treeItemSbd);
                        }
                        // Add all items belonging to modelEntry as child-nodes to the current treeItemModel-node
                        treeItemModel.Items.Add(treeItemSID);

                    }
                    // Add all model-items as top layer to the treeView
                    treeViewDirectory.Items.Add(treeItemModel);
                }
                expandTree();
            }
            else
            {
                btnRefresh.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        private bool checkPriorityValid(IDictionary<IVisioProcessModel, IDictionary<SIDPage, IList<SBDPage>>> models)
        {
            bool valid = true;
            foreach (var modelEntry in models)
            {
                LinkedList<int> usedNumbers = new LinkedList<int>();
                foreach (var sidEntry in modelEntry.Value.OrderBy(i => i.Key.getPriorityOrder()))
                {
                    SIDPage page = sidEntry.Key;
                    int priority = page.getPriorityOrder();
                    if (usedNumbers.Contains(priority))
                    {
                        valid = false;
                        priority = usedNumbers.First() + 2;
                        modelController.updatePagePriority(usedNumbers.First() + 2, page);
                        usedNumbers.AddFirst(priority);
                    }
                    else
                    {
                        if (usedNumbers.Count == 0 || priority > usedNumbers.First())
                        {
                            usedNumbers.AddFirst(priority);
                        }
                        else
                        {
                            usedNumbers.AddLast(priority);
                        }
                    }
                }
            }
            return valid;
        }

        /// <summary>
        /// Expands the whole tree
        /// </summary>
        private void expandTree()
        {
            foreach (TreeViewItem item in treeViewDirectory.Items)
            {
                item.ExpandSubtree();
            }
        }


        private void openWindowChangePropertiesSbd(FrameworkElement node)
        {
            SBDPage clickedOn = (SBDPage)node.Tag;
            if (clickedOn.getExtends() == null) return;
            WindowChangePropertiesSbd changePropertiesSbd = new WindowChangePropertiesSbd(this, addIn, modelController, clickedOn)
            {
                Topmost = true
            };
            changePropertiesSbd.Show();
        }

        private void openWindowChangeProperties(FrameworkElement node)
        {
            SIDPage clickedOn = (SIDPage)node.Tag;
            WindowChangeProperties window = new WindowChangeProperties(this, addIn, modelController, clickedOn,
                modelController.getExtendableSidPages(clickedOn))
            {
                Topmost = true
            };
            window.Show();
        }

        // Used to save positions for dragging and dropping items inside the treeView

        // The point where dragging starts
        private Point lastMouseDown;

        // draggedItem is the item being dragged, _target is the item currently selected as dragging target
        private DirectoryTreeViewItem draggedItem, target;

        /// <summary>
        /// Dragging process starts here.
        /// Remembers the last mouse pos if the left button went down
        /// </summary>
        /// <param name="sender">the mouse button</param>
        /// <param name="e">mouse events</param>
        private void treeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                lastMouseDown = e.GetPosition(treeViewDirectory);
            }

        }

        /// <summary>
        /// Records the movement of the mouse while the left button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                // It is only a drag if the button is pressed
                if (e.LeftButton != MouseButtonState.Pressed) return;
                Point currentPosition = e.GetPosition(treeViewDirectory);

                // If the movement is too little, ignore 
                if ((!(Math.Abs(currentPosition.X - lastMouseDown.X) > 10.0)) &&
                    (!(Math.Abs(currentPosition.Y - lastMouseDown.Y) > 10.0))) return;

                draggedItem = (DirectoryTreeViewItem)treeViewDirectory.SelectedItem;
                if (draggedItem == null || draggedItem.Depth != 1) return;

                // Starts a drag-drop process, waits for its outcome
                DragDropEffects finalDropEffect = DragDrop.DoDragDrop(treeViewDirectory, treeViewDirectory.SelectedValue,
                    DragDropEffects.Move);
                // Once finished, the methods treeViewDragOverHandler and treeViewDropHandler have been called
                // and might have set the drop effect to Move == Drop is valid

                // Checking target is not null and item is dragging(moving)
                if ((finalDropEffect != DragDropEffects.Move) || (target == null)) return;

                // Finally copy the dragged item to the target item and reset variables
                copyItem(draggedItem, target);
                target = null;
                draggedItem = null;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Called by Visio, passing the ongoing DragEventArgs from the DragDrop process started in <see cref="treeView_MouseDown"/>
        /// Visualizes while dragging if the current target (over which the user hovers) is a valid target for the drag & drop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeViewDragOverHandler(object sender, DragEventArgs e)
        {
            try
            {
                Point currentPosition = e.GetPosition(treeViewDirectory);

                if ((Math.Abs(currentPosition.X - lastMouseDown.X) > 10.0) ||
                    (Math.Abs(currentPosition.Y - lastMouseDown.Y) > 10.0))
                {
                    // Get the nearest container in the list as the new drop target
                    TreeViewItem targetItem = getNearestContainer(e.OriginalSource as UIElement);

                    // Verify that this is a valid drop, then visually mark the drop target
                    if (checkDropTargetValid(draggedItem, targetItem))
                    {
                        // Changes the mouse pointer to a symbol that signalizes dropping is possible on this element
                        e.Effects = DragDropEffects.Move;
                        targetItem.Focus();
                    }
                    // Changes the mouse pointer to a symbol that signalizes dropping is not possible on this element
                    else e.Effects = DragDropEffects.None;
                }
                e.Handled = true;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Called by Visio, passing the ongoing DragEventArgs from the DragDrop process started in <see cref="treeView_MouseDown"/>
        /// Decides on the final outcome of the drag & drop (valid or invalid).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeViewDropHandler(object sender, DragEventArgs e)
        {
            try
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;

                // Verify that this is a valid drop and then store the drop target
                DirectoryTreeViewItem targetItem = getNearestContainer(e.OriginalSource as UIElement);
                if (targetItem == null || draggedItem == null || !checkDropTargetValid(draggedItem, targetItem)) return;
                target = targetItem;
                // Allow to drop here
                e.Effects = DragDropEffects.Move;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Checks that the target of the drop is not the selected item itself.
        /// </summary>
        /// <param name="sourceItem">The item that is dragged</param>
        /// <param name="targetItem">The item it is dragged onto</param>
        /// <returns></returns>
        private static bool checkDropTargetValid(HeaderedItemsControl sourceItem, HeaderedItemsControl targetItem)
        {
            // Check whether the target item is meeting the drop conditions
            bool isEqual = !sourceItem.Header.ToString().Equals(targetItem.Header.ToString());
            return isEqual;

        }

        /// <summary>
        /// Copies the source item to the target item (as child) on which it was dragged.
        /// </summary>
        /// <param name="sourceItem">The dragged and selected source item</param>
        /// <param name="targetItem">The target item</param>
        private void copyItem(DirectoryTreeViewItem sourceItem, DirectoryTreeViewItem targetItem)
        {
            if (sourceItem.Depth != 1 || targetItem.Depth > 1) return;
            try
            {
                //finding Parent TreeViewItem of dragged TreeViewItem 
                DirectoryTreeViewItem parentSource = sourceItem.DirectoryParent;

                // save current model of the dragged item (model is saved in the tag of the dragged items parent)
                VisioProcessModel oldModel = (VisioProcessModel)parentSource.Tag;


                // parentItem is not null, because _sourceItem has depth 1
                // -> Remove the dragged item from the parent
                parentSource.Items.Remove(sourceItem);
                DirectoryTreeViewItem addedItem;

                // If target is SID-page
                if (targetItem.Depth == 1)
                {
                    // The priority of target stays the same, the dragged item will end up with the same parent as the target,
                    // but have a priority that is 2 greater than the priority of the target

                    DirectoryTreeViewItem parentTarget = targetItem.DirectoryParent;

                    // Do not add to node as child but insert it one hierarchy above at this place.
                    addedItem = addChildItem(sourceItem, parentTarget, targetItem.Index + 1);

                    // check if priority has to be changed
                    SIDPage targetSid = (SIDPage)targetItem.Tag;
                    SIDPage sourceSid = (SIDPage)addedItem.Tag;

                    if (sourceSid.getPriorityOrder() <= targetSid.getPriorityOrder())
                    {
                        // Increase priority of the addedItems page to 2 greater than target prio
                        modelController.updatePagePriority(targetSid.getPriorityOrder() + 2, sourceSid);
                        addedItem.Header = sourceSid.getPriorityOrder() + " | " + sourceSid.getLayer();
                    }
                    if (parentTarget.Items.Count > addedItem.Index + 1)
                    {
                        // There are nodes below the added one - check them
                        int index = addedItem.Index + 1;
                        SIDPage belowSID = (SIDPage)((TreeViewItem)parentTarget.Items[index]).Tag;

                        // Changes priority for all items with higher index than the addedItem
                        changePriority(parentTarget, index, sourceSid);

                    }
                }
                // Else it is SBD page
                else
                {
                    addedItem = addChildItem(sourceItem, targetItem, 0);
                    //_targetItem.Items.Insert(0, _sourceItem);
                    //ensure that priority changes
                    SIDPage draggedSid = (SIDPage)addedItem.Tag;
                    if (targetItem.Items.Count > 1)
                    {
                        SIDPage referenceSid = (SIDPage)((TreeViewItem)targetItem.Items[1]).Tag;
                        if (draggedSid.getPriorityOrder() >= referenceSid.getPriorityOrder())
                        {
                            if (referenceSid.getPriorityOrder() > 1)
                            {
                                modelController.updatePagePriority(referenceSid.getPriorityOrder() - 1, draggedSid);
                                //draggedSID.setPriorityOrder(referenceSID.GetPriorityOrder() - 1);
                                addedItem.Header = draggedSid.getPriorityOrder() + " | " + draggedSid.getLayer();
                            }
                            else
                            {
                                //set prio of newly added node to 1. change all the others.
                                modelController.updatePagePriority(1, draggedSid);
                                //draggedSID.setPriorityOrder(1);
                                addedItem.Header = draggedSid.getPriorityOrder() + " | " + draggedSid.getLayer();

                                if (targetItem.Items.Count > addedItem.Index + 1)
                                {
                                    //there are nodes below the dragged one - check them
                                    int index = addedItem.Index + 1;
                                    SIDPage belowSid = (SIDPage)((TreeViewItem)targetItem.Items[index]).Tag;

                                    changePriority(targetItem, index, referenceSid);
                                }

                            }
                        }
                    }
                }

                VisioProcessModel newModel = (VisioProcessModel)targetItem.DirectoryParent.Tag;

                SIDPage changedPage = (SIDPage)addedItem.Tag;
                modelController.changeModelForSidPage(changedPage, newModel, oldModel);
                this.applyChanges();

            }
            catch
            {
                // ignored
            }

        }

        /// <summary>
        /// Add a child item to a TreeViewItem
        /// </summary>
        /// <param name="sourceItem">the child</param>
        /// <param name="targetItem">the new parent</param>
        /// <param name="index">the index where the child will be</param>
        /// <returns></returns>
        private static DirectoryTreeViewItem addChildItem(HeaderedItemsControl sourceItem, DirectoryTreeViewItem targetItem, int index)
        {
            // add item in target TreeViewItem 
            DirectoryTreeViewItem child = new DirectoryTreeViewItem(targetItem)
            {
                Header = sourceItem.Header,
                Tag = sourceItem.Tag
            };
            targetItem.Items.Insert(index, child);
            foreach (DirectoryTreeViewItem item in sourceItem.Items)
            {
                addChildItem(item, child, item.Index);
            }
            return child;
        }

        private static DirectoryTreeViewItem getNearestContainer(UIElement element)
        {
            // Walk up the element tree to the nearest tree view item.
            DirectoryTreeViewItem container = element as DirectoryTreeViewItem;
            while ((container == null) && (element != null))
            {
                element = VisualTreeHelper.GetParent(element) as UIElement;
                container = element as DirectoryTreeViewItem;
            }
            return container;
        }


        /// <summary>
        /// Changes the priority of all parent.items with item-index greater than index, if the prio has to be changed.
        /// The priority of an item will only be changed if the previous item contains an object (as tag) with higher prio  than this item.
        /// </summary>
        /// <param name="parent">the parent node in the treeView</param>
        /// <param name="index">the index where this method starts to change priority</param>
        /// <param name="referenceSid">the sid of the treeViewItem at index (i-1)</param>
        private void changePriority(ItemsControl parent, int index, SIDPage referenceSid)
        {
            SIDPage belowSid = (SIDPage)((TreeViewItem)parent.Items[index]).Tag;

            // Do as long as there are items and the priority of the next item is smaller than the current.
            while (parent.Items.Count > index && referenceSid.getPriorityOrder() >= belowSid.getPriorityOrder())
            {
                modelController.updatePagePriority(referenceSid.getPriorityOrder() + 2, belowSid);
                ((TreeViewItem)(parent.Items[index])).Header = belowSid.getPriorityOrder() + " | " + belowSid.getLayer();

                index++;
                if (parent.Items.Count <= index) continue;
                referenceSid = belowSid;
                belowSid = (SIDPage)((TreeViewItem)(parent.Items[index])).Tag;
            }
        }

        // Determine whether one node is a parent 
        // or ancestor of a second node.
        /// <summary>
        /// Checks whether an item contains another item
        /// </summary>
        /// <param name="firstItem">the first item</param>
        /// <param name="secondItem">the second item</param>
        /// <returns>true if first item or one of the first items childs contains the second item, false otherwise</returns>
        private bool containsNode(ItemsControl firstItem, TreeViewItem secondItem)
        {
            if (firstItem.Items.Count == 0)
            {
                return false;
            }

            if (firstItem.Items.Contains(secondItem))
            {
                return true;
            }

            return firstItem.Items.Cast<TreeViewItem>().Any(child => containsNode(child, secondItem));
        }

        private void rightMouseClick(object sender, MouseButtonEventArgs e)
        {
            // Finds the clicked TreeViewItem
            DirectoryTreeViewItem treeViewItem = (DirectoryTreeViewItem)visualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem == null) return;
            // Highlights the right-clicked item
            treeViewItem.Focus();

            // Only SID and SBD pages should be right-clickable
            if (treeViewItem.Depth > 0)
            {
                treeViewItem.ContextMenu = treeViewDirectory.Resources["itemProperties"] as System.Windows.Controls.ContextMenu;

                if (treeViewItem.ContextMenu != null)
                {
                    treeViewItem.ContextMenu.PlacementTarget = treeViewItem;
                    treeViewItem.ContextMenu.IsOpen = true;
                }
            }
            e.Handled = true;

        }


        private void treeItemChangeLayerNameClicked(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            ContextMenu contextMenu = (ContextMenu)LogicalTreeHelper.GetParent(menuItem);
            DirectoryTreeViewItem item = (DirectoryTreeViewItem)contextMenu.PlacementTarget;
            if (item.Depth != 1) return;
            WindowChangeLayerName changeLayer = new WindowChangeLayerName(this, modelController, (SIDPage)item.Tag);
            changeLayer.Show();
        }

        private void treeItemMoveUpClicked(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            ContextMenu contextMenu = (ContextMenu)LogicalTreeHelper.GetParent(menuItem);
            DirectoryTreeViewItem item = (DirectoryTreeViewItem)contextMenu.PlacementTarget;

            DirectoryTreeViewItem parent = item.DirectoryParent;

            if (item.Index <= 0) return;
            SIDPage selectedSid = (SIDPage)item.Tag;
            SIDPage aboveSid = (SIDPage)((TreeViewItem)parent.Items[item.Index - 1]).Tag;

            int oldPriority = selectedSid.getPriorityOrder();

            modelController.updatePagePriority(aboveSid.getPriorityOrder(), selectedSid);
            modelController.updatePagePriority(oldPriority, aboveSid);
            this.applyChanges();
        }
        private void treeItemMoveDownClicked(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            ContextMenu contextMenu = (ContextMenu)LogicalTreeHelper.GetParent(menuItem as DependencyObject);
            DirectoryTreeViewItem item = (DirectoryTreeViewItem)contextMenu.PlacementTarget;

            DirectoryTreeViewItem parent = item.DirectoryParent;

            if (parent.Items.Count > item.Index + 1)
            {
                SIDPage selectedSid = (SIDPage)item.Tag;
                SIDPage belowSid = (SIDPage)((TreeViewItem)parent.Items[item.Index + 1]).Tag;

                modelController.updatePagePriority(belowSid.getPriorityOrder() + 2, selectedSid);

                //check for the pages below
                if (parent.Items.Count > item.Index + 2)
                {
                    int index = item.Index + 2;

                    changePriority(parent, index, selectedSid);
                }
                this.applyChanges();
            }

            addIn.refreshLayerExplorerTreeView();
        }
        private void treeItemPropertiesClicked(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            ContextMenu contextMenu = (ContextMenu)LogicalTreeHelper.GetParent(menuItem);
            DirectoryTreeViewItem item = (DirectoryTreeViewItem)contextMenu.PlacementTarget;
            switch (item.Depth)
            {
                case 1:
                    openWindowChangeProperties(item);
                    break;
                case 2:
                    openWindowChangePropertiesSbd(item);
                    break;
            }
        }

        private void leftMouseClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2) return;
            DirectoryTreeViewItem item = (DirectoryTreeViewItem)visualUpwardSearch(e.OriginalSource as DependencyObject);
            if (item != null && item.Depth > 0)
            {
                //must be sid or sbd.
                if (item.Depth == 1)
                {
                    SIDPage sidPage = (SIDPage)item.Tag;
                    modelController.setActivePage(sidPage);
                }
                else
                {
                    SBDPage sbdPage = (SBDPage)item.Tag;
                    modelController.setActivePage(sbdPage);
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// On left double-click, open the change-properties-window if it is a SID-item
        /// </summary>
        /// <param name="sender">The object being clicked</param>
        /// <param name="args">mouse parameters</param>
        private void treeViewItemMouseDoubleClick(object sender, MouseButtonEventArgs args)
        {
            if (args.ChangedButton != MouseButton.Left) return;
            //if it's a sid node, open form change props
            DirectoryTreeViewItem item = (DirectoryTreeViewItem)visualUpwardSearch(args.OriginalSource as DependencyObject);
            if (item != null && item.Depth == 1)
            {
                openWindowChangeProperties(item);
            }
            else if (item != null && item.Depth == 2)
            {
                openWindowChangePropertiesSbd(item);
            }
            args.Handled = true;
        }

        /// <summary>
        /// Called when the updateWholeController button is clicked
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">parameters</param>
        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            Globals.ThisAddIn.updateClicked();
            modelController = Globals.ThisAddIn.getModelController();
            e.Handled = true;
        }


        /// <summary>
        /// Returns the first visual parent that is a TreeViewItem.
        /// Might be null if there is none.
        /// </summary>
        /// <param name="source">The name of the component</param>
        /// <returns>The TreeViewItem parent</returns>
        static TreeViewItem visualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        public void applyChanges()
        {
            btnRefresh.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
    }


}

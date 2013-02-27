using System.Collections.Generic;
using System.Windows.Input;

namespace System.Windows.Controls
{
    public static class TreeViewWorkarounds
    {
        public static TreeViewItem FindContainer(this TreeView treeView, Predicate<TreeViewItem> condition)
        {
            return FindContainer(treeView.ItemContainerGenerator, treeView.Items, condition);
        }

        private static TreeViewItem FindContainer(ItemContainerGenerator parentItemContainerGenerator, ItemCollection itemCollection, Predicate<TreeViewItem> condition)
        {
            foreach (object curChildItem in itemCollection)
            {
                TreeViewItem containerThatMightMeetTheCondition = (TreeViewItem)parentItemContainerGenerator.ContainerFromItem(curChildItem);

                if (containerThatMightMeetTheCondition == null)
                    return null;

                if (condition(containerThatMightMeetTheCondition))
                    return containerThatMightMeetTheCondition;

                TreeViewItem recursionResult = FindContainer(containerThatMightMeetTheCondition.ItemContainerGenerator, containerThatMightMeetTheCondition.Items, condition);
                if (recursionResult != null)
                    return recursionResult;
            }
            return null;
        }

        public static TreeViewItem ContainerFromItem(this TreeView treeView, object item)
        {
            TreeViewItem containerThatMightContainItem = (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(item);
            if (containerThatMightContainItem != null)
                return containerThatMightContainItem;
            else
                return ContainerFromItem(treeView.ItemContainerGenerator, treeView.Items, item);
        }

        private static TreeViewItem ContainerFromItem(ItemContainerGenerator parentItemContainerGenerator, ItemCollection itemCollection, object item)
        {
            foreach (object curChildItem in itemCollection)
            {
                TreeViewItem parentContainer = (TreeViewItem)parentItemContainerGenerator.ContainerFromItem(curChildItem);
                if (parentContainer == null)
                    return null;
                TreeViewItem containerThatMightContainItem = (TreeViewItem)parentContainer.ItemContainerGenerator.ContainerFromItem(item);
                if (containerThatMightContainItem != null)
                    return containerThatMightContainItem;
                TreeViewItem recursionResult = ContainerFromItem(parentContainer.ItemContainerGenerator, parentContainer.Items, item);
                if (recursionResult != null)
                    return recursionResult;
            }
            return null;
        }

        public static object ItemFromContainer(this TreeView treeView, TreeViewItem container)
        {
            TreeViewItem itemThatMightBelongToContainer = (TreeViewItem)treeView.ItemContainerGenerator.ItemFromContainer(container);
            if (itemThatMightBelongToContainer != null)
                return itemThatMightBelongToContainer;
            else
                return ItemFromContainer(treeView.ItemContainerGenerator, treeView.Items, container);
        }

        private static object ItemFromContainer(ItemContainerGenerator parentItemContainerGenerator, ItemCollection itemCollection, TreeViewItem container)
        {
            foreach (object curChildItem in itemCollection)
            {
                TreeViewItem parentContainer = (TreeViewItem)parentItemContainerGenerator.ContainerFromItem(curChildItem);
                if (parentContainer == null)
                    return null;
                TreeViewItem itemThatMightBelongToContainer = (TreeViewItem)parentContainer.ItemContainerGenerator.ItemFromContainer(container);
                if (itemThatMightBelongToContainer != null)
                    return itemThatMightBelongToContainer;
                TreeViewItem recursionResult = ItemFromContainer(parentContainer.ItemContainerGenerator, parentContainer.Items, container) as TreeViewItem;
                if (recursionResult != null)
                    return recursionResult;
            }
            return null;
        }
    }

    public class TreeViewExtended : TreeView
    {
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewExtended;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeViewItemExtended();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            TreeViewItemExtended treeViewItemExtended = (TreeViewItemExtended)element;
            treeViewItemExtended.ParentTreeView = this;

            base.PrepareContainerForItemOverride(element, item);

            InvokeContainerPrepared(treeViewItemExtended, item);
        }

        public TreeViewExtended()
        {
            this.DefaultStyleKey = typeof(TreeView);
        }

        public IEnumerable<TreeViewItem> Containers
        {
            get
            {
                return GetTreeViewItems(this.ItemContainerGenerator, this.Items);
            }
        }

        private static IEnumerable<TreeViewItem> GetTreeViewItems(ItemContainerGenerator parentItemContainerGenerator, ItemCollection itemCollection)
        {
            foreach (object curChildItem in itemCollection)
            {
                TreeViewItem container = (TreeViewItem)parentItemContainerGenerator.ContainerFromItem(curChildItem);

                if (container != null)
                    yield return container;

                foreach (var treeViewItem in GetTreeViewItems(container.ItemContainerGenerator, container.Items))
                    yield return treeViewItem;
            }
        }

        public event RoutedEventHandler ContainerExpanded;
        internal void InvokeContainerExpanded(object sender, RoutedEventArgs e)
        {
            RoutedEventHandler expanded = ContainerExpanded;
            if (expanded != null) expanded(sender, e);
        }

        public event RoutedEventHandler ContainerCollapsed;
        internal void InvokeContainerCollapsed(object sender, RoutedEventArgs e)
        {
            RoutedEventHandler collapsed = ContainerCollapsed;
            if (collapsed != null) collapsed(sender, e);
        }

        public event MouseButtonEventHandler ContainerMouseLeftButtonDownEventHandler;
        internal void InvokeContainerMouseLeftButtonButtonEventHandler(object sender, MouseButtonEventArgs e)
        {
            MouseButtonEventHandler handler = ContainerMouseLeftButtonDownEventHandler;
            if (handler != null) handler(sender, e);
        }

        public event EventHandler<ContainerPreparedEventArgs> ContainerPrepared;
        internal void InvokeContainerPrepared(TreeViewItemExtended sender, object item)
        {
            EventHandler<ContainerPreparedEventArgs> prepared = ContainerPrepared;
            if (prepared != null) prepared(sender, new ContainerPreparedEventArgs(sender, item));

            if (itemsToDelayExpand.Contains(item))
            {
                sender.IsExpanded = true;
                itemsToDelayExpand.Remove(item);
            }
        }

        private List<object> itemsToDelayExpand = new List<object>();
        public void ExpandDelayItems(params object[] ItemsCorrespondingToTreeViewItemsToExpand)
        {
            itemsToDelayExpand.AddRange(ItemsCorrespondingToTreeViewItemsToExpand);

            foreach (object itemtoTryAndExpand in ItemsCorrespondingToTreeViewItemsToExpand)
            {
                TreeViewItem treeViewItem = this.ContainerFromItem(itemtoTryAndExpand);
                if (treeViewItem != null)
                {
                    treeViewItem.IsExpanded = true;
                    itemsToDelayExpand.Remove(itemtoTryAndExpand);
                }
            }
        }

        private object SelectedItemDelayed = null;
        public void SetSelectedItem(object SelectedItem, params object[] SelectedItemParents)
        {
            ContainerPrepared += new EventHandler<ContainerPreparedEventArgs>(ContainerPrepared_LookForSelectedItem);
            SelectedItemDelayed = SelectedItem;
            ExpandDelayItems(SelectedItemParents);
        }

        private void ContainerPrepared_LookForSelectedItem(object sender, ContainerPreparedEventArgs e)
        {
            if (e.Item == SelectedItemDelayed)
            {
                e.Container.IsSelected = true;
                SelectedItemDelayed = null;
                ContainerPrepared -= new EventHandler<ContainerPreparedEventArgs>(ContainerPrepared_LookForSelectedItem);
            }
        }



    }

    public class ContainerPreparedEventArgs : EventArgs
    {
        public ContainerPreparedEventArgs(TreeViewItemExtended container, object item)
        {
            Container = container;
            Item = item;
        }

        public TreeViewItemExtended Container { get; set; }
        public object Item { get; set; }
    }

    public class TreeViewItemExtended : TreeViewItem
    {
        public TreeViewItemExtended()
        {
            this.Expanded += new RoutedEventHandler(TreeViewItemExtended_Expanded);
            this.Collapsed += new RoutedEventHandler(TreeViewItemExtended_Collapsed);
        }

        void TreeViewItemExtended_Collapsed(object sender, RoutedEventArgs e)
        {
            ParentTreeView.InvokeContainerCollapsed(sender, e);
        }

        void TreeViewItemExtended_Expanded(object sender, RoutedEventArgs e)
        {
            ParentTreeView.InvokeContainerExpanded(sender, e);
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewExtended;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeViewItemExtended();
        }

        public TreeViewExtended ParentTreeView { internal set; get; }
        public TreeViewItemExtended ParentTreeViewItem { internal set; get; }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            TreeViewItemExtended treeViewItemExtended = (TreeViewItemExtended)element;
            treeViewItemExtended.ParentTreeView = this.ParentTreeView;
            treeViewItemExtended.ParentTreeViewItem = this;

            base.PrepareContainerForItemOverride(element, item);

            ParentTreeView.InvokeContainerPrepared(treeViewItemExtended, item);
        }
    }


}


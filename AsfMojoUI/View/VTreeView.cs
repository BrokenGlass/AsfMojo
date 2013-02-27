using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace AsfMojoUI.View
{
    public class VTreeView : TreeView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is VTreeViewItem);
        }
    }

    public class VTreeViewItem : TreeViewItem
    {
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            this.IsSelected = true;
            this.RaiseEvent(e);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is VTreeViewItem);
        }
    }
}

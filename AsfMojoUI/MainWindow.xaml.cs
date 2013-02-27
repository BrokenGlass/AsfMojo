using System.Windows;
using AsfMojoUI.ViewModel;
using System.Windows.Controls;
using System.IO;
using System;
using AsfMojoUI.Model;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media;

using System.Linq;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Net.Cache;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using AsfMojoUI.View;
using System.Diagnostics;
using AsfMojo.Media;
using AsfMojo.Parsing;
using System.Windows.Input;

namespace AsfMojoUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();

        }

        private void Filmstrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Filmstrip.SelectedItem == null)
                return;

            PreviewImage pi = Filmstrip.SelectedItem as PreviewImage;
            long targetPresentationTime = (long)(pi.PresentationTime);

            SelectTreeviewItemByMatch(
                                        x =>
                                        {
                                            long delta = x.Payload[0].PresentationTime - targetPresentationTime;
                                            if (delta > 50)
                                                return 1;
                                            if (delta < -50)
                                                return -1;
                                            else
                                                return 0;
                                        });

            Filmstrip.SelectedIndex = -1;
        }

        private void SelectTreeviewItemByMatch(Func<AsfPacket, int> matchPacketMethod, Func<AsfPacket, int> matchPacketFollowupMethod = null, Func<List<PayloadInfo>, int> matchPayloadMethod = null)
        {
            AsfHierarchy.UpdateLayout();

            foreach (AsfHeaderItem item in AsfHierarchy.Items)
            {
                if (item.Name == "Data Object")
                {
                    AsfDataObjectItem asfDataObjectItem = item as AsfDataObjectItem;

                    TreeViewItem treeViewItem = AsfHierarchy.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                    if (treeViewItem == null)
                        return;
                    treeViewItem.IsExpanded = true;
                    treeViewItem.ApplyTemplate();
                    ItemsPresenter itemsPresenter = (ItemsPresenter)treeViewItem.Template.FindName("ItemsHost", treeViewItem);
                    if (itemsPresenter != null)
                    {
                        itemsPresenter.ApplyTemplate();
                    }
                    else
                    {
                        // The Tree template has not named the ItemsPresenter, 
                        // so walk the descendents and find the child.
                        itemsPresenter = FindVisualChild<ItemsPresenter>(treeViewItem);
                        if (itemsPresenter == null)
                        {
                            treeViewItem.UpdateLayout();

                            itemsPresenter = FindVisualChild<ItemsPresenter>(treeViewItem);
                        }
                    }
                    Panel itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);
                    // Ensure that the generator for this panel has been created.
                    UIElementCollection children = itemsHostPanel.Children; 

                    MyVirtualizingStackPanel msp = itemsHostPanel as MyVirtualizingStackPanel;

                    //binary search for right payload for the time offset requested with a 50 millisecond bucket size
                    int index = 0;

                    index = asfDataObjectItem.Packets.BinarySearchForMatch(matchPacketMethod);
                    if(matchPacketFollowupMethod!=null)
                        index = asfDataObjectItem.Packets.LinearSearchForMatch(index, matchPacketFollowupMethod);

                    if (index >= 0 && index < asfDataObjectItem.Packets.Count)
                    {
                        msp.BringIntoView(index);
                        var selectedPacket = treeViewItem.ItemContainerGenerator.ContainerFromIndex(index);
                        bool bBuilt = false;

                        if (selectedPacket != null)
                        {
                            TreeViewItem packetTreeViewItem = selectedPacket as TreeViewItem;
                            //packetTreeViewItem.IsSelected = true;
                            packetTreeViewItem.IsExpanded = true;

                            //now expand to payload
                            bBuilt = packetTreeViewItem.ApplyTemplate();
                            itemsPresenter = (ItemsPresenter)packetTreeViewItem.Template.FindName("ItemsHost", packetTreeViewItem);
                            if (itemsPresenter != null)
                            {
                                bBuilt = itemsPresenter.ApplyTemplate();
                            }
                            else
                            {
                                // The Tree template has not named the ItemsPresenter, 
                                // so walk the descendents and find the child.
                                itemsPresenter = FindVisualChild<ItemsPresenter>(packetTreeViewItem);
                                if (itemsPresenter == null)
                                {
                                    packetTreeViewItem.UpdateLayout();
                                    itemsPresenter = FindVisualChild<ItemsPresenter>(packetTreeViewItem);
                                }
                            }
                            itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);
                            // Ensure that the generator for this panel has been created.
                            children = itemsHostPanel.Children;
                            msp = itemsHostPanel as MyVirtualizingStackPanel;

                            int payloadIndex = 0;

                            if (matchPayloadMethod != null)
                                payloadIndex = matchPayloadMethod(asfDataObjectItem.Packets[index].Payload);

                            if (payloadIndex < 0 || payloadIndex >= asfDataObjectItem.Packets[index].Payload.Count)
                                payloadIndex = 0;

                            msp.BringIntoView(payloadIndex);
                            var selectedPayload = packetTreeViewItem.ItemContainerGenerator.ContainerFromIndex(payloadIndex);
                            TreeViewItem payloadTreeViewItem = selectedPayload as TreeViewItem;
                            if (payloadTreeViewItem != null)
                            {
                                payloadTreeViewItem.BringIntoView();
                                payloadTreeViewItem.IsSelected = true;
                                payloadTreeViewItem.IsExpanded = true;
                                payloadTreeViewItem.Focus();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Search for an element of a certain type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of element to find.</typeparam>
        /// <param name="visual">The parent element.</param>
        /// <returns></returns>
        private T FindVisualChild<T>(Visual visual) where T : Visual
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                Visual child = (Visual)VisualTreeHelper.GetChild(visual, i);
                if (child != null)
                {
                    T correctlyTyped = child as T;
                    if (correctlyTyped != null)
                    {
                        return correctlyTyped;
                    }

                    T descendent = FindVisualChild<T>(child);
                    if (descendent != null)
                    {
                        return descendent;
                    }
                }
            }

            return null;
        }

        private void timeImage_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            Image image = sender as Image;

            if(e.Delta > 0)
            {
                 int newWidth = (int)(image.Width * 1.1);
                 image.Width = Math.Min(newWidth, image.Source.Width*1.1);
            }
            else
            {
                int newWidth = (int)(image.Width * 0.9);
                image.Width = Math.Max(newWidth, image.Source.Width*0.1);
            }
        }

        private void timeImage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ViewModelLocator.MainStatic.ShowImageDialogCommand.Execute(null);
            }
        }


        private DependencyObject GetBorderParent(DependencyObject dep)
        {
            while ((dep != null) && !(dep is Border))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            return dep;
        }

        private void ButtonPrevPayload_Click(object sender, RoutedEventArgs e)
        {
            PayloadInfo pi = (sender as FrameworkElement).DataContext as PayloadInfo;

            int targetPayloadId = pi.PayloadId - 1;
            int targetStreamId = pi.StreamId;
            SelectPayload(targetPayloadId, targetStreamId, false);
        }

        private void ButtonNextPayload_Click(object sender, RoutedEventArgs e)
        {
            PayloadInfo pi = (sender as FrameworkElement).DataContext as PayloadInfo;

            int targetPayloadId = pi.PayloadId + 1;
            int targetStreamId = pi.StreamId;
            SelectPayload(targetPayloadId, targetStreamId, true);
        }

        private void SelectPayload(int targetPayloadId, int targetStreamId, bool moveForward)
        {
            SelectTreeviewItemByMatch(matchPacketMethod: x =>
                                    {
                                        bool isRightPayload = x.Payload.Where(p => p.PayloadId == targetPayloadId).Any();
                                        if (isRightPayload)
                                            return 0;
                                        else
                                            if (x.Payload[0].PayloadId > targetPayloadId)
                                                return 1;
                                            else
                                                return -1;
                                    },
                                    matchPacketFollowupMethod: x=>
                                    {
                                        bool isRightPayload = x.Payload.Where(p => p.StreamId == targetStreamId && (moveForward ? (p.PayloadId >= targetPayloadId) : (p.PayloadId <= targetPayloadId))).Any();
                                        if (isRightPayload)
                                            return 0;
                                        else 
                                            return moveForward ? 1 : -1;
                                    },
                                    matchPayloadMethod: (List<PayloadInfo> x) =>
                                    {
                                        if(moveForward)
                                        {
                                            for (int index = 0; index < x.Count; index++)
                                            {
                                                bool isRightPayload = x[index].PayloadId >= targetPayloadId && x[index].StreamId == targetStreamId;
                                                if (isRightPayload)
                                                    return index;
                                            }
                                        }
                                        else
                                        {
                                            for (int index = x.Count-1; index >=0; index--)
                                            {
                                                bool isRightPayload = x[index].PayloadId <= targetPayloadId && x[index].StreamId == targetStreamId;
                                                if (isRightPayload)
                                                    return index;
                                            }
                                        }
                                        return 0;
                                    });  

        }


        private void AsfHierarchy_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
        }

        private void AsfHierarchy_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
        }

    }
}

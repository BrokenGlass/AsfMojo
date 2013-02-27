using System;
using System.Windows.Controls;

namespace AsfMojoUI
{
    public class MyVirtualizingStackPanel : VirtualizingStackPanel
    {
        /// <summary>
        /// Publically expose BringIndexIntoView.
        /// </summary>
        public void BringIntoView(int index)
        {
            try
            {
                this.BringIndexIntoView(index);
            }
            catch(Exception) {}
        }
    }

}

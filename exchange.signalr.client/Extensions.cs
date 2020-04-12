using System;
using System.Windows.Forms;

namespace exchange.signalr.client
{
    public static class Extensions
    {
        public static void UpdateListViewItemsThreadSafe(this ListView control, ListViewItem[] listViewItems)
        {
            if (control.InvokeRequired)
            {
                if (control.IsDisposed || control.Disposing) 
                    return;
                UpdateListViewItemsThreadSafe(control, listViewItems);
            }
            else
            {
                if (control.IsDisposed || control.Disposing)
                    return;
                control.Items.Clear();
                control.Items.AddRange(listViewItems);
                control.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }
    }
}

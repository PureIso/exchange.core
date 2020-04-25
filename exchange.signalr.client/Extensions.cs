using System;
using System.Drawing;
using System.Windows.Forms;
using exchange.core.Enums;

namespace exchange.signalR.client.win.form
{
    public static class Extensions
    {
        public static void LogTextEvent(this RichTextBox richTextBox, MessageType messageType, string message)
        {
            if (richTextBox.InvokeRequired)
            {
                richTextBox.BeginInvoke(new Action(delegate {
                    LogTextEvent(richTextBox, messageType, message);
                }));
                return;
            }
            string currentDateTime = DateTime.Now.ToString("hh:mm:ss tt") + " - ";
            // color text.
            switch (messageType)
            {
                case MessageType.Error:
                    richTextBox.SelectionStart = (currentDateTime + message).Length;
                    richTextBox.SelectionColor = Color.Red;
                    break;
                case MessageType.JsonOutput:
                    richTextBox.SelectionStart = (currentDateTime + message).Length;
                    richTextBox.SelectionColor = Color.White;
                    break;
                case MessageType.General:
                    richTextBox.SelectionStart = (currentDateTime + message).Length;
                    richTextBox.SelectionColor = Color.Aquamarine;
                    break;
            }
            // newline if first line, append if else.
            if (richTextBox.Lines.Length == 0)
            {
                richTextBox.AppendText(currentDateTime + message);
                richTextBox.ScrollToCaret();
                richTextBox.AppendText(Environment.NewLine);
            }
            else
            {
                richTextBox.AppendText(currentDateTime + message + Environment.NewLine);
                richTextBox.ScrollToCaret();
            }
        }

        public static void UpdateListViewItemsThreadSafe(this ListView control, ListViewItem[] listViewItems)
        {
            if (control.InvokeRequired)
            {
                control.BeginInvoke(new Action(delegate {
                    UpdateListViewItemsThreadSafe(control, listViewItems);
                }));
                return;
            }
            control.Items.Clear(); 
            control.Items.AddRange(listViewItems); 
            control.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }
    }
}

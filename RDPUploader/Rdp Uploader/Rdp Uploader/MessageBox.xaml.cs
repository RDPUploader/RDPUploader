using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;

namespace RDP_Uploader
{
    /// <summary>
    /// Interaction logic for MessageBox.xaml
    /// </summary>
    public partial class MessageBox : DXWindow
    {
        // Конструктор
        public MessageBox(string message, string title)
        {
            InitializeComponent();
            Title = "  " + title;
            textBox_Message.Text = message;
        }
    }
}

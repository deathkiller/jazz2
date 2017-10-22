using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Editor
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();

            Text = Jazz2.App.AssemblyTitle + " v" + Jazz2.App.AssemblyVersion;
        }
    }
}

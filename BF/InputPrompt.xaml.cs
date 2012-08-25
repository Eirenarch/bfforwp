using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BF
{
    public partial class InputPrompt : UserControl
    {
        public string Text
        {
            get { return tbx.Text; }
            set { tbx.Text = value; }
        }

        public string ErrorMessage
        {
            get { return tbErrorMessage.Text; }
            set { tbErrorMessage.Text = value; }
        }

        public InputPrompt()
        {
            InitializeComponent();
        }

        public void SetTextBoxFocus()
        {
            tbx.Focus();
        }
    }
}

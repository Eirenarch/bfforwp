using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace BF
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        protected void OnNotifyPropertyChanged(string p)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(p));
                    });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}

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
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;

namespace BF
{
    public partial class SnippetsListPage : PhoneApplicationPage
    {
        public SnippetsListPage()
        {
            InitializeComponent();
            
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            BindList();
        }

        private void BindList()
        {
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string[] fileNames = storage.GetFileNames("Snippets\\*");
                lbSnippets.ItemsSource = fileNames;
            }
        }

        private void lbSnippets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbSnippets.SelectedItem != null) //SelectionChanged is raised by the back button because the list is databound again and the SelectedItems is null
            {
                string fileName = lbSnippets.SelectedItem.ToString();
                ((App)App.Current).MainPageViewModel = new MainPageViewModel(fileName);
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            }
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            string fileName = (string)((MenuItem)sender).Tag;
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                storage.DeleteFile(App.SnippetsFolder + fileName);
            }
            BindList();
        }
    }
}
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
using System.ComponentModel;

namespace BF
{

    public partial class TapeCell : UserControl
    {
        public TapeCell()
        {
            InitializeComponent();
        }

        public TapeCellViewModel CellViewModel
        {
            get { return (TapeCellViewModel)GetValue(CellViewModelProperty); }
            set { SetValue(CellViewModelProperty, value); }
        }

        public static readonly DependencyProperty CellViewModelProperty =
            DependencyProperty.Register("CellViewModel", typeof(TapeCellViewModel), typeof(TapeCell),
            new PropertyMetadata(new TapeCellViewModel(), new PropertyChangedCallback(OnCellViewModelChanged)));

        protected virtual void OnCellViewModelChanged(DependencyPropertyChangedEventArgs e)
        {
            var viewModel = (TapeCellViewModel)e.NewValue;
            if (viewModel.CellInfo == null)
            {
                border.Background = (SolidColorBrush)App.Current.Resources["PhoneBackgroundBrush"];
                txtCellValue.Text = "";
                txtCellIndex.Text = "";
            }
            else
            {
                SetCellText();
                viewModel.PropertyChanged += new PropertyChangedEventHandler(viewModel_PropertyChanged);
            }
        }

        private void SetCellText()
        {
            txtCellValue.Text = CellViewModel.CellInfo.CellValue.ToString();
            txtCellIndex.Text = CellViewModel.CellInfo.CellIndex.ToString();
        }

        void viewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    SetCellText();
                });
        }

        private static void OnCellViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TapeCell)d).OnCellViewModelChanged(e);
        }
    }
}

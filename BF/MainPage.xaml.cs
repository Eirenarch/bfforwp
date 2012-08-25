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
using System.Threading;
using Microsoft.Phone.Shell;
using System.Windows.Controls.Primitives;

namespace BF
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const int LeftAnimationTarget = 0;
        private const int RightAnimationTarget = -192;
        private const int StartAnimationPosition = -96;
        private const int AnimationDelayOffset = 10;
        private const int MinAnimationDelay = 33; //Windows Phone updates the screen 60 times per second. Any animation that faster faster than 33ms is pointless
        private ApplicationBarIconButton btnRun;
        private ApplicationBarIconButton btnSave;
        private ApplicationBarIconButton btnLoad;
        private MainPageViewModel viewModel;
        private Popup currentPopup;
        public MainPageViewModel ViewModel
        {
            get { return viewModel; }
            set
            {
                viewModel = value;
                this.DataContext = value;
            }
        }


        public MainPage()
        {
            InitializeComponent();
            ViewModel = ((App)App.Current).MainPageViewModel;
            ViewModel.OnInputEnd += new EventHandler(ViewModel_OnInputEnd);
            ViewModel.OnError += new ErrorEventHandler(ViewModel_OnError);
            ViewModel.OnEnd += new EventHandler(ViewModel_OnEnd);
            ViewModel.OnTapeMovedLeft += new TapeMovedEventHandler(ViewModel_OnTapeMovedLeft);
            ViewModel.OnTapeMovedRight += new TapeMovedEventHandler(ViewModel_OnTapeMovedRight);
            ViewModel.OnSourceIndexChanged += new SourceIndexChangedEventHandler(ViewModel_OnSourceIndexChanged);
            ViewModel.OnTapeInitialized += new TapeMovedEventHandler(ViewModel_OnTapeInitialized);
            btnRun = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
            btnSave = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
            btnLoad = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //This is the main page and I want the application to exit when the user presses the Back button so remove all entries
            while (NavigationService.BackStack.Any())
            {
                NavigationService.RemoveBackEntry();
            }

            if (ViewModel.State == InterpreterState.Running || ViewModel.State == InterpreterState.Cancelling)
            {
                DisableInput();
                //I have no idea why but if I do not set the text here the SetTextBlock method fails because the source is an empty string.
                //Probably it has something to do with the visibility state or the fact that the has explicit binding trigger.
                txtCode.Text = ViewModel.Source;
                if (ViewModel.State == InterpreterState.Cancelling) //prevent pressing cancel second time
                {
                    btnRun.IsEnabled = false;
                }
            }
            ViewModel.RestartInterpretation();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            UpdateViewModelForTextBoxes();
            base.OnNavigatedFrom(e);
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SnippetsListPage.xaml", UriKind.Relative));
        }

        private void ApplicationBarHelp_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/HelpPage.xaml", UriKind.Relative));
        }
        private void ApplicationBarAbout_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            UpdateViewModelForTextBoxes();
            currentPopup = new Popup();
            currentPopup.Width = 460;
            currentPopup.VerticalOffset = 100;
            currentPopup.HorizontalOffset = 10;
            InputPrompt input = new InputPrompt();
            currentPopup.Child = input;
            currentPopup.IsOpen = true;
            input.Width = currentPopup.Width;
            input.Height = currentPopup.Height;
            input.SetTextBoxFocus();
            LayoutRoot.IsHitTestVisible = false;
            ApplicationBar.IsVisible = false;
            input.btnOK.Click += (s, args) =>
            {
                SaveResult result = ViewModel.SaveProgram(input.Text);
                switch (result)
                {
                    case SaveResult.NameExists:
                        {
                            input.ErrorMessage = "A program with this name already exists.";
                        } break;
                    case SaveResult.InvalidName:
                        {
                            input.ErrorMessage = "Invalid file name.";
                        } break;
                    case SaveResult.Success:
                        {
                            ClosePopup();
                            MessageBox.Show("Program saved successfully.");
                        } break;
                    default:
                        {
                            throw new Exception("Unexpected SaveResult type");
                        }
                }
            };

            input.btnCancel.Click += (s, args) => { ClosePopup(); };
        }

        private void UpdateViewModelForTextBoxes()
        {
            //need to manually call the databinding because application bar buttons do not trigger databinding for textboxes
            txtCode.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            txtInput.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void ClosePopup()
        {
            currentPopup.IsOpen = false;
            LayoutRoot.IsHitTestVisible = true;
            currentPopup = null;
            ApplicationBar.IsVisible = true;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (currentPopup != null)
            {
                ClosePopup();
                e.Cancel = true;
            }
        }


        private void btnRun_Click(object sender, EventArgs e)
        {
            if (ViewModel.State == InterpreterState.Running)
            {
                ViewModel.CancelInterpretation();
                btnRun.Text = "Cancelling...";
                btnRun.IsEnabled = false;
            }
            else if (ViewModel.State == InterpreterState.Cancelling)
            {
                throw new InvalidOperationException("Invalid state: \"Cancelling\"");
            }
            else
            {
                if (!String.IsNullOrEmpty(txtCode.Text))
                {
                    DisableInput();
                    UpdateViewModelForTextBoxes();
                    ViewModel.StartInterpretation();
                    SetSourceIndex(0);
                }
                else
                {
                    MessageBox.Show("There is no source code");
                }
            }
        }

        private void DisableInput()
        {
            svtxtCode.Visibility = System.Windows.Visibility.Collapsed;
            txtInput.IsEnabled = false;
            btnRun.Text = "Cancel";
            btnRun.IconUri = new Uri("/icons/appbar.cancel.rest.png", UriKind.Relative);
            svtbCode.Visibility = System.Windows.Visibility.Visible;
            lpDelay.IsEnabled = false;
            btnSave.IsEnabled = false;
            btnLoad.IsEnabled = false;
        }

        private void EnableInput()
        {
            Dispatcher.BeginInvoke(() =>
                {
                    svtxtCode.Visibility = System.Windows.Visibility.Visible;
                    txtInput.IsEnabled = true;
                    btnRun.Text = "Run";
                    btnRun.IconUri = new Uri("/icons/appbar.transport.play.rest.png", UriKind.Relative);
                    btnRun.IsEnabled = true;
                    svtbCode.Visibility = System.Windows.Visibility.Collapsed;
                    lpDelay.IsEnabled = true;
                    btnSave.IsEnabled = true;
                    btnLoad.IsEnabled = true;
                });
        }

        void ViewModel_OnSourceIndexChanged(object sender, SourceIndexChangedEventArgs e)
        {
            SetSourceIndex(e.SourceIndex);
        }

        void ViewModel_OnInputEnd(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() => MessageBox.Show("Warning: The input is shorter than what the program wants to consume. Subsequent input reads will be zeros."));
        }

        void ViewModel_OnError(object sender, ErrorEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(e.ErrorMessage);
            });
        }

        void ViewModel_OnTapeInitialized(object sender, TapeMovedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    itTape.DataContext = e.VisibleCells;
                });
        }

        void ViewModel_OnTapeMovedRight(object sender, TapeMovedEventArgs e)
        {
            PlayTapeAnimation(RightAnimationTarget);
        }

        void ViewModel_OnTapeMovedLeft(object sender, TapeMovedEventArgs e)
        {
            PlayTapeAnimation(LeftAnimationTarget);
        }

        private void PlayTapeAnimation(int targetValue)
        {
            int delay = ViewModel.Delay - AnimationDelayOffset;
            if (delay > MinAnimationDelay)
            {
                ViewModel.SignalStartAnimation();
                Dispatcher.BeginInvoke(() =>
                        {
                            tapeAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(delay));
                            tapeAnimation.From = StartAnimationPosition;
                            tapeAnimation.To = targetValue;
                            tapeStoryboard.Begin();
                        });
            }
            else
            {
                Dispatcher.BeginInvoke(() =>
                        {
                            itTape.DataContext = ViewModel.TapeCells;
                        });
            }
        }

        private void tapeAnimation_Completed(object sender, EventArgs e)
        {
            Canvas.SetLeft(itTape, StartAnimationPosition);
            itTape.DataContext = ViewModel.TapeCells;
            ViewModel.SignalEndAnimation();
        }

        void ViewModel_OnEnd(object sender, EventArgs e)
        {
            EnableInput();
        }

        private void SetSourceIndex(int index)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    SetTextBlock(tbCode, txtCode.Text, index);
                });
        }

        private void SetTextBlock(TextBlock textBlock, string text, int selectedIndex)
        {
            textBlock.Inlines.Clear();
            Run previous = new Run();
            previous.Text = text.Substring(0, selectedIndex);
            Run current = new Run();
            current.Text = text.Substring(selectedIndex, 1);
            current.Foreground = (SolidColorBrush)App.Current.Resources["PhoneAccentBrush"];
            current.FontWeight = FontWeights.ExtraBold;
            Run next = new Run();
            next.Text = text.Substring(selectedIndex + 1, text.Length - selectedIndex - 1);
            textBlock.Inlines.Add(previous);
            textBlock.Inlines.Add(current);
            textBlock.Inlines.Add(next);
        }

    }
}
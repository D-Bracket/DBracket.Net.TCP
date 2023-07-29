using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace DBracket.Net.TCP.DataSync.Example
{
    public sealed partial class MainWindow : Window
    {
        #region "----------------------------- Private Fields ------------------------------"
        internal MainViewModel ViewModel { get; set; }
        private Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public MainWindow()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();

            // simple Task to update the sync state
            Task.Run(() => UpdateSyncStates());
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"

        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private async void UpdateSyncStates()
        {
            while (true)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    TxtSourceIsConnectedToTarget.Text = ViewModel.SourceIsConnectedToTarget.ToString();
                    SourceTargetCycleTime.Text = ViewModel.SourceTargetCycleTime.ToString();
                    SourceCycleTime.Text = ViewModel.SourceCycleTime;

                    TargetCycleTime.Text = ViewModel.TargetCycleTime;
                });


                await Task.Delay(200);
            }
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion


        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"

        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion
        #endregion
    }
}
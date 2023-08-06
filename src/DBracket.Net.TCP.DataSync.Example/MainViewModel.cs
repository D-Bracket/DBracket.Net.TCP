using DBracket.Net.TCP.DataSync.Example.Models;
using DBracket.Net.TCP.DataSync.Example.Utilities;
using System;
using System.Linq;
using System.Net;
using System.Windows.Input;

namespace DBracket.Net.TCP.DataSync.Example
{
    internal sealed class MainViewModel : PropertyChangedBase
    {
        #region "----------------------------- Private Fields ------------------------------"
        private DataSyncSource? _syncSource;
        private TCP.DataSync.WinUI3.DataSyncTarget? _syncTarget;


        private Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        #endregion



        #region "------------------------------ Constructor --------------------------------"
        public MainViewModel()
        {
            //46804
            for (int i = 0; i < 100000; i++)
            {
                People.Add(new Person("James", "Nobody", i, "Somewhere I belong"));
            }

            //for (int i = 0; i < 1; i++)
            //{
            //    TargetPeople.Add(new Person("", "", 0, ""));
            //}

            var settings = new DataSyncSourceSettings(IPAddress.Parse(SourceIPAddress), SourcePort, AlwaysKeepUpdating, UpdateCycleTimeMs);
            _syncSource = new DataSyncSource(settings, People);
            _syncSource.ConncetionStateChanged += HandleConncetionStateChanged;
            _syncSource.CycleTimeChanged += HandleSourceCycleTimeChanged;

            _syncTarget = new TCP.DataSync.WinUI3.DataSyncTarget(IPAddress.Parse("127.0.0.1"), 4000, TargetPeople);
            _syncTarget.CycleTimeChanged += HandleTargetCycleTimeChanged;

            People.First().Name = "Wow";
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"

        #endregion

        #region "----------------------------- Private Methods -----------------------------"
        private void DeleteSourceTestData()
        {
            People.RemoveAt(0);
        }

        private async void StartSourceDataSync()
        {
            var settings = new DataSyncSourceSettings(IPAddress.Parse(SourceIPAddress), SourcePort, AlwaysKeepUpdating, UpdateCycleTimeMs);
            _syncSource.SetSettings(settings);
            _syncSource.StartSyncData();
            SourceTargetCycleTime = _syncSource.TargetCycleTimeMs;
        }
        #endregion

        #region "------------------------------ Event Handling -----------------------------"
        private void HandleConncetionStateChanged(bool state)
        {
            SourceIsConnectedToTarget = state;
        }

        private void HandleSourceCycleTimeChanged(TimeSpan time)
        {
            var microSeconds = (int)time.TotalMicroseconds;
            SourceCycleTime = microSeconds.ToString();
        }


        private void HandleTargetCycleTimeChanged(TimeSpan time)
        {
            var microSeconds = (int)time.TotalMicroseconds;
            TargetCycleTime = microSeconds.ToString();
        }
        #endregion

        #region "----------------------------- Command Handling ----------------------------"
        private void HandleCommands(string parameter)
        {
            switch (parameter)
            {
                case "StartSourceDataSync":
                    StartSourceDataSync();
                    break;

                case "DeleteSourceTestData":
                    DeleteSourceTestData();
                    break;

                case "StartTargetDataSync":
                    _syncSource.StartSyncData();
                    break;

                case "StopSourceDataSync":
                    _syncSource.StopSyncData();
                    break;
            }
        }

        private void HandleCreateDataCommand(double parameter)
        {
            People.Clear();
            var numberOfTestData = (int)parameter;
            for (int i = 0; i < numberOfTestData; i++)
            {
                People.Add(new Person("James", "Nobody", i, "Somewhere I belong"));
            }
        }


        private void HandleDeleteDataCommand(Person person)
        {
            People.Remove(person);
        }
        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"
        public ObservableSyncCollection<Person> People { get => _people; set { _people = value; OnMySelfChanged(string.Empty); } }
        private ObservableSyncCollection<Person> _people = new ObservableSyncCollection<Person>();

        public string SourceIPAddress { get => _sourceIPAddress; set { _sourceIPAddress = value; OnMySelfChanged(value.ToString()); } }
        private string _sourceIPAddress = "127.0.0.1";
        public int SourcePort { get => _sourcePort; set { _sourcePort = value; OnMySelfChanged(value.ToString()); } }
        private int _sourcePort = 4000;

        public int UpdateCycleTimeMs { get => _updateCycleTimeMs; set { _updateCycleTimeMs = value; OnMySelfChanged(value.ToString()); } }
        private int _updateCycleTimeMs = 130;

        public bool AlwaysKeepUpdating { get => _alwaysKeepUpdating; set { _alwaysKeepUpdating = value; OnMySelfChanged(value.ToString()); } }
        private bool _alwaysKeepUpdating = true;

        public bool SourceIsConnectedToTarget { get => _sourceIsConnectedToTarget; set { _sourceIsConnectedToTarget = value; OnMySelfChanged(value.ToString()); } }
        private bool _sourceIsConnectedToTarget = false;

        public int SourceTargetCycleTime { get => _sourceTargetCycleTime; set { _sourceTargetCycleTime = value; OnMySelfChanged(value.ToString()); } }
        private int _sourceTargetCycleTime = 0;

        public string SourceCycleTime { get => _sourceCycleTime; set { _sourceCycleTime = value; OnMySelfChanged(value.ToString()); } }
        private string _sourceCycleTime;



        public ObservableSyncCollection<Person> TargetPeople { get => _targetPeople; set { _targetPeople = value; OnMySelfChanged(string.Empty); } }
        private ObservableSyncCollection<Person> _targetPeople = new ObservableSyncCollection<Person>();

        public string TargetIPAddress { get => _targetIPAddress; set { _targetIPAddress = value; OnMySelfChanged(value.ToString()); } }
        private string _targetIPAddress = "127.0.0.1";
        public string TargetPort { get => _targetPort; set { _targetPort = value; OnMySelfChanged(value.ToString()); } }
        private string _targetPort = "4000";

        public string TargetCycleTime { get => _targetCycleTime; set { _targetCycleTime = value; OnMySelfChanged(value.ToString()); } }
        private string _targetCycleTime;
        #endregion

        #region "--------------------------------- Events ----------------------------------"

        #endregion

        #region "-------------------------------- Commands ---------------------------------"
        public ICommand CreateDataCommand => new RelayCommand<double>(HandleCreateDataCommand);
        public ICommand DeleteDataCommand => new RelayCommand<Person>(HandleDeleteDataCommand);

        public ICommand Commands => new RelayCommand<string>(HandleCommands);
        #endregion
        #endregion
    }
}
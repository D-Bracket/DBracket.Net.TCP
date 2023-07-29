using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DBracket.Net.TCP.DataSync.Example.Utilities
{
    internal abstract class PropertyChangedBase : INotifyPropertyChanged
    {
        #region "----------------------------- Private Fields ------------------------------"

        #endregion



        #region "------------------------------ Constructor --------------------------------"

        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        public void OnMySelfChanged([CallerMemberName]string cmn = "")
        {
            OnPropertyChanged(cmn);
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region "----------------------------- Private Methods -----------------------------"

        #endregion

        #region "------------------------------ Event Handling -----------------------------"

        #endregion
        #endregion



        #region "--------------------------- Public Propterties ----------------------------"
        #region "------------------------------- Properties --------------------------------"

        #endregion

        #region "--------------------------------- Events ----------------------------------"
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion
        #endregion
    }
}
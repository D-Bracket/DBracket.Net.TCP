using System;
using System.Windows.Input;

namespace DBracket.Net.TCP.DataSync.Example.Utilities
{
    internal class RelayCommand<T> : ICommand
    {
        #region "----------------------------- Private Fields ------------------------------"
        private readonly Predicate<T> _canExecute;
        private readonly Action<T> _execute;
        #endregion



        #region "------------------------------ Constructor --------------------------------"
        /// <summary>Initalisieren einer neuen Instanz eines <see cref="DelegateCommand{T}".</summary>
        /// <param name="execute">Ein Delegate, der die Adresse der Methode referenziert, die ausgeführt wird, wenn Command aufgerufen wird.</param>
        /// <remarks><seealso cref="CanExecute"/> gibt immer true zurück.</remarks>
        public RelayCommand(Action<T> execute) : this(execute, null) { }

        /// <summary>Creates a new command.</summary>
        /// <param name="execute">Ein Delegate, der die Adresse der Methode referenziert, die ausgeführt wird, wenn Command aufgerufen wird.</param>
        /// <param name="canExecute">Ein Delegate, der die Adresse der Methode referenziert, die die Logik des Ausführungsstatus beinhaltet.</param>
        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute == null) throw new ArgumentNullException("execute");
            _execute = execute; _canExecute = canExecute;
        }
        #endregion



        #region "--------------------------------- Methods ---------------------------------"
        #region "----------------------------- Public Methods ------------------------------"
        ///<summary>Methode, die aufgerufen wird, um den aktuellen Status zur Zulässigkeit der Ausführung zu ermitteln.</summary>
        ///<param name="parameter">Daten, die von command übermittelt werden. Wenn es keine Daten gibt, dann kann null übergeben werden.</param>
        ///<returns>true, wenn command ausgeführt werden kann, sonst false.</returns>
        public bool CanExecute(object parameter) => _canExecute == null ? true : _canExecute((T)parameter);

        ///<summary>Methode, die aufgerufen wird, wenn command auszuführen ist.</summary>
        ///<param name="parameter">Daten, die von command übermittelt werden. Wenn es keine Daten gibt, dann kann null übergeben werden.</param>
        public void Execute(object parameter) => _execute((T)parameter);

        public void OnCanExecuteChange()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
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
        ///<summary>Ereignis, mit welchem die Änderungen des Ausführungsstatus mitgeteilt wird.</summary>
        public event EventHandler CanExecuteChanged;
        #endregion
        #endregion
    }
}
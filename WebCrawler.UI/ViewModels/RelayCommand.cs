using System;
using System.Threading;
using System.Windows.Input;

namespace GalaSoft.MvvmLight.Command
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;
        private EventHandler _requerySuggestedLocal;

        /// <summary>
        /// Initializes a new instance of the RelayCommand class that
        /// can always execute.
        /// </summary>
        /// <param name="execute">The execution logic. IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </param>
        /// <exception cref="T:System.ArgumentNullException">If the execute argument is null.</exception>
        public RelayCommand(Action execute)
          : this(execute, (Func<bool>)null)
        {
        }

        /// <summary>Initializes a new instance of the RelayCommand class.</summary>
        /// <param name="execute">The execution logic. IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </param>
        /// <param name="canExecute">The execution status logic.</param>
        /// <exception cref="T:System.ArgumentNullException">If the execute argument is null. IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </exception>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }
            _execute = execute;
            if (canExecute == null)
            {
                return;
            }
            _canExecute = new Func<bool>(canExecute);
        }

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecute == null)
                    return;
                EventHandler eventHandler = _requerySuggestedLocal;
                EventHandler comparand;
                do
                {
                    comparand = eventHandler;
                    eventHandler = Interlocked.CompareExchange<EventHandler>(ref _requerySuggestedLocal, comparand + value, comparand);
                }
                while (eventHandler != comparand);
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                if (_canExecute == null)
                    return;
                EventHandler eventHandler = _requerySuggestedLocal;
                EventHandler comparand;
                do
                {
                    comparand = eventHandler;
                    eventHandler = Interlocked.CompareExchange<EventHandler>(ref _requerySuggestedLocal, comparand - value, comparand);
                }
                while (eventHandler != comparand);
                CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:GalaSoft.MvvmLight.CommandWpf.RelayCommand.CanExecuteChanged" /> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">This parameter will always be ignored.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute.Invoke();
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">This parameter will always be ignored.</param>
        public virtual void Execute(object parameter)
        {
            if (!CanExecute(parameter) || _execute == null)
            {
                return;
            }

            _execute.Invoke();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the RelayCommand class that
        /// can always execute.
        /// </summary>
        /// <param name="execute">The execution logic. IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </param>
        /// <exception cref="T:System.ArgumentNullException">If the execute argument is null.</exception>
        public RelayCommand(Action<T> execute)
          : this(execute, (Func<T, bool>)null)
        {
        }

        /// <summary>Initializes a new instance of the RelayCommand class.</summary>
        /// <param name="execute">The execution logic. IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </param>
        /// <param name="canExecute">The execution status logic. IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </param>
        /// <exception cref="T:System.ArgumentNullException">If the execute argument is null.</exception>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }
            _execute = new Action<T>(execute);
            if (canExecute == null)
            {
                return;
            }
            _canExecute = new Func<T, bool>(canExecute);
        }

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecute == null)
                {
                    return;
                }
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                if (_canExecute == null)
                {
                    return;
                }
                CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:GalaSoft.MvvmLight.CommandWpf.RelayCommand`1.CanExecuteChanged" /> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data
        /// to be passed, this object can be set to a null reference</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }
            if (parameter == null && typeof(T).IsValueType)
            {
                return _canExecute.Invoke(default(T));
            }
            if (parameter == null || parameter is T)
            {
                return _canExecute.Invoke((T)parameter);
            }
            return false;
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data
        /// to be passed, this object can be set to a null reference</param>
        public virtual void Execute(object parameter)
        {
            object parameter1 = parameter;
            if (parameter != null && parameter.GetType() != typeof(T) && parameter is IConvertible)
            {
                parameter1 = Convert.ChangeType(parameter, typeof(T), (IFormatProvider)null);
            }
            if (!CanExecute(parameter1) || _execute == null)
            {
                return;
            }
            if (parameter1 == null)
            {
                if (typeof(T).IsValueType)
                {
                    _execute.Invoke(default(T));
                }
                else
                {
                    _execute.Invoke((T)parameter1);
                }
            }
            else
            {
                _execute.Invoke((T)parameter1);
            }
        }
    }
}

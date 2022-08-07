using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WebCrawler.Common
{
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> _propertyBackingDictionary = new Dictionary<string, object>();
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //protected void SetValue<T>(T newValue, ref T oldValue, [CallerMemberName] string propertyName = "")
        //{
        //    if (Equals(newValue, oldValue))
        //    {
        //        return;
        //    }

        //    oldValue = newValue;

        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        protected T GetPropertyValue<T>([CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException("propertyName");
            }

            if (_propertyBackingDictionary.TryGetValue(propertyName, out object value))
            {
                return (T)value;
            }

            return default(T);
        }

        protected bool SetPropertyValue<T>(T newValue, [CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException("propertyName");
            }

            if (EqualityComparer<T>.Default.Equals(newValue, GetPropertyValue<T>(propertyName)))
            {
                return false;
            }

            _propertyBackingDictionary[propertyName] = newValue;
            OnPropertyChanged(propertyName);
         
            return true;
        }
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tekook.Helpers.WPF
{
    /// <summary>
    /// A Model which implements <see cref="INotifyPropertyChanged"/> and provides a simple method to fire the event (<see cref="OnPropertyChanged(string)"/>).
    /// </summary>
    public class PropertyChangedModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invoker for <see cref="INotifyPropertyChanged"/>.
        /// Allows the omit the propertyname by using <see cref="CallerMemberNameAttribute"/>.
        /// </summary>
        /// <param name="propertyName">Name of the Property which changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
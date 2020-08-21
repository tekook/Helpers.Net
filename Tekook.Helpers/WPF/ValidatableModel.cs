using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Tekook.Helpers.WPF
{
    /// <summary>
    /// A validatable Model which can be used by WPF.
    /// </summary>
    public abstract class ValidatableModel : PropertyChangedModel, INotifyDataErrorInfo, INotifyPropertyChanged
    {
        #region INotifyDataErrorInfo

        /// <summary>
        /// Determinates if there are any errors.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return this.PropErrors.Values.Any(x => x != null && x.Count > 0);
            }
        }

        /// <summary>
        /// Contains the errors for each property.
        /// </summary>
        protected ConcurrentDictionary<string, List<string>> PropErrors { get; set; } = new ConcurrentDictionary<string, List<string>>();

        /// <inheritdoc/>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <inheritdoc/>
        public IEnumerable GetErrors(string propertyName)
        {
            if (propertyName == null)
            {
                return null;
            }
            lock (_lock)
            {
                return this.PropErrors.ContainsKey(propertyName) ? this.PropErrors[propertyName] : null;
            }
        }

        /// <summary>
        /// Invoke for <see cref="INotifyDataErrorInfo"/>
        /// </summary>
        /// <param name="propertyName">Name of the Property which changed</param>
        protected virtual void OnPropertyErrorsChanged([CallerMemberName] string propertyName = null)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }

        #endregion INotifyDataErrorInfo

        #region INotifyPropertyChanged

        /// <summary>
        /// Invoker for <see cref="INotifyPropertyChanged"/>.
        /// Fires <see cref="ValidateAsync(string)"/> for any property but <see cref="HasErrors"/>.
        /// </summary>
        /// <param name="propertyName">Name of the Property which changed</param>
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (propertyName != nameof(HasErrors))
            {
                this.ValidateAsync(this.RaiseEventsForAllPropertiesAtOnce ? null : propertyName);
            }
        }

        #endregion INotifyPropertyChanged

        #region Validation

        /// <summary>
        /// Lock object for <see cref="Validate(string)"/>.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// If true validation will always be raised for all properties in <see cref="OnPropertyChanged(string)"/>, not just the changed one.
        /// </summary>
        protected bool RaiseEventsForAllPropertiesAtOnce { get; set; } = true;

        /// <summary>
        /// Validate the object and raise Errors via <see cref="OnPropertyErrorsChanged(string)"/>.
        /// </summary>
        public void Validate(string propertyName = null)
        {
            lock (_lock)
            {
                ValidationContext validationContext = new ValidationContext(this, null, null);
                List<ValidationResult> validationResults = new List<ValidationResult>();
                Validator.TryValidateObject(this, validationContext, validationResults, true);

                foreach (var kv in this.PropErrors.ToList())
                {
                    if (propertyName != null && kv.Key != propertyName)
                    { // Temp for Raising only Events for propertyName if set
                        continue;
                    }
                    if (validationResults.All(r => r.MemberNames.All(m => m != kv.Key)))
                    {
                        this.PropErrors.TryRemove(kv.Key, out List<string> outLi);
                        OnPropertyErrorsChanged(kv.Key);
                    }
                }

                var q = from r in validationResults
                        from m in r.MemberNames
                        group r by m into g
                        select g;

                foreach (var prop in q)
                {
                    if (propertyName != null && prop.Key != propertyName)
                    { // Temp for Raising only Events for propertyName if set
                        continue;
                    }
                    var messages = prop.Select(r => r.ErrorMessage).ToList();

                    if (this.PropErrors.ContainsKey(prop.Key))
                    {
                        this.PropErrors.TryRemove(prop.Key, out List<string> outLi);
                    }
                    this.PropErrors.TryAdd(prop.Key, messages);
                    OnPropertyErrorsChanged(prop.Key);
                }
            }
        }

        /// <summary>
        /// <see cref="Validate(string)">Validates</see> the object in an <see cref="Task"/>.
        /// </summary>
        /// <returns>Task of the validation process.</returns>
        public Task ValidateAsync(string propertyName = null)
        {
            return Task.Run(() => Validate(propertyName));
        }

        #endregion Validation
    }
}
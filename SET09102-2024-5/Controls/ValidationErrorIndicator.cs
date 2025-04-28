using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Windows.Input;

namespace SET09102_2024_5.Controls
{
    public class ValidationErrorIndicator : ContentView
    {
        public static readonly BindableProperty FieldNameProperty =
            BindableProperty.Create(nameof(FieldName), typeof(string), typeof(ValidationErrorIndicator), null);

        public static readonly BindableProperty ValidationErrorsProperty =
            BindableProperty.Create(nameof(ValidationErrors), typeof(Dictionary<string, string>), typeof(ValidationErrorIndicator),
                null, propertyChanged: OnValidationErrorsChanged);

        public static readonly BindableProperty ValidateCommandProperty =
            BindableProperty.Create(nameof(ValidateCommand), typeof(ICommand), typeof(ValidationErrorIndicator), null);

        public string FieldName
        {
            get => (string)GetValue(FieldNameProperty);
            set => SetValue(FieldNameProperty, value);
        }

        public Dictionary<string, string> ValidationErrors
        {
            get => (Dictionary<string, string>)GetValue(ValidationErrorsProperty);
            set => SetValue(ValidationErrorsProperty, value);
        }

        public ICommand ValidateCommand
        {
            get => (ICommand)GetValue(ValidateCommandProperty);
            set => SetValue(ValidateCommandProperty, value);
        }

        private static void OnValidationErrorsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((ValidationErrorIndicator)bindable).UpdateErrorDisplay();
        }

        public ValidationErrorIndicator()
        {
            var errorLabel = new Label
            {
                TextColor = Colors.Red,
                FontSize = 12,
                IsVisible = false
            };

            errorLabel.SetBinding(Label.TextProperty, new Binding("ErrorMessage"));
            errorLabel.SetBinding(Label.IsVisibleProperty, new Binding("HasError"));

            Content = errorLabel;

            BindingContext = this;
        }

        public string ErrorMessage { get; private set; }
        public bool HasError { get; private set; }

        private void UpdateErrorDisplay()
        {
            if (ValidationErrors != null && !string.IsNullOrEmpty(FieldName) && ValidationErrors.ContainsKey(FieldName))
            {
                ErrorMessage = ValidationErrors[FieldName];
                HasError = true;
            }
            else
            {
                ErrorMessage = string.Empty;
                HasError = false;
            }

            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(HasError));
        }

        public void Validate()
        {
            if (ValidateCommand != null && !string.IsNullOrEmpty(FieldName))
            {
                ValidateCommand.Execute(FieldName);
            }
        }
    }
}

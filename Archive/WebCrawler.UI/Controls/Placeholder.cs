using System.Windows;
using System.Windows.Controls;

namespace WebCrawler.UI.Controls
{
    public class Placeholder : DependencyObject
    {
        #region Dependency Properties

        public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.RegisterAttached(
            "PlaceholderText",
            typeof(string),
            typeof(Placeholder),
            new FrameworkPropertyMetadata(string.Empty, PlaceholderTextChanged));

        public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.RegisterAttached(
            "IsEmpty",
            typeof(bool),
            typeof(Placeholder),
            new FrameworkPropertyMetadata(false));

        public static string GetPlaceholderText(DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceholderTextProperty);
        }

        public static void SetPlaceholderText(DependencyObject obj, string value)
        {
            obj.SetValue(PlaceholderTextProperty, value);
        }

        public static bool GetIsEmpty(DependencyObject obj)
        {
            return (bool)obj.GetValue(PlaceholderTextProperty);
        }

        public static void SetIsEmpty(DependencyObject obj, bool value)
        {
            obj.SetValue(PlaceholderTextProperty, value);
        }

        #endregion

        private static void PlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.SetValue(IsEmptyProperty, textBox.Text.Length == 0);

                textBox.TextChanged += (sender, args) => textBox.SetValue(IsEmptyProperty, textBox.Text.Length == 0);
            }
            else if (d is PasswordBox passwordBox)
            {
                passwordBox.SetValue(IsEmptyProperty, passwordBox.Password.Length == 0);

                passwordBox.PasswordChanged += (sender, args) => passwordBox.SetValue(IsEmptyProperty, passwordBox.Password.Length == 0);
            }
        }
    }
}

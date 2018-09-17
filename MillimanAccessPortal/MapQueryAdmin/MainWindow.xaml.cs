using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapQueryAdmin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            environmentName.Content = Utilities.getEnvironmentName();       
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bool fieldsFilled = false;

            // Validate that required fields are filled in
            object[] requiredFields = { approverName, githubUrl, pgsqlUserName, pgsqlPassword, queryText };

            foreach (object field in requiredFields)
            {
                if (ValidateFieldNotEmpty(field))
                {
                    fieldsFilled = true;
                }
            }

            if (!fieldsFilled)
            {
                // Don't do anything else
            }

            // Retrieve a connection string for MAP

            // Execute the query against the target database 
            // Do this in a transaction so that it doesn't get committed unless the audit log record gets written

            // Retrieve a connection string for audit log db

            // Log the query result

            // If the audit log record was inserted, commit the database change
        }

        private bool ValidateFieldNotEmpty(object input)
        {
            bool containsText = false;
            string inputName = "";

            if (input.GetType().Name == nameof(PasswordBox))
            {
                PasswordBox inputAsPasswordbox = input as PasswordBox;
                containsText = !string.IsNullOrWhiteSpace(inputAsPasswordbox.Password);
                inputName = inputAsPasswordbox.Name;
            }
            else if (input.GetType().Name == nameof(TextBox))
            {
                TextBox inputAsTextBox = input as TextBox;
                containsText = !string.IsNullOrWhiteSpace(inputAsTextBox.Text);
                inputName = inputAsTextBox.Name;
            }

            // Display an error if the field is empty
            if (containsText == false)
            {
                MessageBox.Show($"The {inputName} field is required", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return containsText;
        }
    }
}

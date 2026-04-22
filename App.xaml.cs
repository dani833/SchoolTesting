using System;
using System.IO;
using System.Windows;

namespace SchoolTesting
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                File.WriteAllText("error.log", args.ExceptionObject.ToString());
                MessageBox.Show("Произошла ошибка. Подробности в error.log", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            this.DispatcherUnhandledException += (s, args) =>
            {
                File.WriteAllText("error.log", args.Exception.ToString());
                MessageBox.Show("Произошла ошибка. Подробности в error.log", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}

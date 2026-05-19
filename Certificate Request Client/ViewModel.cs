using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertificateManager.Request
{
    public class ViewModel : INotifyPropertyChanged
    {
        public string _APIString = string.Empty;

        public string APIString
        {
            get => _APIString;
            set
            {
                if (_APIString != value)
                {
                    _APIString = value;
                    PropertyChanged?.Invoke(
                        this,
                        new PropertyChangedEventArgs(nameof(APIString)));

                }
            }
        }

        public string _ConfigPath = string.Empty;

        public string ConfigPath
        {
            get => _ConfigPath;
            set
            {
                if (_ConfigPath != value)
                {
                    _ConfigPath = value;
                    PropertyChanged?.Invoke(
                        this,
                        new PropertyChangedEventArgs(nameof(ConfigPath)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertificateManager
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

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

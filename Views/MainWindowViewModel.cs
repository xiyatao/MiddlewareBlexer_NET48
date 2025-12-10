using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kinect_Middleware {
    public class MainWindowViewModel : INotifyPropertyChanged {
        private static MainWindowViewModel instance;
        public MainWindow mainWindow;

        public bool _isAzureKinectRunning;
        public bool isAzureKinectRunning {
            get { return _isAzureKinectRunning; }
            set {
                _isAzureKinectRunning = value;
                OnPropertyChanged();
                OnPropertyChanged("isDisconnectedAvailable");
            }
        }
        public bool _isKinectOneRunning { get; set; }
        public bool isKinectOneRunning {
            get { return _isKinectOneRunning; }
            set {
                _isKinectOneRunning = value;
                OnPropertyChanged();
                OnPropertyChanged("isDisconnectedAvailable");
            }
        }

        public bool isDisconnectedAvailable {
            get {
                return _isKinectOneRunning || _isAzureKinectRunning;
            }
        }

        public string _json { get; set; }
        public string json {
            get { return _json; }
            set {
                _json = value;
                OnPropertyChanged();
            }
        }

        private MainWindowViewModel() {
        }

        public static MainWindowViewModel Instance {
            get {
                if (instance == null) {
                    instance = new MainWindowViewModel();
                }

                return instance;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertName));
        }
    }
}

using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight;
using NTwain;
using NTwain.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

namespace ScanProject.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Private Fields

        private IntPtr _windowHanle;
        private DataSourceVM _selectedDataSources;
        private ICommand _captureCommand;
        private string _capturedImage;
        private TwainCore _twainCore;

        #endregion Private Fields

        #region Public Fields

        public IntPtr WindowHandle
        {
            get => _windowHanle;
            set
            {
                _windowHanle = value;
                DataSources = _twainCore.GetDataSources();
                RaisePropertyChanged(() => DataSources);
            }
        }

        public int State { get { return _twainCore.State; } }

        public ICommand CaptureCommand
        {
            get
            {
                return _captureCommand ?? (_captureCommand = new RelayCommand(async () =>
                {
                    CapturedImage = await _twainCore.ScanDocumentAsync(@"C:\\", "docTest", WindowHandle);
                    RaisePropertyChanged(() => CapturedImage);
                }));
            }
        }

        public ObservableCollection<DataSourceVM> DataSources { get; set; }

        public string CapturedImage
        {
            get => _capturedImage;
            set
            {
                _capturedImage = value;
                RaisePropertyChanged(() => CapturedImage);
                RaisePropertyChanged(() => InfoVisibility);
            }
        }

        public DataSourceVM SelectedDataSources
        {
            get => _selectedDataSources;
            set
            {
                if (_twainCore.State == 4)
                    _twainCore.CurrentSource.Close();

                _selectedDataSources = value;
                _selectedDataSources?.Open();
                _selectedDataSources?.DS.Capabilities.ICapXferMech.SetValue(XferMech.File);
                RaisePropertyChanged(() => SelectedDataSources);
                RaisePropertyChanged(() => CaptureCommand);
                MessageBox.Show("Data Source Opened !");
            }
        }

        public Visibility InfoVisibility
        {
            get => CapturedImage == null ? Visibility.Visible : Visibility.Hidden;
        }

        #endregion Public Fields

        #region Constructor

        public MainWindowViewModel()
        {
            //Open by default the second DataSource
            _twainCore = new TwainCore(1);
            _twainCore.StateChanged += (sender, e) => RaisePropertyChanged(() => State);
        }

        #endregion Constructor

        #region Helpers Methods

        /// <summary>
        /// Generate final from scanner device scan data
        /// </summary>
        /// <param name="e"><see cref="DataTransferredEventArgs"/> all informatione about scan data </param>
        /// <returns></returns>
        private ImageSource GenerateImage(DataTransferredEventArgs e)
        {
            BitmapSource img = null;

            switch (e.TransferType)
            {
                case XferMech.Native:
                    using (var stream = e.GetNativeImageStream())
                    {
                        if (stream != null)
                        {
                            img = stream.ConvertToWpfBitmap(720, 0);
                        }
                    }
                    break;

                case XferMech.File:
                    img = new BitmapImage(new Uri(e.FileDataPath));
                    if (img.CanFreeze)
                    {
                        img.Freeze();
                    }
                    break;

                case XferMech.Memory:
                    break;
            }
            return img;
        }

        /// <summary>
        /// Generate unique name for the new file
        /// </summary>
        /// <param name="dir">Directory where the new file will be saved</param>
        /// <param name="name">The name of the final file </param>
        /// <param name="ext">File extension, in this demo <see cref="FileFormat.Bmp"/> or <see cref="FileFormat.Tiff"/></param>
        /// <returns></returns>
        private string GetUniqueName(string dir, string name, string ext)
        {
            var filePath = Path.Combine(dir, name + ext);
            int next = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(dir, string.Format("{0} ({1}){2}", name, next++, ext));
            }
            return filePath;
        }

        #endregion Helpers Methods
    }
}
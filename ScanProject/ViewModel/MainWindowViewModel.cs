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

        private readonly TwainSession _session;
        private IntPtr _windowHanle;
        private DataSourceVM _selectedDataSources;
        private ICommand _captureCommand;
        private ImageSource _capturedImage;

        #endregion

        #region Public Fields
        public IntPtr WindowHandle
        {
            get => _windowHanle;
            set
            {
                _windowHanle = value;
                var res = _session.Open();
                foreach (var s in _session.Select(s => new DataSourceVM { DS = s }))
                {
                    DataSources.Add(s);
                }
            }
        }
        public int State { get { return _session.State; } }
        public ICommand CaptureCommand
        {
            get
            {
                return _captureCommand ?? (_captureCommand = new RelayCommand(() =>
                {
                    if (!_session.IsSourceOpen)
                        MessageBox.Show("Select first a DataSource");

                    if (_session.State == 4)
                    {
                        var res = _session.CurrentSource.Enable(SourceEnableMode.NoUI, false, WindowHandle);
                    }
                }));
            }
        }
        public ObservableCollection<DataSourceVM> DataSources { get; set; }
        public ImageSource CapturedImage
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
                if (_session.State == 4)
                    _session.CurrentSource.Close();

                _selectedDataSources = value;
                _selectedDataSources?.Open();
                RaisePropertyChanged(() => SelectedDataSources);
                RaisePropertyChanged(() => CaptureCommand);
                MessageBox.Show("Data Source Opened !");
            }
        }
        public Visibility InfoVisibility
        {
            get => CapturedImage == null ? Visibility.Visible : Visibility.Hidden;
        }
        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            DataSources = new ObservableCollection<DataSourceVM>();

            PlatformInfo.Current.PreferNewDSM = false;

            var appId = TWIdentity.CreateFromAssembly(DataGroups.Image | DataGroups.Audio, Assembly.GetEntryAssembly());
            _session = new TwainSession(appId);

            _session.TransferReady += _session_TransferReady;
            _session.TransferError += _session_TransferError;
            _session.DataTransferred += _session_DataTransferred;
            _session.SourceDisabled += _session_SourceDisabled;
            _session.StateChanged += _session_StateChanged;

        }
        #endregion

        #region Helpers Methods
        /// <summary>
        /// Generate final from scanner device scan data
        /// </summary>
        /// <param name="e"><see cref="DataTransferredEventArgs"/> all informatione about scan data </param>
        /// <returns></returns>
        ImageSource GenerateImage(DataTransferredEventArgs e)
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
        string GetUniqueName(string dir, string name, string ext)
        {
            var filePath = Path.Combine(dir, name + ext);
            int next = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(dir, string.Format("{0} ({1}){2}", name, next++, ext));
            }
            return filePath;
        }
        #endregion

        #region Session Event Handlers

        private void _session_StateChanged(object sender, EventArgs e)
        {
            RaisePropertyChanged(() => State);
        }

        private void _session_SourceDisabled(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _session_DataTransferred(object sender, DataTransferredEventArgs e)
        {
            ImageSource img = GenerateImage(e);
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CapturedImage = img;
            }));
        }

        private void _session_TransferError(object sender, TransferErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void _session_TransferReady(object sender, TransferReadyEventArgs e)
        {
            var mech = _session.CurrentSource.Capabilities.ICapXferMech.GetCurrent();
            if (mech == XferMech.File)
            {
                var formats = _session.CurrentSource.Capabilities.ICapImageFileFormat.GetValues();
                var wantFormat = formats.Contains(FileFormat.Tiff) ? FileFormat.Tiff : FileFormat.Bmp;

                var fileSetup = new TWSetupFileXfer
                {
                    Format = wantFormat,
                    FileName = GetUniqueName(Path.GetTempPath(), "filescan", "." + wantFormat)
                };
                var rc = _session.CurrentSource.DGControl.SetupFileXfer.Set(fileSetup);
            }
            else if (mech == XferMech.Memory)
            {
            }
        } 

        #endregion
    }
}

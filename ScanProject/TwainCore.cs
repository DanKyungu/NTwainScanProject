using NTwain;
using NTwain.Data;
using ScanProject.Twain.Core;
using ScanProject.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScanProject
{
    public class TwainCore
    {
        #region Private Property

        private int _state;
        private readonly TwainSession _twainSession;

        private ObservableCollection<DataSourceVM> _dataSources
            = new ObservableCollection<DataSourceVM>();

        private string _tempPath;

        #endregion Private Property

        #region Public Property

        public event EventHandler<StateChangedArgs> StateChanged;

        public int State
        {
            get => _state = _twainSession.State;
            private set => _state = value;
        }

        public ObservableCollection<DataSourceVM> GetDataSources()
        {
            //Clean DataSources
            _dataSources.Clear();

            foreach (var s in _twainSession.Select(s => new DataSourceVM { DS = s }))
            {
                _dataSources.Add(s);
            }

            return _dataSources;
        }

        public DataSource CurrentSource { get => _twainSession.CurrentSource; }

        #endregion Public Property

        #region Constructor

        public TwainCore()
        {
            //Allow old Device DSM drives
            PlatformInfo.Current.PreferNewDSM = false;

            var appId = TWIdentity.CreateFromAssembly(DataGroups.Image | DataGroups.Audio, Assembly.GetExecutingAssembly());
            _twainSession = new TwainSession(appId);

            PlatformInfo.Current.PreferNewDSM = false;

            _twainSession.TransferReady += _twainSession_TransferReady;
            _twainSession.StateChanged += _twainSession_StateChanged;

            if (_twainSession.Open() != ReturnCode.Success)
                throw new InvalidProgramException("Erreur de l'ouverture de la session");
        }

        private void _twainSession_DataTransferred(object sender, DataTransferredEventArgs e)
        {
            MessageBox.Show(e.FileDataPath);
        }

        public TwainCore(int sourceIndex) : this()
        {
            try
            {
                if (!GetDataSources()[sourceIndex].IsOpen)
                {
                    _dataSources[sourceIndex].Open();
                    _dataSources[sourceIndex].DS.Capabilities.ICapXferMech.SetValue(XferMech.File);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Problem occured,cannot communicate with scan device.");
            }
        }

        #endregion Constructor

        #region Event Handlers

        private void _twainSession_TransferReady(object sender, TransferReadyEventArgs e)
        {
            var mech = _twainSession.CurrentSource.Capabilities.ICapXferMech.GetCurrent();
            if (mech == XferMech.File)
            {
                var formats = _twainSession.CurrentSource.Capabilities.ICapImageFileFormat.GetValues();
                var wantFormat = formats.Contains(FileFormat.Tiff) ? FileFormat.Tiff : FileFormat.Bmp;

                var fileSetup = new TWSetupFileXfer
                {
                    Format = wantFormat,
                    FileName = Path.Combine(Path.GetTempPath(), $"tempDoc.{wantFormat}")
                };

                _tempPath = fileSetup.FileName;
                var rc = _twainSession.CurrentSource.DGControl.SetupFileXfer.Set(fileSetup);
            }
        }

        private void _twainSession_StateChanged(object sender, EventArgs e)
        {
            State = _twainSession.State;
            StateChanged?.Invoke(this, new StateChangedArgs() { NewState = State });
        }

        #endregion Event Handlers

        #region Public Methods

        public Task<string> ScanDocumentAsync(string directory, string fileName, IntPtr Handle)
        {
            var tcs = new TaskCompletionSource<string>();

            var fileResult = string.Empty;

            EventHandler<DataTransferredEventArgs> eventHandler = null;

            eventHandler = (sender, e) =>
            {
                //Avoid memory leaks
                _twainSession.DataTransferred -= eventHandler;

                fileResult = TwainHelpers.ConvertImageFromBmpToJpg(_tempPath, Path.GetTempPath(), fileName);
                tcs.TrySetResult(fileResult);
            };

            _twainSession.DataTransferred += eventHandler;

            _twainSession.TransferError += (sender, e) =>
            {
                tcs.TrySetException(new Exception("Error occured during scan"));
            };

            if (_twainSession.State == 4)
                _twainSession.CurrentSource.Enable(SourceEnableMode.NoUI, false, Handle);

            return tcs.Task;
        }

        #endregion Public Methods
    }

    public class StateChangedArgs : EventArgs
    {
        public int NewState { get; set; }
    }
}
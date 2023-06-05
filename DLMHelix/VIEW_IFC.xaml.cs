using Conexoes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Presentation.FederatedModel;
using Xbim.Presentation.LayerStyling;
using Xbim.Presentation.ModelGeomInfo;
using Xbim.Presentation.XplorerPluginSystem;

using Xbim.IO;
using Xbim.Geometry.Engine.Interop;

namespace DLM.helix
{
    /// <summary>
    /// Interaction logic for VIEW_IFC.xaml
    /// </summary>
    public partial class VIEW_IFC : UserControl
    {
        public VIEW_IFC()
        {
            InitializeComponent();
        }
        internal readonly bool PreventPluginLoad = false;



        private void DrawingControl_MeasureChangedEvent(DrawingControl3D m, PolylineGeomInfo e)
        {
            if (e == null)
                return;
            var text = e.ToString();
            if (e.Points.Count() == 1)
            {
                var vspace = e.Points.FirstOrDefault();
                var modelSpace = DrawingControl.ModelPositions.GetPointInverse(new Xbim.Common.Geometry.XbimPoint3D
                    (
                    vspace.Point.X,
                    vspace.Point.Y,
                    vspace.Point.Z
                    ));
                text += $" Model space in meters: X:{modelSpace.X:0.##}, Y:{modelSpace.Y:0.##}, Z:{modelSpace.Z:0.##}";

            }

        }
        private string _temporaryXbimFileName;
        public static ILoggerFactory LoggerFactory { get; private set; }

        private ObservableMruList<string> _mruFiles = new ObservableMruList<string>();
        private string _openedModelFileName;
        protected Microsoft.Extensions.Logging.ILogger Logger { get; private set; }
        private double _deflectionOverride = double.NaN;
        private double _angularDeflectionOverride = double.NaN;
        /// <summary>
        /// determines if the geometry engine will run on parallel threads.
        /// </summary>
        private bool _multiThreading = true;
        public XbimDBAccess FileAccessMode { get; set; } = XbimDBAccess.Read;
        private BackgroundWorker _loadFileBackgroundWorker;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="args"></param>
        public delegate void LoadingCompleteEventHandler(object s, RunWorkerCompletedEventArgs args);
        public event LoadingCompleteEventHandler LoadingComplete;
        /// <summary>
        /// determines if models need to be meshed on opening
        /// </summary>
        private bool _meshModel = true;

        public IfcStore Model
        {
            get
            {
                var op = MainFrame.DataContext as ObjectDataProvider;
                return op == null ? null : op.ObjectInstance as IfcStore;
            }
        }
        private ObjectDataProvider ModelProvider
        {
            get
            {
                return MainFrame.DataContext as ObjectDataProvider;
            }
        }

        public string GetOpenedModelFileName()
        {
            return _openedModelFileName;
        }
        private void OpenAcceptableExtension(object s, DoWorkEventArgs args)
        {
            var worker = s as BackgroundWorker;
            var selectedFilename = args.Argument as string;
            try
            {
                if (worker == null)
                    throw new Exception("Background thread could not be accessed");
                _temporaryXbimFileName = System.IO.Path.GetTempFileName();
                SetOpenedModelFileName(selectedFilename);
                var model = IfcStore.Open(selectedFilename, null, null, worker.ReportProgress, FileAccessMode);
                if (_meshModel)
                {
                    ApplyWorkarounds(model);
                    // mesh direct model
                    if (model.GeometryStore.IsEmpty)
                    {
                        var DoMesh = false;
                        //if (model.ReferencingModel is Xbim.IO.Esent.EsentModel em)
                        //{
                        //    if (!em.CanEdit)
                        //    {
                        //        Logger.LogErrorLogError(0, null, "Xbim models need to be opened in write mode, if they don't have geometry. Use the View/Settings/General tab to configure.");
                        //        DoMesh = false;
                        //    }
                        //}
                        if (DoMesh)
                        {
                            try
                            {
                                var context = new Xbim3DModelContext(model);

                                if (!_multiThreading)
                                    context.MaxThreads = 1;
#if FastExtrusion
                            context.UseSimplifiedFastExtruder = _simpleFastExtrusion;
#endif
                                SetDeflection(model);
                                //upgrade to new geometry representation, uses the default 3D model
                                context.CreateContext(worker.ReportProgress, true);
                            }
                            catch (Exception geomEx)
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine($"Error creating geometry context of '{selectedFilename}' {geomEx.StackTrace}.");
                                var newexception = new Exception(sb.ToString(), geomEx);
                                Conexoes.Utilz.Alerta(newexception, "Error creating geometry context of {filename}", selectedFilename);
                            }
                        }
                    }

                    // mesh references
                    foreach (var modelReference in model.ReferencedModels)
                    {
                        // creates federation geometry contexts if needed
                        Debug.WriteLine(modelReference.Name);
                        if (modelReference.Model == null)
                            continue;
                        if (!modelReference.Model.GeometryStore.IsEmpty)
                            continue;
                        var context = new Xbim3DModelContext(modelReference.Model);
                        if (!_multiThreading)
                            context.MaxThreads = 1;
#if FastExtrusion
                        context.UseSimplifiedFastExtruder = _simpleFastExtrusion;
#endif
                        SetDeflection(modelReference.Model);
                        //upgrade to new geometry representation, uses the default 3D model
                        context.CreateContext(worker.ReportProgress, true);
                    }
                    if (worker.CancellationPending)
                    //if a cancellation has been requested then don't open the resulting file
                    {
                        try
                        {
                            model.Close();
                            if (File.Exists(_temporaryXbimFileName))
                                File.Delete(_temporaryXbimFileName); //tidy up;
                            _temporaryXbimFileName = null;
                            SetOpenedModelFileName(null);
                        }
                        catch (Exception ex)
                        {
                            Conexoes.Utilz.Alerta(ex, "Failed to cancel open of model {filename}", selectedFilename);
                        }
                        return;
                    }
                }
                else
                {
                    Logger.LogWarning("Settings prevent mesh creation.");
                }
                args.Result = model;
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Error opening '{selectedFilename}' {ex.StackTrace}.");
                var newexception = new Exception(sb.ToString(), ex);
                Conexoes.Utilz.Alerta(ex, "Error opening {filename}", selectedFilename);
                args.Result = newexception;
            }
        }

        private void CloseAndDeleteTemporaryFiles()
        {
            try
            {
                if (_loadFileBackgroundWorker != null && _loadFileBackgroundWorker.IsBusy)
                    _loadFileBackgroundWorker.CancelAsync(); //tell it to stop
                SetOpenedModelFileName(null);
                if (Model != null)
                {
                    Model.Dispose();
                    ModelProvider.ObjectInstance = null;
                    ModelProvider.Refresh();
                }
                if (!(DrawingControl.DefaultLayerStyler is SurfaceLayerStyler))
                    SetDefaultModeStyler(null, null);
            }
            finally
            {
                if (!(_loadFileBackgroundWorker != null && _loadFileBackgroundWorker.IsBusy && _loadFileBackgroundWorker.CancellationPending)) //it is still busy but has been cancelled 
                {
                    if (!string.IsNullOrWhiteSpace(_temporaryXbimFileName) && File.Exists(_temporaryXbimFileName))
                        File.Delete(_temporaryXbimFileName);
                    _temporaryXbimFileName = null;
                } //else do nothing it will be cleared up in the worker thread
            }
        }
        private void SetOpenedModelFileName(string ifcFilename)
        {
            _openedModelFileName = ifcFilename;
        }
        private void SetWorkerForFileLoad()
        {
            _loadFileBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _loadFileBackgroundWorker.ProgressChanged += OnProgressChanged;
            _loadFileBackgroundWorker.DoWork += OpenAcceptableExtension;
            _loadFileBackgroundWorker.RunWorkerCompleted += FileLoadCompleted;
        }
        private void OnProgressChanged(object s, ProgressChangedEventArgs args)
        {
            if (args.ProgressPercentage < 0 || args.ProgressPercentage > 100)
                return;

            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Send,
                new Action(() =>
                {
                    ProgressBar.Value = args.ProgressPercentage;
                    StatusMsg.Text = (string)args.UserState;
                }));

        }
        private void ApplyWorkarounds(IfcStore model)
        {
            model.AddRevitWorkArounds();
            model.AddWorkAroundTrimForPolylinesIncorrectlySetToOneForEntireCurve();
        }
        private void FileLoadCompleted(object s, RunWorkerCompletedEventArgs args)
        {
            if (args.Result is IfcStore) //all ok
            {
                //this Triggers the event to load the model into the views 
                ModelProvider.ObjectInstance = args.Result;
                ModelProvider.Refresh();
                ProgressBar.Value = 0;
                StatusMsg.Text = "Ready";
                AddRecentFile();
            }
            else //we have a problem
            {
                var errMsg = args.Result as string;
                if (!string.IsNullOrEmpty(errMsg))
                    MessageBox.Show(errMsg, "Error Opening File", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.None);
                var exception = args.Result as Exception;
                if (exception != null)
                {
                    var sb = new StringBuilder();

                    var indent = "";
                    while (exception != null)
                    {
                        sb.AppendFormat("{0}{1}\n", indent, exception.Message);
                        exception = exception.InnerException;
                        indent += "\t";
                    }
                    MessageBox.Show(sb.ToString(), "Error Opening Ifc File", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.None);
                }
                ProgressBar.Value = 0;
                StatusMsg.Text = "Error/Ready";
                SetOpenedModelFileName("");
            }
            FireLoadingComplete(s, args);
        }
        private void FireLoadingComplete(object s, RunWorkerCompletedEventArgs args)
        {
            if (LoadingComplete != null)
            {
                LoadingComplete(s, args);
            }
        }
        private void SetDeflection(IModel model)
        {
            var mf = model.ModelFactors;
            if (mf == null)
                return;
            if (!double.IsNaN(_angularDeflectionOverride))
                mf.DeflectionAngle = _angularDeflectionOverride;
            if (!double.IsNaN(_deflectionOverride))
                mf.DeflectionTolerance = mf.OneMilliMetre * _deflectionOverride;
        }
        private void SetDefaultModeStyler(object sender, RoutedEventArgs e)
        {
            DrawingControl.DefaultLayerStyler = new SurfaceLayerStyler(this.Logger);
            ConnectStylerFeedBack();
            DrawingControl.ReloadModel();
        }
        private void AddRecentFile()
        {
            _mruFiles.Add(_openedModelFileName);
            //Settings.Default.MRUFiles = new StringCollection();
            //foreach (var item in _mruFiles)
            //{
            //    Settings.Default.MRUFiles.Add(item);
            //}
            //Settings.Default.Save();
        }
        private void ConnectStylerFeedBack()
        {
            if (DrawingControl.DefaultLayerStyler is IProgressiveLayerStyler)
            {
                ((IProgressiveLayerStyler)DrawingControl.DefaultLayerStyler).ProgressChanged += OnProgressChanged;
            }
        }
        public void Close()
        {
            CloseAndDeleteTemporaryFiles();
        }
        public void Abrir(string modelFileName)
        {
            try
            {
                this.Close();

                var arq = modelFileName.AsArquivo();
                if (arq.Exists() && arq.Extensao == "IFC")
                {
                    var fInfo = new FileInfo(modelFileName);
                    if (!fInfo.Exists) // file does not exist; do nothing
                        return;
                    if (fInfo.FullName.ToLower() == GetOpenedModelFileName()) //same file do nothing
                        return;

                    // there's no going back; if it fails after this point the current file should be closed anyway
                    CloseAndDeleteTemporaryFiles();
                    SetOpenedModelFileName(modelFileName.ToLower());
                    ProgressStatusBar.Visibility = Visibility.Visible;
                    SetWorkerForFileLoad();

                    var ext = fInfo.Extension.ToLower();
                    switch (ext)
                    {
                        case ".ifc": //it is an Ifc File
                        case ".ifcxml": //it is an IfcXml File
                        case ".ifczip": //it is a zip file containing xbim or ifc File
                        case ".zip": //it is a zip file containing xbim or ifc File
                        case ".xbimf":
                        case ".xbim":
                            _loadFileBackgroundWorker.RunWorkerAsync(modelFileName);
                            break;
                        default:
                            Logger.LogWarning("Extension '{extension}' has not been recognised.", ext);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                DLM.log.Log(ex);
            }
        }

        private void MainFrame_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var model = IfcStore.Create(null, XbimSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);
                ModelProvider.ObjectInstance = model;
                ModelProvider.Refresh();


                LoggerFactory = new LoggerFactory();
                XbimLogging.LoggerFactory = LoggerFactory;
                Logger = LoggerFactory.CreateLogger<VIEW_IFC>();
                Logger.LogInformation("Xplorer started...");





                if (Xbim.ModelGeometry.XbimEnvironment.RedistInstalled())
                    return;
                Conexoes.Utilz.Alerta("Requisite C++ environment missing, download and install from {VCPath}",
                    Xbim.ModelGeometry.XbimEnvironment.RedistDownloadPath());
            }
            catch (Exception ex)
            {
                DLM.log.Log(ex);
            }

        }
    }
}

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.IO;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Threading;
using AsfMojoUI.Model;
using System.Linq;
using AsfMojo.File;
using AsfMojoUI.Converter;

namespace AsfMojoUI.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public string Title { get; set; }
        public string FileName { get; set; }
        public Dispatcher Dispatcher { get; set; }
        public bool PreviewVisible { get; set; }
        public bool AboutInfoVisible { get; set; }
        public bool ErrorDetailsVisible { get; set; }
        public bool ImageViewVisible { get; set; }
        public bool IsBusy { get; set; }
        public bool IsFileLoaded { get; set; }
        public bool HasVideoStream { get; set; }
        public string CurrentOperation { get; set; }

        public BitmapImage CurrentImageSource { get; set; }
        public System.Drawing.Bitmap CurrentImage { get; set; }

        private ObservableCollection<AsfHeaderItem> _asfObjects;
        public ObservableCollection<AsfHeaderItem> AsfObjects { get { return _asfObjects; } }

        private List<PreviewImage> _loadingImages;
        private ObservableCollection<PreviewImage> _previewImages;
        public ObservableCollection<PreviewImage> PreviewImages { get { return _previewImages; } }
        public ObservableCollection<FileErrorInfo> FileErrorDetails { get; set; }
        private int _previewImagesLoadedCount = 0;
        private int _thumbCount = 20;
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            Dispatcher = Application.Current.Dispatcher;
            _asfObjects = new ObservableCollection<AsfHeaderItem>();
            Title = "ASFMojo";
            CurrentOperation = "Loading...";
            PreviewVisible = false;
            AboutInfoVisible = false;
            IsBusy = false;
            HasVideoStream = false;
            ImageViewVisible = false;

            _previewImages = new ObservableCollection<PreviewImage>();
            FileErrorDetails = new ObservableCollection<FileErrorInfo>();

            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                LoadSampleContent();
            }
            else
            {
                // Code runs "for real"
            }
        }

        private void LoadSampleContent()
        {
            FileName = @"D:\samples\testUnit.wmv";

            foreach (AsfHeaderItem asfItem in AsfInfo.GetHeaderObjects(FileName))
                _asfObjects.Add(asfItem);
        }

        RelayCommand  _updateContentPropertiesCommand;
        public RelayCommand UpdateContentPropertiesCommand
        {
            get
            {
                if (_updateContentPropertiesCommand == null)
                {
                    _updateContentPropertiesCommand = new RelayCommand(TriggerUpdateContentProperties);
                }
                return _updateContentPropertiesCommand;
            }
        }


        RelayCommand<int> _nextPayloadCommand;
        public RelayCommand<int> NextPayloadCommand
        {
            get
            {
                if (_nextPayloadCommand == null)
                {
                    _nextPayloadCommand = new RelayCommand<int>(ShowNextPayload);
                }
                return _nextPayloadCommand;
            }
        }


        RelayCommand _showErrorDetailsCommand;
        public RelayCommand ShowErrorDetailsCommand
        {
            get
            {
                if (_showErrorDetailsCommand == null)
                {
                    _showErrorDetailsCommand = new RelayCommand(ShowErrorDetails, CanShowErrorDetails);
                }
                return _showErrorDetailsCommand;
            }
        }


        RelayCommand _showImageDialogCommand;
        public RelayCommand ShowImageDialogCommand
        {
            get
            {
                if (_showImageDialogCommand == null)
                {
                    _showImageDialogCommand = new RelayCommand(ShowImageDialog);
                }
                return _showImageDialogCommand;
            }
        }


        RelayCommand _showAboutDialogCommand;
        public RelayCommand ShowAboutDialogCommand
        {
            get
            {
                if (_showAboutDialogCommand == null)
                {
                    _showAboutDialogCommand = new RelayCommand(ShowAboutInfo);
                }
                return _showAboutDialogCommand;
            }
        }

        RelayCommand _showPreviewCommand;
        public RelayCommand ShowPreviewCommand
        {
            get
            {
                if (_showPreviewCommand == null)
            {
                _showPreviewCommand = new RelayCommand(ShowPreview, CanShowPreview);
            }
                return _showPreviewCommand;

            }
        }


        RelayCommand<string[]> _dropFilesCommand;
        public RelayCommand<string[]> DropFilesCommand
        {
            get
            {
                if (_dropFilesCommand == null)
                {
                    _dropFilesCommand = new RelayCommand<string[]>(DropFiles, CanDropFiles);
                }
                return _dropFilesCommand;
            }
        }

        RelayCommand _openFileCommand;
        public RelayCommand OpenFileCommand
        {
            get
            {
                if (_openFileCommand == null)
                {
                    _openFileCommand = new RelayCommand(OpenFile, CanOpenFile);
                }
                return _openFileCommand;
            }
        }

        RelayCommand _exportImageCommand;
        public RelayCommand ExportImageCommand
        {
            get
            {
            if (_exportImageCommand == null)
            {
                _exportImageCommand = new RelayCommand(ExportImage, CanExportImage);
            }
            return _exportImageCommand;

            }
        }



        public void TriggerUpdateContentProperties()
        {
            CurrentOperation = "Updating...";
            IsBusy = true;
            RaisePropertyChanged("IsBusy");
            RaisePropertyChanged("CurrentOperation");
            Dispatcher.DelayInvoke(TimeSpan.FromMilliseconds(50),
                                    new Action(() =>
                                    {
                                        UpdateContentProperties();
                                    }));
        }

        private void UpdateContentProperties()
        {
            //update content properties and reload the file (since header size might have changed)
            var contentProperties = _asfObjects.OfType<AsfFileHeaderItem>().First().Items.OfType<AsfContentDescriptionObjectItem>().FirstOrDefault();

            try
            {
                AsfFile.From(FileName)
                       .WithAuthor(contentProperties.EditProperties.Single(x => x.Key == "Author").Value)
                       .WithDescription(contentProperties.EditProperties.Single(x => x.Key == "Description").Value)
                       .WithCopyright(contentProperties.EditProperties.Single(x => x.Key == "Copyright").Value)
                       .WithTitle(contentProperties.EditProperties.Single(x => x.Key == "Title").Value)
                       .WithRating(contentProperties.EditProperties.Single(x => x.Key == "Rating").Value)
                       .Update();
                LoadFile(FileName);
            }
            catch (IOException)
            {
                FileErrorDetails.Clear();
                FileErrorDetails.Add(new FileErrorInfo() { ErrorType = "Update Failed", ErrorDetails = "File is in use by another process" });


                Dispatcher.DelayInvoke(TimeSpan.FromMilliseconds(500),
                                       new Action(() =>
                                       {
                                           ErrorDetailsVisible = true;
                                           IsBusy = false;
                                           RaisePropertyChanged("IsBusy");
                                           RaisePropertyChanged("ErrorDetailsVisible");
                                       }));

            }
        }

        public void ShowNextPayload(int currentPayloadId)
        {

        }


        public bool CanShowErrorDetails()
        {
            return IsFileLoaded;
        }

        public void ShowErrorDetails()
        {
            ErrorDetailsVisible = !ErrorDetailsVisible;
            RaisePropertyChanged("ErrorDetailsVisible");
        }

        public void ShowAboutInfo()
        {
            AboutInfoVisible = !AboutInfoVisible;
            RaisePropertyChanged("AboutInfoVisible");
        }

        public void ShowImageDialog()
        {
            ImageViewVisible = !ImageViewVisible;

            RaisePropertyChanged("CurrentImageSource");
            RaisePropertyChanged("ImageViewVisible");
        }

        public void ShowPreview()
        {
            RaisePropertyChanged("PreviewVisible");
        }

        public void DropFiles(string[] fileNames)
        {
            OpenFile(fileNames[0]);
        }

        public bool CanDropFiles(string[] fileNames)
        {
            return true;
        }


        public bool CanShowPreview()
        {
            return IsFileLoaded && HasVideoStream;
        }


        public void OpenFile()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".wmv"; // Default file extension
            dlg.Filter = "Windows Media files (*.wmv, *.wma, *.asf)|*.wmv;*.wma;*.asf|All files (*.*)|*.*"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> fileSelected = dlg.ShowDialog();

            // Process open file dialog box results
            if (fileSelected ?? false)
                OpenFile(dlg.FileName);
        }

        public void OpenFile(string fileName)
        {
            CurrentOperation = "Loading...";
            IsBusy = true;

            FileName = fileName;
            Title = "ASFMojo - " + Path.GetFileName(fileName);
            RaisePropertyChanged("IsBusy");
            RaisePropertyChanged("CurrentOperation");

            
            Dispatcher.DelayInvoke(TimeSpan.FromMilliseconds(50),
                                   new Action(() =>
                                   {
                                       LoadFile(fileName);
                                   }));

        }

        private void LoadFile(string fileName)
        {
            FileErrorDetails.Clear();
            _asfObjects.Clear();
            TimeToImageConverter.EmptyCache();

            var asfItems = AsfInfo.GetHeaderObjects(fileName);
            if (asfItems == null) //invalid file
            {
                FileErrorDetails.Add(new FileErrorInfo() { ErrorType = "Invalid ASF file", ErrorDetails = "No Header Object found" });
                Dispatcher.DelayInvoke(TimeSpan.FromMilliseconds(500),
                                       new Action(() =>
                                       {
                                           ErrorDetailsVisible = true;
                                           IsBusy = false;
                                           RaisePropertyChanged("IsBusy");
                                           RaisePropertyChanged("ErrorDetailsVisible");
                                       }));
            }
            else
            {

                foreach (AsfHeaderItem asfItem in asfItems)
                    _asfObjects.Add(asfItem);

                double fileLength = AsfHeaderItem.Configuration.Duration;
                HasVideoStream = AsfHeaderItem.Configuration.ImageWidth > 0;

                _previewImagesLoadedCount = 0;
                _previewImages.Clear();
                _loadingImages = new List<PreviewImage>();

                if (HasVideoStream)
                {
                    for (int i = 0; i < _thumbCount; i++)
                    {
                        double startOffset = ((double)i / _thumbCount) * fileLength;
                        PreviewImage pi = new PreviewImage()
                                         {
                                             FileName = fileName,
                                             TimeOffset = startOffset,
                                             DisplayTime = (uint)(startOffset * 1000),
                                             PresentationTime = (uint)(startOffset * 1000) + AsfHeaderItem.Configuration.AsfPreroll
                                         };

                        Action a = new Action(pi.GenerateSource);
                        _loadingImages.Add(pi);
                        a.BeginInvoke(new AsyncCallback(PreviewImageLoaded), a);
                    }
                }

                RaisePropertyChanged("Title");
                Dispatcher.DelayInvoke(TimeSpan.FromMilliseconds(500),
                                       new Action(() =>
                                       {
                                           IsBusy = false;
                                           IsFileLoaded = true;
                                           PreviewVisible = HasVideoStream;
                                           RaisePropertyChanged("IsBusy");
                                           RaisePropertyChanged("PreviewVisible");
                                           RaisePropertyChanged("HasVideoStream");
                                       }));
            }
        }

        private void PreviewImageLoaded(IAsyncResult ar)
        {
            Action a = ar.AsyncState as Action;
            try
            {
                a.EndInvoke(ar);
            }
            catch (Exception)
            {

            }

            Interlocked.Increment(ref _previewImagesLoadedCount);

            if (_previewImagesLoadedCount == _thumbCount) //all images loaded
            {
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                                  new Action(() =>
                                  {
                                    foreach (PreviewImage pi in _loadingImages)
                                        if (pi.ImageLoaded)
                                        {
                                            _previewImages.Add(pi);
                                        }
                                  }));
            }

        }

        public bool CanOpenFile()
        {
            return true;
        }

        public bool CanExportImage()
        {
            return CurrentImage != null;
        }

        public void ExportImage()
        {
            Clipboard.SetImage(CurrentImageSource);
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}
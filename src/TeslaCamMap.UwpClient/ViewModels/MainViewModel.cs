﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeslaCamMap.Lib.Model;
using TeslaCamMap.UwpClient.Commands;
using TeslaCamMap.UwpClient.Model;
using TeslaCamMap.UwpClient.Services;
using Windows.Devices.Geolocation;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace TeslaCamMap.UwpClient.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private FileSystemService _fileSystemService;

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set {
                _isBusy = value;
                OnPropertyChanged();
                PickFolderCommand.RaiseCanExecuteChanged();
            }
        }

        private UwpTeslaEvent _selectedTeslaEvent;
        public UwpTeslaEvent SelectedTeslaEvent
        {
            get { return _selectedTeslaEvent; }
            set
            {
                _selectedTeslaEvent = value;
                OnPropertyChanged();
            }
        }

        private BitmapImage _selectedTeslaEventThumb;
        public BitmapImage SelectedTeslaEventThumb
        {
            get { return _selectedTeslaEventThumb; }
            set
            {
                _selectedTeslaEventThumb = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<MapLayer> _teslaEventMapLayer;
        public ObservableCollection<MapLayer> TeslaEventMapLayer
        {
            get { return _teslaEventMapLayer; }
            set
            {
                _teslaEventMapLayer = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<UwpTeslaEvent> _teslaEvents;
        public ObservableCollection<UwpTeslaEvent> TeslaEvents
        {
            get { return _teslaEvents; }
            set
            {
                _teslaEvents = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand PickFolderCommand { get; set; }
        public RelayCommand ViewVideoCommand { get; set; }

        public MainViewModel()
        {
            _fileSystemService = new FileSystemService();
            PickFolderCommand = new RelayCommand(PickFolderCommandExecute, CanPickFolderCommandExecute);
            ViewVideoCommand = new RelayCommand(ViewVideoCommandExecute, CanViewVideoCommandExecute);


            this.PropertyChanged += MainViewModel_PropertyChanged;
        }

        private bool CanViewVideoCommandExecute(object arg)
        {
            return SelectedTeslaEvent != null;
        }

        private void ViewVideoCommandExecute(object obj)
        {
            ViewFrame.Navigate(typeof(EventDetailsPage), obj);
        }

        private bool CanPickFolderCommandExecute(object arg)
        {
            return !IsBusy;
        }

        // todo: this is a hack
        private async void MainViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedTeslaEvent))
                SelectedTeslaEventThumb = await _fileSystemService.LoadImageFromStorageFile(SelectedTeslaEvent.ThumbnailFile);
        }

        public void OnMapElementClicked(MapElement clickedItem)
        {
            var teslaEvent = (UwpTeslaEvent)clickedItem.Tag;
            SelectedTeslaEvent = teslaEvent;
        }

        private async void PickFolderCommandExecute(object obj)
        {
            FolderPicker picker = new FolderPicker();
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".json");
            picker.FileTypeFilter.Add(".png");
            
            var result = await picker.PickSingleFolderAsync();

            if (result != null)
            {
                IsBusy = true;
                var files = await result.GetFilesAsync(CommonFileQuery.OrderByName);
                var folders = await result.GetFoldersAsync();

                var events = await _fileSystemService.ParseFiles(folders);
                TeslaEvents = new ObservableCollection<UwpTeslaEvent>(events);

                //todo: do this stuff in a IValueConverter instead?
                TeslaEventMapLayer = new ObservableCollection<MapLayer>();
                var layer = new MapElementsLayer();
                foreach (var teslaEvent in events)
                {
                    MapIcon eventMapIcon = new MapIcon();
                    eventMapIcon.Location = new Geopoint(new BasicGeoposition() { Latitude = teslaEvent.EstimatedLatitude, Longitude = teslaEvent.EstimatedLongitude });
                    eventMapIcon.Tag = teslaEvent;
                    layer.MapElements.Add(eventMapIcon);
                }
                TeslaEventMapLayer.Add(layer);
            }

            IsBusy = false;
        }
    }
}

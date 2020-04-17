using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace PikaCore.Areas.Core.Services
{
    public class ArchiveService : IArchiveService, INotifyPropertyChanged
    {
        private Task _task;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private bool CanBeCancelled { get; set; } = true;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Cancel()
        {
            if (!CanBeCancelled) return;
            _tokenSource.Cancel();
            try
            {
                _tokenSource.Token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException e)
            {
                CanBeCancelled = true;
                Debug.WriteLine(e.Message + " Zipping cancelled by user.");
            }
        }

        public async Task<Task> ZipDirectoryAsync(string absolutePath, string output)
        {
            if (File.Exists(output))
            {
                File.Delete(output);
            }
            if (_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Dispose();
                _tokenSource = new CancellationTokenSource();
            }

            _task = Task.Factory.StartNew(() =>
            {
                if (_tokenSource.IsCancellationRequested) return;
                CanBeCancelled = false;
                OnPropertyChanged("CanBeCancelled");
                ZipFile.CreateFromDirectory(absolutePath, output, CompressionLevel.Fastest, false);
                CanBeCancelled = true;
            }, _tokenSource.Token);
            return _task;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public bool WasStartedAlready()
        {
            return !CanBeCancelled;
        }
    }
}

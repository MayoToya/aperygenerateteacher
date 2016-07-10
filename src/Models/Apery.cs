using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Model;
using Prism.Mvvm;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace AperyGenerateTeacherGUI.Models
{
    public sealed class Apery : BindableBase, IDisposable
    {
        private Tuple<bool, bool> _isAperyIdleAndStarted = new Tuple<bool, bool>(false,false);
        public Tuple<bool, bool> IsAperyIdleAndStarted 
        {
            get { return this._isAperyIdleAndStarted; }
            set { this.SetProperty(ref this._isAperyIdleAndStarted, value); }
        }

        private string _log;
        public string Log
        {
            get { return this._log; }
            set { this.SetProperty(ref this._log, value); }
        }

        private bool _isAperyIdle;
        public bool IsAperyIdle
        {
            get { return this._isAperyIdle; }
            set { this.SetProperty(ref this._isAperyIdle, value); }
        }

        private double _progress;
        public double Progress
        {
            get { return this._progress; }
            set { this.SetProperty(ref this._progress, value); }
        }
        private string _outFile;
        private readonly Random _random;
        private readonly Process _aperyInstance;
        private readonly IDisposable _aperyStandardOutput;
        private static readonly Lazy<Apery> _apery = new Lazy<Apery>(() => new Apery());
        private bool _isAperyStarted;
        public static Apery Instance => _apery.Value;

        private Apery()
        {
            var startInfo = new ProcessStartInfo("apery.exe")
            {
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            this._aperyInstance = new Process { StartInfo = startInfo };

            this._random = new Random();

            this._aperyStandardOutput = Observable.FromEvent<DataReceivedEventHandler, DataReceivedEventArgs>(
                    h => (sender, e) => h(e),
                    h => this._aperyInstance.OutputDataReceived += h,
                    h => this._aperyInstance.OutputDataReceived -= h)
                    .TakeUntil(Observable.FromEventPattern(
                        h => this._aperyInstance.Exited += h,
                        h => this._aperyInstance.Exited -= h))
                 .Subscribe(e =>
                 {
                     var line = e.Data;

                     if (string.IsNullOrEmpty(line)) return;

                     this.Log = line;

                     if (Regex.IsMatch(line, @"\d+\.\d*%"))
                     {
                         Progress = double.Parse(Regex.Match(line, @"\d+\.\d*%").Value.Replace("%", ""));
                     }
                     else if (Regex.IsMatch(line, "^Made"))
                     {
                         this._aperyInstance.CancelOutputRead();
                         TreatResultAsync();
                     }
                 }
                 );
        }

        public void Run(bool canRun)
        {
            if (!canRun) return;
            this._aperyInstance.Start();
            this.IsAperyIdleAndStarted = new Tuple<bool, bool>(true,true);
            this.IsAperyIdle = true;
            this._isAperyStarted = true;
        }

        private async void TreatResultAsync()
        {
            var shufOutfile = $"shuf{_outFile}";

            await Task.Run(() =>
            {
                #region 教師データシャッフル

                this.Log = "教師データシャッフル中";

                var shuffleStartInfo = new ProcessStartInfo("shuffle_hcpe.exe", $"{_outFile}  {shufOutfile}")
                {
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                using (var process = new Process { StartInfo = shuffleStartInfo })
                {
                    process.Start();
                    process.WaitForExit(); //本家v1.0.1から
                }

                File.Delete(_outFile);
                this.Log = "教師データシャッフル完了";

                #endregion
            });

            await SendResultToAwsAsync(shufOutfile);

            this.IsAperyIdle = true;
        }

        public void RunProcess(short threads, long teacherNodes)
        {
            this.IsAperyIdleAndStarted=new Tuple<bool, bool>(false,true);
            this.IsAperyIdle = false;
            this._outFile = $"out_{RandomString(20)}.hcpe";
            this._aperyInstance.StandardInput.WriteLine($"make_teacher roots.hcp {this._outFile} {threads} {teacherNodes}");
            this.Log = "Start";
            this._aperyInstance.BeginOutputReadLine();
        }

        private async Task SendResultToAwsAsync(string filePath)
        {
            this.Log = "sending...";
            try
            {
                using (var amazonS3Client = new AmazonS3Client(new AnonymousAWSCredentials(), RegionEndpoint.USWest1))
                {

                    try
                    {
                        var request = new PutObjectRequest
                        {
                            BucketName = "apery-teacher-v1.0.3",
                            FilePath = filePath,
                        };

                        var response = await amazonS3Client.PutObjectAsync(request);

                        this.Log = response.HttpStatusCode == HttpStatusCode.OK ? "サーバーに教師データを送信完了しました。ご協力ありがとうございました。" : $"サーバーに何か問題あるかも？ : {response.HttpStatusCode.ToString()}";
                    }
                    catch (AmazonS3Exception amazonS3Excetion)
                    {
                        if (amazonS3Excetion.ErrorCode != null &&
                            (amazonS3Excetion.ErrorCode.Equals("InvalidAccessKeyId") ||
                             amazonS3Excetion.ErrorCode.Equals("InvalidSeculity")))
                        {
                            this.Log = "サーバーにアクセス出来ませんでした。";
                        }
                        else
                        {
                            this.Log = "サーバーへの教師データデータ送信に失敗しました。";
                        }
                    }

                }
            }
            catch (Exception)
            {
                this.Log = "サーバー接続に失敗しました。";
            }
            finally
            {
                //ファイル送信のループ処理がないなら削除するタイミングはここでは
                File.Delete(filePath);
            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (this._isAperyStarted)
                    {
                        if (!this._aperyInstance.HasExited)
                        {
                            this._aperyInstance.Kill();
                        }
                    }

                    this._aperyInstance.Dispose();

                    this._aperyStandardOutput.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Apery() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        private string RandomString(int stringLength)
        {
            const string candidates = "0123456789abcdefghijklmnopqrstuvwxyz";
            var candidatesLength = candidates.Length;

            return new string(
                Enumerable.Range(1, stringLength)
                .AsParallel()
                .Select(_ => candidates[this._random.Next(candidatesLength)])
                .ToArray()
                );
        }

    }
}

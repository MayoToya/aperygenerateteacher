﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Model;
using Prism.Mvvm;
using System.Threading.Tasks;
using SevenZip.SDK.Compress.LZMA;
using System.Reactive.Linq;

namespace AperyGenerateTeacherGUI.Models
{
    public sealed class Apery : BindableBase, IDisposable
    {
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

            this._aperyInstance = new Process() { StartInfo = startInfo };
            this._aperyInstance.Start();
            this.IsAperyIdle = true;
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

                     if (!string.IsNullOrEmpty(line))
                     {
                         this.Log = line;
                     }

                     if (Regex.IsMatch(line, @"\d+\.\d*%"))
                     {
                         Progress = double.Parse(Regex.Match(line, @"\d+\.\d*%").Value.Replace("%", ""));
                     }
                     else if (Regex.IsMatch(line, "^Made"))
                     {
                         this._aperyInstance.CancelOutputRead();
                         TreatResultAsync();
                     }
                 });
        }

        private async void TreatResultAsync()
        {
            await Task.Run(() =>
            {
                #region 教師データシャッフル
                this.Log = "教師データシャッフル中";

                var shufOutfile = $"shuf{_outFile}";
                var shuffleStartInfo = new ProcessStartInfo("shuffle_fspe.exe", $"{_outFile}  {shufOutfile}")
                {
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                using (var process = new Process() { StartInfo = shuffleStartInfo })
                {
                    process.Start();
                    process.WaitForExit(); //本家v1.0.1から
                }

                File.Delete(_outFile);
                this.Log = "教師データシャッフル完了";
                #endregion

                #region 教師データ圧縮
                this.Log = "教師データ圧縮中";

                var outCompressedFile = $"{shufOutfile}.7z";
                CompressFile(shufOutfile, outCompressedFile);

                this.Log = "教師データ圧縮完了";
                File.Delete(shufOutfile);
                #endregion

                #region send aws s3
                SendResultToAws(outCompressedFile);
                #endregion

                this.IsAperyIdle = true;

            });

        }


        public async void RunProcessAsync(short threads, long teacherNodes)
        {
            await Task.Run(() =>
            {
                this.IsAperyIdle = false;
                this._outFile = $"out_{RandomString(20)}.fspe";
                var cmd = $"make_teacher roots.fsp {this._outFile} {threads} {teacherNodes}";
                this._aperyInstance.StandardInput.WriteLine(cmd);
                this._aperyInstance.BeginOutputReadLine();
            })
            ;
        }

        private void SendResultToAws(string filePath)
        {
            try
            {
                using (var amazonS3Client = new AmazonS3Client(new AnonymousAWSCredentials(), RegionEndpoint.USWest1))
                {

                    try
                    {
                        var request = new PutObjectRequest
                        {
                            BucketName = "apery-teacher-v1.0.1",
                            FilePath = filePath,
                        };

                        var response = amazonS3Client.PutObject(request);

                        this.Log = "サーバーに教師データを送信完了しました。ご協力ありがとうございました。";

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



        private static void CompressFile(string inFile, string outFile)
        {
            var coder = new Encoder();

            using (var input = new FileStream(inFile, FileMode.Open))
            {
                using (var output = new FileStream(outFile, FileMode.Create))
                {
                    coder.WriteCoderProperties(output);
                    output.Write(BitConverter.GetBytes(input.Length), 0, 8);

                    coder.Code(input, output, input.Length, -1, null);
                    output.Flush();
                }
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

        private string RandomString(int length)
        {
            var candidates = "0123456789abcdefghijklmnopqrstuvwxyz";
            var list = new char[length];
            for (var i = 0; i < length; ++i)
                list[i] = candidates[this._random.Next(0, candidates.Length)];
            return new string(list);
        }



    }
}

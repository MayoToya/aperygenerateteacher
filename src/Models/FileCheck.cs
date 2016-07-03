using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Prism.Mvvm;

namespace AperyGenerateTeacherGUI.Models
{
    public class FileCheck : BindableBase
    {
        private Tuple<bool, bool> _checkCompletedAndOk;

        public Tuple<bool, bool> CheckCompletedAndOk
        {
            get { return this._checkCompletedAndOk; }
            set { this.SetProperty(ref this._checkCompletedAndOk, value); }
        }

        private bool _isCheckCompleted = false;
        public bool IsCheckCompleted
        {
            get { return this._isCheckCompleted; }
            set { this.SetProperty(ref this._isCheckCompleted, value); }
        }

        private bool _areAllFilesOk = false;
        public bool AreAllFilesOk
        {
            get { return this._areAllFilesOk; }
            set { this.SetProperty(ref this._areAllFilesOk, value); }
        }

        public FileCheck()
        {
            this.CheckCompletedAndOk = new Tuple<bool, bool>(false, false);
            Do();
        }

        private async void Do()
        {
            this.CheckCompletedAndOk = new Tuple<bool, bool>(true,
                await FileIsOkAsync("20160307\\KK_synthesized.bin", 52488, "F8F441C72A6C1A800D3DE22F7C1855C7")
                && await FileIsOkAsync("shuffle_fspe.exe", 864768, "5A8C7C467E3AD9C3FF3FF8D0122C0A7B")
                && await FileIsOkAsync("apery.exe", 1359360, "C617CA7483B705F13BB0AFEF625D91A2")
                && await FileIsOkAsync("20160307\\KKP_synthesized.bin", 81251424, "EC58D04F60E8692315FB0CD9FCE0D898")
                && await FileIsOkAsync("20160307\\KPP_synthesized.bin", 776402496, "04350C44C0ACC996A4D204452C4A8E53")
                && await FileIsOkAsync("roots.fsp", 4229347640, "78E8D3FB8A6FB905C0896AABBCDB1B44")
                );
        }

        //ファイルの存在確認もここでやる
        private async Task<bool> FileIsOkAsync(string fileName, long fileLength, string fileHash)
        {
            return await System.Threading.Tasks.Task.Run(() =>
              {
                  if (!File.Exists(fileName))
                  {
                      return false;
                  }

                //ファイルを開く
                using (
                      var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096,
                          useAsync: true))
                  {
                      var fileSize = fs.Length;
                      if (fileSize != fileLength)
                      {
                          return false;
                      }

                    //MD5CryptoServiceProviderオブジェクトを作成
                    byte[] bs;
                      using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
                      {
                        //ハッシュ値を計算する
                        bs = md5.ComputeHash(fs);
                      }

                      return (fileHash == BitConverter.ToString(bs).ToUpper().Replace("-", ""));
                  }
              }
              );
        }
    }
}


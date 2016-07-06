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
                && await FileIsOkAsync("shuffle_hcpe.exe", 864768, "8992244402DDD36236B68D3BB7CD4A7E")//
                && await FileIsOkAsync("apery.exe", 1363968, "7ADD53D76A7F54BA0C8DD409310ECDFC")//
                && await FileIsOkAsync("20160307\\KKP_synthesized.bin", 81251424, "EC58D04F60E8692315FB0CD9FCE0D898")
                && await FileIsOkAsync("20160307\\KPP_synthesized.bin", 776402496, "04350C44C0ACC996A4D204452C4A8E53")
                && await FileIsOkAsync("roots.hcp", 1499386784, "8D052340BBBF518D05D27F26FD2861A1")//
                );
        }


        private static async Task<bool> FileIsOkAsyncUsingReadAsync(string fileName, long fileLength, string fileHash)
        {
            if (!File.Exists(fileName))
            {
                return false;
            }

            //ファイルを開く
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                var fileSize = fs.Length;
                if (fileSize != fileLength)
                {
                    return false;
                }

                var buffer = new byte[fileSize];

                while ((await fs.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                }

                byte[] bs;
                using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
                {
                    //ここが同期なのが辛い
                    //ハッシュ値を計算する
                    bs = md5.ComputeHash(buffer);
                }

                return (fileHash == BitConverter.ToString(bs).ToUpper().Replace("-", ""));
            }
        }

        //ファイルの存在確認もここでやる
        private static async Task<bool> FileIsOkAsync(string fileName, long fileLength, string fileHash)
        {
            return await Task.Run(() =>
              {
                  if (!File.Exists(fileName))
                  {
                      return false;
                  }

                  //ファイルを開く
                  using (
                        var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
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


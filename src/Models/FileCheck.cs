using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Prism.Mvvm;

namespace AperyGenerateTeacherGUI.Models
{
    public class FileProperties
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public string Md5Hash { get; set; }
    }

    public class FileCheck : BindableBase
    {
        private static FileProperties[] FilesOfApery { get; } = {
            new FileProperties
            {
                Name = "apery.exe",
                Size = 1363968,
                Md5Hash = "7ADD53D76A7F54BA0C8DD409310ECDFC"
            },
            new FileProperties
            {
                Name = "roots.hcp",
                Size = 1499386784,
                Md5Hash = "8D052340BBBF518D05D27F26FD2861A1"
            },
            new FileProperties
            {
                Name = "shuffle_hcpe.exe",
                Size = 864768,
                Md5Hash = "8992244402DDD36236B68D3BB7CD4A7E"
            },
            new FileProperties
            {
                Name = "20160307\\KK_synthesized.bin",
                Size = 52488,
                Md5Hash = "F8F441C72A6C1A800D3DE22F7C1855C7"
            },
            new FileProperties
            {
                Name = "20160307\\KKP_synthesized.bin",
                Size = 81251424,
                Md5Hash = "EC58D04F60E8692315FB0CD9FCE0D898"
            },
            new FileProperties
            {
                Name = "20160307\\KPP_synthesized.bin",
                Size = 776402496,
                Md5Hash = "04350C44C0ACC996A4D204452C4A8E53"
            }
        };

        private Tuple<bool, bool> _checkCompletedAndOk;
        public Tuple<bool, bool> CheckCompletedAndOk
        {
            get { return this._checkCompletedAndOk; }
            set { this.SetProperty(ref this._checkCompletedAndOk, value); }
        }

        public bool IsAperyGood
        {
            get;
        }

        public FileCheck()
        {
            var apery = FilesOfApery[0];
            this.IsAperyGood = FileIsOk(apery.Name, apery.Size, apery.Md5Hash);

            if (IsAperyGood)
            {
                this.CheckCompletedAndOk = new Tuple<bool, bool>(false,false);
                Do();
            }
            else
            {
                this.CheckCompletedAndOk = new Tuple<bool, bool>(true, false);
            }
        }

        private async void Do()
        {
                this.CheckCompletedAndOk = new Tuple<bool, bool>(true,
                    (await Task.WhenAll(Enumerable.Range(1, 5).Select(
                        async x =>
                        {
                            var file = FilesOfApery[x];
                            return file.Size < 100000000
                                ? await FileIsOkAsyncUsingReadAsync(file.Name, file.Size, file.Md5Hash)
                                : await FileIsOkAsync(file.Name, file.Size, file.Md5Hash);
                        }))).All(x => x));
        }

        private static bool FileIsOk(string fileName, long fileLength, string fileHash)
        {
            if (!File.Exists(fileName))
            {
                return false;
            }

            //ファイルを開く
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096))
            {
                var fileSize = fs.Length;
                if (fileSize != fileLength)
                {
                    return false;
                }

                byte[] bs;
                using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
                {
                    //ハッシュ値を計算する
                    bs = md5.ComputeHash(fs);
                }

                return fileHash == BitConverter.ToString(bs).ToUpper().Replace("-", "");
            }
        }


        //tough to RAM...
        private static async Task<bool> FileIsOkAsyncUsingReadAsync(string fileName, long fileLength, string fileHash)
        {
            if (!File.Exists(fileName))
            {
                return false;
            }

            byte[] buffer;

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

                buffer = new byte[fileSize];

                await fs.ReadAsync(buffer, 0, buffer.Length);
            }

            return await Task.Run(() =>
            {
                byte[] bs;
                using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
                {
                    //ハッシュ値を計算する
                    bs = md5.ComputeHash(buffer);
                }


                return fileHash == BitConverter.ToString(bs).ToUpper().Replace("-", "");
            }
            );
        }

        private static async Task<bool> FileIsOkAsync(string fileName, long fileLength, string fileHash)
        {
            return await Task.Run(() =>
              {
                  //ファイルの存在確認もここでやる
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


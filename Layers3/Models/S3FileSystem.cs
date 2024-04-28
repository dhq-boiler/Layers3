using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using DokanNet;
using Layers3.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using FileAccess = DokanNet.FileAccess;
using Timer = System.Timers.Timer;

namespace Layers3.Models
{
    class S3FileSystem : IDokanOperations
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3FileSystem(IAmazonS3 s3Client, string bucketName, string driveLetter)
        {
            _s3Client = s3Client;
            _bucketName = bucketName;
        }

        public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options,
            FileAttributes attributes, IDokanFileInfo info)
        {
            if (fileName == "\\desktop.ini" || fileName.EndsWith("\\desktop.ini") || fileName.TrimStart(Path.DirectorySeparatorChar).Length == 0)
            {
                return DokanResult.Success;
            }

            var isFolderRegex = new Regex(@"新しいフォルダー(\s\(\d+?\))/?$");

            var isDirectory = isFolderRegex.IsMatch(fileName);

            if (isDirectory)
            {
                if (mode == FileMode.Open)
                {
                    // ディレクトリ作成処理を実装
                    var request = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName.Substring(1).Replace('\\', '/').TrimStart(Path.DirectorySeparatorChar) + "/"
                    };
                    _s3Client.PutObjectAsync(request).Wait();
                    Debug.WriteLine($"PUT Dir: {request.Key}");
                }
                return DokanResult.Success;
            }
            else
            {
               //do nothing
            }
            return DokanResult.Success;
        }

        private bool ExistsOnS3(string key)
        {
            var request = new GetObjectRequest()
            {
                BucketName = _bucketName,
                Key = key,
            };
            try
            {
                var response = _s3Client.GetObjectAsync(request).GetAwaiter().GetResult();
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Amazon.S3.AmazonS3Exception e)
            {
                return false;
            }
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {
            var key = fileName.TrimStart(Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(key) || key == "desktop.ini")
                return;
            if (info.DeleteOnClose)
            {
                _s3Client.DeleteObjectAsync(_bucketName, key + (info.IsDirectory ? "/" : string.Empty)).Wait();
            }
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
        }

        //S3のバケットのファイルを読み込む
        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            Debug.WriteLine($"ReadFile filename:{fileName}, offset:{offset}");

            while (dictionary.Any(x => x.Value.TimerIsRunning))
            {
                Task.Delay(1000).Wait();
            }

            var key = fileName.TrimStart(Path.DirectorySeparatorChar);
            key = key.Replace('\\', '/');

            if (key.EndsWith("desktop.ini") || key.EndsWith("folder.jpg"))
            {
                bytesRead = 0;
                return DokanResult.FileNotFound;
            }
            
            var transferUtility = new TransferUtility(_s3Client);
            var request = new TransferUtilityOpenStreamRequest()
            {
                BucketName = _bucketName,
                Key = key,
            };

            try
            {
                using var stream = transferUtility.OpenStream(request);
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = offset;
                bytesRead = ms.Read(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                bytesRead = 0;
            }

            return DokanResult.Success;
        }

        public class S3FileWriteContext
        {
            public string UploadId { get; set; }
            public long CurrentOffset { get; set; }
            public long FileSize { get; set; }
            public List<PartETag> PartETags { get; set; } = new List<PartETag>();
        }

        class A
        {
            public List<SignalManager.Buffer> Buffers { get; set; } = new();

            public TimeSpan IntervalThreshold { get; } = TimeSpan.FromMilliseconds(3000);

            public DateTime LastCallTime{ get; set; }

            public object LockObject { get; } = new();

            public Timer Timer { get; } = new Timer();

            public bool TimerIsRunning { get; private set; }

            public A(string key, Action<string> lazyrun)
            {

                Timer.Elapsed += (sender, args) =>
                {
                    Debug.WriteLine("Elapsed");
                    if (DateTime.Now - LastCallTime >= IntervalThreshold)
                    {
                        // 最後の呼び出しから設定時間が経過した場合の処理
                        Debug.WriteLine("Special processing after the last call.");
                        lazyrun(key);
                        var timer = (Timer)sender;
                        timer.Dispose();
                    }
                };
                Timer.Interval = 250;
                Timer.AutoReset = true;
                TimerIsRunning = true;
            }

            private readonly object _lockObject = new();

            public Stream GetBuffer()
            {
                lock (_lockObject)
                {
                    Debug.WriteLine($"GetBuffer");
                    var buffers = Buffers.OrderBy(x => x.Offset).ToList();
                    var seqNum = 0;
                    var buffer = new MemoryStream();
                    buffer.Position = 0;
                    DumpToFile(buffers[0].Stream);
                    foreach (var b in buffers)
                    {
                        b.Stream.Position = 0;
                        b.Stream.CopyTo(buffer);
                        b.Stream.Position = 0;
                    }

                    buffer.Position = 0;
                    DumpToFile(buffer);
                    buffer.Position = 0;
                    return buffer;
                }
            }

            private void DumpToFile(MemoryStream buffer)
            {
                using (var fs = new FileStream("C:\\Users\\boiler\\OneDrive\\デスクトップ\\dump.txt", FileMode.OpenOrCreate))
                {
                    buffer.Position = 0;
                    buffer.CopyTo(fs);
                }
            }

            public SignalManager.Buffer Pop()
            {
                var buffer = Buffers.First();
                Buffers.Remove(buffer);
                return buffer;
            }
        }

        private readonly ConcurrentDictionary<string, A> dictionary = new();
        private readonly object _lockObject = new();

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {

            const int MinPartSize = 6 * 1024 * 1024; // 5MB

            Debug.WriteLine($"WriteFile {fileName} {(offset+buffer.Length) / 1024d / 1024d}MB");
            var key = fileName.TrimStart(Path.DirectorySeparatorChar);
            key = key.Replace('\\', '/');

            Begin(key);

            lock (_lockObject)
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key].Buffers.Add(new SignalManager.Buffer()
                    {
                        SequenceNumber = dictionary[key].Buffers.Count,
                        Offset = offset,
                        Stream = new MemoryStream(buffer.ToArray())
                    });
                }
                else
                {
                    dictionary.TryAdd(key, new A(key, key => LazyRun(key))
                    {
                        Buffers = new List<SignalManager.Buffer>()
                        {
                            new SignalManager.Buffer()
                            {
                                SequenceNumber = 0,
                                Offset = offset,
                                Stream = new MemoryStream(buffer.ToArray())
                            }
                        }
                    });
                }
            }

            End(key);

            bytesWritten = buffer.Length;

            return DokanResult.Success;
        }

        private static void DumpToFile(MemoryStream buffer)
        {
            using (var fs = new FileStream("C:\\Users\\boiler\\OneDrive\\デスクトップ\\dump.txt", FileMode.OpenOrCreate))
            {
                buffer.Position = 0;
                buffer.CopyTo(fs);
            }
        }

        private void Begin(string key)
        {
        }

        private readonly object _lockObject_2 = new();
        private Dictionary<string, DateTime> _lastCallTimeDic = new();

        private void LazyRun(string key)
        {
            lock (_lockObject_2)
            {
                if (_lastCallTimeDic.ContainsKey(key))
                {
                    var lastCallTime = _lastCallTimeDic[key];
                    if (DateTime.Now - lastCallTime < TimeSpan.FromSeconds(5))
                    {
                        return;
                    }
                }

                if (dictionary.ContainsKey(key))
                {
                    var a = dictionary[key];
                    var allLength = a.Buffers.Sum(x => x.Stream.Length);
                    var unitLength = 6 * 1024 * 1024;

                    using var stream = a.GetBuffer();
                    stream.Position = 0;
                    TransferUtilityUploadRequest request = new TransferUtilityUploadRequest
                    {
                        BucketName = _bucketName,
                        Key = key,
                        InputStream = stream,
                        CannedACL = S3CannedACL.Private,
                    };

                    var transferUtility = new TransferUtility(_s3Client);
                    transferUtility.Upload(request);
                    var entry = dictionary.SingleOrDefault(x => x.Key == key);
                    if (entry.Value is not null)
                    {
                        dictionary.TryRemove(entry);
                    }
                }

                if (_lastCallTimeDic.ContainsKey(key))
                {
                    var dateTime = _lastCallTimeDic[key];
                    _lastCallTimeDic.Remove(key);
                    _lastCallTimeDic.Add(key, DateTime.Now);
                }
                else
                {
                    _lastCallTimeDic.Add(key, DateTime.Now);
                }
            }
        }

        private object lockObject = new();

        private void End(string key)
        {
            if (dictionary.ContainsKey(key))
            {
                Debug.WriteLine($"End: {key}");
                var a = dictionary[key];
                
                a.LastCallTime = DateTime.Now;
                a.Timer.Stop();
                a.Timer.Start();
            }
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            Debug.WriteLine("FlushFileBuffers");
            return DokanResult.Success;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            //Debug.WriteLine("GetFileInformation");
            fileName = fileName.Replace("\\", "/");
            var key = fileName.Replace("\\", "/");

            if (string.IsNullOrEmpty(key))
            {
                // ルートディレクトリ
                fileInfo = new FileInformation
                {
                    FileName = fileName,
                    Attributes = FileAttributes.Directory,
                    CreationTime = DateTime.Now,
                    LastAccessTime = DateTime.Now,
                    LastWriteTime = DateTime.Now
                };
                return DokanResult.Success;
            }

            var prefix = Path.GetDirectoryName(key.Substring(1));
            if (!string.IsNullOrEmpty(prefix))
            {
                prefix = prefix.Replace('\\', '/');
            }
            prefix += "/";

            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix
            };
            if (request.Prefix == "/")
            {
                request.Prefix = string.Empty;
            }
            var response = _s3Client.ListObjectsV2Async(request).Result;

            if (key.Length > 0 && (response.S3Objects.Any(o => o.Key == key.Substring(1) + "/") || response.S3Objects.Any(o => o.Key.StartsWith(key.Substring(1) + "/"))))
            {
                // ディレクトリ
                fileInfo = new FileInformation
                {
                    FileName = fileName,
                    Attributes = FileAttributes.Directory,
                    CreationTime = DateTime.Now,
                    LastAccessTime = DateTime.Now,
                    LastWriteTime = DateTime.Now
                };
                return DokanResult.Success;
            }
            else if (response.S3Objects.Any(o => o.Key == key.Substring(1)))
            {
                // ファイル
                var s3Object = response.S3Objects.First(o => o.Key == key.Substring(1));
                fileInfo = new FileInformation
                {
                    FileName = fileName,
                    Attributes = FileAttributes.Normal,
                    CreationTime = s3Object.LastModified,
                    LastAccessTime = s3Object.LastModified,
                    LastWriteTime = s3Object.LastModified,
                    Length = s3Object.Size
                };
                return DokanResult.Success;
            }
                
            fileInfo = new FileInformation();
            return DokanResult.Success;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            var prefix = fileName.TrimStart(Path.DirectorySeparatorChar);
            var result = _s3Client.ListObjectsAsync(_bucketName, prefix).Result;

            files = result.S3Objects
                .Select(x => new FileInformation
                {
                    Attributes = FileAttributes.Normal,
                    FileName = x.Key.Substring(prefix.Length).TrimStart(Path.DirectorySeparatorChar),
                    Length = x.Size
                })
                .ToArray();

            return DokanResult.Success;
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            //Debug.WriteLine("FindFilesWithPattern");
            var prefix = fileName.TrimStart(Path.DirectorySeparatorChar);
            prefix = prefix.Replace("\\", "/");
            var result = _s3Client.ListObjectsAsync(_bucketName, prefix).Result;

            var regex = new Regex(@"^(/[^/]+)+/");
            var regex2 = new Regex(@"^/([^/]+)");
            
            if (prefix.Length > 0)
            {
                result.S3Objects.ToList()
                    .ForEach(x => x.Key = x.Key.IndexOf($"{prefix}/") != -1 ? x.Key.Remove(x.Key.IndexOf($"{prefix}/"), $"{prefix}/".Length) : x.Key);
            }

            var list = new List<FileInformation>();

            foreach (var s3Object in result.S3Objects)
            {
                var key = s3Object.Key;
                if (string.IsNullOrEmpty(key) ||
                    key.EndsWith("desktop.ini") ||
                    key.EndsWith("desktop.ini/") ||
                    key.EndsWith("folder.jpg") ||
                    key.EndsWith("folder.jpg/"))
                {
                    continue;
                }

                if (key.EndsWith("/") && !key.Contains("\\"))
                {
                    list.Add(new FileInformation()
                    {
                        Attributes = FileAttributes.Directory,
                        FileName = key.Substring(0, key.Length - 1)
                    });
                }
                else if (key.Contains("/"))
                {
                    continue;
                }
                else
                {
                    list.Add(new FileInformation()
                    {
                        Attributes = FileAttributes.Normal,
                        FileName = key,
                        Length = s3Object.Size
                    });
                }
            }

            files = list;

            return DokanResult.Success;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            //Debug.WriteLine("SetFileAttributes");
            // S3には適用されない
            return DokanResult.Success;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime,
            IDokanFileInfo info)
        {
            //Debug.WriteLine("SetFileTime");
            // S3には適用されない
            return DokanResult.Success;
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            //Debug.WriteLine("DeleteFile");
            // ファイル削除処理を実装
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName
            };
            _s3Client.DeleteObjectAsync(request).Wait();
            return DokanResult.Success;
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            //Debug.WriteLine("DeleteDirectory");
            // ディレクトリ削除処理を実装
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = fileName
            };
            var response = _s3Client.ListObjectsV2Async(request).Result;

            foreach (var s3Object in response.S3Objects)
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = s3Object.Key
                };
                _s3Client.DeleteObjectAsync(deleteRequest).Wait();
            }

            return DokanResult.Success;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            //Debug.WriteLine("MoveFile");

            // 移動元のファイル名と移動先のファイル名を取得
            var sourceKey = oldName.TrimStart(Path.DirectorySeparatorChar).Replace('\\', '/');
            var destinationKey = newName.TrimStart(Path.DirectorySeparatorChar).Replace('\\', '/');

            try
            {
                // 移動元のファイルを取得
                var sourceRequest = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = sourceKey
                };
                var sourceResponse = _s3Client.GetObjectAsync(sourceRequest).Result;

                // 移動先のファイルにコピーする
                var copyRequest = new CopyObjectRequest
                {
                    SourceBucket = _bucketName,
                    SourceKey = sourceKey,
                    DestinationBucket = _bucketName,
                    DestinationKey = destinationKey
                };
                _s3Client.CopyObjectAsync(copyRequest).Wait();

                // 移動元のファイルを削除する
                _s3Client.DeleteObjectAsync(_bucketName, sourceKey).Wait();

                // 移動元がディレクトリの場合、移動先のディレクトリ内のファイルのキーを変更する
                if (IsDirectory(oldName))
                {
                    var prefix = sourceKey + "/";
                    var request = new ListObjectsV2Request
                    {
                        BucketName = _bucketName,
                        Prefix = prefix
                    };
                    var response = _s3Client.ListObjectsV2Async(request).Result;

                    foreach (var s3Object in response.S3Objects)
                    {
                        var newKey = destinationKey + s3Object.Key.Substring(prefix.Length);
                        var copyObjectRequest = new CopyObjectRequest
                        {
                            SourceBucket = _bucketName,
                            SourceKey = s3Object.Key,
                            DestinationBucket = _bucketName,
                            DestinationKey = newKey
                        };
                        _s3Client.CopyObjectAsync(copyObjectRequest).Wait();
                        _s3Client.DeleteObjectAsync(_bucketName, s3Object.Key).Wait();
                    }
                }
            }
            catch (Exception e)
            {
                //ファイルではなくディレクトリの場合
                var sourceRequest = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = sourceKey + "/"
                };
                var sourceResponse = _s3Client.GetObjectAsync(sourceRequest).Result;

                // 移動先のフォルダにコピーする
                var copyRequest = new CopyObjectRequest
                {
                    SourceBucket = _bucketName,
                    SourceKey = sourceKey + "/",
                    DestinationBucket = _bucketName,
                    DestinationKey = destinationKey + "/",
                };
                _s3Client.CopyObjectAsync(copyRequest).Wait();

                {
                    var prefix = sourceKey + "/";
                    var request = new ListObjectsV2Request
                    {
                        BucketName = _bucketName,
                        Prefix = prefix
                    };
                    var response = _s3Client.ListObjectsV2Async(request).Result;

                    foreach (var s3Object in response.S3Objects.Where(x => Path.GetFileName(x.Key).Any()))
                    {
                        var newKey = destinationKey + "/" + s3Object.Key.Substring(prefix.Length);
                        var copyObjectRequest = new CopyObjectRequest
                        {
                            SourceBucket = _bucketName,
                            SourceKey = s3Object.Key,
                            DestinationBucket = _bucketName,
                            DestinationKey = newKey
                        };
                        _s3Client.CopyObjectAsync(copyObjectRequest).Wait();
                    }
                }

                try
                {
                    var prefix = sourceKey + "/";
                    var request = new ListObjectsV2Request
                    {
                        BucketName = _bucketName,
                        Prefix = prefix
                    };
                    var response = _s3Client.ListObjectsV2Async(request).Result;

                    foreach (var s3Object in response.S3Objects)
                    {
                        _s3Client.DeleteObjectAsync(_bucketName, s3Object.Key).Wait();
                    }

                    // 移動元のファイルを削除する
                    _s3Client.DeleteObjectAsync(_bucketName, sourceKey + "/").Wait();
                }
                catch (Exception exception)
                {
                }

                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(5000);
                    try
                    {
                        //ゴミファイルを削除
                        _s3Client.DeleteObjectAsync(_bucketName, destinationKey).Wait();
                    }
                    catch (Exception exception)
                    {
                    }
                });
            }
            

            return DokanResult.Success;
        }

        private bool IsDirectory(string fileName)
        {
            var key = fileName.TrimStart(Path.DirectorySeparatorChar).Replace('\\', '/');
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = key + "/"
            };
            var response = _s3Client.ListObjectsV2Async(request).Result;
            return response.S3Objects.Any();
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            //Debug.WriteLine("SetEndOfFile");
            // S3には適用されない
            return DokanResult.Success;
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            //Debug.WriteLine("SetAllocationSize");
            // S3には適用されない
            return DokanResult.Success;
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            //Debug.WriteLine("LockFile");
            // S3には適用されない
            return DokanResult.Success;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            //Debug.WriteLine("UnlockFile");
            // S3には適用されない
            return DokanResult.Success;
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes,
            IDokanFileInfo info)
        {
            //Debug.WriteLine("GetDiskFreeSpace");
            // バケット内のオブジェクトを列挙してサイズを計算する
            long totalSize = 0;
            string continuationToken = null;
            do
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    ContinuationToken = continuationToken
                };
                var response = _s3Client.ListObjectsV2Async(request).Result;
                totalSize += response.S3Objects.Sum(x => x.Size);
                continuationToken = response.NextContinuationToken;
            } while (continuationToken != null);

            freeBytesAvailable = long.MaxValue;
            totalNumberOfBytes = totalSize;
            totalNumberOfFreeBytes = long.MaxValue;
            return DokanResult.Success;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName,
            out uint maximumComponentLength, IDokanFileInfo info)
        {
            //Debug.WriteLine("GetVolumeInforamtion");
            volumeLabel = $"AWS S3({_bucketName})";
            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch |
                       FileSystemFeatures.PersistentAcls | FileSystemFeatures.SupportsRemoteStorage;
            fileSystemName = "S3FS";
            maximumComponentLength = 255;
            return DokanResult.Success;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections,
            IDokanFileInfo info)
        {
            //Debug.WriteLine("GetFileSecurity");
            // S3には適用されない
            security = null;
            return DokanResult.NotImplemented;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections,
            IDokanFileInfo info)
        {
            //Debug.WriteLine("SetFileSecurity");
            // S3には適用されない
            return DokanResult.NotImplemented;
        }

        public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
        {
            //Debug.WriteLine("Mounted");
            // マウント時の処理があれば記述する
            return DokanResult.Success;
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            //Debug.WriteLine("Unmounted");
            // アンマウント時の処理があれば記述する
            return DokanResult.Success;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            //Debug.WriteLine("FindStreams");
            // S3にはファイルストリームの概念がない
            streams = new FileInformation[0];
            return DokanResult.NotImplemented;
        }

        private static string ConvertToRegex(string searchPattern)
        {
            string regex = "^" + Regex.Escape(searchPattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".")
                .Replace("\\.", "\\.") + "$";
            return regex;
        }
    }
}

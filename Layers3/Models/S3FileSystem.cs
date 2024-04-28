using Amazon.S3;
using Amazon.S3.Model;
using DokanNet;
using Layers3.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using Amazon.S3.Transfer;
using FileAccess = DokanNet.FileAccess;
using Timer = System.Timers.Timer;

namespace Layers3.Models
{
    class S3FileSystem : IDokanOperations
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _driveLetter;
        private ConcurrentDictionary<string, S3FileWriteContext> _writeContextDic = new();

        public S3FileSystem(IAmazonS3 s3Client, string bucketName, string driveLetter)
        {
            _s3Client = s3Client;
            _bucketName = bucketName;
            _driveLetter = driveLetter;
        }

        public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode, FileOptions options,
            FileAttributes attributes, IDokanFileInfo info)
        {
            if (fileName.TrimStart(Path.DirectorySeparatorChar).Length == 0)
            {
                return DokanResult.Success;
            }

            if (info.IsDirectory)
            {
                if (mode == FileMode.CreateNew)
                {
                    // ディレクトリ作成処理を実装
                    var request = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName.TrimStart(Path.DirectorySeparatorChar) + "/"
                    };
                    _s3Client.PutObjectAsync(request).Wait();
                }
                return DokanResult.Success;
            }
            return DokanResult.Success;
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {
            var key = fileName.TrimStart(Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(key) || key == "desktop.ini")
                return;
            if (info.DeleteOnClose)
            {
                _s3Client.DeleteObjectAsync(_bucketName, key).Wait();
            }
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
            //Debug.WriteLine("CloseFile");
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {

            while (dictionary.Any(x => x.Value.TimerIsRunning))
            {
                Task.Delay(1000).Wait();
            }

            Debug.WriteLine("ReadFile");
            var key = fileName.TrimStart(Path.DirectorySeparatorChar);
            key = key.Replace('\\', '/');

            if (key.EndsWith("desktop.ini") || key.EndsWith("folder.jpg"))
            {
                bytesRead = 0;
                return DokanResult.FileNotFound;
            }

            const int MinPartSize = 5 * 1024 * 1024; // 5MB

            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ByteRange = new ByteRange(offset, offset + buffer.Length - 1)
            };

            //if (buffer.Length < MinPartSize)
            //{
            //    using (var response = _s3Client.GetObjectAsync(request).Result)
            //    using (var stream = response.ResponseStream)
            //    {
            //        bytesRead = stream.Read(buffer, 0, buffer.Length);
            //    }
            //    return DokanResult.Success;
            //}
            //else
            //{
                var partSize = MinPartSize;
                var tasks = new List<Task<int>>();
                var remainingBytes = buffer.Length;
                var currentOffset = offset;

                byte[] partBuffer = new byte[5 * 1024 * 1024];

                while (remainingBytes > 0)
                {
                    var readSize = Math.Min(partSize, remainingBytes);
                    partBuffer = new byte[readSize];
                    var partRequest = new GetObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = key,
                        ByteRange = new ByteRange(currentOffset, currentOffset + readSize - 1)
                    };

                    tasks.Add(Task.Run(async () =>
                    {
                        using (var response = await _s3Client.GetObjectAsync(partRequest))
                        using (var stream = response.ResponseStream)
                        {
                            return await stream.ReadAsync(partBuffer, 0, partBuffer.Length);
                        }
                    }));

                    remainingBytes -= readSize;
                    currentOffset += readSize;
                }

                Task.WaitAll(tasks.ToArray());

                bytesRead = 0;
                foreach (var task in tasks)
                {
                    var partBytesRead = task.Result;
                    Buffer.BlockCopy(partBuffer, 0, buffer, bytesRead, partBytesRead);
                    bytesRead += partBytesRead;
                }

                return DokanResult.Success;
            //}
        }

        public class S3FileWriteContext
        {
            public string UploadId { get; set; }
            public long CurrentOffset { get; set; }
            public long FileSize { get; set; }
            public List<PartETag> PartETags { get; set; } = new List<PartETag>();
        }

        //private static IEnumerable<string> GetOpenFilesForProcess(int processId)
        //{
        //    string query = $"SELECT * FROM CIM_ProcessExecutable WHERE Antecedent = \"Win32_Process.Handle='{processId}'\"";

        //    using (var searcher = new ManagementObjectSearcher(query))
        //    {
        //        foreach (var item in searcher.Get())
        //        {
        //            string fileName = item["Dependent"].ToString().Split('"')[1];
        //            yield return fileName;
        //        }
        //    }
        //}

        //private long GetFileSize(string fileName, int infoProcessId)
        //{
        //    var key = fileName.TrimStart(Path.DirectorySeparatorChar);
        //    var files = GetOpenFilesForProcess(infoProcessId);
        //    foreach (var file in files)
        //    {
        //        if (file.Contains(key))
        //        {
        //            return new FileInfo(file).Length;
        //        }
        //    }

        //    return 0;
        //}

        ////static volatile bool isLastCallCompleted = false;
        //private ConcurrentDictionary<string, bool> _isLastCallCompletedDic = new();
        ////static ManualResetEvent waitHandle = new ManualResetEvent(false);
        //static object lockObject = new object();
        //private ConcurrentDictionary<string, bool> _signals = new();
        //private SignalManager signalManager = new SignalManager();

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
                        //timer.AutoReset = false;
                        //timer.Enabled = false;
                        //timer.Stop();
                        //TimerIsRunning = false;
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
                    //buffers.ForEach(x => x.SequenceNumber = seqNum++);
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

            #region obsolete
            //signalManager.HandlePart(key);


            //if (offset == 0)
            //{
            //    Debug.WriteLine($"New file upload:{key}");

            //    //if (buffer.Length < MinPartSize)
            //    //{
            //    //    // ファイルサイズが最小パートサイズ未満の場合は、通常のアップロードを行う
            //    //    using (var stream = new MemoryStream(buffer))
            //    //    {
            //    //        var putRequest = new PutObjectRequest
            //    //        {
            //    //            BucketName = _bucketName,
            //    //            Key = key,
            //    //            InputStream = stream
            //    //        };
            //    //        _s3Client.PutObjectAsync(putRequest).Wait();
            //    //        bytesWritten = buffer.Length;
            //    //    }
            //    //    return DokanResult.Success;
            //    //}

            //    // 新しいマルチパートアップロードを開始する
            //    var initiateRequest = new InitiateMultipartUploadRequest
            //    {
            //        BucketName = _bucketName,
            //        Key = key
            //    };
            //    var initResponse = _s3Client.InitiateMultipartUploadAsync(initiateRequest).Result;

            //    // アップロードIDと現在のオフセットを格納する
            //    _writeContextDic.TryAdd(key, new S3FileWriteContext()
            //    {
            //        UploadId = initResponse.UploadId,
            //        CurrentOffset = 0,
            //        FileSize = 0
            //    });

            //    //_signals.TryAdd(key, true);
            //}

            ////while (!_writeContextDic.ContainsKey(key))
            ////{
            ////    // マルチパートアップロードの開始が完了するまで待機
            ////    System.Threading.Thread.Sleep(10);
            ////}

            //_writeContextDic.TryGetValue(key, out var context);
            //if (context is null)
            //{
            //    bytesWritten = 0;
            //    return DokanResult.Success;
            //}
            ////using (var stream = new MemoryStream(buffer))
            ////{
            //    lock (context)
            //    {
            //        bytesWritten = buffer.Length;
            //    //Debug.WriteLine($"Upload part:{key}, offset:{offset}, length:{stream.Length}");

            //    signalManager.PushAndClearBuffer(key, _bucketName, context, _s3Client);

            //    //var partNumber = context.PartETags.Any() ? context.PartETags.Max(x => x.PartNumber) + 1 : 1;


            //    //// 現在のオフセットとファイルサイズを更新する
            //    //context.CurrentOffset += buffer.Length;
            //    //context.FileSize = Math.Max(context.FileSize, context.CurrentOffset);

            //    var flag = signalManager.FinishPart(key, buffer, _bucketName, _s3Client, length =>
            //        {

            //            if (length < MinPartSize)
            //            {
            //                // ファイルサイズが最小パートサイズ未満の場合は、通常のアップロードを行う
            //                //using (var stream = new MemoryStream(buffer))
            //                //{
            //                var signal = signalManager[key];


            //                    var putRequest = new PutObjectRequest
            //                    {
            //                        BucketName = _bucketName,
            //                        Key = key,
            //                        InputStream = signal.GetBuffer()
            //                    };
            //                    _s3Client.PutObjectAsync(putRequest).Wait();
            //                //}
            //                Debug.WriteLine($"Complete singlepart upload:{key}");
            //            }
            //            else
            //            {
            //                var partETags = context.PartETags.ToList();
            //                lock (context)
            //                {
            //                    partETags = context.PartETags.ToList();
            //                }

            //                // マルチパートアップロードを完了する
            //                var completeRequest = new CompleteMultipartUploadRequest
            //                {
            //                    BucketName = _bucketName,
            //                    Key = key,
            //                    UploadId = context.UploadId,
            //                    PartETags = partETags
            //                };
            //                var result = _s3Client.CompleteMultipartUploadAsync(completeRequest).Result;
            //                Debug.WriteLine($"Complete multipart upload:{key}");
            //            }

            //            _writeContextDic.TryRemove(key, out _);
            //        });

            //        if (flag)
            //        {
            //            using (var stream = new MemoryStream(buffer))
            //            {
            //                // ファイルサイズが最小パートサイズ未満の場合は、通常のアップロードを行う
            //                var putRequest = new PutObjectRequest
            //                {
            //                    BucketName = _bucketName,
            //                    Key = key,
            //                    InputStream = stream
            //                };
            //                _s3Client.PutObjectAsync(putRequest).Wait();
            //                bytesWritten = buffer.Length;
            //            }
            //        }
            //    }

            ////_writeContextDic.TryGetValue(key, out context);
            ////if (context is null)
            ////{
            ////    return DokanResult.Success;
            ////}
            ////if (context.CurrentOffset == context.FileSize)
            ////{

            ////var isLastCallCompleted = _isLastCallCompletedDic[key];

            ////// 最後の呼び出しが完了したかどうかを確認
            ////if (!isLastCallCompleted)
            ////{
            ////    lock (lockObject)
            ////    {
            ////        if (!isLastCallCompleted)
            ////        {
            ////            // 特別な処理を実行
            ////            //Console.WriteLine("Last call - Performing special operation.");
            ////            //isLastCallCompleted = true;
            ////            _isLastCallCompletedDic.TryRemove(key, out isLastCallCompleted);



            ////            //// 特別な処理が完了したことを通知
            ////            //waitHandle.Set();
            ////        }
            ////    }
            ////}

            ////var action = new Action(() =>
            ////{
            ////    Debug.WriteLine($"Complete multipart upload:{key}");
            ////    var partETags = context.PartETags.ToList();
            ////    lock (context)
            ////    {
            ////        partETags = context.PartETags.ToList();
            ////    }

            ////    // マルチパートアップロードを完了する
            ////    var completeRequest = new CompleteMultipartUploadRequest
            ////    {
            ////        BucketName = _bucketName,
            ////        Key = key,
            ////        UploadId = context.UploadId,
            ////        PartETags = partETags
            ////    };
            ////    _s3Client.CompleteMultipartUploadAsync(completeRequest).Wait();
            ////    _writeContextDic.TryRemove(key, out _);
            ////});
            ////}
            #endregion obsolete

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
                
                
                //lock (a.LockObject)
                //{
                //    a.EndDateTime = DateTime.Now;
                //}

                //Task.Run(() =>
                //{
                //    lock (a.LockObject)
                //    {
                //        if (a.EndDateTime + Interval > DateTime.Now)
                //        {
                //            return;
                //        }
                //    }

                //    Debug.WriteLine($"{a.EndDateTime + Interval}, {DateTime.Now}");
                //    if (a.EndDateTime + Interval < DateTime.Now)
                //    {
                //        LazyRun(key);
                //        dictionary.TryRemove(dictionary.Single(x => x.Key == key));
                //    }
                //});
            }
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            Debug.WriteLine("FlushFileBuffers");
            ////// S3には適用されない
            ////return DokanResult.Success;
            //var key = fileName.TrimStart(Path.DirectorySeparatorChar);
            //key = key.Replace('\\', '/');

            //_writeContextDic.TryGetValue(key, out var context);
            //Debug.WriteLine($"Complete multipart upload:{key}");
            //var partETags = context.PartETags.ToList();
            //lock (context)
            //{
            //    partETags = context.PartETags.ToList();
            //}
            //// マルチパートアップロードを完了する
            //var completeRequest = new CompleteMultipartUploadRequest
            //{
            //    BucketName = _bucketName,
            //    Key = key,
            //    UploadId = context.UploadId,
            //    PartETags = partETags
            //};
            //_s3Client.CompleteMultipartUploadAsync(completeRequest).Wait();
            //_writeContextDic.TryRemove(key, out _);

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

            //response.S3Objects.ToList().ForEach(x => x.Key = x.Key.Remove(x.Key.IndexOf(request.Prefix), request.Prefix.Length));

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


            var dirs = result.S3Objects
                .Select(x =>
                {
                    x.Key = "/" + x.Key;
                    return x;
                })
                .Where(x => System.Text.RegularExpressions.Regex.IsMatch(x.Key, ConvertToRegex(searchPattern)))
                .GroupBy(x => regex.Match(x.Key).Value)
                .Select(group => regex2.Match(group.Key).Value)
                .Distinct()
                .Where(x => x.Length > 0)
                .Select(x => new FileInformation()
                {
                    Attributes = FileAttributes.Directory,
                    FileName = x.Substring(1)
                }).ToArray();

            files = result.S3Objects
                .Where(x => System.Text.RegularExpressions.Regex.IsMatch(x.Key, ConvertToRegex(searchPattern)))
                .Where(x => !x.Key.Substring(1).Contains("/"))
                .Select(x => new FileInformation()
                {
                    Attributes = FileAttributes.Normal,
                    FileName = x.Key.Substring(1),
                    Length = x.Size
                })
                .Union(dirs)
                .ToArray();

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

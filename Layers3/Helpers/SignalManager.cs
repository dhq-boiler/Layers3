using Amazon.S3;
using Amazon.S3.Model;
using Layers3.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Layers3.Helpers
{
    internal class SignalManager
    {
        private ConcurrentDictionary<string, Signal> signals = new ConcurrentDictionary<string, Signal>();

        public TimeSpan SignalTimeout { get; set; } = TimeSpan.FromMilliseconds(10000);

        public Signal this[string key] => signals[key];

        public void HandlePart(string key)
        {
            signals.TryAdd(key, new Signal()
            {
                IsAlive = true
            });
        }

        public void PoolBuffer(string key, MemoryStream buffer)
        {
            signals.AddOrUpdate(key, new Signal()
            {
                IsAlive = true,
            }, (k, v) =>
            {
                v.Buffers.Add(new Buffer()
                    {
                        SequenceNumber = v.SequenceNumber++,
                        Stream = buffer,
                    });
                return v;
            });
        }

        public void PushAndClearBuffer(string key, string bucketName, S3FileSystem.S3FileWriteContext context, IAmazonS3 s3Client)
        {
            Debug.WriteLine("PushAndClearBuffer");

            signals.TryGetValue(key, out var signal);

            var buffers = signal.Buffers.ToList();
            var unitLength = buffers.Sum(x => x.Stream.Length);

            //if (unitLength >= 6 * 1024 * 1024 || signal.SequenceNumber > 1)
            //{
            using var stream = signal.GetBuffer();

            stream.Position = 0;

            var uploadRequest = new UploadPartRequest
            {
                BucketName = bucketName,
                Key = key,
                UploadId = context.UploadId,
                PartNumber = context.PartETags.Any() ? context.PartETags.Max(x => x.PartNumber) + 1 : 1,
                PartSize = unitLength,
                InputStream = stream
            };
            var uploadResponse = s3Client.UploadPartAsync(uploadRequest).Result;
            Debug.WriteLine($"Upload. SequenceNumber:{signal.SequenceNumber}");

            // パートの ETag 情報を保存する
            context.PartETags.Add(new PartETag
            {
                PartNumber = context.PartETags.Any() ? context.PartETags.Max(x => x.PartNumber) + 1 : 1,
                ETag = uploadResponse.ETag
            });

            // マルチパートアップロードのパートを正しい順序でソートする
            context.PartETags.Sort((x, y) => x.PartNumber.CompareTo(y.PartNumber));

            signal.Length += unitLength;

            //signal.SequenceNumber++;
            //}
        }

        public bool FinishPart(string key, byte[] buffer, string bucketName, IAmazonS3 _s3Client, Action<long> action)
        {

            var flag = false;

            Task.Run(async () =>
            {
                
                if (signals.TryGetValue(key, out Signal signal) && signal.IsAlive)
                {
                    //該当のシグナルが存在する場合、シグナルを削除
                    //signals.AddOrUpdate(key, signal, (k, v) =>
                    //{
                    //    v.CancellationTokenSource.Cancel();
                    //    v.Buffers.Add(new Buffer() { Stream = new MemoryStream(buffer) });
                    //    return v;
                    //});
                    signal.Buffers.LastOrDefault()?.CancellationTokenSource?.Cancel();
                    signal.Buffers.Add(new Buffer() { Stream = new MemoryStream(buffer) });

                    await Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(SignalTimeout);

                        //一連の処理が完了して、終了処理を行う
                        try
                        {
                            var signalLength = signal.Length;
                            action(signalLength);
                        }
                        catch (Exception e)
                        {
                            flag = true;
                        }

                        signal.Buffers.Clear();

                        //シグナルを削除
                        signals.TryRemove(key, out _);
                        Debug.WriteLine($"Signal Lost. key:{key}");
                    }, signal.Buffers.Last().CancellationTokenSource.Token);
                }
            });

            return flag;
        }

        public class Signal
        {
            public int SequenceNumber { get; set; } = 0;
            public List<Buffer> Buffers { get; } = new List<Buffer>();
            public bool IsAlive { get; set; }
            public DateTime UntilAlive { get; set; }
            public long Length { get; set; }

            public MemoryStream GetBuffer()
            {
                Debug.WriteLine($"GetBuffer. SequenceNumber:{SequenceNumber}");
                var buffers = Buffers.OrderBy(x => x.SequenceNumber).ToList();
                var buffer = new MemoryStream();
                foreach (var b in buffers)
                {
                    b.Stream.Position = 0;
                    b.Stream.CopyTo(buffer);
                }
                buffer.Position = 0;
                return buffer;
            }
        }

        public class Buffer
        {
            public MemoryStream Stream { get; set; }
            public int SequenceNumber { get; set; }
            public long Offset { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        }
    }
}

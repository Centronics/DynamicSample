using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DynamicSample
{
    internal static class Logger
    {
        class IntStorage
        {
            volatile int _usersCount;

            public void IncUserCount()
            {
                Interlocked.Increment(ref _usersCount);
            }

            public void DecUserCount()
            {
                Interlocked.Decrement(ref _usersCount);
            }

            public ConcurrentQueue<string> StringsObject { get; } = new ConcurrentQueue<string>();

            public void WaitForFree()
            {
                while (_usersCount > 0)
                    Thread.Sleep(200);
            }
        }

        static FileStream _logFileStream;

        static TextWriter _logFileStreamWriter;

        static IntStorage _intStorage;

        static ManualResetEvent _stopBackground;

        static ReaderWriterLockSlim _rwlSync;

        static volatile int _isStopMode;

        static Thread _workerThread;

        static bool IsStopMode
        {
            get => _isStopMode != 0;

            set
            {
                if (value)
                {
                    Interlocked.CompareExchange(ref _isStopMode, 1, 0);
                    return;
                }

                Interlocked.CompareExchange(ref _isStopMode, 0, 1);
            }
        }

        public static void Initialize()
        {
            try
            {
                _logFileStream = new FileStream(Path.Combine(Application.StartupPath, @"dSample.log"), FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete);
                _logFileStreamWriter = new StreamWriter(_logFileStream, Encoding.UTF8);

                _stopBackground = new ManualResetEvent(false);
                _rwlSync = new ReaderWriterLockSlim();

                RenewLogStringsQueue(false);

                Thread t = new Thread(ThreadWriteFunction)
                {
                    IsBackground = true,
                    Name = @"LoggerThread"
                };

                t.Start();

                _workerThread = t;
                IsStopMode = false;
            }
            catch
            {
                IsStopMode = true;
                throw;
            }
        }

        public static void Deinitialize()
        {
            try
            {
                _stopBackground.Set();
                _workerThread.Join();

                _logFileStreamWriter.Dispose();
                _logFileStream.Dispose();

                _rwlSync.Dispose();
                _stopBackground.Dispose();

                _logFileStreamWriter = null;
                _logFileStream = null;
                _rwlSync = null;
                _stopBackground = null;
                _workerThread = null;
            }
            finally
            {
                IsStopMode = true;
            }
        }

        static IntStorage GetLogStringsQueue()
        {
            if (!_rwlSync.TryEnterReadLock(100))
                return null;

            try
            {
                _intStorage?.IncUserCount();
                return _intStorage;
            }
            finally
            {
                _rwlSync.ExitReadLock();
            }
        }

        static IntStorage RenewLogStringsQueue(bool reset)
        {
            if (!_rwlSync.TryEnterWriteLock(1000))
                return null;

            try
            {
                IntStorage logStrings = _intStorage;
                _intStorage = reset ? null : new IntStorage();
                return logStrings;
            }
            finally
            {
                _rwlSync.ExitWriteLock();
            }
        }

        /// <summary>
        ///     Записывает сообщение в лог-файл, в синхронном режиме.
        /// </summary>
        /// <param name="logstr">Строка лога, которую надо записать.</param>
        /// <param name="exp">Проверяет параметр расширенного лога <see cref="Launcher.ExplicitLogEnabled"/>.</param>
        /// <remarks>
        ///     Метод потокобезопасен.
        ///     К сообщению автоматически добавляются текущие дата и время в полном формате.
        /// </remarks>
        public static void WriteLog(string logstr, bool exp = false)
        {
            if (IsStopMode)
                return;

            try
            {
                switch (exp)
                {
                    case false when !Launcher.DebugLogEnabled:
                    case true when !Launcher.ExplicitLogEnabled:
                        return;
                }

                IntStorage t = GetLogStringsQueue();

                if (t == null)
                    return;

                if (logstr == null)
                    logstr = string.Empty;

                try
                {
                    t.StringsObject.Enqueue($@"{DateTime.Now:dd.MM.yyyy HH:mm:ss:ms} {logstr}");
                }
                finally
                {
                    t.DecUserCount();
                }
            }
            catch
            {
                IsStopMode = true;
                throw;
            }
        }

        static void ThreadWriteFunction()
        {
            try
            {
                WaitHandle[] waitHandles = { _stopBackground };

                while (true)
                {
                    switch (WaitHandle.WaitAny(waitHandles, 30000))
                    {
                        case WaitHandle.WaitTimeout:
                            WriteToDisk(RenewLogStringsQueue(false));
                            continue;
                        case 0:
                            WriteToDisk(RenewLogStringsQueue(true));
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                IsStopMode = true;
                ShowOnceUserMessageInOtherThread($@"Ошибка при записи логов: {ex.Message}");
            }

            return;

            void WriteToDisk(IntStorage logStrings)
            {
                if (logStrings == null)
                    return;

                logStrings.WaitForFree();
                int capacity = GetCapacity();

                if (capacity < 1)
                    return;

                char[] sb = new char[capacity];
                CreateBigString();
                _logFileStreamWriter.Write(sb);
                return;

                int GetCapacity()
                {
                    if (!logStrings.StringsObject.Any())
                        return 0;

                    int nc = Environment.NewLine.Length;
                    return logStrings.StringsObject.Sum(str => str.Length + nc);
                }

                void CreateBigString()
                {
                    string snl = Environment.NewLine;
                    int nc = snl.Length;
                    int pos = 0;

                    foreach (string s in logStrings.StringsObject)
                    {
                        int sLen = s.Length;
                        s.CopyTo(0, sb, pos, sLen);
                        pos += sLen;
                        snl.CopyTo(0, sb, pos, nc);
                        pos += nc;
                    }
                }
            }
        }

        static void ShowOnceUserMessageInOtherThread(string addMes)
        {
            string m = string.IsNullOrWhiteSpace(addMes) ? LogRefreshedMessage : addMes;

            new Thread(() =>
            {
                try
                {
                    MessageBox.Show(m, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {
                    // ignored
                }
            })
            {
                Name = @"MessageThread"
            }.Start();
        }

        /// <summary>
        ///     Строка сообщения.
        /// </summary>
        /// <remarks>
        ///     Призвана сократить длину строк в коде.
        /// </remarks>
        const string LogRefreshedMessage = @"Содержимое лог-файла обновлено. Есть новые сообщения.";
    }
}

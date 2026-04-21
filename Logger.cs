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
        public enum LogLevel
        {
            INFO,
            DEBUG,
            ERROR
        }

        class IntStorage
        {
            volatile int _usersCount;

            public void IncUserCount() => Interlocked.Increment(ref _usersCount);

            public void DecUserCount() => Interlocked.Decrement(ref _usersCount);

            public ConcurrentQueue<string> StringsObject { get; } = new ConcurrentQueue<string>();

            public void WaitForFree()
            {
                do
                    Thread.Sleep(WaitForFreeMillisecondsTimeout);
                while (_usersCount > 0);
            }
        }

        static IntStorage _intStorage = new IntStorage();

        static ManualResetEvent _stopBackground;

        static ReaderWriterLockSlim _rwlSync;

        static volatile int _isStopMode = 1;

        const int LogWaitingSecondsTimeout = 2;

        const int WriteWaitingSecondsTimeout = 10;

        const int ReadWaitingSecondsTimeout = 1;

        const int WaitForFreeMillisecondsTimeout = 200;

        [ThreadStatic]
        static string _threadInfo;

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
            Deinitialize();

            try
            {
                IsStopMode = false;

                _stopBackground = new ManualResetEvent(false);
                _rwlSync = new ReaderWriterLockSlim();

                new Thread(ThreadWriteFunction)
                {
                    Name = @"LoggerThread"
                }.Start();
            }
            catch
            {
                IsStopMode = true;
                throw;
            }
        }

        public static void Deinitialize()
        {
            IsStopMode = true;
            _stopBackground?.Set();
        }

        static IntStorage GetLogStringsQueue()
        {
            if (!_rwlSync.TryEnterReadLock(ReadWaitingSecondsTimeout * 1000))
                throw new Exception();

            IntStorage storage;

            try
            {
                storage = _intStorage;
            }
            finally
            {
                _rwlSync.ExitReadLock();
            }

            storage.IncUserCount();

            return storage;
        }

        static IntStorage RenewLogStringsQueue()
        {
            IntStorage storage = new IntStorage();

            if (!_rwlSync.TryEnterWriteLock(WriteWaitingSecondsTimeout * 1000))
                throw new Exception();

            try
            {
                (_intStorage, storage) = (storage, _intStorage);
            }
            finally
            {
                _rwlSync.ExitWriteLock();
            }

            return storage;
        }

        /// <summary>
        ///     Записывает сообщение в лог-файл, в синхронном режиме.
        /// </summary>
        /// <param name="logFunc">Строка лога, которую надо записать.</param>
        /// <param name="level">Тип лога (сведения, отладка, ошибка).</param>
        /// <param name="exp">Проверяет параметр расширенного лога <see cref="Launcher.ExplicitLogEnabled"/>.</param>
        /// <remarks>
        ///     Метод потокобезопасен.
        ///     К сообщению автоматически добавляются текущие дата и время в полном формате.
        /// </remarks>
        public static void WriteLog(Func<string> logFunc = null, LogLevel level = LogLevel.INFO, bool exp = false)
        {
            if (IsStopMode)
                return;

            try
            {
                if (level != LogLevel.ERROR && level != LogLevel.INFO)
                    switch (exp)
                    {
                        case false when !Launcher.DebugLogEnabled:
                        case true when !Launcher.ExplicitLogEnabled:
                            return;
                    }

                string dt = $@"{DateTime.Now:dd.MM.yyyy HH:mm:ss.ms}";

                if (_threadInfo is null)
                {
                    string tName = Thread.CurrentThread.Name;
                    string name = string.IsNullOrEmpty(tName) ? @"<<<Unknown thread>>>" : tName;
                    _threadInfo = $@"TID = {Thread.CurrentThread.ManagedThreadId}, Thread name = {name}";
                }

                string ls = @"<EMPTY_MESSAGE>";

                if (!(logFunc is null))
                {
                    try
                    {
                        ls = logFunc();
                    }
                    catch (Exception ex)
                    {
                        ls = $@"Ошибка при формировании сообщения лога: {ex.Message}";
                    }
                }

                string logString = $@"{dt} [{_threadInfo}] [{level}] {ls}";

                IntStorage t = GetLogStringsQueue();

                try
                {
                    t.StringsObject.Enqueue(logString);
                }
                finally
                {
                    t.DecUserCount();
                }
            }
            catch (Exception ex)
            {
                IsStopMode = true;
                ShowUserMessageInOtherThread($@"Ошибка очереди логов: {ex.Message}");
            }
        }

        sealed class LogFile : IDisposable
        {
            readonly FileStream _logFileStream;

            readonly TextWriter _logFileStreamWriter;

            public LogFile()
            {
                _logFileStream = new FileStream($@"{Launcher.BaseFilePath}_{nameof(LogFile)}.log", FileMode.Append,
                    FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                _logFileStreamWriter = new StreamWriter(_logFileStream, Encoding.UTF8);
            }

            public void Write(char[] buffer) => _logFileStreamWriter.Write(buffer);

            void Close()
            {
                _logFileStreamWriter.Dispose();
                _logFileStream.Dispose();
            }

            public void Dispose()
            {
                Close();
                GC.SuppressFinalize(this);
            }

            ~LogFile()
            {
                Close();
            }
        }

        static void ThreadWriteFunction()
        {
            try
            {
                WaitHandle[] waitHandles = { _stopBackground };

                while (true)
                {
                    int r = WaitHandle.WaitAny(waitHandles, LogWaitingSecondsTimeout * 1000);

                    WriteToDisk();

                    if (r == WaitHandle.WaitTimeout)
                        continue;

                    if (r != 0)
                        ShowUserMessageInOtherThread($@"Неизвестный результат при записи логов: {r}");

                    _rwlSync?.Dispose();
                    _stopBackground?.Dispose();

                    _rwlSync = null;
                    _stopBackground = null;
                    IsStopMode = true;

                    return;
                }
            }
            catch (Exception ex)
            {
                IsStopMode = true;
                ShowUserMessageInOtherThread($@"Ошибка при записи логов: {ex.Message}");
            }
        }

        static void WriteToDisk()
        {
            IntStorage logStrings = RenewLogStringsQueue();
            logStrings.WaitForFree();

            using (LogFile lf = new LogFile())
            {
                int nc = Environment.NewLine.Length;

                int capacity = logStrings.StringsObject.Sum(str => str.Length + nc);

                if (capacity < 1)
                    return;

                char[] sb = new char[capacity];
                int pos = 0;

                foreach (string s in logStrings.StringsObject)
                {
                    int sLen = s.Length;
                    s.CopyTo(0, sb, pos, sLen);
                    pos += sLen;
                    Environment.NewLine.CopyTo(0, sb, pos, nc);
                    pos += nc;
                }

                lf.Write(sb);
            }
        }

        static void ShowUserMessageInOtherThread(string addMes)
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

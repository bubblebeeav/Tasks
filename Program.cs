// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   Представляет класс описывающий точку входа с приложение
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace FilesScan
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;

    /// <summary>
    /// Представляет класс описывающий точку входа с приложение
    /// </summary>
    public class DirectoryScanner
    {
        /// <summary>
        /// Результат сканирования файла
        /// </summary>
        private struct ScanResult
        {
            /// <summary>
            /// Имя файла 
            /// </summary>
            public string FileName;

            /// <summary>
            /// Хэш файла
            /// </summary>
            public string Hash;

            /// <summary>
            /// Результат выполнения 
            /// </summary>
            public string Res;
        }

        /// <summary>
        /// Список файлов для сканирования
        /// </summary>
        private static readonly SyncQueue<string> FilesToScan = new SyncQueue<string>();

        /// <summary>
        /// Список файлов для сканирования
        /// </summary>
        private static readonly SyncQueue<ScanResult> fileScanResults = new SyncQueue<ScanResult>();

        /// <summary>
        /// Объект синхронизации
        /// </summary>
        private static readonly object Sync = new object();

        /// <summary>
        /// Признак того, что сканирования файлов завершено
        /// </summary>
        private static volatile bool IsScanFolderCompleted = false;

        /// <summary>
        /// Сканирует заданную папку на наличие файлов 
        /// </summary>
        /// <param name="folder">
        /// папка для сканирования
        /// </param>
        private static void ScanFolder(string folder)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(folder);
                var files = directoryInfo.GetFiles();
                foreach (var fileInfo in files)
                {
                    FilesToScan.Enqueue(fileInfo.FullName);
                }

                var directories = directoryInfo.GetDirectories();
                foreach (var directory in directories)
                {
                    ScanFolder(directory.FullName);
                }
            }
            catch (UnauthorizedAccessException exception)
            {
                Console.WriteLine(string.Format("Ошибка доступа к {0}", exception));
            }
        }

        /// <summary>
        /// Рассчитывает контрольную сумму указанного файла
        /// </summary>
        /// <param name="filename">
        /// файл для которого рассчитывается контрольная сумма
        /// </param>
        /// <returns>
        /// контрольная сумма файла.
        /// </returns>
        private static string GetMd5Checksum(string filename)
        {
            using (var fs = File.OpenRead(filename))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                var checkSum = md5.ComputeHash(fs);
                fs.Close();
                var result = BitConverter.ToString(checkSum).Replace("-", string.Empty);
                return result;
            }
        }

        /// <summary>
        /// Процедура потока подсчета контрольных сумм
        /// </summary>
        private static void CalculateHashThread()
        {
            while (FilesToScan.Count > 0 || !IsScanFolderCompleted)
            {
                var filename = string.Empty;

                lock (Sync)
                {
                    if (FilesToScan.Count > 0)
                    {
                        filename = FilesToScan.Dequeue();
                    }
                }

                var Rs = string.Format("OK");
                var Hs = string.Empty;

                if (!string.IsNullOrEmpty(filename))
                {
                    Console.WriteLine(string.Format("Подсчет контрольной суммы файла {0}", filename));
                    try
                    {
                        Hs = GetMd5Checksum(filename);
                    }
                    catch (IOException exception)
                    {
                        Rs = string.Format("Ошибка открытия файла {0}: {1}", filename, exception);
                        Console.WriteLine(Rs);
                    }
                    fileScanResults.Enqueue(new ScanResult { FileName = filename, Hash = Hs, Res = Rs });
                }

                if (fileScanResults.Count == 0)
                {
                    Thread.Sleep(100);
                }
            }
            Console.WriteLine("Подсчет контрольных сумм окончен");

        }

        /// <summary>
        /// Процедура потока сохранения результатов сканирования в БД
        /// </summary>
        private static void SaveToBaseThread()
        {
            var connection = new SqlConnection("Server=localhost\\SQLEXPRESS;Integrated security=SSPI;database=filesdirs");
            //var connection = new SqlConnection(@"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = C:\C:\WORK\Project1\Task2\Task2\Task2\DB\filesdirs.mdf; Integrated Security = True");
            try
            {
                connection.Open();
                while (fileScanResults.Count > 0 || !IsScanFolderCompleted)
                {
                    var trasaction = connection.BeginTransaction();
                    var fileScanResult = fileScanResults.Dequeue();
                    var query = string.Format(
                        "INSERT INTO files (filname, hashsum, result) Values('{0}','{1}','{2}')",
                        fileScanResult.FileName,
                        fileScanResult.Hash,
                        fileScanResult.Res);

                    var command = new SqlCommand(query, connection, trasaction);
                    command.ExecuteNonQuery();
                    trasaction.Commit();
                }
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }

            Console.WriteLine("Запись данных в БД окончена");
        }

        /// <summary>
        /// Точка входа в приложение
        /// </summary>
        /// <param name="args">аргументы запуска приложения</param>
        public static void Main(string[] args)
        {
            if (args == null || args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                
                throw new ArgumentException("   Не указана папка для сканирования");
            }
            var startFolder = args[0];

            // Количество поток подсчета контрольных сумм
            var сountTheads = 4;

            if (args.Length > 1)
            {
                var arg = Convert.ToInt32(args[1]);
                сountTheads = arg <= 0 ? 4 : arg;
            }

            var scanFolderThread = new Thread(
                o =>
                {
                    ScanFolder((string)o);
                    IsScanFolderCompleted = true;
                    Console.WriteLine("Сканирования окончено");
                });

            scanFolderThread.Start(startFolder);

            for (int threadIndex = 0; threadIndex < сountTheads; threadIndex++)
            {
                var thread = new Thread(CalculateHashThread);
                thread.Start();
            }
            var saveToBaseThread = new Thread(SaveToBaseThread);
            saveToBaseThread.Start();
            Console.ReadLine();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        // Задача 1: Компрессия и Декомпрессия строки
        Console.WriteLine("=== Задача 1 ===");
        string input = "aaabbcccdde";
        string compressed = Compress(input);
        string decompressed = Decompress(compressed);
        Console.WriteLine($"Исходная строка: {input}");
        Console.WriteLine($"Сжатая строка: {compressed}");
        Console.WriteLine($"Восстановленная строка: {decompressed}");

        // Задача 2: Потокобезопасный сервер
        Console.WriteLine("\n=== Задача 2 ===");
        var tasks = new List<Task>();

        // Чтение
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                Console.WriteLine($"Reader: count = {Server.GetCount()}");
            }));
        }

        // Запись
        tasks.Add(Task.Run(() => Server.AddToCount(10)));
        tasks.Add(Task.Run(() => Server.AddToCount(5)));

        Task.WaitAll(tasks.ToArray());
        Console.WriteLine($"Final count: {Server.GetCount()}");

        // Задача 3: Обработка логов
        Console.WriteLine("\n=== Задача 3 ===");
        ProcessLogs("logs.txt", "output.txt", "problems.txt");
        Console.WriteLine("Логи обработаны. Результаты в output.txt и problems.txt.");
    }

    // ==== Задача 1 ====
    static string Compress(string input)
    {
        StringBuilder sb = new();
        int count = 1;

        for (int i = 1; i <= input.Length; i++)
        {
            if (i < input.Length && input[i] == input[i - 1])
                count++;
            else
            {
                sb.Append(input[i - 1]);
                if (count > 1) sb.Append(count);
                count = 1;
            }
        }
        return sb.ToString();
    }

    static string Decompress(string compressed)
    {
        StringBuilder sb = new();
        for (int i = 0; i < compressed.Length; i++)
        {
            char ch = compressed[i];
            int j = i + 1;
            while (j < compressed.Length && char.IsDigit(compressed[j]))
                j++;

            string countStr = compressed.Substring(i + 1, j - i - 1);
            int count = string.IsNullOrEmpty(countStr) ? 1 : int.Parse(countStr);

            sb.Append(new string(ch, count));
            i = j - 1;
        }
        return sb.ToString();
    }

    // ==== Задача 2 ====
    public static class Server
    {
        private static int count = 0;
        private static ReaderWriterLockSlim rwLock = new();

        public static int GetCount()
        {
            rwLock.EnterReadLock();
            try
            {
                Thread.Sleep(50); // Симулируем задержку
                return count;
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        public static void AddToCount(int value)
        {
            rwLock.EnterWriteLock();
            try
            {
                Thread.Sleep(100); // Симулируем запись
                count += value;
                Console.WriteLine($"Writer added {value}, count now {count}");
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }
    }

    // ==== Задача 3 ====
    static void ProcessLogs(string inputPath, string outputPath, string problemsPath)
    {
        string[] lines = File.Exists(inputPath) ? File.ReadAllLines(inputPath) : Array.Empty<string>();

        var output = new List<string>();
        var problems = new List<string>();

        foreach (var line in lines)
        {
            try
            {
                string formatted = FormatLog(line);
                if (formatted != null)
                    output.Add(formatted);
                else
                    problems.Add(line);
            }
            catch
            {
                problems.Add(line);
            }
        }

        File.WriteAllLines(outputPath, output);
        File.WriteAllLines(problemsPath, problems);
    }

    static string FormatLog(string line)
    {
        // Format 1
        var match1 = Regex.Match(line, @"^(\d{2}\.\d{2}\.\d{4}) (\d{2}:\d{2}:\d{2}\.\d{3}) (\w+) (.+)$");
        if (match1.Success)
        {
            string date = DateTime.ParseExact(match1.Groups[1].Value, "dd.MM.yyyy", null).ToString("yyyy-MM-dd");
            string time = match1.Groups[2].Value;
            string level = NormalizeLevel(match1.Groups[3].Value);
            string message = match1.Groups[4].Value;

            return $"{date}\t{time}\t{level}\tDEFAULT\t{message}";
        }

        // Format 2
        var match2 = Regex.Match(line, @"^(\d{4}-\d{2}-\d{2}) (\d{2}:\d{2}:\d{2}\.\d+)\|\s*(\w+)\|.*?\|([^\|]+)\|\s*(.+)$");
        if (match2.Success)
        {
            string date = match2.Groups[1].Value;
            string time = match2.Groups[2].Value;
            string level = NormalizeLevel(match2.Groups[3].Value);
            string method = match2.Groups[4].Value.Trim();
            string message = match2.Groups[5].Value;

            return $"{date}\t{time}\t{level}\t{method}\t{message}";
        }

        return null;
    }

    static string NormalizeLevel(string input)
    {
        return input.ToUpper() switch
        {
            "INFORMATION" => "INFO",
            "WARNING" => "WARN",
            "INFO" => "INFO",
            "WARN" => "WARN",
            "ERROR" => "ERROR",
            "DEBUG" => "DEBUG",
            _ => input.ToUpper()
        };
    }
}

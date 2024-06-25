using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class DefectSummary
{
    static void Main(string[] args)
    {
        string inputFilePath = "Тесты.txt";  // Путь к входному файлу
        string outputFilePath = "DefectSummary.txt";  // Путь к выходному файлу

        // Структура данных для хранения информации о браках
        var defectData = new Dictionary<(string TestName, int DeviceNumber), Dictionary<string, int>>();

        string currentTestName = null;
        int currentDeviceNumber = -1;
        bool inTestFunctionSection = false;

        foreach (var line in File.ReadLines(inputFilePath))
        {
            // Проверка на название теста
            if (line.StartsWith("."))
            {
                currentTestName = line.Trim(',', ' ');
                inTestFunctionSection = false;
                continue;
            }

            // Проверка на начало секции TEST FUNCTION
            if (line.Trim().Equals("TEST FUNCTION"))
            {
                inTestFunctionSection = true;
                continue;
            }

            // Проверка на номер прибора
            if (line.StartsWith("    N0:"))
            {
                if (int.TryParse(line.Substring(8).Trim(), out int deviceNumber))
                {
                    currentDeviceNumber = deviceNumber;
                }
                continue;
            }

            // Обработка строк с браком в секции TEST FUNCTION
            if (currentTestName != null && currentDeviceNumber != -1 && inTestFunctionSection)
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("C") && int.TryParse(parts[i].Substring(1), out _))
                    {
                        string defectClass = parts[i];
                        var key = (currentTestName, currentDeviceNumber);

                        if (!defectData.ContainsKey(key))
                        {
                            defectData[key] = new Dictionary<string, int>();
                        }

                        if (!defectData[key].ContainsKey(defectClass))
                        {
                            defectData[key][defectClass] = 0;
                        }

                        defectData[key][defectClass]++;
                    }
                }
            }
        }

        var allDefectClasses = defectData.SelectMany(d => d.Value.Keys).Distinct().OrderBy(c => c).ToList();

        // Запись сводной таблицы в выходной файл
        using (var writer = new StreamWriter(outputFilePath))
        {
            // Запись заголовков
            writer.WriteLine("+----------------+---------------+--------------------+" + string.Join("", allDefectClasses.Select(c => "----------+")));
            writer.WriteLine("| Название теста | Номер прибора | Общее количество   |" + string.Join("", allDefectClasses.Select(c => $" {c,-8} |")));
            writer.WriteLine("+----------------+---------------+--------------------+" + string.Join("", allDefectClasses.Select(c => "----------+")));

            // Запись данных
            foreach (var entry in defectData)
            {
                var testName = entry.Key.TestName;
                var deviceNumber = entry.Key.DeviceNumber;
                var totalDefects = entry.Value.Values.Sum();
                var defectCounts = entry.Value;

                writer.Write($"| {testName,-14} | {deviceNumber,-13} | {totalDefects,-18} |");
                foreach (var defectClass in allDefectClasses)
                {
                    writer.Write($" {(defectCounts.ContainsKey(defectClass) ? defectCounts[defectClass] : 0),-8} |");
                }
                writer.WriteLine();
            }

            // Запись нижней границы таблицы
            writer.WriteLine("+----------------+---------------+--------------------+" + string.Join("", allDefectClasses.Select(c => "----------+")));
        }

        Console.WriteLine("Сводная таблица по браку записана в " + outputFilePath);
    }
}

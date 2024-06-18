using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestProcess
{
    class TestResult
    {
        public string FullTestName { get; set; }
        public List<TestDetail> TestDetails { get; set; }
    }

    class TestDetail
    {
        public string TestType { get; set; }
        public string ParameterName { get; set; }
        public bool IsFailure { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string inputFilePath = "Тесты.txt";
            string outputFilePath = "СводнаяТаблица.txt";

            var testResults = ParseTestResults(inputFilePath);

            var summary = testResults
                .SelectMany(tr => tr.TestDetails.Select(td => new
                {
                    tr.FullTestName,
                    td.TestType,
                    td.ParameterName,
                    td.IsFailure
                }))
                .GroupBy(x => new { x.FullTestName, x.TestType, x.ParameterName })
                .Select(g => new
                {
                    g.Key.FullTestName,
                    g.Key.TestType,
                    g.Key.ParameterName,
                    TotalTests = g.Count(),
                    Failures = g.Count(x => x.IsFailure),
                    Successes = g.Count(x => !x.IsFailure)
                })
                .OrderByDescending(x => x.Failures)
                .ThenBy(x => x.FullTestName)
                .ToList();

            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                writer.WriteLine("| Полное название теста       | Тип теста       | Параметр         | Всего тестов | Успешных тестов | Неуспешных тестов |");
                writer.WriteLine("|-----------------------------|-----------------|------------------|--------------|-----------------|-------------------|");
                foreach (var item in summary)
                {
                    writer.WriteLine($"| {item.FullTestName,-27} | {item.TestType,-15} | {item.ParameterName,-16} | {item.TotalTests,-12} | {item.Successes,-15} | {item.Failures,-17} |");
                }
            }

            Console.WriteLine("Обработка завершена. Результаты сохранены в файл: " + outputFilePath);
        }

        static List<TestResult> ParseTestResults(string inputFilePath)
        {
            var testResults = new List<TestResult>();
            string[] lines = File.ReadAllLines(inputFilePath);

            TestResult currentTest = null;
            string currentTestType = null;
            string currentDeviceNumber = null;

            foreach (string line in lines)
            {
                if (line.StartsWith("."))
                {
                    if (currentTest != null)
                    {
                        testResults.Add(currentTest);
                    }

                    currentTest = new TestResult
                    {
                        FullTestName = line.Trim(),
                        TestDetails = new List<TestDetail>()
                    };
                }
                else if (line.StartsWith("N0:"))
                {
                    currentDeviceNumber = line.Trim();
                    if (currentTest != null)
                    {
                        currentTest.FullTestName += $" {currentDeviceNumber}";
                    }
                }
                else if (line.StartsWith("TEST CONTACT") || line.StartsWith("TEST FUNCTION") || line.StartsWith("TEST STATICS"))
                {
                    currentTestType = line.Trim();
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4 && currentTest != null)
                    {
                        string parameterName = parts[0];
                        bool isFailure = parts[3].StartsWith("C");

                        currentTest.TestDetails.Add(new TestDetail
                        {
                            TestType = currentTestType,
                            ParameterName = parameterName,
                            IsFailure = isFailure
                        });
                    }
                }
            }

            if (currentTest != null)
            {
                testResults.Add(currentTest);
            }

            return testResults;
        }
    }
}

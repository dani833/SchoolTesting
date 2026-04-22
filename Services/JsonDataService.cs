using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SchoolTesting.Models;

namespace SchoolTesting.Services
{
    public class JsonDataService
    {
        private readonly string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private readonly string usersFile;
        private readonly string testsFolder;
        private readonly string resultsFolder;

        public JsonDataService()
        {
            usersFile = Path.Combine(dataFolder, "users.json");
            testsFolder = Path.Combine(dataFolder, "Tests");
            resultsFolder = Path.Combine(dataFolder, "Results");
            EnsureDirectories();
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(dataFolder);
            Directory.CreateDirectory(testsFolder);
            Directory.CreateDirectory(resultsFolder);
            if (!File.Exists(usersFile)) File.WriteAllText(usersFile, "[]");
        }

        public List<User> LoadUsers() =>
            JsonConvert.DeserializeObject<List<User>>(File.ReadAllText(usersFile)) ?? new List<User>();

        public void SaveUsers(List<User> users) =>
            File.WriteAllText(usersFile, JsonConvert.SerializeObject(users, Formatting.Indented));

        public void SaveTest(Test test)
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented };
            File.WriteAllText(Path.Combine(testsFolder, test.Id + ".json"), JsonConvert.SerializeObject(test, settings));
        }

        public List<Test> LoadAllTests()
        {
            var tests = new List<Test>();
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            foreach (var f in Directory.GetFiles(testsFolder, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(f);
                    var test = JsonConvert.DeserializeObject<Test>(json, settings);
                    if (test != null)
                    {
                        tests.Add(test);
                        System.Diagnostics.Debug.WriteLine($"[LOAD] Тест '{test.Title}', вопросов: {test.Questions.Count}");
                        foreach (var q in test.Questions)
                            if (q is MultipleChoiceQuestion mc)
                                System.Diagnostics.Debug.WriteLine($"[LOAD]   MC вариантов: {mc.Options.Count}");
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ERROR] {ex.Message}"); }
            }
            return tests;
        }
        public List<TestResult> LoadAllResults()
        {
            var all = new List<TestResult>();
            foreach (var f in Directory.GetFiles(resultsFolder, "student_*.json"))
                try { all.AddRange(JsonConvert.DeserializeObject<List<TestResult>>(File.ReadAllText(f)) ?? new List<TestResult>()); } catch { }
            return all;
        }
        public void DeleteTest(string testId)
        {
            var path = Path.Combine(testsFolder, testId + ".json");
            if (File.Exists(path)) File.Delete(path);
        }

        private string GetResultPath(int studentId) =>
            Path.Combine(resultsFolder, $"student_{studentId}.json");

        public List<TestResult> LoadResults(int studentId)
        {
            var path = GetResultPath(studentId);
            return File.Exists(path)
                ? JsonConvert.DeserializeObject<List<TestResult>>(File.ReadAllText(path)) ?? new List<TestResult>()
                : new List<TestResult>();
        }

        public void SaveResult(TestResult result)
        {
            var path = GetResultPath(result.StudentId);
            var results = LoadResults(result.StudentId);
            var existing = results.FirstOrDefault(r => r.TestId == result.TestId);
            if (existing != null) results.Remove(existing);
            results.Add(result);
            File.WriteAllText(path, JsonConvert.SerializeObject(results, Formatting.Indented));
        }
    }
}
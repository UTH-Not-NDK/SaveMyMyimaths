using System.Net;
using System.Text.Json;
using SaveMyMyimaths.Records;
using SaveMyMyimaths.UI;

namespace SaveMyMyimaths.Registry;

public static class Registry
{
    public static readonly CookieContainer Container = new();

    public static readonly List<TaskInfoRecord> TaskInfoRecordsList = new();

    private static readonly Dictionary<string, ProgressBar> CurrentProgresses = new();

    public static Dictionary<string, AccountInfoRecord> Accounts = new();

    public static void NewProgress(string key, string description, int taskAmount)
    {
        var newProgress = new ProgressBar(description, taskAmount);
        CurrentProgresses[key] = newProgress;
        newProgress.Start();

        newProgress.TaskCompleted += (_, _) => CurrentProgresses.Remove(key);
    }

    public static ProgressBar GetProgress(string key)
    {
        return CurrentProgresses[key];
    }

    public static void ParseAccountInfoFromFile()
    {
        if (!File.Exists(Path.Combine("storage", "account.dat")))
        {
            if (!Directory.Exists("storage")) Directory.CreateDirectory("storage");
            File.Create(Path.Combine("storage", "account.dat"));
            return;
        }

        string data = File.ReadAllText(Path.Combine("storage", "account.dat"));
        if (data == "") return;
        Accounts =
            JsonSerializer.Deserialize<Dictionary<string, AccountInfoRecord>>(Utilities.Utilities.Decrypt(data)) ??
            new Dictionary<string, AccountInfoRecord>();
    }

    public static void DumpAccountInfoToFile()
    {
        if (!File.Exists(Path.Combine("storage", "account.dat")))
        {
            if (!Directory.Exists("storage")) Directory.CreateDirectory("storage");
            File.Create(Path.Combine("storage", "account.dat"));
            return;
        }

        File.WriteAllText(Path.Combine("storage", "account.dat"),
            Utilities.Utilities.Encrypt(JsonSerializer.Serialize(Accounts)));
    }
}
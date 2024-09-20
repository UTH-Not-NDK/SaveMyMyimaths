using Newtonsoft.Json.Linq;
using SaveMyMyimaths;
using SaveMyMyimaths.Records;
using SaveMyMyimaths.Registry;
using Spectre.Console;

const string version = "v1.0.1";
TaskCompletionSource<bool> tcs = new();

AnsiConsole.MarkupLine("[bold #1abc9c]{0}[/]", Markup.Escape(File.ReadAllText(Path.Combine("storage", "logo.txt"))));
var rule = new Rule($"Ver {version} by [bold #39c5bb]UTH_OFFICIAL[/].")
{
    Justification = Justify.Left
};
AnsiConsole.Write(rule);
AnsiConsole.WriteLine();

var checkUpdate = Task.Run(async () => await CheckUpdate());

AccountInfoRecord acc;

Registry.ParseAccountInfoFromFile();
if (Registry.Accounts.Count != 0)
{
    List<string> accounts = new();

    foreach (KeyValuePair<string, AccountInfoRecord> account in Registry.Accounts) accounts.Add($"{account.Key}");

    accounts.Add("[red bold]None of them are mine![/]");

    string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title(
            "Select your account(If none is yours, select the [red bold]red[/] one)." +
            "\n[grey](Press [blue]<space>[/] to toggle account, [green]<enter>[/] to accept)[/]")
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more tasks)[/]")
        .AddChoices(accounts));

    if (!Registry.Accounts.TryGetValue(choice, out acc!)) acc = PromptAccount();
}
else
{
    acc = PromptAccount();
}

var cirno = new CanvasImage(Path.Combine("storage", "cirno.png"));

await LoginHandler.Login(acc);
await LoginHandler.FindAllTaskAsync();

if (Registry.TaskInfoRecordsList.Count == 0)
{
    AnsiConsole.Write(cirno);
    AnsiConsole.MarkupLine(
        "[#c4ecfc]All tasks are already finished! You can now pay respect to Cirno.[/]");
    AnsiConsole.MarkupLine("Press any key to exit...");
    Console.ReadKey();
    return;
}

List<string> tasks = new List<string>();
foreach (var task in Registry.TaskInfoRecordsList) tasks.Add($"{task.Name}\n\t- {task.DueDayInfo}");

List<string> requestedTasks = AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
    .Title("Unfinished Task List")
    .Required()
    .PageSize(10)
    .MoreChoicesText("[grey](Move up and down to reveal more tasks)[/]")
    .InstructionsText("[grey](Press [blue]<space>[/] to toggle a task, " +
                      "[green]<enter>[/] to accept)[/]")
    .AddChoiceGroup("All Tasks", tasks));

foreach (string task in requestedTasks)
{
    AnsiConsole.MarkupLine($"Finishing [bold]task#{requestedTasks.FindIndex(f => f == task)}[/]");
    await CrackHandler.DoTaskAsync(tasks.FindIndex(f => f == task));
}

AnsiConsole.MarkupLine("All finished!");
tcs.SetResult(true);
await checkUpdate;
AnsiConsole.MarkupLine("Press any key to exit...");
Console.ReadKey();

AccountInfoRecord PromptAccount()
{
    string schoolUsername =
        AnsiConsole.Prompt(
            new TextPrompt<string>("[underline bold]School Username[/]: ").PromptStyle("red"));
    string schoolPassword =
        AnsiConsole.Prompt(new TextPrompt<string>("[bold]School Password[/]: ").PromptStyle("red"));
    string portalUsername =
        AnsiConsole.Prompt(new TextPrompt<string>("[underline bold]Student Portal Username[/]: ")
            .PromptStyle("red"));
    string portalPassword =
        AnsiConsole.Prompt(
            new TextPrompt<string>("[bold]Student Portal Password[/]: ").PromptStyle("red"));

    return new AccountInfoRecord(schoolUsername, schoolPassword, portalUsername, portalPassword);
}

async Task CheckUpdate()
{
    string owner = "UTH-Not-NDK";
    string repo = "SaveMyMyimaths";

    using var client = new HttpClient();
    try
    {
        client.DefaultRequestHeaders.Add("User-Agent", "C# App");
        string response =
            await client.GetStringAsync($"https://api.github.com/repos/{owner}/{repo}/releases/latest");
        var latestRelease = JObject.Parse(response);
        string latestVersion = latestRelease["tag_name"]!.ToString();

        AnsiConsole.MarkupLine(
            latestVersion != version
                ? $"New version available, click [blue link=https://github.com/repos/{owner}/{repo}/releases/latest]here[/] to download."
                : "You are using the latest version.");
    }
    catch (Exception)
    {
        await tcs.Task;
        AnsiConsole.MarkupLine("[red]Unable to check update, f**king GFW.[/]");
    }
}
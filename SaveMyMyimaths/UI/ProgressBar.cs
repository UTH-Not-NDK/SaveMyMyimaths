using Spectre.Console;

namespace SaveMyMyimaths.UI;

public sealed class ProgressBar(string description, int taskAmount)
{
    private TaskCompletionSource<bool> _tcs = new();
    private string Description { get; set; } = description;

    private int TaskAmount { get; } = taskAmount;

    public event EventHandler? TaskCompleted;

    public async void Start()
    {
        await AnsiConsole.Progress().Columns(new TaskDescriptionColumn(), new PercentageColumn(),
            new SpinnerColumn(Spinner.Known.Balloon2), new ElapsedTimeColumn()).StartAsync(async ctx =>
        {
            var task = ctx.AddTask(Description);
            int taskFinished = 0;

            while (!ctx.IsFinished)
            {
                await _tcs.Task;
                taskFinished++;
                // ReSharper disable once PossibleLossOfFraction
                task.Increment(100 / TaskAmount);

                if (taskFinished == TaskAmount)
                {
                    task.Increment(100);
                    OnTaskCompleted();
                }

                task.Description = Description;
                _tcs = new TaskCompletionSource<bool>();
            }
        });
    }

    public void Update(string description)
    {
        Description = description;
        _tcs.SetResult(true);
    }

    private void OnTaskCompleted()
    {
        TaskCompleted?.Invoke(this, EventArgs.Empty);
    }
}
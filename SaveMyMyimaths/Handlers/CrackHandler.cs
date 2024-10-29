using System.Diagnostics;
using System.Text;
using System.Web;
using HtmlAgilityPack;
using SaveMyMyimaths.Records;
using Spectre.Console;

namespace SaveMyMyimaths;

public class CrackHandler
{
    private static readonly int TaskAmount = 5;

    public static async Task DoTaskAsync(int index)
    {
        var markParameters = await LoadTaskAsync(index);

        var client = new HttpClient(new HttpClientHandler { CookieContainer = Registry.Registry.Container });
        var request = new HttpRequestMessage(HttpMethod.Post, "https://app.myimaths.com/api/legacy/save/mark");
        Utilities.Utilities.AddHeaders(ref request);

        var body = Utilities.Utilities.ToQueryString(new Dictionary<string, string?>
        {
            { "taskID", markParameters.TaskId.ToString() },
            { "realID", markParameters.RealId.ToString() },
            { "q1score", markParameters.Q1Score == 0 ? null : markParameters.Q1Score.ToString() },
            { "q2score", markParameters.Q2Score == 0 ? null : markParameters.Q2Score.ToString() },
            { "q3score", markParameters.Q3Score == 0 ? null : markParameters.Q3Score.ToString() },
            { "q4score", markParameters.Q4Score == 0 ? null : markParameters.Q4Score.ToString() },
            { "sCode", markParameters.GetSCode().ToString() },
            { "studentID", markParameters.StudentId.ToString() },
            { "authToken", markParameters.AuthToken },
            { "time_spent", new Random().Next(600_000, 1_260_000).ToString() }
        });
        
        request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

        Registry.Registry.GetProgress("1").Update("Uploading marks...");
        var response = await client.SendAsync(request);
        
        Registry.Registry.GetProgress("1").Update("Finished.");
    }

    private static async Task<MarkParameterRecord> LoadTaskAsync(int index)
    {
        var markParameters = await GetMarkParameterRecordAsync(index);

        var client = new HttpClient(new HttpClientHandler { CookieContainer = Registry.Registry.Container });

        var request = (HttpContent)new StringContent(Utilities.Utilities.ToQueryString(new Dictionary<string, string?>
            {
                { "taskID", markParameters.TaskId.ToString() },
                { "realID", markParameters.RealId.ToString() },
                { "authCode", null }
            }),
            Encoding.UTF8, "application/x-www-form-urlencoded");

        Registry.Registry.GetProgress("1").Update("Authenticating...");
        var response = await client.PostAsync("https://app.myimaths.com/api/legacy/auth", request);
        Utilities.Utilities.UpdateCookie(response);

        string content = await response.Content.ReadAsStringAsync();
        markParameters = markParameters with { AuthToken = HttpUtility.ParseQueryString(content)["authToken"] };

        client = new HttpClient(new HttpClientHandler { CookieContainer = Registry.Registry.Container });
        request = new StringContent(Utilities.Utilities.ToQueryString(new Dictionary<string, string?>
        {
            { "taskID", markParameters.TaskId.ToString() },
            { "realID", markParameters.RealId.ToString() }
        }));

        Registry.Registry.GetProgress("1").Update("Updating Cookie...");
        Utilities.Utilities.UpdateCookie(await client.PostAsync("https://app.myimaths.com/api/legacy/launch", request));

        return markParameters;
    }

    private static async Task<MarkParameterRecord> GetMarkParameterRecordAsync(int index)
    {
        var client = new HttpClient(new HttpClientHandler { CookieContainer = Registry.Registry.Container });
        Registry.Registry.NewProgress("1", "Requesting task info...", TaskAmount);
        var response =
            await client.GetAsync($"https://app.myimaths.com{Registry.Registry.TaskInfoRecordsList[index].Href}");
        Utilities.Utilities.UpdateCookie(response);

        var doc = new HtmlDocument();
        doc.LoadHtml(await response.Content.ReadAsStringAsync());

        string? src =
            doc.DocumentNode.SelectSingleNode("//embed/@src").GetAttributeValue("src", string.Empty);

        Registry.Registry.GetProgress("1").Update("Parsing task info...");
        return await MarkParameterRecord.ParseFromPlayerAsync(src);
    }
}
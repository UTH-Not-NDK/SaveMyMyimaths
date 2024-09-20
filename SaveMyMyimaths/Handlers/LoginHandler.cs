using System.Text;
using System.Web;
using HtmlAgilityPack;
using SaveMyMyimaths.Records;

namespace SaveMyMyimaths;

public static class LoginHandler
{
    private static string _csrf = "";
    private static readonly int TaskAmount = 6;
    private static AccountInfoRecord _accountInfo = null!;

    public static async Task Login(AccountInfoRecord account)
    {
        _accountInfo = account;

        await SchoolLoginAsync(account.SchoolUsername, account.SchoolPassword);
        await PortalLoginAsync(account.PortalUsername, account.PortalPassword);
    }

    private static async Task SchoolLoginAsync(string username, string password)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://login.myimaths.com/login");

        Utilities.Utilities.AddHeaders(ref request);
        request.Headers.Add("Referer", "https://login.myimaths.com/login");
        request.Headers.Add("Origin", "https://login.myimaths.com");

        Registry.Registry.NewProgress("0", "Fetching CSRF token", TaskAmount);
        _csrf = await FetchCsrfAsync("https://login.myimaths.com/login", false);

        string requestString =
            "utf8=%E2%9C%93&authenticity_token=" + HttpUtility.UrlEncode(_csrf, Encoding.UTF8) +
            "&_form_generated_at=" +
            HttpUtility.UrlEncode(DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"), Encoding.UTF8) +
            "&account%5Buser_name%5D=" + HttpUtility.UrlEncode(username, Encoding.UTF8) +
            "&account%5Bpassword%5D=" + HttpUtility.UrlEncode(password, Encoding.UTF8) + "&commit=Log+in";

        request.Content = new StringContent(requestString, Encoding.UTF8, "application/x-www-form-urlencoded");

        Registry.Registry.GetProgress("0").Update("Logging in to Myimaths...");
        var response = await client.SendAsync(request);
        Utilities.Utilities.UpdateCookie(response);

        Registry.Registry.GetProgress("0").Update("Updating Cookie...");
        client = new HttpClient(new HttpClientHandler { CookieContainer = Registry.Registry.Container });
        response = await client.GetAsync("https://app.myimaths.com/myportal/library/39?login_modal=true");
        Utilities.Utilities.UpdateCookie(response);
    }


    private static async Task<string> FetchCsrfAsync(string uri, bool isCookieNeeded)
    {
        var doc = new HtmlDocument();
        var client = isCookieNeeded
            ? new HttpClient(new HttpClientHandler { CookieContainer = Registry.Registry.Container })
            : new HttpClient();

        var response = await client.GetAsync(uri);
        string responseContent = await response.Content.ReadAsStringAsync();
        doc.LoadHtml(responseContent);

        return doc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']")
            .GetAttributeValue("content", string.Empty);
    }

    private static async Task PortalLoginAsync(string portalUsername, string portalPassword)
    {
        var client = new HttpClient(new HttpClientHandler { CookieContainer = Registry.Registry.Container });
        var request = new HttpRequestMessage(HttpMethod.Post, "https://app.myimaths.com/myportal/student/authenticate");

        Utilities.Utilities.AddHeaders(ref request);
        request.Headers.Add("Referer", "https://app.myimaths.com/myportal/library/39?login_modal=true");
        request.Headers.Add("Origin", "https://app.myimaths.com");

        Registry.Registry.GetProgress("0").Update("Fetching CSRF token...");
        _csrf = await FetchCsrfAsync("https://app.myimaths.com/myportal/library/39", true);

        string requestString =
            "utf8=%E2%9C%93&authenticity_token=" + HttpUtility.UrlEncode(_csrf, Encoding.UTF8) +
            "&student%5Buser_name%5D=" + HttpUtility.UrlEncode(portalUsername, Encoding.UTF8) +
            "&student%5Bpassword%5D=" + HttpUtility.UrlEncode(portalPassword, Encoding.UTF8) + "&commit=Log+in";
        request.Content = new StringContent(requestString, Encoding.UTF8, "application/x-www-form-urlencoded");

        Registry.Registry.GetProgress("0").Update("Logging in to the student portal...");
        var response = await client.SendAsync(request);
        Utilities.Utilities.UpdateCookie(response);
    }

    public static async Task FindAllTaskAsync()
    {
        var client = new HttpClient(new HttpClientHandler { CookieContainer = Registry.Registry.Container });

        Registry.Registry.GetProgress("0").Update("Fetching unfinished tasks...");
        var response = await client.GetAsync("https://app.myimaths.com/myportal/student/my_homework");
        Utilities.Utilities.UpdateCookie(response);

        Registry.Registry.GetProgress("0").Update("Finished.");

        string responseContent = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(responseContent);

        var hrefs = doc.DocumentNode.SelectNodes("//a[@class='primary btn btn-m']");
        var infos = doc.DocumentNode.SelectNodes("//h2[@class='accordion-header']/button");

        string? studentName = doc.DocumentNode
            .SelectSingleNode("//div[@class='student-name d-flex align-items-center']/div[not(@*)]")
            .InnerText;
        Registry.Registry.Accounts.TryAdd(studentName, _accountInfo);
        Registry.Registry.DumpAccountInfoToFile();

        if (hrefs == null) return;
        //I know this is extremely not elegant, but man, I know nothing about algorithms and please forgive me. 
        for (int i = 0; i < hrefs.Count; i++)
        {
            string? href = hrefs[i].GetAttributeValue("href", string.Empty);
            string name = infos[i].InnerText.Trim().Split("\n\n\n\n")[0];
            string dueDayInfo = infos[i].InnerText.Trim().Split("\n\n\n\n")[1];

            Registry.Registry.TaskInfoRecordsList.Add(new TaskInfoRecord(href, name, dueDayInfo));
        }
    }
}
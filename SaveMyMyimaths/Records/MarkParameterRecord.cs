using System.Numerics;
using System.Web;
using System.Xml;

namespace SaveMyMyimaths.Records;

//thanks to alexwh, best puzzle hunter ever.
public record MarkParameterRecord(
    int AuthCode,
    int TaskId,
    int RealId,
    int StudentId,
    int Q1Score,
    int Q2Score,
    string? AuthToken = "")
{
    //this really should NOT be used when you have parameters obtained elsewhere except static.myimaths.com.
    //i literally mean that.
    public static async Task<MarkParameterRecord> ParseFromPlayerAsync(string src)
    {
        var client = new HttpClient(new HttpClientHandler { CookieContainer = Registry.Registry.Container });
        client.DefaultRequestHeaders.Referrer = new Uri(src);

        var srcUri = new Uri(src);
        var parameter = HttpUtility.ParseQueryString(srcUri.Query);
        var response =
            await client.GetAsync(
                HttpUtility.UrlDecode($"{parameter["assetHost"]}{parameter["contentPath"]}content.xml"));
        string content = (await response.Content.ReadAsStringAsync()).Replace("&int;", "");

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore
        };

        using var reader = XmlReader.Create(new StringReader(content), settings);
        var doc = new XmlDocument();
        doc.Load(reader);

        var manager = new XmlNamespaceManager(doc.NameTable);
        manager.AddNamespace("ns", "http://www.mymaths.co.uk/XMLSchema");

        var nodes = doc.SelectNodes("//ns:homeworkQuestion", manager);

        return new MarkParameterRecord(Convert.ToInt32(parameter["authCode"]), Convert.ToInt32(parameter["taskID"]),
            Convert.ToInt32(parameter["realID"]), Convert.ToInt32(parameter["studentID"]),
            Convert.ToInt32(nodes![0]!.Attributes!["questionmarks"]!.Value),
            Convert.ToInt32(nodes[1]!.Attributes!["questionmarks"]!.Value));
    }

    public BigInteger GetSCode()
    {
        var sCode = new BigInteger(AuthCode) * TaskId + Q1Score * 100 + Q2Score;
        sCode *= 10000;
        sCode += new BigInteger(TaskId) * TaskId;

        return sCode;
    }
}
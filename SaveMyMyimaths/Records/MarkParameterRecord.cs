using System.Numerics;
using System.Text.RegularExpressions;
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
    int Q3Score,
    int Q4Score,
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
        string content = await response.Content.ReadAsStringAsync();

        var marks = ParseFromString(content);
        
        return new MarkParameterRecord(
            Convert.ToInt32(parameter["authCode"]), 
            Convert.ToInt32(parameter["taskID"]),
            Convert.ToInt32(parameter["realID"]), 
            Convert.ToInt32(parameter["studentID"]),
            marks.Q1Score, marks.Q2Score,
            marks.Q3Score, marks.Q4Score);
    }

    public static MarkParameterRecord ParseFromString(string content)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null                   
        };
        content = Regex.Replace(content, @"&[^;]*;", string.Empty);

        using var reader = XmlReader.Create(new StringReader(content), settings);
        var doc = new XmlDocument();
        doc.Load(reader);

        var manager = new XmlNamespaceManager(doc.NameTable);
        manager.AddNamespace("ns", "http://www.mymaths.co.uk/XMLSchema");
        var nodes = doc.SelectNodes("//ns:homeworkQuestion", manager);

        int q1s = 0, q2s = 0, q3s = 0, q4s = 0;
        try
        {
            q1s = Convert.ToInt32(nodes[0]!.Attributes!["questionmarks"]!.Value);
            q2s = Convert.ToInt32(nodes[1]!.Attributes!["questionmarks"]!.Value);
            q3s = Convert.ToInt32(nodes[2]!.Attributes!["questionmarks"]!.Value);
            q4s = Convert.ToInt32(nodes[3]!.Attributes!["questionmarks"]!.Value);
        }
        catch {}

        return new MarkParameterRecord(0, 0, 0, 0, 
            q1s, q2s, q3s, q4s
        );
        
    }

    public BigInteger GetSCode()
    {
        var sCode = new BigInteger(AuthCode) * TaskId + Q1Score * 100 + Q2Score;
        sCode *= 10000;
        sCode += new BigInteger(TaskId) * TaskId;

        return sCode;
    }
}
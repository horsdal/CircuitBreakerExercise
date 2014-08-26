namespace CircuitBreakerExercise
{
  using System.Net;
  using Newtonsoft.Json.Linq;

  public class BreakingkService
  {
    public string GetMagicValue()
    {
      var jsonResult = new WebClient().DownloadString("http://breaker.cloudapp.net/christian");
      var parsedResult = JObject.Parse(jsonResult);
      return parsedResult["magicValue"].Value<int>().ToString();
    }
  }
}

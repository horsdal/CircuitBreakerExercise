namespace CircuitBreakerExercise
{
  using System;
  using System.Net;
  using System.Threading;
  using System.Threading.Tasks;
  using Newtonsoft.Json.Linq;

  public class BreakingkService
  {
    private CircuitBreaker circuitBreaker = new CircuitBreaker();
    private string failureText =  "Breaker service is experiencing temporary problems. Please come back later";

    public string GetMagicValue()
    {
      return 
        circuitBreaker.PerformRequest(
          request: this.GetMagicValueFromService,
          onFailure: _ => this.failureText,
          onNoCall: () => this.failureText);
    }

    private string GetMagicValueFromService()
    {
      var jsonResult = new WebClientWithTimeout().DownloadString("http://breaker.cloudapp.net/christian");
      var parsedResult = JObject.Parse(jsonResult);
      var magicValue = parsedResult["magicValue"].Value<int>().ToString();
      return magicValue;
    }

    class WebClientWithTimeout : WebClient
    {
      protected override WebRequest GetWebRequest(Uri address)
      {
        var req = base.GetWebRequest(address);
        req.Timeout = 10000;
        return req;
      }
    }

  }

  public class CircuitBreaker
  {
    private ICircuitBreakerState state = new ClosedState();
    private int maxCount = 2;
    private TimeSpan timeout = new TimeSpan(0, 0, 30);

    public T PerformRequest<T>(Func<T> request, Func<Exception, T> onFailure = null, Func<T> onNoCall = null)
    {
      try
      {
        return this.TryPerformRequest(request, onNoCall ?? (() => default(T)));
      }
      catch (Exception e)
      {
        return this.HandleFailedRequest(onFailure ?? (_ => default(T)) , e);
      }
    }

    private T TryPerformRequest<T>(Func<T> request, Func<T> onNoCall)
    {
      T result = this.state.AllowCall(out this.state) 
        ? request() 
        : onNoCall();
      this.state = this.state.CallSucceeded();

      return result;
    }

    private T HandleFailedRequest<T>(Func<Exception, T> onFailure, Exception e)
    {
      this.state = this.state.CallFailed();
      return onFailure(e);
    }

  }


  public interface ICircuitBreakerState
  {
    bool AllowCall(out ICircuitBreakerState state);
    ICircuitBreakerState CallSucceeded();
    ICircuitBreakerState CallFailed();
  }

  public class ClosedState : ICircuitBreakerState
  {
    private readonly int maxCount = 2;
    private int count = 0;

    bool ICircuitBreakerState.AllowCall(out ICircuitBreakerState state)
    {
      state = this;
      return true;
    }

    public ICircuitBreakerState CallSucceeded()
    {
      count = 0;
      return this;
    }

    public ICircuitBreakerState CallFailed()
    {
      count++;
      if (count >= maxCount)
        return new OpenState();
      return this;
    }
  }

  public class OpenState : ICircuitBreakerState
  {
    private readonly TimeSpan timeout = new TimeSpan(hours: 0, minutes: 0, seconds: 30);
    private readonly DateTime openUntil;

    public OpenState()
    {
      this.openUntil = DateTime.Now.Add(timeout);
    }

    bool ICircuitBreakerState.AllowCall(out ICircuitBreakerState state)
    {
      var allow = DateTime.Now > this.openUntil;
      state = allow ? (ICircuitBreakerState) new HalfOpenState() : this;
      return allow;
    }

    public ICircuitBreakerState CallSucceeded()
    {
      return this;
    }

    public ICircuitBreakerState CallFailed()
    {
      return this;
    }
  }

  public class HalfOpenState : ICircuitBreakerState
  {
    bool ICircuitBreakerState.AllowCall(out ICircuitBreakerState state)
    {
      state = this;
      return true;
    }

    public ICircuitBreakerState CallSucceeded()
    {
        return new ClosedState();
    }

    public ICircuitBreakerState CallFailed()
    {
      return new OpenState();
    }
  }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpPerfTestLib
{
    public class HttpMonster
    {
        ILog _log;

        string _url;

        string _method;

        string _data;

        bool _stop;

        DateTime _startTime;

        DateTime _stopTime;

        List<Task> _tasks;

        long _totalElapsedMilliseconds;

        long _completeCount;

        long _invokeCount;

        long _failedCount;

        int _userCount;

        LocalDataStoreSlot _httpClientSlot;
        public HttpMonster(ILog log)
        {
            _log = log;
            int worker = 0;
            int io = 0;
            ThreadPool.GetMaxThreads(out worker, out io);
            ThreadPool.SetMaxThreads(worker, io * 2);

            ServicePointManager.MaxServicePoints = 20;
            //ServicePointManager.SetTcpKeepAlive(false, 100000, 100000);
            //ServicePointManager.MaxServicePointIdleTime
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">支持http</param>
        /// <param name="method">支持GET</param>
        /// <param name="data">POST请求需要写入的数据</param>
        public void Start(string url, string method, string data, int userCount)
        {
            _url = url;
            _method = method;
            _data = data;
            _userCount = userCount;
            int processorCount = Environment.ProcessorCount;

            _tasks = new List<Task>();
            for (int i = 0; i < processorCount * 2; i++)
            {
                _tasks.Add(Task.Factory.StartNew(Run));
            }
            _startTime = DateTime.Now;
            _log.Info("started {0} {1} {2}", method, url, data);
        }

        public void LogState()
        {
            LogStateWithTime(DateTime.Now);
        }

        public void Stop()
        {
            _stop = true;
            Task.WaitAll(_tasks.ToArray());
            _stopTime = DateTime.Now;
            _log.Info("StartTime:{0}, StopTime:{1}", _startTime, _stopTime);
            LogStateWithTime(_stopTime);

        }

        private void LogStateWithTime(DateTime time)
        {
            _log.Info("log time {0}", time);
            _log.Info("invoke:{0}, complete:{1}, failed:{2}", _invokeCount, _completeCount, _failedCount);
            int second = (int)(time - _startTime).TotalSeconds;
            _log.Info("invoke per second:{0}, complete per second:{1}", _invokeCount / second, _completeCount / second);
            _log.Info("total elapsed milliseconds: {0}, avg elapsed milliseconds: {1}", _totalElapsedMilliseconds, _completeCount > 0 ? _totalElapsedMilliseconds / _completeCount : 0);
        }

        private void Run()
        {
            Thread.SetData(Thread.GetNamedDataSlot("HttpClient"), new HttpClient());
            Stopwatch watch = new Stopwatch();
            while (!_stop)
            {
                watch.Start();
                DoSend();
                watch.Stop();
                if (watch.ElapsedMilliseconds < 1000)
                    Thread.Sleep(1000 - (int)watch.ElapsedMilliseconds);
                watch.Reset();
            }
        }

        private void DoSend()
        {
            int userCountPerTask = _userCount / _tasks.Count;
            for (int i = 0; i < userCountPerTask; i++)
            {
                WithWebRequest();
            }
        }

        private void WithWebRequest()
        {
            HttpWebRequest request = CreateWebRequest();
            Interlocked.Increment(ref _invokeCount);
            try
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                request.GetResponse().Dispose();
                watch.Stop();
                OnComplete(watch);
            }
            catch
            {
                Interlocked.Increment(ref _failedCount);
            }
        }

        private void WithWebRequestEvent()
        {

            HttpWebRequest request = CreateWebRequest();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            request.BeginGetResponse(OnReceive, new RequestContext() { Request = request, Watch = watch });
            Interlocked.Increment(ref _invokeCount);
        }

        private HttpWebRequest CreateWebRequest()
        {
            HttpWebRequest request = WebRequest.CreateHttp(_url);
            request.ConnectionGroupName = _url;
            request.KeepAlive = true;
            request.Method = "GET";
            return request;
        }

        private void WithHttpClient()
        {
            try
            {
                HttpClient client = Thread.GetData(Thread.GetNamedDataSlot("HttpClient")) as HttpClient;
                Interlocked.Increment(ref _invokeCount);
                Stopwatch watch = Stopwatch.StartNew();
                client.GetAsync(_url).ContinueWith((p) =>
                {
                    watch.Stop();
                    OnComplete(watch);
                }
                );
            }
            catch
            {
                Interlocked.Increment(ref _failedCount);
            }
        }

        private void OnReceive(IAsyncResult asyncResult)
        {
            RequestContext context = asyncResult.AsyncState as RequestContext;
            try
            {
                context.Request.EndGetResponse(asyncResult).Dispose();
                context.Watch.Stop();
                OnComplete(context.Watch);
            }
            catch
            {
                Interlocked.Increment(ref _failedCount);
            }
        }

        private void OnComplete(Stopwatch watch)
        {
            Interlocked.Increment(ref _completeCount);
            Interlocked.Add(ref _totalElapsedMilliseconds, watch.ElapsedMilliseconds);
        }

        class RequestContext
        {
            public HttpWebRequest Request { get; set; }
            public Stopwatch Watch { get; set; }
        }
    }
}

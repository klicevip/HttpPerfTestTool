using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpPerfTestTool
{
    public class HttpMonster
    {
        ILog _log;

        string _url;

        string _method;

        string _data;

        HttpClient _client;

        bool _stop;

        DateTime _startTime;

        DateTime _stopTime;

        List<Task> _tasks;

        long _totalElapsedMilliseconds;

        long _completeCount;

        public HttpMonster(ILog log)
        {
            _log = log;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">支持http</param>
        /// <param name="method">支持GET，POST</param>
        /// <param name="data">POST请求需要写入的数据</param>
        public void Start(string url, string method, string data)
        {
            _url = url;
            _method = method;
            _data = data;
            _client = new HttpClient();
            int processorCount = Environment.ProcessorCount;
            _startTime = DateTime.Now;
            _tasks = new List<Task>();
            for (int i = 0; i < processorCount * 2; i++)
            {
                _tasks.Add(Task.Factory.StartNew(Run));
            }
        }

        public void Stop()
        {
            _stop = true;
            Task.WaitAll(_tasks.ToArray());
            _stopTime = DateTime.Now;
            _log.Info("StartTime:{0}, StopTime:{1}", _startTime, _stopTime);
            _log.Info("CompleteCount:{0}, TotalElapsedMilliseconds:{1}, TPS:{2}", _completeCount, _totalElapsedMilliseconds, _completeCount / ((_stopTime - _startTime).TotalSeconds));
            
        }

        private void Run()
        {
            while (!_stop)
            {
                DoSend();
            }
        }

        private async Task DoSend()
        {
            Stopwatch watch = Stopwatch.StartNew();
            await _client.GetAsync(_url).ContinueWith((p) => 
            {
                watch.Stop();
                UpdateCount(watch);
            }); ;
        }



        private void UpdateCount(Stopwatch watch)
        {
            Interlocked.Increment(ref _completeCount);
            Interlocked.Exchange(ref _totalElapsedMilliseconds, _totalElapsedMilliseconds + watch.ElapsedMilliseconds);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HttpPerfTestTool
{
    public interface ILog
    {
        void Info(string format, params object[] pars);
    }
}

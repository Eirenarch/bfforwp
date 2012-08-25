using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace BF
{
    public enum InternalInterpreterState
    {
        Uninitialized,
        Running,
        FinalStep,
        Finished
    }

    public class InterpreterData
    {
        public string Source { get; set; }
        public int SourceIndex { get; set; }
        public int TapeIndex { get; set; }
        public byte[] Tape {get; set;}
        public int[] LoopsBeginnings { get; set; }
        public int CellWindowRadius { get; set; }
        public InternalInterpreterState InternalState { get; set; }
    }
}

using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BF
{
    public enum StepType
    {
        None, //used for loops
        PointerMovedLeft,
        PointerMovedRight,
        CellIncrement,
        CellDecrement,
        Output,
        Input,
        UnmatchedClosingBracketLoopError,
        UnmatchedOpeningBracketLoopError,
        NegativeTapeIndexError,
        MaxTapeSizeExceededError,
        End
    }

    public class CellInfo
    {
        public CellInfo(int cellIndex, byte cellValue, bool isCurrent)
        {
            CellIndex = cellIndex;
            CellValue = cellValue;
            IsCurrent = isCurrent;
        }
        public int CellIndex { get; private set; }
        public byte CellValue { get; private set; }
        public bool IsCurrent { get; private set; }
    }

    public class StepInfo
    {
        public StepInfo(StepType type, int sourceIndex, CellInfo[] visibleCells)
        {
            Type = type;
            SourceIndex = sourceIndex;
            VisibleCells = visibleCells;
            if (visibleCells != null)
            {
                CurrentCellValue = (from c in visibleCells
                                    where c.IsCurrent
                                    select c).Single().CellValue;
            }
        }

        public byte CurrentCellValue { get; private set; }
        public StepType Type { get; private set; }
        public int SourceIndex { get; private set; }
        public CellInfo[] VisibleCells { get; private set; }
    }
}

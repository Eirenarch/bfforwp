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
using System.Linq;

namespace BF
{
    public class Interpreter
    {
        public const int MaxTapeSize = 1024 * 1024 * 64; //64MB 

        private readonly string source;
        private int sourceIndex;
        private int tapeIndex;
        private byte[] tape;
        private Stack<int> loopsBeginnings = new Stack<int>();
        private InternalInterpreterState internalState;

        public delegate byte InputRetriever();
        private readonly InputRetriever inputRetriever;
        public delegate void OutputCallback(byte output);
        private readonly OutputCallback outputCallback;

        public int CellWindowRadius { get; private set; }

        public Interpreter(string source, InputRetriever inputRetriever, OutputCallback outputCallback, int cellWindowRadius)
        {
            this.source = source;

            if (inputRetriever == null)
            {
                throw new ArgumentNullException("inputRetriever");
            }
            this.inputRetriever = inputRetriever;

            if (outputCallback == null)
            {
                throw new ArgumentNullException("outputCallback");
            }
            this.outputCallback = outputCallback;

            if (cellWindowRadius < 1 || cellWindowRadius > 100)
            {
                throw new ArgumentOutOfRangeException("cellWindowRadius", "Cell window radius should be between 1 and 100");
            }
            CellWindowRadius = cellWindowRadius;
            internalState = InternalInterpreterState.Uninitialized;
        }

        public Interpreter(InterpreterData data, InputRetriever inputRetriever, OutputCallback outputCallback)
            : this(data.Source, inputRetriever, outputCallback, data.CellWindowRadius)
        {
            sourceIndex = data.SourceIndex;
            internalState = data.InternalState;
            tape = data.Tape;
            tapeIndex = data.TapeIndex;
            loopsBeginnings = new Stack<int>(data.LoopsBeginnings.Reverse());
        }

        public InterpreterData GetInterpreterData()
        {
            return new InterpreterData
            {
                Source = source,
                SourceIndex = sourceIndex,
                Tape = (byte[])tape.Clone(),
                TapeIndex = tapeIndex,
                InternalState = internalState,
                CellWindowRadius = CellWindowRadius,
                LoopsBeginnings = loopsBeginnings.ToArray()
            };
        }

        public StepInfo ExecuteStep()
        {
            switch (internalState)
            {
                case InternalInterpreterState.Uninitialized:
                    sourceIndex = 0;
                    tapeIndex = 0;
                    tape = new byte[256];
                    loopsBeginnings.Clear();
                    if (String.IsNullOrEmpty(source))
                    {
                        internalState = InternalInterpreterState.Finished;
                        return new StepInfo(StepType.End, -1, null);
                    }

                    internalState = InternalInterpreterState.Running;
                    return ProcessToken();
                case InternalInterpreterState.Running:
                    sourceIndex++;
                    if (sourceIndex < source.Length)
                    {
                        return ProcessToken();
                    }

                    internalState = InternalInterpreterState.FinalStep;
                    goto lbFinalStep;
                case InternalInterpreterState.FinalStep:
                lbFinalStep:
                    internalState = InternalInterpreterState.Finished;
                    if (loopsBeginnings.Count == 0)
                    {
                        return new StepInfo(StepType.End, sourceIndex, GetVisibleCells());
                    }
                    else
                    {
                        return new StepInfo(StepType.UnmatchedOpeningBracketLoopError, loopsBeginnings.Pop() + 1, GetVisibleCells());
                    }
                case InternalInterpreterState.Finished:
                    throw new Exception("Execution has finished");
                default:
                    throw new Exception("Unexpected internal state");
            }
        }

        //My beautiful code gone because of the need to save the sate of the interpreter when Tombstoning
        //I'll leave it here as a proof that state can hide everywhere, even in the most beautiful code and we should write our programs in Haskell
        //public IEnumerable<StepInfo> Run()
        //{
        //    sourceIndex = 0;
        //    tapeIndex = 0;
        //    tape = new byte[256];
        //    loopsBeginnings.Clear();

        //    if (String.IsNullOrEmpty(source))
        //    {
        //        yield return new StepInfo(StepType.End, -1, null);
        //        yield break;
        //    }

        //    while (sourceIndex < source.Length)
        //    {
        //        yield return ProcessToken();
        //        sourceIndex++;
        //    }

        //    yield return new StepInfo(StepType.End, sourceIndex, GetVisibleCells());
        //}

        private StepInfo ProcessToken()
        {
            switch (source[sourceIndex])
            {
                case '+':
                    {
                        tape[tapeIndex]++;
                        return new StepInfo(StepType.CellIncrement, sourceIndex, GetVisibleCells());
                    }
                case '-':
                    {
                        tape[tapeIndex]--;
                        return new StepInfo(StepType.CellDecrement, sourceIndex, GetVisibleCells());
                    }
                case '[':
                    {
                        if (tape[tapeIndex] == 0)
                        {
                            int oldIndex = sourceIndex;
                            var loopEndings = new Stack<int>();
                            while (source.Length > (sourceIndex + 1))
                            {
                                sourceIndex++;
                                char c = source[sourceIndex];
                                if (c == '[')
                                {
                                    loopEndings.Push(sourceIndex - 1);
                                }
                                else if (c == ']')
                                {
                                    if (loopEndings.Count == 0)
                                    {
                                        return new StepInfo(StepType.None, sourceIndex, GetVisibleCells());
                                    }
                                    else
                                    {
                                        loopEndings.Pop();
                                    }
                                }
                            }
                            return new StepInfo(StepType.UnmatchedOpeningBracketLoopError, oldIndex, GetVisibleCells());
                        }
                        else
                        {
                            loopsBeginnings.Push(sourceIndex - 1);
                            return new StepInfo(StepType.None, sourceIndex, GetVisibleCells());
                        }
                    }
                case ']':
                    {
                        if (loopsBeginnings.Count == 0)
                        {
                            return new StepInfo(StepType.UnmatchedClosingBracketLoopError, sourceIndex, GetVisibleCells());
                        }
                        else if (tape[tapeIndex] == 0)
                        {
                            loopsBeginnings.Pop();
                            return new StepInfo(StepType.None, sourceIndex, GetVisibleCells());
                        }
                        else
                        {
                            int oldIndex = sourceIndex;
                            sourceIndex = loopsBeginnings.Pop();
                            return new StepInfo(StepType.None, oldIndex, GetVisibleCells());
                        }
                    }
                case '>':
                    {
                        tapeIndex++;
                        if (tapeIndex >= MaxTapeSize)
                        {
                            return new StepInfo(StepType.MaxTapeSizeExceededError, sourceIndex, null);
                        }
                        else if (tapeIndex >= tape.Length)
                        {
                            byte[] oldTape = tape;
                            tape = new byte[oldTape.Length * 2];
                            oldTape.CopyTo(tape, 0);
                        }

                        return new StepInfo(StepType.PointerMovedRight, sourceIndex, GetVisibleCells());
                    }
                case '<':
                    {
                        tapeIndex--;
                        if (tapeIndex < 0)
                        {
                            return new StepInfo(StepType.NegativeTapeIndexError, sourceIndex, null);
                        }
                        else
                        {
                            return new StepInfo(StepType.PointerMovedLeft, sourceIndex, GetVisibleCells());
                        }
                    }
                case '.':
                    {
                        outputCallback(tape[tapeIndex]);
                        return new StepInfo(StepType.Output, sourceIndex, GetVisibleCells());
                    }
                case ',':
                    {
                        tape[tapeIndex] = inputRetriever();
                        return new StepInfo(StepType.Input, sourceIndex, GetVisibleCells());
                    }
                default:
                    {
                        //ignore the character as comment
                        while (source.Length > (sourceIndex + 1))
                        {
                            sourceIndex++;
                            char c = source[sourceIndex];
                            if ("+-><[].,".Contains(c))
                            {
                                //call process token recursively to get the next real token
                                return ProcessToken();
                            }
                        }

                        return new StepInfo(StepType.None, sourceIndex, GetVisibleCells());
                    }
            }
        }


        public CellInfo[] GetVisibleCells()
        {
            return (from index in Enumerable.Range(tapeIndex - CellWindowRadius, CellWindowRadius * 2 + 1)
                    where index >= 0 && index < tape.Length
                    select new CellInfo(index, tape[index], index == tapeIndex)).ToArray();
        }
    }
}

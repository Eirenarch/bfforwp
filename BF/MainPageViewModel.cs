using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO.IsolatedStorage;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.Serialization;

namespace BF
{
    public enum SaveResult
    {
        Success,
        InvalidName,
        NameExists
    }

    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public string ErrorMessage { get; private set; }
    }

    public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);

    public class TapeMovedEventArgs : EventArgs
    {
        public TapeMovedEventArgs(IEnumerable<TapeCellViewModel> visibleCells)
        {
            VisibleCells = visibleCells;
        }

        public IEnumerable<TapeCellViewModel> VisibleCells { get; private set; }
    }

    public delegate void TapeMovedEventHandler(object sender, TapeMovedEventArgs e);

    public class SourceIndexChangedEventArgs : EventArgs
    {
        public SourceIndexChangedEventArgs(int sourceIndex)
        {
            SourceIndex = sourceIndex;
        }
        public int SourceIndex { get; private set; }
    }

    public delegate void SourceIndexChangedEventHandler(object sender, SourceIndexChangedEventArgs e);

    public class MainPageViewModel : ViewModelBase
    {
        /// <summary>
        /// How many tape cells to the left and to the right of the current cell should be displayed
        /// </summary>
        public const int CellWindowRadius = 3;

        public event EventHandler OnInputEnd;
        public event EventHandler OnEnd;
        public event ErrorEventHandler OnError;
        public event TapeMovedEventHandler OnTapeMovedLeft;
        public event TapeMovedEventHandler OnTapeMovedRight;
        public event TapeMovedEventHandler OnTapeInitialized;
        public event SourceIndexChangedEventHandler OnSourceIndexChanged;

        private int[] delayOptions = new[] { 0, 5, 10, 25, 50, 100, 250, 500, 1000, 2000 };
        public IEnumerable<int> DelayOptions
        {
            get { return delayOptions; }
        }

        public int Delay { get { return delayOptions[SelectedDelayIndex]; } }

        private IEnumerable<TapeCellViewModel> tapeCells;
        public IEnumerable<TapeCellViewModel> TapeCells
        {
            get { return tapeCells; }
            private set
            {
                if (tapeCells != value)
                {
                    tapeCells = value;
                    OnNotifyPropertyChanged("TapeCells");
                }
            }
        }

        private int selectedDelayIndex = 5;
        public int SelectedDelayIndex
        {
            get { return selectedDelayIndex; }
            set
            {
                if (selectedDelayIndex != value)
                {
                    selectedDelayIndex = value;
                    OnNotifyPropertyChanged("SelectedDelay");
                }
            }
        }

        private string source = "";
        public string Source
        {
            get { return source; }
            set
            {
                if (source != value)
                {
                    source = value;
                    OnNotifyPropertyChanged("Source");
                }
            }
        }

        private string inputText;
        public string InputText
        {
            get { return inputText; }
            set
            {
                if (inputText != value)
                {
                    inputText = value;
                    OnNotifyPropertyChanged("InputText");
                }
            }
        }

        private string outputText;
        public string OutputText
        {
            get { return outputText; }
            set
            {
                if (outputText != value)
                {
                    outputText = value;
                    OnNotifyPropertyChanged("OutputText");
                }
            }
        }

        public InterpreterState State { get; private set; }
        private ManualResetEvent animationEventHandler = new ManualResetEvent(true);

        public void SignalStartAnimation()
        {
            animationEventHandler.Reset();
        }

        public void SignalEndAnimation()
        {
            animationEventHandler.Set();
        }

        private Interpreter bfInterpreter;
        private int inputIndex = 0;
        private bool inputWarningFlag = false;
        /// <summary>
        /// Used when the cell value is changed to avoid rebinding the whole tape
        /// </summary>
        private TapeCellViewModel currentCell;

        //indicates whether the interpretation should be restarted
        private bool needRestart = false;

        public MainPageViewModel()
        {
        }

        public MainPageViewModel(string fileName)
        {
            fileName = App.SnippetsFolder + fileName;
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!String.IsNullOrEmpty(fileName) && storage.FileExists(fileName))
                {
                    using (var file = storage.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                    {
                        DataContractSerializer serializer = new DataContractSerializer(typeof(ProgramData));
                        var data = (ProgramData)serializer.ReadObject(file);
                        Init(data);
                    }
                }
            }
        }

        public MainPageViewModel(ProgramData data)
        {
            Init(data);
        }

        private void Init(ProgramData data)
        {
            InputText = data.Input;
            OutputText = data.Output;
            inputIndex = data.InputIndex;
            State = data.InterpreterState;
            SelectedDelayIndex = data.SelectedDelayIndex;
            Source = data.Source;
            if (data.InterpreterState != InterpreterState.Ready)
            {
                bfInterpreter = new Interpreter(data.InterpreterData, ReadInput, WriteOutput);
                needRestart = true;
            }
        }

        private object programDataLock = new object();

        public ProgramData GetProgramData()
        {
            lock (programDataLock)
            {
                InterpreterData data = null;
                if (bfInterpreter != null)
                {
                    data = bfInterpreter.GetInterpreterData();
                }
                return new ProgramData
                {
                    Input = InputText,
                    InputIndex = inputIndex,
                    InterpreterState = State,
                    Output = outputText,
                    SelectedDelayIndex = selectedDelayIndex,
                    Source = source,
                    InterpreterData = data
                };
            }
        }

        public SaveResult SaveProgram(string name)
        {
            name = name.Trim();
            if (!Regex.IsMatch(name, @"^[\w. ]{1,50}$"))
            {
                return SaveResult.InvalidName;
            }

            name = App.SnippetsFolder + name;

            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (storage.FileExists(name))
                {
                    return SaveResult.NameExists;
                }
                else
                {
                    using (var stream = new IsolatedStorageFileStream(name, FileMode.Create, storage))
                    {
                        DataContractSerializer serializer = new DataContractSerializer(typeof(ProgramData));
                        serializer.WriteObject(stream, GetProgramData());
                    }

                    return SaveResult.Success;
                }
            }
        }

        public void StartInterpretation()
        {
            Initialize();
            StartInterpretationWorkItem();
        }

        public void RestartInterpretation()
        {
            if (needRestart)
            {
                IEnumerable<TapeCellViewModel> visibleCells = GetTapeWindow(bfInterpreter.GetVisibleCells());
                InitializeTape(visibleCells);
                StartInterpretationWorkItem();
                needRestart = false;
            }
        }

        private void StartInterpretationWorkItem()
        {
            ThreadPool.QueueUserWorkItem(stateInfo =>
            {
                while (true)
                {
                    StepInfo executionStep = null;
                    lock (programDataLock)
                    {
                        executionStep = bfInterpreter.ExecuteStep();
                    }

                    animationEventHandler.WaitOne();

                    switch (executionStep.Type)
                    {
                        case StepType.None:
                            break;
                        case StepType.End:
                            goto loopEnd;
                        case StepType.PointerMovedLeft:
                            {
                                IEnumerable<TapeCellViewModel> visibleCells = GetTapeWindow(executionStep.VisibleCells);
                                if (OnTapeMovedLeft != null)
                                {
                                    OnTapeMovedLeft(this, new TapeMovedEventArgs(visibleCells));
                                }
                            } break;
                        case StepType.PointerMovedRight:
                            {
                                IEnumerable<TapeCellViewModel> visibleCells = GetTapeWindow(executionStep.VisibleCells);
                                if (OnTapeMovedRight != null)
                                {
                                    OnTapeMovedRight(this, new TapeMovedEventArgs(visibleCells));
                                }
                            } break;
                        case StepType.CellIncrement:
                        case StepType.CellDecrement:
                        case StepType.Input:
                            {
                                CellInfo oldCellInfo = currentCell.CellInfo;
                                currentCell.CellInfo = new CellInfo(oldCellInfo.CellIndex, executionStep.CurrentCellValue, oldCellInfo.IsCurrent);
                            } break;
                        case StepType.Output:
                            break;
                        case StepType.UnmatchedClosingBracketLoopError:
                            {
                                RaiseError("Error: Unmatched \"]\"!");
                            } goto loopEnd;
                        case StepType.UnmatchedOpeningBracketLoopError:
                            {
                                RaiseError("Error: Unmatched \"[\"!");
                            } goto loopEnd;
                        case StepType.NegativeTapeIndexError:
                            {
                                RaiseError("Negative cell index reached. Array cannot be expanded to the left.");
                            } goto loopEnd;
                        case StepType.MaxTapeSizeExceededError:
                            {
                                RaiseError("Not sure what it took to achieve this but your program just passed the maximum tape size of "
                                    + Interpreter.MaxTapeSize + " bytes. This is " + Math.Round(Interpreter.MaxTapeSize / (double)(1024 * 1024), 2)
                                    + " MB! The interpreter will now exit. Consider upgrading to the desktop version. :)");
                            } goto loopEnd;
                        default:
                            {
                                throw new InvalidOperationException("Unexpected step type: " + executionStep.Type);
                            }
                    }

                    if (OnSourceIndexChanged != null)
                    {
                        OnSourceIndexChanged(this, new SourceIndexChangedEventArgs(executionStep.SourceIndex));
                    }

                    Thread.Sleep(Delay);

                    if (State == InterpreterState.Cancelling)
                    {
                        State = InterpreterState.Ready;
                        break;
                    }
                }
            loopEnd:
                EndInterpretation();
            });
        }

        private void Initialize()
        {
            lock (programDataLock)
            {
                bfInterpreter = new Interpreter(Source, ReadInput, WriteOutput, CellWindowRadius);
                inputIndex = 0;
                inputWarningFlag = false;
                OutputText = "";
                //setup the tape in case the pointer is not moved
                IEnumerable<TapeCellViewModel> visibleCells = GetTapeWindow(new[] { new CellInfo(cellIndex: 0, cellValue: 0, isCurrent: true),
                                                                                    new CellInfo(cellIndex: 1, cellValue: 0, isCurrent: false),
                                                                                    new CellInfo(cellIndex: 2, cellValue: 0, isCurrent: false),
                                                                                    new CellInfo(cellIndex: 3, cellValue: 0, isCurrent: false) });
                InitializeTape(visibleCells);
                State = InterpreterState.Running;
            }
        }

        private void InitializeTape(IEnumerable<TapeCellViewModel> visibleCells)
        {
            if (OnTapeInitialized != null)
            {
                OnTapeInitialized(this, new TapeMovedEventArgs(visibleCells));
            }
        }

        public void CancelInterpretation()
        {
            State = InterpreterState.Cancelling;
        }

        private void RaiseError(string message)
        {
            if (OnError != null)
            {
                OnError(this, new ErrorEventArgs(message));
            }
        }

        private IEnumerable<TapeCellViewModel> GetTapeWindow(CellInfo[] visibleCells)
        {
            int startCellsCount = visibleCells.TakeWhile(c => !c.IsCurrent).Count();
            int endCellsCount = visibleCells.SkipWhile(c => !c.IsCurrent).Count() - 1;

            var cells = Enumerable.Repeat(new TapeCellViewModel(), Math.Max(MainPageViewModel.CellWindowRadius - startCellsCount, 0)) //pad left
                .Concat(visibleCells.Select(c => new TapeCellViewModel { CellInfo = c }))
                .Concat(Enumerable.Repeat(new TapeCellViewModel(), Math.Max(MainPageViewModel.CellWindowRadius - endCellsCount, 0))).ToArray(); //pad right

            currentCell = (from c in cells
                           where c.CellInfo != null && c.CellInfo.IsCurrent
                           select c).Single();

            TapeCells = cells;
            return cells;
        }

        private byte ReadInput()
        {

            if (inputIndex >= inputText.Length)
            {
                if (!inputWarningFlag)
                {
                    inputWarningFlag = true;
                    if (OnInputEnd != null)
                    {
                        OnInputEnd(this, EventArgs.Empty);
                    }
                }

                return 0;
            }

            char value = inputText[inputIndex];
            inputIndex++;
            //process new line as 10
            if (value == '\r' && (inputIndex + 1) >= inputText.Length && inputText[inputIndex + 1] == '\n')
            {
                inputIndex++;
                return 10;
            }
            else
            {
                return (byte)value;
            }
        }

        private void WriteOutput(byte output)
        {
            if (output == 10) //in brainfuck end of line is 10
            {
                OutputText += Environment.NewLine;
            }
            else
            {
                OutputText += (char)output;
            }
        }

        private void EndInterpretation()
        {
            State = InterpreterState.Ready;
            if (OnEnd != null)
            {
                OnEnd(this, EventArgs.Empty);
            }
            bfInterpreter = null;
        }
    }
}

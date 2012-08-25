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

namespace BF
{
    public class ProgramData
    {
        public string Source { get; set; }
        public string Input { get; set; }
        public int InputIndex { get; set; }
        public string Output { get; set; }
        public int SelectedDelayIndex { get; set; }
        public InterpreterState InterpreterState { get; set; }
        public InterpreterData InterpreterData { get; set; }
    }

    public class SnippetInfo
    {
        public ProgramData ProgramData { get; set; }
        public string Name { get; set; }
        public static SnippetInfo[] GetDefaultSnippets()
        {
            return new SnippetInfo[]
            {
                //since there is no "clear" function and the app saves the last program by default I'm adding this
                new SnippetInfo
                {
                    Name = "Empty Program",
                    ProgramData = new ProgramData
                    {
                        Input = "",
                        InputIndex = 0,
                        InterpreterData = null,
                        InterpreterState = InterpreterState.Ready,
                        SelectedDelayIndex = 5,
                        Output = "",
                        Source = ""
                    }
                },

                new SnippetInfo
                {
                    Name = "Tutorial 0 - Hello, World!",
                    ProgramData = new ProgramData
                    {
                        Input = "",
                        InputIndex = 0,
                        InterpreterData = null,
                        InterpreterState = InterpreterState.Ready,
                        SelectedDelayIndex = 5,
                        Output = "",
                        Source = 
@"This program prints ""Hello World!"" Keep in mind that this is far from the simplest program in BF You may want to check the other tutorials first

initialize counter (cell #0) to 10
+++++ +++++
use loop to set the next four cells to 70/100/30/10
[
  add  7 to cell #1
  > +++++ ++
  add 10 to cell #2
  > +++++ +++++
  add  3 to cell #3
  > +++
  add  1 to cell #4
  > +
  decrement counter (cell #0)
  <<<< -
]
print 'H'
> ++ .
print 'e'
> + .
print 'l'
+++++ ++ .
print 'l'
.
print 'o'
+++ .
print ' '
> ++ .
print 'W'
<< +++++ +++++ +++++ .
print 'o'
> .
print 'r'
+++ .
print 'l'
----- - .
print 'd'
----- --- .
print '!'
> + .
print '\n'
> .
"
                    }
                },

                new SnippetInfo
                {
                    Name = "Tutorial 1 - Basics",
                    ProgramData = new ProgramData
                    {
                        Input = "a",
                        InputIndex = 0,
                        InterpreterData = null,
                        InterpreterState = InterpreterState.Ready,
                        SelectedDelayIndex = 5,
                        Output = "",
                        Source = 
@"This program reads a single character input and then prints it three times via loop It uses all BF instructions

read input into the current cell
,
move the pointer right
>
set the value 3 into the current cell
this cell serves as loop counter
+++
start a loop
[
  decrement the loop counter
  -
  move the pointer to the left
  this is the cell where the input is stored
  <
  print the value of the current cell (the input)
  .
  move the pointer to the right
  this is the cell with the counter
  >
end the loop
when the counter reaches 0 the program will halt
if it is not 0 the execution will jump to the start of the loop
]
"
                    }
                },

                new SnippetInfo
                {
                    Name = "Tutorial 2 - Constants",
                    ProgramData = new ProgramData
                    {
                        Input = "",
                        InputIndex = 0,
                        InterpreterData = null,
                        InterpreterState = InterpreterState.Ready,
                        SelectedDelayIndex = 5,
                        Output = "",
                        Source = 
@"If you want to write number literals in BF you need to define them in BF code

the simplest way write the number with pluses
here is the number 3
+++
>

for larger numbers this is not practical therefore we use loops to define larger numbers
here is the number 27 in BF code
Note that it uses two cells
+++[>+++++++++<-]>
>
initialie another cell with the value 5
+++++
you can change the value of a cell to 0 like this
[-]
"
                    }
                },

                new SnippetInfo
                {
                    Name = "Tutorial 3 - Copy",
                    ProgramData = new ProgramData
                    {
                        Input = "",
                        InputIndex = 0,
                        InterpreterData = null,
                        InterpreterState = InterpreterState.Ready,
                        SelectedDelayIndex = 5,
                        Output = "",
                        Source = 
@"This program demonstrates how to copy the value of one cell into another

+++++
we use a loop to copy the value of y into x and a temporary cell
[
  copy the value into the target cell (x)
  >+
  copy the value into the temporary cell
  >+
  decrement the original cell (y)
  <<-
when y reaches 0 we exit the loop
]
we need to return the value into y
we get the value from the temporary cell and copy it via a loop
>>[<<+>>-]
"
                    }
                },

                new SnippetInfo
                {
                    Name = "Tutorial 4 - Addition",
                    ProgramData = new ProgramData
                    {
                        Input = "",
                        InputIndex = 0,
                        InterpreterData = null,
                        InterpreterState = InterpreterState.Ready,
                        SelectedDelayIndex = 5,
                        Output = "",
                        Source = 
@"This program demonstrates addition and substraction

x = x plus y
initialize x
+++
>
initialize y
++++
This loop increments x and a temporary variable while decrementing y
[
  increment x
  <+
  increment the temporary variable
  >>+
  decrement y
  <-
]
we need to return the value into y
we get the value from the temporary cell and copy it via a loop
>[<+>-]
see if you can figure out substraction with a similar algorithm
"
                    }
                },

                new SnippetInfo
                {
                    Name = "Tutorial 5 - Multiplication",
                    ProgramData = new ProgramData
                    {
                        Input = "",
                        InputIndex = 0,
                        InterpreterData = null,
                        InterpreterState = InterpreterState.Ready,
                        SelectedDelayIndex = 5,
                        Output = "",
                        Source = 
@"This program demonstrates multiplication

x = x * y
initialize x as 2
++
initialize y as 3
>
+++
move x into a cell 2 via loop
cell 2 will be counter for outer loop
<[>>+<<-]
move to cell 2
>>
loop as many times as the value in cell 2
the outer loop starts here
[
  move to y
  <
  add y to x and cell 3
  cell 3 will store the value of temporary
  y will serve as counter for the inner loop
  the inner loop starts here
  [
    <+
    >>>+
    <<-
  ]
  move to cell 3
  >>
  restore the value of cell 3 into y
  for the next iteration of the main loop
  [
    <<+
    >>-
  ]
  decrement cell 2 for the next iteration
  <-
]
"
                    }
                },

                new SnippetInfo
                {
                    Name = "Tutorial 6 - IF-THEN-ELSE",
                    ProgramData = new ProgramData
                    {
                        Input = "1",
                        InputIndex = 0,
                        InterpreterData = null,
                        InterpreterState = InterpreterState.Ready,
                        SelectedDelayIndex = 5,
                        Output = "",
                        Source = 
@"This program demonstrates how we can create IF/THEN/ELSE construct

read input 0 is False != 0 is True
leave the input blank to test with 0
,
initialize cell 1 with 1 for the ELSE part
>+<
THEN part starts here
[
  initialize cell 3 with 84 'T'
  >>++++++++++++[>+++++++<-]>
  print 'T' from cell 3
  .
  decrement cell 1
  prevents the ELSE part from executing
  <<-
  make sure the loop runs only once
  <[-]
]
put the pointer in cell 1
>
ELSE part starts here
[
  initialize cell 3 with 70 'F'
  >+++++++[>++++++++++<-]>
  print F
  .
  make sure the loop runs only once
  <<[-]
]
"
                    }
                }
            };
        }
    }
}

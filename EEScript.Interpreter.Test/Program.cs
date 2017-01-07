using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EEScript.Interpreter.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            EEScriptInterpreter.Execute("email", "password", "PW7hyzuIJZcEI", @"
(0:87) Whenever someone says something with {draw} in it,
   (3:149) everyplace the triggering player can see,
    (4:67) only where there is a foreground block 10,
     (5:254) place a foreground block 11.
            ");

            Console.ReadLine();
        }
    }
}

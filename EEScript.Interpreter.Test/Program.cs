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
(0:66) Whenever someone moves,
   (3:128) where the triggering player is at,
     (5:254) place a foreground block 10. 
            ");

            Console.ReadLine();
        }
    }
}

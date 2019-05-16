using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SCAMP;

namespace SCAMPCompile
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string programScript = args[0];
                int count = args.Length;
                int wrongParams = (count - 1) % 2;
                if(wrongParams>0)
                {
                    Console.WriteLine("Wrong parameters count");
                    return;
                }
                int paramcount = ((count - 1)/ 2);
                List<ProgramParam> programParams = new List<ProgramParam>();
                for(int i = 1; i <= paramcount; i++)
                {
                    var pp = new ProgramParam(args[2*i -1], args[2*i]);
                    programParams.Add(pp);
                }
                programScript = programScript.Replace("\\r\\n", "\r\n");
                ConstantList constants = new ConstantList();
                foreach(var pp in programParams)
                {
                    Constant constant = new Constant();
                    constant.Name = pp.Name;
                    constant.Value = pp.ParamStr;
                    constant.Comment = "FromParam";
                }
                var assembly = new Assembly(programScript, constants);
                var listing = assembly.GetListing();
                var programBytes = Assembly.Compile(listing);
                foreach(var b in programBytes)
                {
                    Console.Write(b.ToString("X2"));
                }
                Console.WriteLine();
            }
            catch(Exception e)
            {
                Console.WriteLine("Can't compile script: "+ e.Message);
            }
        }
    }

    public class ProgramParam
    {
        public string ParamStr;
        public string Name;
        public ProgramParam(string name, string str)
        {
            Name = name;
            ParamStr = str;
        }
    }
}

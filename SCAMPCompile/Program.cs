using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using SCAMP;

namespace SCAMPCompile
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ConstantList constants = new ConstantList();
                string programScript = null;
                //foreach(var p in args)
                //{
                //    Console.WriteLine(p);
                //}
                foreach (var p in args)
                {
                    Match m;
                    if ((m = Regex.Match(p, @"-(?<op>[fdx])(?<key>[^=]*)(?>=(?<value>.*))?")).Success)
                    {
                        var op = m.Groups[@"op"].Value;
                        var key = m.Groups[@"key"]?.Value;
                        var value = m.Groups[@"value"]?.Value;
                        switch (op)
                        {
                            case @"f":
                                if (programScript != null)
                                {
                                    throw new Exception("Duplicate program block");
                                }
                                string fname = "";
                                if (File.Exists(key + ".sasm")) fname = key + ".sasm";
                                if (File.Exists(key + ".asm")) fname = key + ".asm";
                                if (fname != "")
                                    programScript = File.ReadAllText(fname);
                                else
                                    throw new Exception("Assembler file not found");
                                break;
                            case @"d":
                                constants.Add(new Constant() { Name = key, Value = value });
                                break;
                            case @"x":
                                Device device;
                                XmlSerializer serializer = new XmlSerializer(typeof(Device));
                                try
                                {
                                    var fs = new FileStream(key+".xml", FileMode.Open);
                                    device = serializer.Deserialize(fs) as Device;
                                    foreach (var port in device.Port)
                                    {
                                        constants.Add(new Constant() { Name = port.Alias, Value = port.Address.ToString() });
                                    }
                                }
                                catch
                                {
                                    device = new Device();
                                }
                                break;
                            default:
                                throw new Exception("Unknown switch '" + op + "'");
                        }
                    }
                    else
                    {
                        if (programScript != null)
                        {
                            throw new Exception("Duplicate program block");
                        }
                        programScript = p;
                    }
                }
                programScript = programScript.Replace("\\r\\n", "\r\n");
                programScript = programScript.Replace("\\n", "\n");

                var assembly = new Assembly(programScript, constants);
                var listing = assembly.GetListing();
                var programBytes = Assembly.Compile(listing);
                foreach (var b in programBytes)
                {
                    Console.Write(b.ToString("X2"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't compile script: " + e.Message);
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

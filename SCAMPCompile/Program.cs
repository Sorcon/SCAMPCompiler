using System;
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
            string programScript = null;
            string device_str = "";
            string device_name = "";
            try
            {
                ConstantList constants = new ConstantList();

                //foreach(var p in args)
                //{
                //    Console.WriteLine(p);
                //}
                try
                {
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
                                    if (key.EndsWith(".asm") || key.EndsWith(".ASM") || key.EndsWith(".sasm") || key.EndsWith(".SASM"))
                                        fname = key;
                                    else
                                    {
                                        if (File.Exists(key + ".sasm")) fname = key + ".sasm";
                                        if (File.Exists(key + ".asm")) fname = key + ".asm";
                                    }
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
                                    try
                                    {
                                        device_name = key + ".xml";
                                        device_str = File.ReadAllText(key + ".xml");
                                        device_str.Replace("\r", "");
                                        device_str.Replace("\n", "");
                                        XmlSerializer serializer = new XmlSerializer(typeof(Device));
                                        TextReader reader = new StringReader(device_str);
                                        device = (Device)serializer.Deserialize(reader);
                                        //Console.WriteLine(device.name);
                                        //var fs = new FileStream(key + ".xml", FileMode.Open);

                                        //device = serializer.Deserialize() as Device;
                                        foreach (var port in device.Port)
                                        {
                                            constants.Add(new Constant() { Name = port.Alias, Value = port.Address.ToString() });
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        device = new Device();
                                        string errorMessage = "";
                                        errorMessage+= "Device XML serialize exception! Message:" + e.Message;
                                        Exception t = e.InnerException;
                                        do
                                        {
                                            errorMessage+=t.Message;
                                            t = t.InnerException;
                                        } while (t.InnerException != null);

                                        Console.WriteLine($"{{\"result\": -4, \"message\": \"{errorMessage}\"}}");
                                        return;

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
                } catch (Exception exc)
                {
                    Console.WriteLine($"{{\"result\": -3, \"message\": \"{exc.Message}\"}}");
                    return;
                }
                
                try
                {
                    programScript = programScript.Replace("\\r\\n", "\r\n");
                    programScript = programScript.Replace("\\n", "\n");

                    var assembly = new Assembly(programScript, constants);
                    var listing = assembly.GetListing();
                    var programBytes = Assembly.Compile(listing);

                    string binPropgram = "";
                    foreach (var b in programBytes)
                    {
                        binPropgram += b.ToString("X2");
                    }
                    Console.WriteLine($"{{\"result\": 1, \"message\":\"{binPropgram}\"}}");
                    return;
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"{{\"result\": -1, \"message\": \"{exc.Message}\"}}");
                    return;
                }
                
                
            }
            catch (Exception e)
            {
                string errorMessage = "";
                errorMessage+= "Can't compile script! " + e.Message;
                errorMessage+= "Device file name:" + device_name + " with data:" + device_str;

                Console.WriteLine($"{{\"result\": -2, \"message\": \"{errorMessage}\"}}");
                return;
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

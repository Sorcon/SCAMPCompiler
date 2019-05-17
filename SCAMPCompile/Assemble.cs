using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCAMP
{
    public class Assembly
    {

        private Opcode MatchOpcode(string code, int line_number)
        {
            Match m;
            if ((m = Regex.Match(code, @"^\s+(?<mnemonic>\w+)(?>\s+(?<parameters>.+))?$", RegexOptions.None)).Success)
            {
                var mnemonic = m.Groups["mnemonic"].Value;
                var parameters =
                    (from pp in m.Groups["parameters"].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                     select new Parameter(pp.Trim())).ToArray();
                try
                {
                    var opcode = Opcode.Parse(mnemonic, parameters);
                    opcode.SourceLineNumber = line_number;
                    return opcode;
                }
                catch (Exception e)
                {
                    throw new EAssembler(e.Message) { LineNumber = line_number };
                }
            }
            return null;
        }

        private void GetListing_AddOpcode(List<Opcode> listing, Opcode opcode)
        {
            // first pass resolve
            if (!opcode.Resolved && !(opcode is OpcodeBRA))
            {
                try
                {
                    opcode.Resolve((string name) => {
                        return Constants.GetValueByName(name);
                    });
                }
                catch { }
            }
            // optimize PSH & NIBs
            var count = listing.Count;
            int value;
            if (
                count > 0 &&
                opcode.Label == null &&
                opcode is OpcodeNIB opcode_nib && opcode_nib.Nibble.Resolved &&
                listing[count - 1] is OpcodePSH opcode_psh && opcode_psh.Resolved &&
                (value = (((int)opcode_psh.Nibble.Value & 0x0F) << 4) | ((int)opcode_nib.Nibble.Value & 0x0F)) < 64
            )
            {
                opcode_psh.Nibble = new Parameter((decimal)value);
            }
            else
            {
                opcode.Address = count;
                listing.Add(opcode);
                // add known label to consts
                if (opcode.Label != null)
                {
                    Constants.Add(new Constant() { Name = opcode.Label, Value = count.ToString() });
                }
            }
        }

        protected readonly ConstantList _Constants;

        protected readonly MacroList _Macros;

        protected readonly List<Opcode> _Body;

        public class EAssembler : Exception
        {

            public EAssembler(string message) : base(message) { }

            public int LineNumber
            {
                get; set;
            }

        }

        public Assembly(string text, ConstantList predefined = null)
        {
            _Constants = predefined ?? new ConstantList();
            _Macros = new MacroList();
            _Body = new List<Opcode>();

            Macro macro = null;
            string label = null;

            var lines = text.Replace("\r", "").Split(new char[] { '\n' });
            var line_count = lines.Length;

            // load & prepare code
            for (var line_number = 1; line_number <= line_count; line_number++)
            {
                var line = lines[line_number - 1];

                Match m;
                Opcode o;
                if ((m = Regex.Match(line, @"^(?<code>[^;]+(?<!\s))\s*(?>;\s*(?<comment>.*(?<!\s)))?\s*$", RegexOptions.None)).Success)
                {
                    var code = m.Groups["code"].Value;
                    var comment = m.Groups["comment"].Value;

                    if (String.IsNullOrWhiteSpace(code))
                        continue;

                    if (macro == null)
                    {
                        // check for label
                        if ((m = Regex.Match(code, @"^(?<label>\w+)$", RegexOptions.None)).Success)
                        {
                            if (label != null)
                                throw new EAssembler("Label can't be prepended with another label") { LineNumber = line_number };

                            label = m.Groups["label"].Value;
                        }
                        // check macro start directive
                        else if ((m = Regex.Match(code, @"^(?<alias>\w+)\s+macro$", RegexOptions.IgnoreCase)).Success)
                        {
                            if (label != null)
                                throw new EAssembler("Macro can't be prepended with label") { LineNumber = line_number };
                            var alias = m.Groups["alias"].Value;
                            if (_Macros["alias"] != null)
                                throw new EAssembler("Macro '" + alias + "' can't be redefined") { LineNumber = line_number };

                            macro = new Macro(alias);
                        }
                        // check for constant definition
                        else if (
                            (m = Regex.Match(code, @"^(?<name>\w+)\s+EQU\s+(?<value>.+)$", RegexOptions.IgnoreCase)).Success ||
                            (m = Regex.Match(code, @"^#define\s+(?<name>\w+)\s+(?<value>.+)$", RegexOptions.IgnoreCase)).Success
                        )
                        {
                            if (label != null)
                                throw new EAssembler("Constant can't be prepended with label") { LineNumber = line_number };

                            var name = m.Groups["name"].Value.TrimEnd();
                            var value = m.Groups["value"].Value.TrimEnd();

                            var constant = new Constant()
                            {
                                Name = name,
                                Value = value,
                                Comment = comment
                            };

                            _Constants.Add(constant);
                        }
                        // check for opcode
                        else if ((o = MatchOpcode(code, line_number)) != null)
                        {
                            o.Label = label;
                            _Body.Add(o);
                            label = null;
                        }
                        // unexpected
                        else
                        {
                            throw new EAssembler("Unexpected syntax")
                            {
                                Source = line,
                                LineNumber = line_number
                            };
                        }
                    }
                    else
                    {
                        // check macro end directive
                        if ((m = Regex.Match(code, @"^\s*endm$", RegexOptions.IgnoreCase)).Success)
                        {
                            _Macros.Add(macro);
                            macro = null;
                        }
                        // check for label
                        else if ((m = Regex.Match(code, @"^(?<label>\w+)$", RegexOptions.None)).Success)
                        {
                            throw new EAssembler("Labels can't be placed inside a macro (for branch instructions use relative offsets instead)") { LineNumber = line_number };
                        }
                        // check for opcode
                        else if ((o = MatchOpcode(code, line_number)) != null)
                        {
                            macro.Body.Add(o);
                        }
                        // unexpected
                        else
                        {
                            throw new EAssembler("Unexpected syntax")
                            {
                                Source = line,
                                LineNumber = line_number
                            };
                        }
                    }
                }
            }


        }

        public ConstantList Constants => _Constants;
        public MacroList Macros => _Macros;
        public List<Opcode> Body => _Body;

        public static byte[] Compile(ICollection<Opcode> listing)
        {
            var bytes = new List<byte>();
            foreach (var opcode in listing)
            {
                var value = opcode.Value;
                bytes.Add(value);
            }
            return bytes.ToArray();
        }

        public List<Opcode> GetListing()
        {
            var listing = new List<Opcode>();
            foreach (var opcode in Body)
            {
                // expand macros
                if (opcode is OpcodeMacro opcode_outer)
                {
                    var macro = Macros[opcode_outer.Mnemonic];
                    if (macro == null)
                    {
                        throw new EAssembler("Macro definition for '" + opcode.Mnemonic + "' not found") { LineNumber = opcode.SourceLineNumber };
                    }
                    var label = opcode_outer.Label;
                    var line_number = opcode_outer.SourceLineNumber;
                    foreach (var oo in macro.Body)
                    {
                        var opcode_inner = oo.Clone() as Opcode;
                        opcode_inner.Label = label;
                        opcode_inner.SourceLineNumber = line_number;
                        label = null;
                        // substitute parameters
                        var parameters_count = opcode_inner.Parameters.Length;
                        for (var i = 0; i < parameters_count; i++)
                        {
                            var parameter = opcode_inner.Parameters[i];
                            if (parameter.Resolved)
                                continue;
                            opcode_inner.Parameters[i].Raw = Regex.Replace(parameter.Raw, @"\$([0-9]+)\b", (Match m) => {
                                if (!UInt16.TryParse(m.Groups[1].Value, out UInt16 index) || index < 1 || index > opcode.Parameters.Length)
                                {
                                    throw new EAssembler("Macro '" + opcode.Mnemonic + "' doesn't have parameter '" + index + "'") { LineNumber = opcode_inner.SourceLineNumber };
                                }
                                return "(" + opcode.Parameters[index - 1].Raw + ")";
                            });
                        }
                        GetListing_AddOpcode(listing, opcode_inner);
                    }
                }
                else
                {
                    GetListing_AddOpcode(listing, opcode);
                }
            }
            // second pass resolve labels
            foreach (var opcode in listing)
            {
                try
                {
                    if (opcode is OpcodeBRA opcode_branch)
                    {
                        opcode.Resolve((string name) => {
                            var address = Calculator.Calc(Constants.GetValueByName(name), (string equ) => {
                                return Constants.GetValueByName(equ);
                            });
                            int offset = (int)address - opcode.Address - 1;
                            if (offset > 63 || offset < -64)
                            {
                                throw new EAssembler("Branch is too far") { LineNumber = opcode.SourceLineNumber };
                            }
                            return offset.ToString();
                        });
                    }
                    else
                    {
                        opcode.Resolve((string name) => {
                            return Constants.GetValueByName(name);
                        });
                    }
                }
                catch (Exception e)
                {
                    throw new EAssembler(e.Message) { LineNumber = opcode.SourceLineNumber };
                }
            }
            return listing;
        }

    }
}

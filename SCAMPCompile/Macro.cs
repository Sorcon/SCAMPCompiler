using System.Collections.Generic;

namespace SCAMP
{
    public class Macro
    {
        private readonly string _Alias;
        public string Alias => _Alias;

        public List<Opcode> _Body = new List<Opcode>();
        public List<Opcode> Body => _Body;

        public Macro(string alias)
        {
            _Alias = alias;
        }
    }

    public class MacroList : List<Macro>
    {
        public Macro this[string index]
        {
            get
            {
                foreach (var item in this)
                {
                    if (item.Alias.Equals(index))
                    {
                        return item;
                    }
                }
                return null;
            }
        }
    }
}
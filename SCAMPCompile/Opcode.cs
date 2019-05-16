using System;

namespace SCAMP {
	public abstract partial class Opcode : ICloneable {

		public class EOpcodeParseSource : Exception {

			public EOpcodeParseSource(string message) : base(message) {
			}

		}

		public int SourceLineNumber
		{
			get; set;
		} = -1;

		public string Label
		{
			get; set;
		}

		public abstract string Mnemonic
		{
			get;
		}

		public Parameter[] Parameters
		{
			get; set;
		}

		public bool Resolved
		{
			get {
				foreach (var p in Parameters) {
					if (!p.Resolved)
						return false;
				}
				return true;
			}
		}

		public abstract byte Value
		{
			get;
		}

		public int Address
		{
			get; set;
		}

		public static Opcode NewOpcode(string mnemonic) {
			switch (mnemonic.ToUpper()) {
				case "BRA":
				return new OpcodeBRA();
				case "PSH":
				return new OpcodePSH();
				case "POP":
				return new OpcodePOP();
				case "DUP":
				return new OpcodeDUP();
				case "INV":
				return new OpcodeINV();
				case "CMP":
				return new OpcodeCMP();
				case "INC":
				return new OpcodeINC();
				case "DEC":
				return new OpcodeDEC();
				case "SWP":
				return new OpcodeSWP();
				case "ADD":
				return new OpcodeADD();
				case "SUB":
				return new OpcodeSUB();
				case "AND":
				return new OpcodeAND();
				case "IOR":
				return new OpcodeIOR();
				case "XOR":
				return new OpcodeXOR();
				case "MUL":
				return new OpcodeMUL();
				case "DIV":
				return new OpcodeDIV();
				case "JMP":
				return new OpcodeJMP();
				case "FOR":
				return new OpcodeFOR();
				case "NIB":
				return new OpcodeNIB();
				case "INP":
				return new OpcodeINP();
				case "SHL":
				return new OpcodeSHL();
				case "SHR":
				return new OpcodeSHR();
				case "NOP":
				return new OpcodeNOP();
				case "CLF":
				return new OpcodeCLF();
				case "SNZ":
				return new OpcodeSNZ();
				case "SKZ":
				return new OpcodeSKZ();
				case "SKP":
				return new OpcodeSKP();
				case "SKN":
				return new OpcodeSKN();
				case "EXP":
				return new OpcodeEXP();
				case "ZAP":
				return new OpcodeZAP();
				case "MOV":
				return new OpcodeMOV();
				case "CPY":
				return new OpcodeCPY();
				case "RET":
				return new OpcodeRET();
				default:
				return new OpcodeMacro(mnemonic);
			}
		}

		public static Opcode Parse(string mnemonic, Parameter[] parameters) {
			// create opcode
			var opcode = NewOpcode(mnemonic);

			// check number of parameters & assign them
			if (opcode is OpcodeBRA opcode_branch) {
				if (parameters.Length != 1)
					throw new EOpcodeParseSource("Opcode '" + mnemonic + "' require exactly one parameter");
				opcode_branch.Offset = parameters[0];
			}
			else if (opcode is OpcodePSH opcode_push) {
				if (parameters.Length != 1)
					throw new EOpcodeParseSource("Opcode '" + mnemonic + "' require exactly one parameter");
				opcode_push.Nibble = parameters[0];
			}
			else if (opcode is OpcodeSized opcode_sized) {
				if (parameters.Length != 1)
					throw new EOpcodeParseSource("Opcode '" + mnemonic + "' require exactly one parameter");
				opcode_sized.Size = parameters[0];
			}
			else if (opcode is OpcodeNIB opcode_nib) {
				if (parameters.Length != 1)
					throw new EOpcodeParseSource("Opcode '" + mnemonic + "' require exactly one parameter");
				opcode_nib.Nibble = parameters[0];
			}
			else if (opcode is OpcodeINP opcode_inp) {
				if (parameters.Length != 2)
					throw new EOpcodeParseSource("Opcode '" + mnemonic + "' require exactly two parameters");
				opcode_inp.AddrSize = parameters[0];
				opcode_inp.Size = parameters[1];
			}
			else if (opcode is OpcodeShift opcode_shift) {
				if (parameters.Length != 1)
					throw new EOpcodeParseSource("Opcode '" + mnemonic + "' require exactly one parameter");
				opcode_shift.Count = parameters[0];
			}
			else if (opcode is OpcodeSimple) {
				if (parameters.Length > 0)
					throw new EOpcodeParseSource("Opcode '" + mnemonic + "' doesn't require parameters");
			}
			else if (opcode is OpcodeMacro opcode_macro) {
				opcode_macro.Parameters = parameters;
			}

			return opcode;
		}

		public static Opcode Parse(byte value) {
			if ((value & 0x80) == 0) {
				if ((value & 0x40) == 0) {
					// BRA
					return new OpcodeBRA() { Offset = new Parameter(value & 0x3F) };
				}
				else {
					// PSH
					return new OpcodePSH() { Nibble = new Parameter(value & 0x3F) };
				}
			}
			else {
				if ((value & 0x40) == 0) {
					// Sized
					var size = new Parameter((int)OpcodeSized.ParseOperandSize((byte)((value >> 4) & 0x03)));
					switch (value & 0x0F) {
						case 0:
						return new OpcodeFOR() { Size = size };
						case 1:
						return new OpcodeJMP() { Size = size };
						case 2:
						return new OpcodePOP() { Size = size };
						case 3:
						return new OpcodeDUP() { Size = size };
						case 4:
						return new OpcodeINV() { Size = size };
						case 5:
						return new OpcodeINC() { Size = size };
						case 6:
						return new OpcodeDEC() { Size = size };
						case 7:
						return new OpcodeSWP() { Size = size };
						case 8:
						return new OpcodeADD() { Size = size };
						case 9:
						return new OpcodeSUB() { Size = size };
						case 10:
						return new OpcodeCMP() { Size = size };
						case 11:
						return new OpcodeAND() { Size = size };
						case 12:
						return new OpcodeIOR() { Size = size };
						case 13:
						return new OpcodeXOR() { Size = size };
						case 14:
						return new OpcodeMUL() { Size = size };
						case 15:
						return new OpcodeDIV() { Size = size };
						default:
						return null;
					}
				}
				else {
					if ((value & 0x20) == 0) {
						if ((value & 0x10) == 0) {
							// NIB
							return new OpcodeNIB() { Nibble = new Parameter(value & 0x0F) };
						}
						else {
							// INP
							return new OpcodeINP() {
								AddrSize = new Parameter((value >> 2) & 0x03),
								Size = new Parameter(value & 0x03)
							};
						}
					}
					else {
						if ((value & 0x10) == 0) {
							// Shift
							var count = new Parameter(value & 7);
							if ((value & 0x08) == 0) {
								// SHL
								return new OpcodeSHL() { Count = count };
							}
							else {
								// SHR
								return new OpcodeSHL() { Count = count };
							}
						}
						else {
							// Simple
							switch (value & 0x0F) {
								case 0:
								return new OpcodeNOP();
								case 1:
								return new OpcodeCLF();
								case 2:
								return new OpcodeINF();
								case 3:
								return new OpcodeNFS();
								case 4:
								return new OpcodeNOP();
								case 5:
								return new OpcodeNOP();
								case 6:
								return new OpcodeNOP();
								case 7:
								return new OpcodeSNZ();
								case 8:
								return new OpcodeSKZ();
								case 9:
								return new OpcodeSKP();
								case 10:
								return new OpcodeSKN();
								case 11:
								return new OpcodeNOP();
								case 12:
								return new OpcodeZAP();
								case 13:
								return new OpcodeMOV();
								case 14:
								return new OpcodeCPY();
								case 15:
								return new OpcodeRET();
								default:
								return null;
							}
						}
					}
				}
			}
		}

		public void Resolve(Calculator.ValueResolveCallBack call_back) {
			foreach (var p in Parameters) {
				p.Resolve(call_back);
			}
		}
		public override string ToString() {
			return Address.ToString("X4") + " " + Mnemonic + " " + String.Join<Parameter>(", ", Parameters);
		}
		public object Clone() {
			var result = MemberwiseClone() as Opcode;
			var parameters_count = Parameters.Length;
			result.Parameters = new Parameter[parameters_count];
			for (var i = 0; i < parameters_count; i++) {
				result.Parameters[i] = Parameters[i].Clone() as Parameter;
			}
			return result;
		}

	}

	public class OpcodeBRA : Opcode {

		public OpcodeBRA() {
			Parameters = new Parameter[1];
		}

		public Parameter Offset
		{
			get {
				return Parameters[0];
			}
			set {
				Parameters[0] = value;
			}
		}

		public override byte Value
		{
			get {
				var offset = (int)Offset.Value;
				if (offset < -32 || offset > 31) {
					throw new Exception("Branch offset is too far use JMP instead");
				}
				return (byte)(0x00 | (offset & 0x3F));
			}
		}

		public override string Mnemonic => "BRA";

	}

	public class OpcodePSH : Opcode {

		public OpcodePSH() {
			Parameters = new Parameter[1];
		}

		public Parameter Nibble
		{
			get {
				return Parameters[0];
			}
			set {
				Parameters[0] = value;
			}
		}

		public override byte Value => (byte)(0x40 | ((int)Nibble.Value & 0x3F));

		public override string Mnemonic => "PSH";

	}

	public abstract class OpcodeSized : Opcode {

		public OpcodeSized() {
			Parameters = new Parameter[1];
		}

		public enum OperandSize {
			Byte,
			Word,
			DWord,
			Undefined
		}

		protected abstract int Subcode
		{
			get;
		}

		public Parameter Size
		{
			get {
				return Parameters[0];
			}
			set {
				Parameters[0] = value;
			}
		}

		public override byte Value => (byte)(0x80 | (((int)Size.Value & 0x03) << 4) | (Subcode & 0x0F));

		public static OperandSize ParseOperandSize(byte value) {
			switch (value & 3) {
				case 0:
				return OperandSize.Byte;

				case 1:
				return OperandSize.Word;

				case 2:
				return OperandSize.DWord;

				default:
				return OperandSize.Undefined;
			}
		}

	}

	public class OpcodePOP : OpcodeSized {
		public override string Mnemonic => "POP";
		protected override int Subcode => 2;
	}

	public class OpcodeDUP : OpcodeSized {
		public override string Mnemonic => "DUP";
		protected override int Subcode => 3;
	}

	public class OpcodeINV : OpcodeSized {
		public override string Mnemonic => "INV";
		protected override int Subcode => 4;
	}

	public class OpcodeINC : OpcodeSized {
		public override string Mnemonic => "INC";
		protected override int Subcode => 5;
	}

	public class OpcodeDEC : OpcodeSized {
		public override string Mnemonic => "DEC";
		protected override int Subcode => 6;
	}

	public class OpcodeSWP : OpcodeSized {
		public override string Mnemonic => "SWP";
		protected override int Subcode => 7;
	}

	public class OpcodeADD : OpcodeSized {
		public override string Mnemonic => "ADD";
		protected override int Subcode => 8;
	}

	public class OpcodeSUB : OpcodeSized {
		public override string Mnemonic => "SUB";
		protected override int Subcode => 9;
	}

	public class OpcodeCMP : OpcodeSized {
		public override string Mnemonic => "CMP";
		protected override int Subcode => 10;
	}

	public class OpcodeAND : OpcodeSized {
		public override string Mnemonic => "AND";
		protected override int Subcode => 11;
	}

	public class OpcodeIOR : OpcodeSized {
		public override string Mnemonic => "IOR";
		protected override int Subcode => 12;
	}

	public class OpcodeXOR : OpcodeSized {
		public override string Mnemonic => "XOR";
		protected override int Subcode => 13;
	}

	public class OpcodeMUL : OpcodeSized {
		public override string Mnemonic => "MUL";
		protected override int Subcode => 14;
	}

	public class OpcodeDIV : OpcodeSized {
		public override string Mnemonic => "DIV";
		protected override int Subcode => 15;
	}

	public class OpcodeJMP : OpcodeSized {
		public override string Mnemonic => "JMP";
		protected override int Subcode => 1;
	}

	public class OpcodeFOR : OpcodeSized {
		public override string Mnemonic => "FOR";
		protected override int Subcode => 0;
	}

	public class OpcodeNIB : Opcode {
		public OpcodeNIB() {
			Parameters = new Parameter[1];
		}
		public Parameter Nibble
		{
			get {
				return Parameters[0];
			}
			set {
				Parameters[0] = value;
			}
		}
		public override byte Value => (byte)(0xC0 | ((int)Nibble.Value & 0x0F));
		public override string Mnemonic => "NIB";
	}

	public class OpcodeINP : Opcode {
		public OpcodeINP() {
			Parameters = new Parameter[2];
		}

		public enum AddressSize {
			A8,
			A16,
			A32,
			Undefined
		}

		public Parameter Size
		{
			get {
				return Parameters[0];
			}
			set {
				Parameters[0] = value;
			}
		}

		public Parameter AddrSize
		{
			get {
				return Parameters[1];
			}
			set {
				Parameters[1] = value;
			}
		}

		public override byte Value => (byte)(0xD0 | (((int)AddrSize.Value & 0x03) << 2) | ((int)Size.Value & 0x03));

		public override string Mnemonic => "INP";
	}

	public abstract class OpcodeShift : Opcode {
		public OpcodeShift() {
			Parameters = new Parameter[1];
		}
		public Parameter Count
		{
			get {
				return Parameters[0];
			}
			set {
				Parameters[0] = value;
			}
		}
	}

	public class OpcodeSHL : OpcodeShift {
		public override byte Value => (byte)(0xE0 | ((int)Count.Value & 7));
		public override string Mnemonic => "SHL";
	}

	public class OpcodeSHR : OpcodeShift {
		public override byte Value => (byte)(0xE8 | ((int)Count.Value & 7));
		public override string Mnemonic => "SHR";
	}

	public abstract class OpcodeSimple : Opcode {
		public OpcodeSimple() {
			Parameters = new Parameter[0];
		}
		protected abstract int Subcode
		{
			get;
		}
		public override byte Value => (byte)(0xF0 | (Subcode & 0x0F));
	}

	public class OpcodeNOP : OpcodeSimple {
		public override string Mnemonic => "NOP";
		protected override int Subcode => 0;
	}

	public class OpcodeCLF : OpcodeSimple {
		public override string Mnemonic => "CLF";
		protected override int Subcode => 1;
	}

	public class OpcodeINF : OpcodeSimple {
		public override string Mnemonic => "INF";
		protected override int Subcode => 2;
	}

	public class OpcodeNFS : OpcodeSimple {
		public override string Mnemonic => "NFS";
		protected override int Subcode => 3;
	}

	public class OpcodeSNZ : OpcodeSimple {
		public override string Mnemonic => "SNZ";
		protected override int Subcode => 7;
	}

	public class OpcodeSKZ : OpcodeSimple {
		public override string Mnemonic => "SKZ";
		protected override int Subcode => 8;
	}

	public class OpcodeSKP : OpcodeSimple {
		public override string Mnemonic => "SKP";
		protected override int Subcode => 9;
	}

	public class OpcodeSKN : OpcodeSimple {
		public override string Mnemonic => "SKN";
		protected override int Subcode => 10;
	}

	public class OpcodeEXP : OpcodeSimple {
		public override string Mnemonic => "EXP";
		protected override int Subcode => 11;
	}

	public class OpcodeZAP : OpcodeSimple {
		public override string Mnemonic => "ZAP";
		protected override int Subcode => 12;
	}

	public class OpcodeMOV : OpcodeSimple {
		public override string Mnemonic => "MOV";
		protected override int Subcode => 13;
	}

	public class OpcodeCPY : OpcodeSimple {
		public override string Mnemonic => "CPY";
		protected override int Subcode => 14;
	}

	public class OpcodeRET : OpcodeSimple {
		public override string Mnemonic => "RET";
		protected override int Subcode => 15;
	}

	public class OpcodeMacro : Opcode {

		private readonly string _Mnemonic;

		public OpcodeMacro(string mnemonic) {
			_Mnemonic = mnemonic;
		}

		public override string Mnemonic => _Mnemonic;

		public override byte Value => throw new NotSupportedException("Macro should not be put inside one another");

	}
}
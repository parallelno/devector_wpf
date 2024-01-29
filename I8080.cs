// Intel 8080 (soviet analog KR580VM80A) microprocessor core model
//
// credints
// https://github.com/superzazu/8080/blob/master/i8080.c
// https://github.com/amensch/e8080/blob/master/e8080/Intel8080/i8080.cs
// https://github.com/svofski/vector06sdl/blob/master/src/i8080.cpp

// Vector06 cpu timings:
// every Vector06c instruction consists of one to six machine cycles
// every Vector06c machine cycle consists of four active states aften called t-states (T1, T2, etc)
// each t-state triggered by 3 Mhz clock clock

using System;
using static devector.Debugger;
using static devector.Memory;

namespace devector
{
	public class I8080
	{
		private delegate void InstructionAction(); // each instruction action has to handle all the machine cycles for each instruction. when the execution finished, the output is INSTR_EXECUTED
		private static InstructionAction[] actions = new InstructionAction[0x100];

		public UInt64 cc; // clock cycles. it's the debug related data
		public UInt16 pc, sp; // program counter, stack pointer
		public byte a, b, c, d, e, h, l; // registers
		public byte instruction_register; // an internal register that stores the fetched instruction

		// Arithmetic and Logic Unit (ALU)
		public byte TMP;    // an 8-bit temporary register
		public byte ACT;    // an 8-bit temporary accumulator
		public byte W;      // an 8-bit temporary hi addr
		public byte Z;      // an 8-bit temporary low addr
		public bool flag_s; // sign
		public bool flag_z; // zero
		public bool flag_ac;// auxiliary carry (half-carry)
		public bool flag_p; // parity
		public bool flag_c; // carry
		public bool unused_flaf_1; // unused, always 1 in Vector06c
		public bool unused_flaf_3; // unused, always 0 in Vector06c
		public bool unused_flaf_5; // unused, always 0 in Vector06c

		public int machine_cycle; // a machine cycle index of the currently executed instruction
		const UInt64 MACHINE_CC = 4; // a number of clock cycles one machine cycle takes
		public const int INSTR_EXECUTED = 0; // machine_cycle index indicating the instruction executon is over

		// interruption
		public bool INTE; // set if an iterrupt enabled
		public bool IFF; // set by the 50 Hz interruption timer. it is ON until an iterruption call (RST7)
		public bool HLTA; // indicates that HLT instruction is executed
		public bool ei_pending; // if set, the interruption call is pending until the next instruction
		const byte OPCODE_RST7 = 0xff;
		const byte OPCODE_HLT = 0x76;

		// memory + io interface
		public delegate byte MemoryReadDelegate(uint addr, Memory.AddrSpace addr_space = Memory.AddrSpace.RAM);
		public delegate void MemoryWriteDelegate(uint addr, byte value, AddrSpace addr_space = Memory.AddrSpace.RAM);
		public delegate byte InputDelegate(byte port);
		public delegate void OutputDelegate(byte port, byte value);
		public delegate void DebugMemAccessDelegate(uint addr, MemAccess mem_access, AddrSpace addr_space = AddrSpace.RAM);

		MemoryReadDelegate memory_read;
		MemoryWriteDelegate memory_write;
		InputDelegate input;
		OutputDelegate output;
		DebugMemAccessDelegate debug_mem_access;

		public I8080(
			MemoryReadDelegate _memory_read,
			MemoryWriteDelegate _memory_write,
			InputDelegate _input,
			OutputDelegate _output,
			DebugMemAccessDelegate _debug_mem_access)
		{
			memory_read = _memory_read;
			memory_write = _memory_write;
			input = _input;
			output = _output;
			debug_mem_access = _debug_mem_access;

			unused_flaf_1 = true;
			unused_flaf_3 = false;
			unused_flaf_5 = false;

			init();
		}

		public void init()
		{
			cc = pc = sp = 0;
			a = b = c = d = e = h = l = instruction_register = TMP = ACT = W = Z = 0;
			flag_s = flag_z = flag_ac = flag_p = flag_c = INTE = false;

			machine_cycle = 0;
			HLTA = INTE = IFF = ei_pending = false;

			incode_actions();
		}

		public void execute_machine_cycle(bool T50HZ)
		{
			IFF |= T50HZ & INTE;

			if (machine_cycle == 0)
			{
				// interrupt processing
				if (IFF && !ei_pending)
				{
					INTE = false;
					IFF = false;
					HLTA = false;
					instruction_register = OPCODE_RST7;
				}
				// normal instruction execution
				else
				{
					if (instruction_register == OPCODE_HLT)
					{
						pc--; // move the program counter back if the last instruction was HLT
					}

					ei_pending = false;
					instruction_register = read_instr_move_pc();
				}
			}

			decode();
			cc += MACHINE_CC;
		}

		void decode()
		{
			actions[instruction_register]();
            machine_cycle++;
			machine_cycle %= M_CYCLES[instruction_register];
		}

		// an instruction execution time in macine cycles
		static readonly int[] M_CYCLES = new int[] {
		//  0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F
			1, 3, 2, 2, 2, 2, 2, 1, 1, 3, 2, 2, 2, 2, 2, 1, // 0
			1, 3, 2, 2, 2, 2, 2, 1, 1, 3, 2, 2, 2, 2, 2, 1, // 1
			1, 3, 5, 2, 2, 2, 2, 1, 1, 3, 5, 2, 2, 2, 2, 1, // 2
			1, 3, 4, 2, 3, 3, 3, 1, 1, 3, 4, 2, 2, 2, 2, 1, // 3

			2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 4
			2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 5
			2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 6
			2, 2, 2, 2, 2, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 7

			1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 1, // 8
			1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 1, // 9
			1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 1, // A
			1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 1, // B

			4, 3, 3, 3, 6, 4, 2, 4, 4, 3, 3, 3, 6, 6, 2, 4, // C
			4, 3, 3, 3, 6, 4, 2, 4, 4, 3, 3, 3, 6, 6, 2, 4, // D
			4, 3, 3, 6, 6, 4, 2, 4, 4, 2, 3, 1, 6, 6, 2, 4, // E
			4, 3, 3, 1, 6, 4, 2, 4, 4, 2, 3, 1, 6, 6, 2, 4  // F
		};

		//===============================================================================
		//
		// memory helpers
		//
		//===============================================================================

		byte read_instr_move_pc()
		{
			debug_mem_access(pc, Debugger.MemAccess.RUN);
			byte op_code = memory_read(pc, AddrSpace.RAM);
			pc++;
			return op_code;
		}

		byte read_byte(uint addr, AddrSpace addr_space = AddrSpace.RAM)
		{
			debug_mem_access(addr, Debugger.MemAccess.READ, addr_space);
			return memory_read(addr, addr_space);
		}

		void write_byte(uint addr, byte value, AddrSpace addr_space = AddrSpace.RAM)
		{
			memory_write(addr, value, addr_space);
			debug_mem_access(addr, Debugger.MemAccess.WRITE, addr_space);
		}

		byte read_byte_move_pc(Memory.AddrSpace addr_space = Memory.AddrSpace.RAM)
		{
			var result = read_byte(pc, addr_space);
			pc++;
			return result;
		}

		#region register helpers
		//===============================================================================
		//
		// registers helpers
		//
		//===============================================================================

		public byte i8080_get_flags()
		{
			int psw = 0;
			psw |= flag_s ? 1 << 7 : 0;
			psw |= flag_z ? 1 << 6 : 0;
			psw |= flag_ac ? 1 << 4 : 0;
			psw |= flag_p ? 1 << 2 : 0;
			psw |= flag_c ? 1 << 0 : 0;

			psw |= unused_flaf_1 ? 1 << 1 : 0;
			psw |= unused_flaf_3 ? 1 << 1 : 0;
			psw |= unused_flaf_5 ? 1 << 1 : 0;
			return (byte)psw;
		}

		public UInt16 i8080_get_af()
		{
			return (UInt16)(a << 8 | i8080_get_flags());
		}

		public void i8080_set_flags(byte psw)
		{
			flag_s = ((psw >> 7) & 1) == 1;
			flag_z = ((psw >> 6) & 1) == 1;
			flag_ac = ((psw >> 4) & 1) == 1;
			flag_p = ((psw >> 2) & 1) == 1;
			flag_c = ((psw >> 0) & 1) == 1;

			unused_flaf_1 = true;
			unused_flaf_3 = false;
			unused_flaf_5 = false;
		}

		public UInt16 i8080_get_bc() => (UInt16)((b << 8) | c);

		public void i8080_set_bc(UInt16 val)
		{
			b = (byte)(val >> 8);
			c = (byte)(val & 0xFF);
		}

		public UInt16 i8080_get_de() => (UInt16)((d << 8) | e);

		public void i8080_set_de(UInt16 val)
		{
			d = (byte)(val >> 8);
			e = (byte)(val & 0xFF);
		}

		public UInt16 i8080_get_hl() => (UInt16)((h << 8) | l);

		public void i8080_set_hl(UInt16 val)
		{
			h = (byte)(val >> 8);
			l = (byte)(val & 0xFF);
		}
		#endregion

		#region instruction helpers
		//===============================================================================
		//
		// instruction helpers
		//
		//===============================================================================

		static readonly bool[] parity_table = new bool[]
		{
			true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
			false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
			false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
			true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
			false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
			true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
			true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
			false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
			false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
			true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
			true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
			false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
			true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
			false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
			false, true, true, false, true, false, false, true, true, false, false, true, false, true, true, false,
			true, false, false, true, false, true, true, false, false, true, true, false, true, false, false, true,
		};
		// returns the parity of a byte: 0 if a number of set bits of `val` is odd, else 1
		bool get_parity(byte val)
		{
			return parity_table[val];
		}
		// determines if there was a carry between bit 'bit_no' and 'bit_no - 1' during the calculation of 'a + b + cy'.
		bool get_carry(int bit_no, byte _a, byte _b, bool _cy)
		{
			int result = _a + _b + (_cy ? 1 : 0);
			int carry = result ^ _a ^ _b;
			return (carry & (1 << bit_no)) != 0;
		}

		void set_z_s_p(byte val)
		{
			flag_z = val == 0;
			flag_s = (val >> 7) == 1;
			flag_p = get_parity(val);
		}

		// rotate register A left
		void rlc()
		{
			flag_c = a >> 7 == 1;
			a = (byte)(a << 1);
			a += (byte)(flag_c ? 1 : 0);
		}

		// rotate register A right
		void rrc()
		{
			flag_c = (a & 1) == 1;
			a = (byte)(a >> 1);
			a |= (byte)(flag_c ? 1 << 7 : 0);
		}

		// rotate register A left with the carry flag
		void ral()
		{
			bool cy = flag_c;
			flag_c = a >> 7 == 1;
			a = (byte)(a << 1);
			a |= (byte)(cy ? 1 : 0);
		}

		// rotate register A right with the carry flag
		void rar()
		{
			bool cy = flag_c;
			flag_c = (a & 1) == 1;
			a = (byte)(a >> 1);
			a |= (byte)(cy ? 1 << 7 : 0);
		}

		void mov_r_r(ref byte ddd, byte sss)
		{
			if (machine_cycle == 0)
			{
				TMP = sss;
			}
			else
			{
				ddd = TMP;
			}
		}

		void load_r_p(ref byte ddd, UInt16 addr)
		{
			if (machine_cycle == 1)
			{
				ddd = read_byte(addr);
			}
		}

		void mov_m_r(byte sss)
		{
			if (machine_cycle == 0)
			{
				TMP = sss;
			}
			else
			{
				write_byte(i8080_get_hl(), TMP);
			}
		}

		void mvi_r_d(ref byte ddd)
		{
			if (machine_cycle == 1)
			{
				ddd = read_byte_move_pc();
			}
		}

		void mvi_m_d()
		{
			if (machine_cycle == 1)
			{
				TMP = read_byte_move_pc();
			}
			else if (machine_cycle == 2)
			{
				write_byte(i8080_get_hl(), TMP);
			}
		}

		void lda()
		{
			if (machine_cycle == 1)
			{
				Z = read_byte_move_pc();
			}
			else if (machine_cycle == 2)
			{
				W = read_byte_move_pc();
			}
			else if (machine_cycle == 3)
			{
				a = read_byte((UInt16)(W << 8 | Z));
			}
		}

		void sta()
		{
			if (machine_cycle == 1)
			{
				Z = read_byte_move_pc();
			}
			else if (machine_cycle == 2)
			{
				W = read_byte_move_pc();
			}
			else if (machine_cycle == 3)
			{
				write_byte((UInt16)(W << 8 | Z), a);
			}
		}

		void stax(UInt16 addr)
		{
			if (machine_cycle == 1)
			{
				write_byte(addr, a);
			}
		}

		void lxi(ref byte hb, ref byte lb)
		{
			if (machine_cycle == 1)
			{
				lb = read_byte_move_pc();
			}
			else if (machine_cycle == 2)
			{
				hb = read_byte_move_pc();
			}
		}

		void lxi_sp()
		{
			if (machine_cycle == 1)
			{
				byte lb = read_byte_move_pc();
				sp = (UInt16)(sp & 0xff00 | lb);
			}
			else if (machine_cycle == 2)
			{
				byte hb = read_byte_move_pc();
				sp = (UInt16)(hb << 8 | sp & 0xff);
			}
		}

		void lhld()
		{
			if (machine_cycle == 1)
			{
				Z = read_byte_move_pc();
			}
			else if (machine_cycle == 2)
			{
				W = read_byte_move_pc();
			}
			else if (machine_cycle == 3)
			{
				l = read_byte((UInt16)(W << 8 | Z));
				Z++;
				W += (byte)(Z == 0 ? 1 : 0);
			}
			else if (machine_cycle == 4)
			{
				h = read_byte((UInt16)(W << 8 | Z));
			}
		}

		void shld()
		{
			if (machine_cycle == 1)
			{
				Z = read_byte_move_pc();
			}
			else if (machine_cycle == 2)
			{
				W = read_byte_move_pc();
			}
			else if (machine_cycle == 3)
			{
				write_byte((UInt16)(W << 8 | Z), l);
				Z++;
				W += (byte)(Z == 0 ? 1 : 0);
			}
			else if (machine_cycle == 4)
			{
				write_byte((UInt16)(W << 8 | Z), h);
			}
		}

		void sphl()
		{
			if (machine_cycle == 1)
			{
				sp = i8080_get_hl();
			}
		}

		void xchg()
		{
			TMP = d;
			d = h;
			h = TMP;

			TMP = e;
			e = l;
			l = TMP;
		}

		void xthl()
		{
			if (machine_cycle == 1)
			{
				Z = read_byte(sp, Memory.AddrSpace.STACK);
			}
			else if (machine_cycle == 2)
			{
				W = read_byte(sp + 1u, Memory.AddrSpace.STACK);
			}
			else if (machine_cycle == 3)
			{
				write_byte(sp, l, Memory.AddrSpace.STACK);
			}
			else if (machine_cycle == 4)
			{
				write_byte(sp, h, Memory.AddrSpace.STACK);
			}
			else if (machine_cycle == 5)
			{
				h = W;
				l = Z;
			}
		}

		void push(byte hb, byte lb)
		{
			if (machine_cycle == 0)
			{
				sp--;
			}
			else if (machine_cycle == 1)
			{
				write_byte(sp, hb, Memory.AddrSpace.STACK);
			}
			else if (machine_cycle == 2)
			{
				sp--;
			}
			else if (machine_cycle == 3)
			{
				write_byte(sp, lb, Memory.AddrSpace.STACK);
			}
		}

		void pop(ref byte hb, ref byte lb)
		{
			if (machine_cycle == 1)
			{
				lb = read_byte(sp, Memory.AddrSpace.STACK);
				sp++;
			}
			else if (machine_cycle == 2)
			{
				hb = read_byte(sp, Memory.AddrSpace.STACK);
				sp++;
			}
		}

		// adds a value (+ an optional carry flag) to a register
		void add(byte _a, byte _b, bool _cy)
		{

			a = (byte)(_a + _b + (_cy ? 1 : 0));
			flag_c = get_carry(8, _a, _b, _cy);
			flag_ac = get_carry(4, _a, _b, _cy);
			set_z_s_p(a);
		}

		void add_m(bool _cy)
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte(i8080_get_hl());
				add(ACT, TMP, _cy);
			}
		}

		void adi(bool _cy)
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte_move_pc();
				add(ACT, TMP, _cy);
			}
		}

		// substracts a byte (+ an optional carry flag) from a register
		// see https://stackoverflow.com/a/8037485
		void sub(byte _a, byte _b, bool _cy)
		{
			add(_a, (byte)(~_b), !_cy);
			flag_c = !flag_c;
		}

		void sub_m(bool _cy)
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte(i8080_get_hl());
				sub(ACT, TMP, _cy);
			}
		}

		void sbi(bool _cy)
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte_move_pc();
				sub(ACT, TMP, _cy);
			}
		}

		void dad(UInt16 val)
		{
			if (machine_cycle == 1)
			{
				ACT = (byte)(val & 0xff);
				TMP = l;
				var res = ACT + TMP;
				flag_c = (res >> 8) == 1;
				l = (byte)(res);
			}
			else if (machine_cycle == 2)
			{
				ACT = (byte)(val >> 8);
				TMP = h;
				var result = ACT + TMP + (flag_c ? 1 : 0);
				flag_c = (result >> 8) == 1;
				h = (byte)(result);
			}
		}

		void inr(ref byte ddd)
		{
			if (machine_cycle == 0)
			{
				TMP = ddd;
				TMP++;
				flag_ac = (TMP & 0xF) == 0;
				set_z_s_p(TMP);
			}
			else if (machine_cycle == 1)
			{
				ddd = TMP;
			}
		}

		void inr_m()
		{
			if (machine_cycle == 1)
			{
				TMP = read_byte(i8080_get_hl());
				TMP++;
				flag_ac = (TMP & 0xF) == 0;
				set_z_s_p(TMP);
			}
			else if (machine_cycle == 2)
			{
				write_byte(i8080_get_hl(), TMP);
			}
		}

		void dcr(ref byte ddd)
		{
			if (machine_cycle == 0)
			{
				TMP = ddd;
				TMP--;
				flag_ac = !((TMP & 0xF) == 0xF);
				set_z_s_p(TMP);
			}
			else if (machine_cycle == 1)
			{
				ddd = TMP;
			}
		}

		void dcr_m()
		{
			if (machine_cycle == 1)
			{
				TMP = read_byte(i8080_get_hl());
				TMP--;
				flag_ac = !((TMP & 0xF) == 0xF);
				set_z_s_p(TMP);
			}
			else if (machine_cycle == 2)
			{
				write_byte(i8080_get_hl(), TMP);
			}
		}

		void inx(ref byte hb, ref byte lb)
		{
			if (machine_cycle == 0)
			{
				Z = (byte)(lb + 1);
				W = (byte)(Z == 0 ? hb + 1 : hb);
			}
			else if (machine_cycle == 1)
			{
				hb = W;
				lb = Z;
			}
		}

		void inx_sp()
		{
			if (machine_cycle == 0)
			{
				Z = (byte)(sp + 1);
				W = (byte)(Z == 0 ? sp >> 8 + 1 : sp >> 8);
			}
			else if (machine_cycle == 1)
			{
				sp = (UInt16)(W << 8 | Z);
			}
		}

		void dcx(ref byte hb, ref byte lb)
		{
			if (machine_cycle == 0)
			{
				Z = (byte)(lb - 1);
				W = (byte)(Z == 0xff ? hb - 1 : hb);
			}
			else if (machine_cycle == 1)
			{
				hb = W;
				lb = Z;
			}
		}

		void dcx_sp()
		{
			if (machine_cycle == 0)
			{
				Z = (byte)(sp - 1);
				W = (byte)(Z == 0xff ? sp >> 8 - 1 : sp >> 8);
			}
			else if (machine_cycle == 1)
			{
				sp = (UInt16)(W << 8 | Z);
			}
		}

		// Decimal Adjust Accumulator: the eight-bit number in register A is adjusted
		// to form two four-bit binary-coded-decimal digits.
		// For example, if A=$2B and DAA is executed, A becomes $31.
		void daa()
		{
			bool cy = flag_c;
			byte correction = 0;

			byte lsb = (byte)(a & 0x0F);
			byte msb = (byte)(a >> 4);

			if (flag_ac || lsb > 9)
			{
				correction += 0x06;
			}

			if (flag_c || msb > 9 || (msb >= 9 && lsb > 9))
			{
				correction += 0x60;
				cy = true;
			}

			add(a, correction, false);
			flag_c = cy;
		}

		void ana(byte sss)
		{
			ACT = a;
			TMP = sss;
			a = (byte)(ACT & TMP);
			flag_c = false;
			flag_ac = ((ACT | TMP) & 0x08) != 0;
			set_z_s_p(a);
		}

		void ana_m()
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte(i8080_get_hl());
				a = (byte)(ACT & TMP);
				flag_c = false;
				flag_ac = ((ACT | TMP) & 0x08) != 0;
				set_z_s_p(a);
			}
		}

		void ani()
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte_move_pc();
				a = (byte)(ACT & TMP);
				flag_c = false;
				flag_ac = ((ACT | TMP) & 0x08) != 0;
				set_z_s_p(a);
			}
		}

		// executes a logic "xor" between register A and a byte, then stores the
		// result in register A
		void xra(byte sss)
		{
			ACT = a;
			TMP = sss;
			a = (byte)(ACT ^ TMP);
			flag_c = false;
			flag_ac = false;
			set_z_s_p(a);
		}

		void xra_m()
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte(i8080_get_hl());
				a = (byte)(ACT ^ TMP);
				flag_c = false;
				flag_ac = false;
				set_z_s_p(a);
			}
		}

		void xri()
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte_move_pc();
				a = (byte)(ACT ^ TMP);
				flag_c = false;
				flag_ac = false;
				set_z_s_p(a);
			}
		}

		// executes a logic "or" between register A and a byte, then stores the
		// result in register A
		void ora(byte sss)
		{
			ACT = a;
			TMP = sss;
			a = (byte)(ACT | TMP);
			flag_c = false;
			flag_ac = false;
			set_z_s_p(a);
		}

		void ora_m()
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte(i8080_get_hl());
				a = (byte)(ACT | TMP);
				flag_c = false;
				flag_ac = false;
				set_z_s_p(a);
			}
		}

		void ori()
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte_move_pc();
				a = (byte)(ACT | TMP);
				flag_c = false;
				flag_ac = false;
				set_z_s_p(a);
			}
		}

		// compares the register A to another byte
		void cmp(byte sss)
		{
			ACT = a;
			TMP = sss;
			UInt16 result = (UInt16)(ACT - TMP);
			flag_c = result >> 8 == 1;
			flag_ac = (~(ACT ^ result ^ TMP) & 0x10) == 0x10;
			set_z_s_p((byte)(result & 0xFF));
		}

		void cmp_m()
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte(i8080_get_hl());
				UInt16 result = (UInt16)(ACT - TMP);
				flag_c = result >> 8 == 1;
				flag_ac = (~(ACT ^ result ^ TMP) & 0x10) == 0x10;
				set_z_s_p((byte)(result & 0xFF));
			}
		}

		void cpi()
		{
			if (machine_cycle == 0)
			{
				ACT = a;
			}
			else if (machine_cycle == 1)
			{
				TMP = read_byte_move_pc();
				UInt16 result = (UInt16)(ACT - TMP);
				flag_c = result >> 8 == 1;
				flag_ac = (~(ACT ^ result ^ TMP) & 0x10) == 0x10;
				set_z_s_p((byte)(result & 0xFF));
			}
		}

		void jmp(bool condition = true)
		{
			if (machine_cycle == 1)
			{
				Z = read_byte_move_pc();
			}
			else if (machine_cycle == 2)
			{
				W = read_byte_move_pc();
				if (condition)
				{
					pc = (UInt16)(W << 8 | Z);
				}
			}
		}

		void pchl()
		{
			if (machine_cycle == 1)
			{
				pc = i8080_get_hl();
			}
		}

		// pushes the current pc to the stack, then jumps to an address
		void call(bool condition = true)
		{
			if (machine_cycle == 0)
			{
				if (condition)
				{
					sp--;
				}
			}
			else if (machine_cycle == 1)
			{
				Z = read_byte_move_pc();
			}
			else if (machine_cycle == 2)
			{
				W = read_byte_move_pc();
			}
			else if (machine_cycle == 3)
			{
				write_byte(sp, (byte)(pc >> 8), Memory.AddrSpace.STACK);
				if (condition)
				{
					sp--;
				}
				else
				{
					machine_cycle = 5;
				}
			}
			else if (machine_cycle == 4)
			{
				write_byte(sp, (byte)(pc & 0xff), Memory.AddrSpace.STACK);
			}
			else if (machine_cycle == 5)
			{
				pc = (UInt16)(W << 8 | Z);
			}
		}

		// pushes the current pc to the stack, then jumps to an address
		void rst(byte addr)
		{
			if (machine_cycle == 0)
			{
				sp--;
			}
			else if (machine_cycle == 1)
			{
				write_byte(sp, (byte)(pc >> 8), Memory.AddrSpace.STACK);
				sp--;
			}
			else if (machine_cycle == 2)
			{
				W = 0;
				Z = addr;
				write_byte(sp, (byte)(pc & 0xff), Memory.AddrSpace.STACK);
			}
			else if (machine_cycle == 3)
			{
				pc = (UInt16)(W << 8 | Z);
			}
		}

		// returns from subroutine
		void ret()
		{
			if (machine_cycle == 1)
			{
				Z = read_byte(sp, Memory.AddrSpace.STACK);
				sp++;
			}
			else if (machine_cycle == 2)
			{
				W = read_byte(sp, Memory.AddrSpace.STACK);
				sp++;
				pc = (UInt16)(W << 8 | Z);
			}
		}

		// returns from subroutine if a condition is met
		void ret_cond(bool condition)
		{
			if (machine_cycle == 1)
			{
				if (!condition) machine_cycle = 3;
			}
			else if (machine_cycle == 2)
			{
				Z = read_byte(sp, Memory.AddrSpace.STACK);
				sp++;
			}
			else if (machine_cycle == 3)
			{
				W = read_byte(sp, Memory.AddrSpace.STACK);
				sp++;
				pc = (UInt16)(W << 8 | Z);
			}
		}

		void in_d()
		{
			if (machine_cycle == 1)
			{
				W = 0;
				Z = read_byte_move_pc();
				a = input(Z);
			}
		}

		void out_d()
		{
			if (machine_cycle == 1)
			{
				W = 0;
				Z = read_byte_move_pc();
				output(Z, a);
			}
		}
		#endregion

		private void incode_actions()
		{
			actions[0x7F] = () => { mov_r_r(ref a, a); }; // MOV A,A
			actions[0x78] = () => { mov_r_r(ref a, b); }; // MOV A,B
			actions[0x79] = () => { mov_r_r(ref a, c); }; // MOV A,C
			actions[0x7A] = () => { mov_r_r(ref a, d); }; // MOV A,D
			actions[0x7B] = () => { mov_r_r(ref a, e); }; // MOV A,E
			actions[0x7C] = () => { mov_r_r(ref a, h); }; // MOV A,H
			actions[0x7D] = () => { mov_r_r(ref a, l); }; // MOV A,L

			actions[0x47] = () => { mov_r_r(ref b, a); }; // MOV B,A
			actions[0x40] = () => { mov_r_r(ref b, b); }; // MOV B,B
			actions[0x41] = () => { mov_r_r(ref b, c); }; // MOV B,C
			actions[0x42] = () => { mov_r_r(ref b, d); }; // MOV B,D
			actions[0x43] = () => { mov_r_r(ref b, e); }; // MOV B,E
			actions[0x44] = () => { mov_r_r(ref b, h); }; // MOV B,H
			actions[0x45] = () => { mov_r_r(ref b, l); }; // MOV B,L

			actions[0x4F] = () => { mov_r_r(ref c, a); }; // MOV C,A
			actions[0x48] = () => { mov_r_r(ref c, b); }; // MOV C,B
			actions[0x49] = () => { mov_r_r(ref c, c); }; // MOV C,C
			actions[0x4A] = () => { mov_r_r(ref c, d); }; // MOV C,D
			actions[0x4B] = () => { mov_r_r(ref c, e); }; // MOV C,E
			actions[0x4C] = () => { mov_r_r(ref c, h); }; // MOV C,H
			actions[0x4D] = () => { mov_r_r(ref c, l); }; // MOV C,L

			actions[0x57] = () => { mov_r_r(ref d, a); }; // MOV D,A
			actions[0x50] = () => { mov_r_r(ref d, b); }; // MOV D,B
			actions[0x51] = () => { mov_r_r(ref d, c); }; // MOV D,C
			actions[0x52] = () => { mov_r_r(ref d, d); }; // MOV D,D
			actions[0x53] = () => { mov_r_r(ref d, e); }; // MOV D,E
			actions[0x54] = () => { mov_r_r(ref d, h); }; // MOV D,H
			actions[0x55] = () => { mov_r_r(ref d, l); }; // MOV D,L

			actions[0x5F] = () => { mov_r_r(ref e, a); }; // MOV E,A
			actions[0x58] = () => { mov_r_r(ref e, b); }; // MOV E,B
			actions[0x59] = () => { mov_r_r(ref e, c); }; // MOV E,C
			actions[0x5A] = () => { mov_r_r(ref e, d); }; // MOV E,D
			actions[0x5B] = () => { mov_r_r(ref e, e); }; // MOV E,E
			actions[0x5C] = () => { mov_r_r(ref e, h); }; // MOV E,H
			actions[0x5D] = () => { mov_r_r(ref e, l); }; // MOV E,L

			actions[0x67] = () => { mov_r_r(ref h, a); }; // MOV H,A
			actions[0x60] = () => { mov_r_r(ref h, b); }; // MOV H,B
			actions[0x61] = () => { mov_r_r(ref h, c); }; // MOV H,C
			actions[0x62] = () => { mov_r_r(ref h, d); }; // MOV H,D
			actions[0x63] = () => { mov_r_r(ref h, e); }; // MOV H,E
			actions[0x64] = () => { mov_r_r(ref h, h); }; // MOV H,H
			actions[0x65] = () => { mov_r_r(ref h, l); }; // MOV H,L

			actions[0x6F] = () => { mov_r_r(ref l, a); }; // MOV L,A
			actions[0x68] = () => { mov_r_r(ref l, b); }; // MOV L,B
			actions[0x69] = () => { mov_r_r(ref l, c); }; // MOV L,C
			actions[0x6A] = () => { mov_r_r(ref l, d); }; // MOV L,D
			actions[0x6B] = () => { mov_r_r(ref l, e); }; // MOV L,E
			actions[0x6C] = () => { mov_r_r(ref l, h); }; // MOV L,H
			actions[0x6D] = () => { mov_r_r(ref l, l); }; // MOV L,L

			actions[0x7E] = () => { load_r_p(ref a, i8080_get_hl()); }; // MOV A,M
			actions[0x46] = () => { load_r_p(ref b, i8080_get_hl()); }; // MOV B,M
			actions[0x4E] = () => { load_r_p(ref c, i8080_get_hl()); }; // MOV C,M
			actions[0x56] = () => { load_r_p(ref d, i8080_get_hl()); }; // MOV D,M
			actions[0x5E] = () => { load_r_p(ref e, i8080_get_hl()); }; // MOV E,M
			actions[0x66] = () => { load_r_p(ref h, i8080_get_hl()); }; // MOV H,M
			actions[0x6E] = () => { load_r_p(ref l, i8080_get_hl()); }; // MOV L,M

			actions[0x77] = () => { mov_m_r(a); }; // MOV M,A
			actions[0x70] = () => { mov_m_r(b); }; // MOV M,B
			actions[0x71] = () => { mov_m_r(c); }; // MOV M,C
			actions[0x72] = () => { mov_m_r(d); }; // MOV M,D
			actions[0x73] = () => { mov_m_r(e); }; // MOV M,E
			actions[0x74] = () => { mov_m_r(h); }; // MOV M,H
			actions[0x75] = () => { mov_m_r(l); }; // MOV M,L

			actions[0x3E] = () => { mvi_r_d(ref a); }; // MVI A,byte
			actions[0x06] = () => { mvi_r_d(ref b); }; // MVI B,byte
			actions[0x0E] = () => { mvi_r_d(ref c); }; // MVI C,byte
			actions[0x16] = () => { mvi_r_d(ref d); }; // MVI D,byte
			actions[0x1E] = () => { mvi_r_d(ref e); }; // MVI E,byte
			actions[0x26] = () => { mvi_r_d(ref h); }; // MVI H,byte
			actions[0x2E] = () => { mvi_r_d(ref l); }; // MVI L,byte
			actions[0x36] = () => { mvi_m_d(); }; // MVI M,byte

			actions[0x0A] = () => { load_r_p(ref a, i8080_get_bc()); }; // LDAX B
			actions[0x1A] = () => { load_r_p(ref a, i8080_get_de()); }; // LDAX D
			actions[0x3A] = () => { lda(); }; // LDA word

			actions[0x02] = () => { stax(i8080_get_bc()); }; // STAX B
			actions[0x12] = () => { stax(i8080_get_de()); }; // STAX D
			actions[0x32] = () => { sta(); }; // STA word

			actions[0x01] = () => { lxi(ref b, ref c); }; // LXI B,word
			actions[0x11] = () => { lxi(ref d, ref e); }; // LXI D,word
			actions[0x21] = () => { lxi(ref h, ref l); }; // LXI H,word
			actions[0x31] = () => { lxi_sp(); }; // LXI SP,word
			actions[0x2A] = () => { lhld(); }; // LHLD
			actions[0x22] = () => { shld(); }; // SHLD
			actions[0xF9] = () => { sphl(); }; // SPHL

			actions[0xEB] = () => { xchg(); }; // XCHG
			actions[0xE3] = () => { xthl(); }; // XTHL

			actions[0xC5] = () => { push(b, c); }; // PUSH B
			actions[0xD5] = () => { push(d, e); }; // PUSH D
			actions[0xE5] = () => { push(h, l); }; // PUSH H
			actions[0xF5] = () => { push(a, i8080_get_flags()); }; // PUSH PSW
			actions[0xC1] = () => { pop(ref b, ref c); }; // POP B
			actions[0xD1] = () => { pop(ref d, ref e); }; // POP D
			actions[0xE1] = () => { pop(ref h, ref l); }; // POP H
			actions[0xF1] = () => { pop(ref a, ref TMP); i8080_set_flags(TMP); }; // POP PSW

			actions[0x87] = () => { add(a, a, false); }; // ADD A
			actions[0x80] = () => { add(a, b, false); }; // ADD B
			actions[0x81] = () => { add(a, c, false); }; // ADD C
			actions[0x82] = () => { add(a, d, false); }; // ADD D
			actions[0x83] = () => { add(a, e, false); }; // ADD E
			actions[0x84] = () => { add(a, h, false); }; // ADD H
			actions[0x85] = () => { add(a, l, false); }; // ADD L
			actions[0x86] = () => { add_m(false); }; // ADD M
			actions[0xC6] = () => { adi(false); }; // ADI byte

			actions[0x8F] = () => { add(a, a, flag_c); }; // ADC A
			actions[0x88] = () => { add(a, b, flag_c); }; // ADC B
			actions[0x89] = () => { add(a, c, flag_c); }; // ADC C
			actions[0x8A] = () => { add(a, d, flag_c); }; // ADC D
			actions[0x8B] = () => { add(a, e, flag_c); }; // ADC E
			actions[0x8C] = () => { add(a, h, flag_c); }; // ADC H
			actions[0x8D] = () => { add(a, l, flag_c); }; // ADC L
			actions[0x8E] = () => { add_m(flag_c); }; // ADC M
			actions[0xCE] = () => { adi(flag_c); }; // ACI byte

			actions[0x97] = () => { sub(a, a, false); }; // SUB A
			actions[0x90] = () => { sub(a, b, false); }; // SUB B
			actions[0x91] = () => { sub(a, c, false); }; // SUB C
			actions[0x92] = () => { sub(a, d, false); }; // SUB D
			actions[0x93] = () => { sub(a, e, false); }; // SUB E
			actions[0x94] = () => { sub(a, h, false); }; // SUB H
			actions[0x95] = () => { sub(a, l, false); }; // SUB L
			actions[0x96] = () => { sub_m(false); }; // SUB M
			actions[0xD6] = () => { sbi(false); }; // SUI byte

			actions[0x9F] = () => { sub(a, a, flag_c); }; // SBB A
			actions[0x98] = () => { sub(a, b, flag_c); }; // SBB B
			actions[0x99] = () => { sub(a, c, flag_c); }; // SBB C
			actions[0x9A] = () => { sub(a, d, flag_c); }; // SBB D
			actions[0x9B] = () => { sub(a, e, flag_c); }; // SBB E
			actions[0x9C] = () => { sub(a, h, flag_c); }; // SBB H
			actions[0x9D] = () => { sub(a, l, flag_c); }; // SBB L
			actions[0x9E] = () => { sub_m(flag_c); }; // SBB M
			actions[0xDE] = () => { sbi(flag_c); }; // SBI byte

			actions[0x09] = () => { dad(i8080_get_bc()); }; // DAD B
			actions[0x19] = () => { dad(i8080_get_de()); }; // DAD D
			actions[0x29] = () => { dad(i8080_get_hl()); }; // DAD H
			actions[0x39] = () => { dad(sp); }; // DAD SP

			actions[0x3C] = () => { inr(ref a); }; // INR A
			actions[0x04] = () => { inr(ref b); }; // INR B
			actions[0x0C] = () => { inr(ref c); }; // INR C
			actions[0x14] = () => { inr(ref d); }; // INR D
			actions[0x1C] = () => { inr(ref e); }; // INR E
			actions[0x24] = () => { inr(ref h); }; // INR H
			actions[0x2C] = () => { inr(ref l); }; // INR L
			actions[0x34] = () => { inr_m(); }; // INR M

			actions[0x3D] = () => { dcr(ref a); }; // DCR A
			actions[0x05] = () => { dcr(ref b); }; // DCR B
			actions[0x0D] = () => { dcr(ref c); }; // DCR C
			actions[0x15] = () => { dcr(ref d); }; // DCR D
			actions[0x1D] = () => { dcr(ref e); }; // DCR E
			actions[0x25] = () => { dcr(ref h); }; // DCR H
			actions[0x2D] = () => { dcr(ref l); }; // DCR L
			actions[0x35] = () => { dcr_m(); }; // DCR M

			actions[0x03] = () => { inx(ref b, ref c); }; // INX B
			actions[0x13] = () => { inx(ref d, ref e); }; // INX D
			actions[0x23] = () => { inx(ref h, ref l); }; // INX H
			actions[0x33] = () => { inx_sp(); }; // INX SP

			actions[0x0B] = () => { dcx(ref b, ref c); }; // DCX B
			actions[0x1B] = () => { dcx(ref d, ref e); }; // DCX D
			actions[0x2B] = () => { dcx(ref h, ref l); }; // DCX H
			actions[0x3B] = () => { dcx_sp(); }; // DCX SP

			actions[0x27] = () => { daa(); }; // DAA
			actions[0x2F] = () => { a = (byte)(~a); }; // CMA
			actions[0x37] = () => { flag_c = true; }; // STC
			actions[0x3F] = () => { flag_c = !flag_c; }; // CMC

			actions[0x07] = () => { rlc(); }; // RLC (rotate left)
			actions[0x0F] = () => { rrc(); }; // RRC (rotate right)
			actions[0x17] = () => { ral(); }; // RAL
			actions[0x1F] = () => { rar(); }; // RAR

			actions[0xA7] = () => { ana(a); }; // ANA A
			actions[0xA0] = () => { ana(b); }; // ANA B
			actions[0xA1] = () => { ana(c); }; // ANA C
			actions[0xA2] = () => { ana(d); }; // ANA D
			actions[0xA3] = () => { ana(e); }; // ANA E
			actions[0xA4] = () => { ana(h); }; // ANA H
			actions[0xA5] = () => { ana(l); }; // ANA L
			actions[0xA6] = () => { ana_m(); }; // ANA M
			actions[0xE6] = () => { ani(); }; // ANI byte

			actions[0xAF] = () => { xra(a); }; // XRA A
			actions[0xA8] = () => { xra(b); }; // XRA B
			actions[0xA9] = () => { xra(c); }; // XRA C
			actions[0xAA] = () => { xra(d); }; // XRA D
			actions[0xAB] = () => { xra(e); }; // XRA E
			actions[0xAC] = () => { xra(h); }; // XRA H
			actions[0xAD] = () => { xra(l); }; // XRA L
			actions[0xAE] = () => { xra_m(); }; // XRA M
			actions[0xEE] = () => { xri(); }; // XRI byte

			actions[0xB7] = () => { ora(a); }; // ORA A
			actions[0xB0] = () => { ora(b); }; // ORA B
			actions[0xB1] = () => { ora(c); }; // ORA C
			actions[0xB2] = () => { ora(d); }; // ORA D
			actions[0xB3] = () => { ora(e); }; // ORA E
			actions[0xB4] = () => { ora(h); }; // ORA H
			actions[0xB5] = () => { ora(l); }; // ORA L
			actions[0xB6] = () => { ora_m(); }; // ORA M
			actions[0xF6] = () => { ori(); }; // ORI byte

			actions[0xBF] = () => { cmp(a); }; // CMP A
			actions[0xB8] = () => { cmp(b); }; // CMP B
			actions[0xB9] = () => { cmp(c); }; // CMP C
			actions[0xBA] = () => { cmp(d); }; // CMP D
			actions[0xBB] = () => { cmp(e); }; // CMP E
			actions[0xBC] = () => { cmp(h); }; // CMP H
			actions[0xBD] = () => { cmp(l); }; // CMP L
			actions[0xBE] = () => { cmp_m(); }; // CMP M
			actions[0xFE] = () => { cpi(); }; // CPI byte

			actions[0xC3] = () => { jmp(); }; // JMP
			actions[0xCB] = () => { jmp(); }; // undocumented JMP
			actions[0xC2] = () => { jmp(flag_z == false); }; // JNZ
			actions[0xCA] = () => { jmp(flag_z == true); }; // JZ
			actions[0xD2] = () => { jmp(flag_c == false); }; // JNC
			actions[0xDA] = () => { jmp(flag_c == true); }; // JC
			actions[0xE2] = () => { jmp(flag_p == false); }; // JPO
			actions[0xEA] = () => { jmp(flag_p == true); }; // JPE
			actions[0xF2] = () => { jmp(flag_s == false); }; // JP
			actions[0xFA] = () => { jmp(flag_s == true); }; // JM

			actions[0xE9] = () => { pchl(); }; // PCHL
			actions[0xCD] = () => { call(); }; // CALL
			actions[0xDD] = () => { call(); }; // undocumented CALL
			actions[0xED] = () => { call(); }; // undocumented CALL
			actions[0xFD] = () => { call(); }; // undocumented CALL

			actions[0xC4] = () => { call(flag_z == false); }; // CNZ
			actions[0xCC] = () => { call(flag_z == true); }; // CZ
			actions[0xD4] = () => { call(flag_c == false); }; // CNC
			actions[0xDC] = () => { call(flag_c == true); }; // CC
			actions[0xE4] = () => { call(flag_p == false); }; // CPO
			actions[0xEC] = () => { call(flag_p == true); }; // CPE
			actions[0xF4] = () => { call(flag_s == false); }; // CP
			actions[0xFC] = () => { call(flag_s == true); }; // CM

			actions[0xC9] = () => { ret(); }; // RET
			actions[0xD9] = () => { ret(); }; // undocumented RET
			actions[0xC0] = () => { ret_cond(flag_z == false); }; // RNZ
			actions[0xC8] = () => { ret_cond(flag_z == true); }; // RZ
			actions[0xD0] = () => { ret_cond(flag_c == false); }; // RNC
			actions[0xD8] = () => { ret_cond(flag_c == true); }; // RC
			actions[0xE0] = () => { ret_cond(flag_p == false); }; // RPO
			actions[0xE8] = () => { ret_cond(flag_p == true); }; // RPE
			actions[0xF0] = () => { ret_cond(flag_s == false); }; // RP
			actions[0xF8] = () => { ret_cond(flag_s == true); }; // RM

			actions[0xC7] = () => { rst(0x00); }; // RST 0
			actions[0xCF] = () => { rst(0x08); }; // RST 1
			actions[0xD7] = () => { rst(0x10); }; // RST 2
			actions[0xDF] = () => { rst(0x18); }; // RST 3
			actions[0xE7] = () => { rst(0x20); }; // RST 4
			actions[0xEF] = () => { rst(0x28); }; // RST 5
			actions[0xF7] = () => { rst(0x30); }; // RST 6
			actions[0xFF] = () => { rst(0x38); }; // RST 7

			actions[0xDB] = () => { in_d(); }; // IN
			actions[0xD3] = () => { out_d(); }; // OUT

			actions[0xF3] = () => { INTE = false; }; // DI
			actions[0xFB] = () => { INTE = true; ei_pending = true; }; // EI
			actions[0x76] = () => { HLTA = true; }; // HLT

			actions[0x00] = () => { }; // NOP
			actions[0x08] = () => { }; // undocumented NOP
			actions[0x10] = () => { }; // undocumented NOP
			actions[0x18] = () => { }; // undocumented NOP
			actions[0x20] = () => { }; // undocumented NOP
			actions[0x28] = () => { }; // undocumented NOP
			actions[0x30] = () => { }; // undocumented NOP
			actions[0x38] = () => { }; // undocumented NOP
		}
	}
}
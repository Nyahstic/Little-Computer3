using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LC3.Emulation.Core
{
    public class Assembler
    {
        private ushort[] program = new ushort[1];
        private int programAssemblyHead = 0;
        public enum Register
        {
            R_R0 = 0,
            R_R1,
            R_R2,
            R_R3,
            R_R4,
            R_R5,
            R_R6,
            R_R7,
            R_PC,
            R_COND,
            R_COUNT
        };

        public enum TrapCode
        {
            TRAP_GETC = 0x20,  /* get character from keyboard, not echoed onto the terminal */
            TRAP_OUT = 0x21,   /* output a character */
            TRAP_PUTS = 0x22,  /* output a word string */
            TRAP_IN = 0x23,    /* get character from keyboard, echoed onto the terminal */
            TRAP_PUTSP = 0x24, /* output a byte string */
            TRAP_HALT = 0x25   /* halt the program */
        };

        public enum OpCode
        {
            OP_BR = 0, /* branch */
            OP_ADD,    /* add  */
            OP_LD,     /* load */
            OP_ST,     /* store */
            OP_JSR,    /* jump register */
            OP_AND,    /* bitwise and */
            OP_LDR,    /* load register */
            OP_STR,    /* store register */
            OP_RTI,    /* unused */
            OP_NOT,    /* bitwise not */
            OP_LDI,    /* load indirect */
            OP_STI,    /* store indirect */
            OP_JMP,    /* jump */
            OP_RES,    /* reserved (unused) */
            OP_LEA,    /* load effective address */
            OP_TRAP,    /* execute trap */
            OP_RET      /* Pseudo opcode, in reality its JMP R7 */
        };
        public enum Flags
        {
            FL_POS = 1 << 0, /* P */
            FL_ZRO = 1 << 1, /* Z */
            FL_NEG = 1 << 2, /* N */
        };

        ushort _tempOP = 0;
        public Assembler Assemble(OpCode opCode, Flags flags, ushort pcoffset9)
        {
            _expandProgram();

            _tempOP = (ushort)((int)opCode << 12);
            _tempOP = (ushort)(_tempOP | _signExtend((ushort)((int)flags & 0x1FF), 9));

            return this;
        }

        private void _expandProgram()
        {
            if (programAssemblyHead >= program.Length)
            {
                Array.Resize(ref program, program.Length + 1);
            }
        }

        private ushort _signExtend(ushort value, int bitCount)
        {
            if (((value >> (bitCount - 1)) & 1) == 1)
            {
                value |= (ushort)(0xFFFF << bitCount);
            }
            return value;
        }
    }
}

using System.Reflection.Emit;

namespace LC3.Emulation.Core
{
    public class Processor
    {
        public bool Running = false;

        public enum Register{ 
            R_R0 = 0,
            R_R1,
            R_R2,
            R_R3,
            R_R4,
            R_R6,
            R_R7,
            R_PC,
            R_COND, // R_COND = R_CONDITION
            R_COUNT
        };
        public ushort[] Memory = new ushort[ushort.MaxValue + 1];
        public ushort[] RegisterFile = new ushort[(int)Register.R_COUNT];
        
        public enum Opcodeenum
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
            OP_TRAP    /* execute trap */
        };

        [Flags]
        public enum Flags
        {
            FL_POS = 1 << 0, /* P */
            FL_ZRO = 1 << 1, /* Z */
            FL_NEG = 1 << 2, /* N */
        }; 


        public void Step()
        {
            RegisterFile[(int)Register.R_COND] = (ushort)Flags.FL_ZRO;

            ushort instruction = Memory[RegisterFile[(int)Register.R_PC]++];

            ushort opcode = (ushort)(instruction >> 12);


        }
    }
}   

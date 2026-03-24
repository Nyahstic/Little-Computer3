using System.Reflection.Emit;

namespace LC3.Emulation.Core
{
    public class Processor
    {
        public Processor(ushort ProgramCouterStart = 0x3000)
        {
            RegisterFile[(int)Register.R_PC] = ProgramCouterStart; // Default starting address for the program counter
            // for funsies
            for (int i = 0; i < memory.Length; i++)
            {
                memory[i] = (ushort)Random.Shared.Next(0, ushort.MaxValue); // Fill memory with its own address for testing
            }
        }


        // Event to signal debug information
        public event Action<string>? OnDebugInfo;

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
        private ushort[] memory = new ushort[ushort.MaxValue + 1];
        public ushort[] RegisterFile { get; private set; } = new ushort[(int)Register.R_COUNT];
        
        public enum OpCode : ushort
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


            ushort instruction = MemoryRead(RegisterFile[(int)Register.R_PC]++);

            ushort opcode = (ushort)(instruction >> 12);
            ushort r0, r1, r2, imm5, pcoffset9;
            bool immFlag;

            r0 = (ushort)((instruction >> 9) & 0x7);
            r1 = (ushort)((instruction >> 6) & 0x7);
            r2 = (ushort)(instruction & 0x7);
            immFlag = ((instruction >> 5) & 0x1) == 1;
            imm5 = _signExtend((ushort)(instruction & 0x1F), 5);
            pcoffset9 = _signExtend((ushort)(instruction & 0x1FF), 9);

            //OnDebugInfo?.Invoke($"[DEBUG] Executing instruction at address: 0x{RegisterFile[(int)Register.R_PC]:X4}, Instruction: 0x{memory[RegisterFile[(int)Register.R_PC]]:X4}");
            //OnDebugInfo?.Invoke($"[DEBUG] Decoded instruction - Opcode: {(OpCode)opcode}");

            switch (opcode)
            {
                case (ushort)OpCode.OP_BR:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_ADD:
                    if (immFlag)
                    {
                        RegisterFile[r0] = (ushort)(RegisterFile[r1] + imm5);
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_ADD with immediate value: R{r0} = R{r1} + {imm5} => 0x{RegisterFile[r0]:X4}");
                    }
                    else
                    {
                        RegisterFile[r0] = (ushort)(RegisterFile[r1] + RegisterFile[r2]);
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_ADD with register value: R{r0} = R{r1} + R{r2} => 0x{RegisterFile[r0]:X4}");
                    }

                    _updateFlags(r0);
                    break;

                case (ushort)OpCode.OP_LD:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_ST:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_JSR:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_AND:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_LDR:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_STR:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_RTI:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_NOT:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_LDI:
                    OnDebugInfo?.Invoke($"[DEBUG] Executing OP_LDI, on Register {(Register)r0}, with offset of {pcoffset9}");
                    RegisterFile[r0] = MemoryRead((ushort)(RegisterFile[(int)Register.R_PC] + pcoffset9));
                    _updateFlags(r0);
                    break;

                case (ushort)OpCode.OP_STI:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_JMP:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_RES:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_LEA:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_TRAP:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                 default:
                    throw new InvalidOperationException($"Unknown opcode: {opcode}");
            }
        }

        private ushort MemoryRead(ushort Address)
        {
            return memory[Address % ushort.MaxValue];
        }

        public bool LoadImage(string path) {
            if (!File.Exists(path))
            {
                OnDebugInfo?.Invoke($"[FILE] Image path does not exist: {path}");
                return false;
            }

            return false;
        }

        private ushort _signExtend(ushort value, int bitCount)
        {
            if (((value >> (bitCount - 1)) & 1) == 1)
            {
                value |= (ushort)(0xFFFF << bitCount);
            }
            return value;
        }

        private void _updateFlags(ushort RegisterToCheck)
        {
            if (RegisterFile[RegisterToCheck] == 0) {
                RegisterFile[(int)Register.R_COND] = (ushort)Flags.FL_ZRO;
            } 
            else if ((RegisterFile[RegisterToCheck] >> 15) != 0)
            {
                RegisterFile[(int)Register.R_COND] = (ushort)Flags.FL_NEG;
            }
            else
            {
                RegisterFile[(int)Register.R_COND] = (ushort)Flags.FL_POS;
            }
        }
    }
}   
